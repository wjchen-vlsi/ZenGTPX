using System.Globalization;
using ZenGTPX.Gtp;
using ZenGTPX.Config;
using ZenGTPX.Zen;

var startup = InitializeZen(args);
if (startup is null)
{
    return 1;
}

using var engine = startup.Value.Engine;
var outputLock = new object();
using var trace = CreateTraceWriter(startup.Value.Options, AppContext.BaseDirectory);
var session = new GtpSession(engine, WriteAnalysisOutput);

while (Console.In.ReadLine() is { } line)
{
    Trace("< " + line);
    var command = GtpCommandParser.Parse(line);
    if (command is null)
    {
        session.InterruptAnalysis();
        continue;
    }

    var result = session.Execute(command);
    if (result.OutputBeforeResponse.Length > 0)
    {
        Trace("> " + result.OutputBeforeResponse.TrimEnd());
    }

    Trace("> " + result.Response.Format().TrimEnd());
    lock (outputLock)
    {
        if (result.OutputBeforeResponse.Length > 0)
        {
            Console.Out.Write(result.OutputBeforeResponse);
        }

        Console.Out.Write(result.Response.Format());
        Console.Out.Flush();
    }

    if (result.ShouldQuit)
    {
        break;
    }
}

return 0;

static (ZenEngine Engine, ZenGtpOptions Options)? InitializeZen(string[] args)
{
    var baseDirectory = AppContext.BaseDirectory;

    try
    {
        var options = ZenGtpOptionsLoader.Load(args, Environment.CurrentDirectory, baseDirectory);
        return (ZenEngine.CreateInitialized(options, baseDirectory), options);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"ZenGTPX startup failed: {ex.Message}");
        return null;
    }
}

void WriteAnalysisOutput(string output)
{
    Trace("> " + output.TrimEnd());
    lock (outputLock)
    {
        Console.Out.Write(output);
        Console.Out.Flush();
    }
}

static StreamWriter? CreateTraceWriter(ZenGtpOptions options, string baseDirectory)
{
    var path = Environment.GetEnvironmentVariable("ZENGTPX_TRACE_PATH");
    if (string.IsNullOrWhiteSpace(path))
    {
        path = options.TracePath;
    }

    if (string.IsNullOrWhiteSpace(path))
    {
        return null;
    }

    try
    {
        path = ExpandTracePath(path, baseDirectory);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            AutoFlush = true,
        };
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"ZenGTPX trace disabled: {ex.Message}");
        return null;
    }
}

static string ExpandTracePath(string path, string baseDirectory)
{
    var now = DateTimeOffset.Now;
    var expanded = path
        .Replace("{timestamp}", now.ToString("yyyyMMdd-HHmmss"), StringComparison.OrdinalIgnoreCase)
        .Replace("{date}", now.ToString("yyyyMMdd"), StringComparison.OrdinalIgnoreCase)
        .Replace("{pid}", Environment.ProcessId.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

    return Path.IsPathRooted(expanded)
        ? Path.GetFullPath(expanded)
        : Path.GetFullPath(Path.Combine(baseDirectory, expanded));
}

void Trace(string message)
{
    trace?.WriteLine($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} {message}");
}
