using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZenGTPX.Config;

public sealed class ZenConfigurationService
{
    public const string ProtocolName = "zengtp-config";
    public const int ProtocolVersion = 1;

    public static readonly string[] Commands =
    [
        "zengtp_config_version",
        "zengtp_config_schema",
        "zengtp_config_get",
        "zengtp_config_set",
        "zengtp_config_save",
        "zengtp_config_reset",
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ZenGtpOptions _template;
    private readonly Action<ZenGtpOptions> _apply;
    private readonly Func<ZenGtpOptions> _getEffective;
    private readonly ZenConfigurationProfile _defaults;
    private readonly ZenConfigurationProfile _startup;
    private ZenConfigurationProfile _selected;
    private ZenConfigurationProfile _saved;
    private ZenGtpOptions _lastApplied;

    public ZenConfigurationService(
        ZenGtpOptions startupOptions,
        Action<ZenGtpOptions> apply,
        Func<ZenGtpOptions>? getEffective = null)
    {
        _template = startupOptions;
        _apply = apply;
        _lastApplied = startupOptions;
        _getEffective = getEffective ?? (() => _lastApplied);
        _defaults = ZenConfigurationProfile.FromOptions(
            ZenGtpOptionsResolver.ApplyModeDefaults(new ZenGtpOptions()));
        _startup = ZenConfigurationProfile.FromOptions(startupOptions);
        _selected = _startup;
        _saved = _startup;
    }

    public string GetVersionJson()
    {
        return Serialize(new
        {
            protocol = ProtocolName,
            version = ProtocolVersion,
            persistenceOwner = "client",
            commands = Commands,
        });
    }

    public string GetSchemaJson()
    {
        var defaultValues = _defaults;
        return Serialize(new
        {
            protocol = ProtocolName,
            version = ProtocolVersion,
            persistenceOwner = "client",
            batchSemantics = "atomic",
            fields = new object[]
            {
                Field("mode", "string", "basic", defaultValues.Mode, enumValues: ["rank", "fixed-time", "advanced"]),
                Field("rankPreset", "string", "basic", defaultValues.RankPreset, enumValues: RankPresetTable.Names, activeWhen: "mode=rank"),
                Field("maxTimeSeconds", "number", "basic", defaultValues.MaxTimeSeconds, minimum: 0, activeWhen: "mode=fixed-time|advanced"),
                Field("maxSimulations", "integer", "advanced", defaultValues.MaxSimulations, minimum: 1, maximum: int.MaxValue, activeWhen: "mode=advanced"),
                Field("pnLevel", "integer", "advanced", defaultValues.PnLevel, minimum: 0, maximum: 3, activeWhen: "mode=advanced"),
                Field("pnWeight", "number", "advanced", defaultValues.PnWeight, minimum: 0, activeWhen: "mode=advanced"),
                Field("vnMixRate", "number", "advanced", defaultValues.VnMixRate, minimum: 0, maximum: 1, activeWhen: "mode=advanced"),
                Field("threads", "integer", "advanced", defaultValues.Threads, minimum: 1),
                Field("finalScoreRule", "string", "advanced", defaultValues.FinalScoreRule, enumValues: ["japanese", "territory", "chinese", "area"]),
                Field("resignThreshold", "number", "advanced", defaultValues.ResignThreshold, minimum: 0, maximum: 1),
            },
            state = CreateState(),
        });
    }

    public string GetStateJson()
    {
        return Serialize(new
        {
            protocol = ProtocolName,
            version = ProtocolVersion,
            operation = "get",
            state = CreateState(),
        });
    }

    public string SetJson(string json)
    {
        ZenConfigurationProfile candidate;
        try
        {
            using var document = JsonDocument.Parse(json);
            candidate = ReadProfileUpdate(document.RootElement, _selected);
        }
        catch (JsonException ex)
        {
            throw new ZenConfigurationException("invalid_json", "configuration payload must be valid JSON", detail: ex.Message);
        }

        ValidateProfile(candidate);
        Apply(candidate);
        return Serialize(new
        {
            protocol = ProtocolName,
            version = ProtocolVersion,
            operation = "set",
            state = CreateState(),
        });
    }

    public string SaveJson()
    {
        _saved = _selected;
        return Serialize(new
        {
            protocol = ProtocolName,
            version = ProtocolVersion,
            operation = "save",
            persistenceOwner = "client",
            profile = _saved,
            state = CreateState(),
        });
    }

    public string ResetJson(string target)
    {
        var normalized = target.ToLowerInvariant();
        var profile = normalized switch
        {
            "saved" => _saved,
            "startup" => _startup,
            "defaults" => _defaults,
            _ => throw new ZenConfigurationException(
                "invalid_value",
                "reset target must be one of: saved, startup, defaults",
                parameter: "target"),
        };

        Apply(profile);
        return Serialize(new
        {
            protocol = ProtocolName,
            version = ProtocolVersion,
            operation = "reset",
            target = normalized,
            state = CreateState(),
        });
    }

    public static string FormatError(ZenConfigurationException exception)
    {
        return Serialize(new
        {
            protocol = ProtocolName,
            version = ProtocolVersion,
            error = new
            {
                code = exception.Code,
                parameter = exception.Parameter,
                message = exception.Message,
                detail = exception.Detail,
            },
        });
    }

    public static ZenConfigurationException InvalidArguments(string message)
    {
        return new ZenConfigurationException("invalid_arguments", message);
    }

    private void Apply(ZenConfigurationProfile candidate)
    {
        var options = Resolve(candidate);
        var previousSelected = _selected;
        var previousOptions = _getEffective();

        try
        {
            _apply(options);
            _lastApplied = options;
            _selected = candidate;
        }
        catch (Exception ex)
        {
            try
            {
                _apply(previousOptions);
                _lastApplied = previousOptions;
            }
            catch
            {
                // Preserve the original apply error. The caller will restart the process if native rollback failed.
            }

            _selected = previousSelected;
            throw new ZenConfigurationException("apply_failed", "configuration could not be applied", detail: ex.Message);
        }
    }

    private ConfigurationState CreateState()
    {
        return new ConfigurationState(
            Selected: _selected,
            Effective: ZenConfigurationProfile.FromOptions(_getEffective()),
            Dirty: _selected != _saved,
            RestartRequired: false,
            PersistenceOwner: "client");
    }

    private ZenGtpOptions Resolve(ZenConfigurationProfile profile)
    {
        var options = profile.ApplyTo(_template);
        options = ZenGtpOptionsResolver.ApplyModeDefaults(options);
        options.Validate();
        return options;
    }

    private static ZenConfigurationProfile ReadProfileUpdate(JsonElement root, ZenConfigurationProfile current)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new ZenConfigurationException("invalid_type", "configuration payload must be a JSON object");
        }

        var candidate = current;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in root.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                throw new ZenConfigurationException("duplicate_parameter", "parameter appears more than once", property.Name);
            }

            candidate = property.Name.ToLowerInvariant() switch
            {
                "mode" => candidate with { Mode = ReadString(property) },
                "rankpreset" => candidate with { RankPreset = ReadString(property) },
                "maxtimeseconds" => candidate with { MaxTimeSeconds = ReadDouble(property) },
                "maxsimulations" => candidate with { MaxSimulations = ReadInteger(property) },
                "pnlevel" => candidate with { PnLevel = ReadInteger(property) },
                "pnweight" => candidate with { PnWeight = ReadDouble(property) },
                "vnmixrate" => candidate with { VnMixRate = ReadDouble(property) },
                "threads" => candidate with { Threads = ReadInteger(property) },
                "finalscorerule" => candidate with { FinalScoreRule = ReadString(property) },
                "resignthreshold" => candidate with { ResignThreshold = ReadDouble(property) },
                _ => throw new ZenConfigurationException("unknown_parameter", "unknown configuration parameter", property.Name),
            };
        }

        return candidate with
        {
            Mode = candidate.Mode.ToLowerInvariant() switch
            {
                "fixedtime" => "fixed-time",
                var value => value,
            },
            RankPreset = candidate.RankPreset.ToLowerInvariant(),
            FinalScoreRule = candidate.FinalScoreRule.ToLowerInvariant(),
        };
    }

    private static string ReadString(JsonProperty property)
    {
        if (property.Value.ValueKind != JsonValueKind.String)
        {
            throw new ZenConfigurationException("invalid_type", "parameter must be a string", property.Name);
        }

        return property.Value.GetString() ?? "";
    }

    private static int ReadInteger(JsonProperty property)
    {
        if (property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetInt32(out var value))
        {
            throw new ZenConfigurationException("invalid_type", "parameter must be a 32-bit integer", property.Name);
        }

        return value;
    }

    private static double ReadDouble(JsonProperty property)
    {
        if (property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetDouble(out var value) || !double.IsFinite(value))
        {
            throw new ZenConfigurationException("invalid_type", "parameter must be a finite number", property.Name);
        }

        return value;
    }

    private static void ValidateProfile(ZenConfigurationProfile profile)
    {
        if (profile.Mode is not ("rank" or "fixed-time" or "advanced"))
        {
            throw InvalidValue("mode", "mode must be one of: rank, fixed-time, advanced");
        }

        if (!RankPresetTable.Contains(profile.RankPreset))
        {
            throw InvalidValue("rankPreset", "rankPreset must be between 6k and 9d");
        }

        if (profile.MaxTimeSeconds < 0)
        {
            throw InvalidValue("maxTimeSeconds", "maxTimeSeconds must not be negative");
        }

        if (profile.MaxSimulations <= 0)
        {
            throw InvalidValue("maxSimulations", "maxSimulations must be positive");
        }

        if (profile.PnLevel < 0 || profile.PnLevel > 3)
        {
            throw InvalidValue("pnLevel", "pnLevel must be between 0 and 3");
        }

        if (profile.PnWeight < 0)
        {
            throw InvalidValue("pnWeight", "pnWeight must not be negative");
        }

        if (profile.VnMixRate < 0 || profile.VnMixRate > 1)
        {
            throw InvalidValue("vnMixRate", "vnMixRate must be between 0 and 1");
        }

        if (profile.Threads <= 0)
        {
            throw InvalidValue("threads", "threads must be positive");
        }

        if (!ZenGtpOptions.IsSupportedFinalScoreRule(profile.FinalScoreRule))
        {
            throw InvalidValue("finalScoreRule", "finalScoreRule must be one of: japanese, territory, chinese, area");
        }

        if (profile.ResignThreshold < 0 || profile.ResignThreshold > 1)
        {
            throw InvalidValue("resignThreshold", "resignThreshold must be between 0 and 1");
        }
    }

    private static ZenConfigurationException InvalidValue(string parameter, string message)
    {
        return new ZenConfigurationException("invalid_value", message, parameter);
    }

    private static object Field(
        string name,
        string type,
        string group,
        object defaultValue,
        object? minimum = null,
        object? maximum = null,
        IReadOnlyList<string>? enumValues = null,
        string? activeWhen = null)
    {
        return new
        {
            name,
            type,
            group,
            defaultValue,
            minimum,
            maximum,
            enumValues,
            activeWhen,
            apply = "next-search",
            requiresRestart = false,
        };
    }

    private static string Serialize(object value) => JsonSerializer.Serialize(value, JsonOptions);

    private sealed record ConfigurationState(
        ZenConfigurationProfile Selected,
        ZenConfigurationProfile Effective,
        bool Dirty,
        bool RestartRequired,
        string PersistenceOwner);
}

public sealed record ZenConfigurationProfile
{
    public string Mode { get; init; } = "rank";

    public string RankPreset { get; init; } = "9d";

    public double MaxTimeSeconds { get; init; } = 60.0;

    public int MaxSimulations { get; init; } = 6000;

    public int PnLevel { get; init; } = 3;

    public double PnWeight { get; init; } = 1.0;

    public double VnMixRate { get; init; } = 0.75;

    public int Threads { get; init; } = 4;

    public string FinalScoreRule { get; init; } = "japanese";

    public double ResignThreshold { get; init; } = 0.1;

    internal static ZenConfigurationProfile FromOptions(ZenGtpOptions options)
    {
        return new ZenConfigurationProfile
        {
            Mode = options.Mode,
            RankPreset = options.RankPreset,
            MaxTimeSeconds = options.MaxTimeSeconds,
            MaxSimulations = options.MaxSimulations,
            PnLevel = options.PnLevel,
            PnWeight = options.PnWeight,
            VnMixRate = options.VnMixRate,
            Threads = options.Threads,
            FinalScoreRule = options.FinalScoreRule,
            ResignThreshold = options.ResignThreshold,
        };
    }

    internal ZenGtpOptions ApplyTo(ZenGtpOptions options)
    {
        return options with
        {
            Mode = Mode,
            RankPreset = RankPreset,
            MaxTimeSeconds = MaxTimeSeconds,
            MaxSimulations = MaxSimulations,
            PnLevel = PnLevel,
            PnWeight = PnWeight,
            VnMixRate = VnMixRate,
            Threads = Threads,
            FinalScoreRule = FinalScoreRule,
            ResignThreshold = ResignThreshold,
        };
    }
}

public sealed class ZenConfigurationException : Exception
{
    public ZenConfigurationException(
        string code,
        string message,
        string? parameter = null,
        string? detail = null)
        : base(message)
    {
        Code = code;
        Parameter = parameter;
        Detail = detail;
    }

    public string Code { get; }

    public string? Parameter { get; }

    public string? Detail { get; }
}
