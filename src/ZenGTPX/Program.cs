using ZenGTPX.Gtp;
using ZenGTPX.Config;
using ZenGTPX.Zen;

using var engine = InitializeZen(args);
if (engine is null)
{
    return 1;
}

var outputLock = new object();
using var trace = CreateTraceWriter();
var session = new GtpSession(engine, WriteAnalysisOutput);

while (Console.In.ReadLine() is { } line)
{
    Trace("< " + line);
    var command = GtpCommandParser.Parse(line);
    if (command is null)
    {
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

static ZenEngine? InitializeZen(string[] args)
{
    var baseDirectory = AppContext.BaseDirectory;

    try
    {
        var options = ZenGtpOptionsLoader.Load(args, Environment.CurrentDirectory, baseDirectory);
        return ZenEngine.CreateInitialized(options, baseDirectory);
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

static StreamWriter? CreateTraceWriter()
{
    var path = Environment.GetEnvironmentVariable("ZENGTPX_TRACE_PATH");
    if (string.IsNullOrWhiteSpace(path))
    {
        return null;
    }

    try
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
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

void Trace(string message)
{
    trace?.WriteLine($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} {message}");
}
