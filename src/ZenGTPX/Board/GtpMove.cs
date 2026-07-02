namespace ZenGTPX.Board;

public readonly record struct GtpMove(BoardCoordinate? Coordinate, bool IsPass, bool IsResign)
{
    public static GtpMove Pass { get; } = new(null, IsPass: true, IsResign: false);

    public static GtpMove Resign { get; } = new(null, IsPass: false, IsResign: true);

    public static GtpMove Play(BoardCoordinate coordinate) => new(coordinate, IsPass: false, IsResign: false);
}
