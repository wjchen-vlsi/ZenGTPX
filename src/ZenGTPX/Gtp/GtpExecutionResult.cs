namespace ZenGTPX.Gtp;

public sealed record GtpExecutionResult(
    GtpResponse Response,
    bool ShouldQuit,
    string OutputBeforeResponse = "",
    bool SuppressResponse = false);
