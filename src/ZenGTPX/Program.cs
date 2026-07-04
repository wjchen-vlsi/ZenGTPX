using ZenGTPX.Gtp;
using ZenGTPX.Config;
using ZenGTPX.Zen;

using var engine = InitializeZen(args);
if (engine is null)
{
    return 1;
}

var session = new GtpSession(engine);

while (Console.In.ReadLine() is { } line)
{
    var command = GtpCommandParser.Parse(line);
    if (command is null)
    {
        continue;
    }

    var result = session.Execute(command);
    if (result.OutputBeforeResponse.Length > 0)
    {
        Console.Out.Write(result.OutputBeforeResponse);
    }

    Console.Out.Write(result.Response.Format());
    Console.Out.Flush();

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
