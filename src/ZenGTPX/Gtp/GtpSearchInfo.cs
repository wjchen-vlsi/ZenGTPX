using ZenGTPX.Board;

namespace ZenGTPX.Gtp;

public sealed record GtpSearchInfo(
    GtpMove Move,
    int Playouts,
    double Winrate,
    double TimeSeconds);
