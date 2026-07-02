namespace ZenGTPX.Gtp;

public sealed record GtpCommand(string? Id, string Name, IReadOnlyList<string> Arguments);
