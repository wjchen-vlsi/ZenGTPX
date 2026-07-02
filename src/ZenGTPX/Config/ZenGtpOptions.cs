using System.Globalization;
using System.Text.Json;

namespace ZenGTPX.Config;

public sealed record ZenGtpOptions
{
    private const int SupportedHandicap = 0;

    public string ZenDll { get; init; } = "Zen.dll";

    public int BoardSize { get; init; } = 19;

    public double Komi { get; init; } = 7.5;

    public int Handicap { get; init; } = 0;

    public int Threads { get; init; } = 1;

    public double MaxTimeSeconds { get; init; } = 1.0;

    public double? MaxTime { get; init; }

    public int MaxSimulations { get; init; } = 100;

    public double ResignThreshold { get; init; } = 0.1;

    public int PnLevel { get; init; } = 2;

    public double PnWeight { get; init; } = 1.0;

    public double VnMixRate { get; init; } = 0.55;

    public void Validate()
    {
        if (BoardSize <= 0 || BoardSize > 25)
        {
            throw new InvalidOperationException("boardSize must be between 1 and 25.");
        }

        if (Handicap != SupportedHandicap)
        {
            throw new InvalidOperationException("handicap is not supported in the first version; set handicap to 0.");
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
            "zendll" => options with { ZenDll = value },
            "boardsize" => options with { BoardSize = ParseInt(value, key, lineNumber) },
            "komi" => options with { Komi = ParseDouble(value, key, lineNumber) },
            "handicap" => options with { Handicap = ParseInt(value, key, lineNumber) },
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
        var normalized = options.NormalizeAliases();
        var overridden = normalized with
        {
            ZenDll = GetArgumentValue(args, "--zenDll") ?? normalized.ZenDll,
            BoardSize = GetIntArgument(args, "--boardSize") ?? normalized.BoardSize,
            Komi = GetDoubleArgument(args, "--komi") ?? normalized.Komi,
            Handicap = GetIntArgument(args, "--handicap") ?? normalized.Handicap,
            Threads = GetIntArgument(args, "--threads") ?? normalized.Threads,
            MaxTimeSeconds = GetDoubleArgument(args, "--maxTimeSeconds") ?? GetDoubleArgument(args, "--maxTime") ?? normalized.MaxTimeSeconds,
            MaxSimulations = GetIntArgument(args, "--maxSimulations") ?? normalized.MaxSimulations,
            ResignThreshold = GetDoubleArgument(args, "--resignThreshold") ?? normalized.ResignThreshold,
            PnLevel = GetIntArgument(args, "--pnLevel") ?? normalized.PnLevel,
            PnWeight = GetDoubleArgument(args, "--pnWeight") ?? normalized.PnWeight,
            VnMixRate = GetDoubleArgument(args, "--vnMixRate") ?? normalized.VnMixRate,
        };

        return overridden.ValidateAndReturn();
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
}
