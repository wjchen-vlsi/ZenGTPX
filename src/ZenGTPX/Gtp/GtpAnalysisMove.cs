using ZenGTPX.Board;

namespace ZenGTPX.Gtp;

public sealed record GtpAnalysisMove(
    GtpMove Move,
    int Playouts,
    double Winrate,
    string PrincipalVariation,
    double? Prior = null);
