using ZenGTPX.Board;

namespace ZenGTPX.Gtp;

public sealed record GtpPolicyPoint(BoardCoordinate Coordinate, int Value, double Normalized);
