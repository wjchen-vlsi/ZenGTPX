using ZenGTPX.Board;

namespace ZenGTPX.Gtp;

public sealed record GtpSearchInfo(
    GtpMove Move,
    int Playouts,
    double Winrate,
    double TimeSeconds,
    string StopReason = "",
    double MaxTimeSeconds = 0,
    int MaxSimulations = 0,
    int Threads = 0);
