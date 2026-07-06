using System.Globalization;
using System.Text.Json;

namespace ZenGTPX.Config;

public sealed record ZenGtpOptions
{
    public string Mode { get; init; } = "rank";

    public string RankPreset { get; init; } = "9d";

    public string ZenDll { get; init; } = "Zen.dll";

    public string GtpName { get; init; } = "ZenGTPX";

    public string TracePath { get; init; } = "";

    public int BoardSize { get; init; } = 19;

    public double Komi { get; init; } = 7.5;

    public int Handicap { get; init; } = 0;

    public string FinalScoreRule { get; init; } = "japanese";

    public int Threads { get; init; } = 4;

    public double MaxTimeSeconds { get; init; } = 60.0;

    public double? MaxTime { get; init; }

    public int MaxSimulations { get; init; } = 6000;

    public double ResignThreshold { get; init; } = 0.1;

    public int PnLevel { get; init; } = 3;

    public double PnWeight { get; init; } = 0.75;

    public double VnMixRate { get; init; } = 1.0;

    public void Validate()
    {
        if (BoardSize <= 0 || BoardSize > 25)
        {
            throw new InvalidOperationException("boardSize must be between 1 and 25.");
        }

        if (!IsSupportedMode(Mode))
        {
            throw new InvalidOperationException("mode must be one of: rank, fixed-time, advanced.");
        }

        if (Handicap < 0)
        {
            throw new InvalidOperationException("handicap must not be negative.");
        }

        if (string.IsNullOrWhiteSpace(GtpName))
        {
            throw new InvalidOperationException("gtpName must not be empty.");
        }

        if (!IsSupportedFinalScoreRule(FinalScoreRule))
        {
            throw new InvalidOperationException("finalScoreRule must be one of: japanese, territory, chinese, area.");
        }

        if (Threads <= 0)
        {
            throw new InvalidOperationException("threads must be positive.");
        }

        if (MaxTimeSeconds < 0)
        {
            throw new InvalidOperationException("maxTimeSeconds must not be negative.");
        }

        if (MaxSimulations <= 0)
        {
            throw new InvalidOperationException("maxSimulations must be positive.");
        }

        if (ResignThreshold < 0 || ResignThreshold > 1)
        {
            throw new InvalidOperationException("resignThreshold must be between 0 and 1.");
        }
    }

    private static bool IsSupportedMode(string mode)
    {
        return mode.Equals("rank", StringComparison.OrdinalIgnoreCase)
            || mode.Equals("fixed-time", StringComparison.OrdinalIgnoreCase)
            || mode.Equals("fixedtime", StringComparison.OrdinalIgnoreCase)
            || mode.Equals("advanced", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTerritoryScoringRule(string rule)
    {
        return rule.Equals("japanese", StringComparison.OrdinalIgnoreCase)
            || rule.Equals("territory", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportedFinalScoreRule(string rule)
    {
        return IsTerritoryScoringRule(rule)
            || rule.Equals("chinese", StringComparison.OrdinalIgnoreCase)
            || rule.Equals("area", StringComparison.OrdinalIgnoreCase);
    }

    public string ResolveZenDllPath(string baseDirectory)
    {
        return Path.IsPathRooted(ZenDll)
            ? Path.GetFullPath(ZenDll)
            : Path.GetFullPath(Path.Combine(baseDirectory, ZenDll));
    }
}

public static class ZenGtpOptionsLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ZenGtpOptions Load(string[] args, string baseDirectory)
    {
        return Load(args, baseDirectory, baseDirectory);
    }

    public static ZenGtpOptions Load(string[] args, string baseDirectory, string defaultConfigDirectory)
    {
        var configPath = GetArgumentValue(args, "--config");
        var options = LoadConfigOrDefault(configPath, baseDirectory, defaultConfigDirectory);

        return ApplyCommandLineOverrides(options, args);
    }

    private static ZenGtpOptions LoadConfigOrDefault(string? configPath, string baseDirectory, string defaultConfigDirectory)
    {
        if (configPath is not null)
        {
            return LoadConfig(configPath, baseDirectory);
        }

        var defaultConfigPath = Path.Combine(defaultConfigDirectory, "zen7.cfg");
        return File.Exists(defaultConfigPath)
            ? LoadConfig(defaultConfigPath, defaultConfigDirectory)
            : new ZenGtpOptions();
    }

    private static ZenGtpOptions LoadConfig(string configPath, string baseDirectory)
    {
        var fullPath = Path.IsPathRooted(configPath)
            ? Path.GetFullPath(configPath)
            : Path.GetFullPath(Path.Combine(baseDirectory, configPath));

        if (Path.GetExtension(fullPath).Equals(".cfg", StringComparison.OrdinalIgnoreCase))
        {
            return LoadCfgConfig(fullPath);
        }

        using var stream = File.OpenRead(fullPath);
        var options = JsonSerializer.Deserialize<ZenGtpOptions>(stream, JsonOptions);
        return options ?? throw new InvalidOperationException($"Config file is empty: {fullPath}");
    }

    private static ZenGtpOptions LoadCfgConfig(string fullPath)
    {
        var options = new ZenGtpOptions();
        var lineNumber = 0;

        foreach (var rawLine in File.ReadLines(fullPath))
        {
            lineNumber++;
            var line = StripComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                throw new InvalidOperationException($"Invalid config line {lineNumber}: expected key = value.");
            }

            var key = line[..separator].Trim();
            var value = Unquote(line[(separator + 1)..].Trim());
            options = ApplyCfgValue(options, key, value, lineNumber);
        }

        return options;
    }

    private static ZenGtpOptions ApplyCfgValue(ZenGtpOptions options, string key, string value, int lineNumber)
    {
        return key.ToLowerInvariant() switch
        {
            "mode" => options with { Mode = value },
            "rankpreset" => options with { RankPreset = value },
            "zendll" => options with { ZenDll = value },
            "gtpname" => options with { GtpName = value },
            "tracepath" => options with { TracePath = value },
            "boardsize" => options with { BoardSize = ParseInt(value, key, lineNumber) },
            "komi" => options with { Komi = ParseDouble(value, key, lineNumber) },
            "handicap" => options with { Handicap = ParseInt(value, key, lineNumber) },
            "finalscorerule" => options with { FinalScoreRule = value },
            "threads" => options with { Threads = ParseInt(value, key, lineNumber) },
            "maxtimeseconds" => options with { MaxTimeSeconds = ParseDouble(value, key, lineNumber) },
            "maxtime" => options with { MaxTimeSeconds = ParseDouble(value, key, lineNumber) },
            "maxsimulations" => options with { MaxSimulations = ParseInt(value, key, lineNumber) },
            "resignthreshold" => options with { ResignThreshold = ParseDouble(value, key, lineNumber) },
            "pnlevel" => options with { PnLevel = ParseInt(value, key, lineNumber) },
            "pnweight" => options with { PnWeight = ParseDouble(value, key, lineNumber) },
            "vnmixrate" => options with { VnMixRate = ParseDouble(value, key, lineNumber) },
            _ => throw new InvalidOperationException($"Unknown config key on line {lineNumber}: {key}"),
        };
    }

    private static ZenGtpOptions ApplyCommandLineOverrides(ZenGtpOptions options, string[] args)
    {
        var normalized = options.NormalizeAliases().ApplyModeDefaults();
        var overridden = normalized with
        {
            Mode = GetArgumentValue(args, "--mode") ?? normalized.Mode,
            RankPreset = GetArgumentValue(args, "--rankPreset") ?? normalized.RankPreset,
            ZenDll = GetArgumentValue(args, "--zenDll") ?? normalized.ZenDll,
            GtpName = GetArgumentValue(args, "--gtpName") ?? normalized.GtpName,
            TracePath = GetArgumentValue(args, "--tracePath") ?? normalized.TracePath,
            BoardSize = GetIntArgument(args, "--boardSize") ?? normalized.BoardSize,
            Komi = GetDoubleArgument(args, "--komi") ?? normalized.Komi,
            Handicap = GetIntArgument(args, "--handicap") ?? normalized.Handicap,
            FinalScoreRule = GetArgumentValue(args, "--finalScoreRule") ?? normalized.FinalScoreRule,
            Threads = GetIntArgument(args, "--threads") ?? normalized.Threads,
            MaxTimeSeconds = GetDoubleArgument(args, "--maxTimeSeconds") ?? GetDoubleArgument(args, "--maxTime") ?? normalized.MaxTimeSeconds,
            MaxSimulations = GetIntArgument(args, "--maxSimulations") ?? normalized.MaxSimulations,
            ResignThreshold = GetDoubleArgument(args, "--resignThreshold") ?? normalized.ResignThreshold,
            PnLevel = GetIntArgument(args, "--pnLevel") ?? normalized.PnLevel,
            PnWeight = GetDoubleArgument(args, "--pnWeight") ?? normalized.PnWeight,
            VnMixRate = GetDoubleArgument(args, "--vnMixRate") ?? normalized.VnMixRate,
        };

        return overridden.ApplyModeDefaults().ApplyAdvancedCommandLineOverrides(args).ValidateAndReturn();
    }

    private static ZenGtpOptions ApplyAdvancedCommandLineOverrides(this ZenGtpOptions options, string[] args)
    {
        return options with
        {
            MaxTimeSeconds = GetDoubleArgument(args, "--maxTimeSeconds") ?? GetDoubleArgument(args, "--maxTime") ?? options.MaxTimeSeconds,
            MaxSimulations = GetIntArgument(args, "--maxSimulations") ?? options.MaxSimulations,
            PnLevel = GetIntArgument(args, "--pnLevel") ?? options.PnLevel,
            PnWeight = GetDoubleArgument(args, "--pnWeight") ?? options.PnWeight,
            VnMixRate = GetDoubleArgument(args, "--vnMixRate") ?? options.VnMixRate,
        };
    }

    private static string? GetArgumentValue(string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    throw new ArgumentException($"{name} requires a value.");
                }

                return args[i + 1];
            }
        }

        return null;
    }

    private static int? GetIntArgument(string[] args, string name)
    {
        var value = GetArgumentValue(args, name);
        return value is null ? null : int.Parse(value, CultureInfo.InvariantCulture);
    }

    private static double? GetDoubleArgument(string[] args, string name)
    {
        var value = GetArgumentValue(args, name);
        return value is null ? null : double.Parse(value, CultureInfo.InvariantCulture);
    }

    private static int ParseInt(string value, string key, int lineNumber)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            throw new InvalidOperationException($"Invalid integer for {key} on line {lineNumber}: {value}");
        }

        return result;
    }

    private static double ParseDouble(string value, string key, int lineNumber)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        {
            throw new InvalidOperationException($"Invalid number for {key} on line {lineNumber}: {value}");
        }

        return result;
    }

    private static string StripComment(string line)
    {
        var commentStart = line.IndexOf('#');
        return commentStart < 0 ? line : line[..commentStart];
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }

    private static ZenGtpOptions ValidateAndReturn(this ZenGtpOptions options)
    {
        options.Validate();
        return options;
    }

    private static ZenGtpOptions NormalizeAliases(this ZenGtpOptions options)
    {
        return options.MaxTime is null
            ? options
            : options with { MaxTimeSeconds = options.MaxTime.Value };
    }

    private static ZenGtpOptions ApplyModeDefaults(this ZenGtpOptions options)
    {
        if (options.Mode.Equals("advanced", StringComparison.OrdinalIgnoreCase))
        {
            return options;
        }

        if (options.Mode.Equals("fixed-time", StringComparison.OrdinalIgnoreCase)
            || options.Mode.Equals("fixedtime", StringComparison.OrdinalIgnoreCase))
        {
            return options with
            {
                Mode = "fixed-time",
                MaxSimulations = 1_000_000,
                PnLevel = 3,
                PnWeight = 1.0,
                VnMixRate = 0.75,
            };
        }

        if (options.Mode.Equals("rank", StringComparison.OrdinalIgnoreCase))
        {
            var preset = RankPresetTable.Get(options.RankPreset);
            return options with
            {
                Mode = "rank",
                RankPreset = preset.Name,
                Threads = Math.Min(options.Threads, 4),
                MaxTimeSeconds = 60.0,
                MaxSimulations = preset.MaxSimulations,
                PnLevel = preset.PnLevel,
                PnWeight = preset.PnWeight,
                VnMixRate = preset.VnMixRate,
            };
        }

        return options;
    }
}

internal readonly record struct RankPreset(
    string Name,
    int MaxSimulations,
    int PnLevel,
    double PnWeight,
    double VnMixRate);

internal static class RankPresetTable
{
    private static readonly Dictionary<string, RankPreset> Presets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["6k"] = new("6k", 1000, 0, 0.30, 1.6),
        ["5k"] = new("5k", 1100, 0, 0.30, 1.4),
        ["4k"] = new("4k", 1200, 0, 0.30, 1.0),
        ["3k"] = new("3k", 1300, 1, 0.30, 2.4),
        ["2k"] = new("2k", 1400, 1, 0.30, 2.0),
        ["1k"] = new("1k", 1600, 1, 0.30, 1.6),
        ["1d"] = new("1d", 1800, 1, 0.35, 1.3),
        ["2d"] = new("2d", 2000, 1, 0.40, 1.0),
        ["3d"] = new("3d", 2200, 2, 0.45, 2.0),
        ["4d"] = new("4d", 2400, 2, 0.50, 1.5),
        ["5d"] = new("5d", 2700, 2, 0.55, 1.0),
        ["6d"] = new("6d", 3000, 3, 0.60, 4.4),
        ["7d"] = new("7d", 3500, 3, 0.65, 2.8),
        ["8d"] = new("8d", 4000, 3, 0.70, 1.4),
        ["9d"] = new("9d", 6000, 3, 0.75, 1.0),
    };

    public static RankPreset Get(string name)
    {
        if (Presets.TryGetValue(name, out var preset))
        {
            return preset;
        }

        throw new InvalidOperationException("rankPreset must be one of: 6k, 5k, 4k, 3k, 2k, 1k, 1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d, 9d.");
    }
}
