namespace ZenGTPX.Board;

public static class GtpVertex
{
    private const char SkippedColumn = 'I';

    public static GtpMove Parse(string value, int boardSize)
    {
        ValidateBoardSize(boardSize);

        if (value.Equals("pass", StringComparison.OrdinalIgnoreCase))
        {
            return GtpMove.Pass;
        }

        if (value.Equals("resign", StringComparison.OrdinalIgnoreCase))
        {
            return GtpMove.Resign;
        }

        if (value.Length < 2)
        {
            throw new FormatException($"Invalid GTP vertex: {value}");
        }

        var column = char.ToUpperInvariant(value[0]);
        if (column < 'A' || column > 'Z' || column == SkippedColumn)
        {
            throw new FormatException($"Invalid GTP vertex column: {value}");
        }

        if (!int.TryParse(value[1..], out var row))
        {
            throw new FormatException($"Invalid GTP vertex row: {value}");
        }

        var x = column > SkippedColumn ? column - 'A' - 1 : column - 'A';
        var y = boardSize - row;

        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize)
        {
            throw new FormatException($"GTP vertex is outside board: {value}");
        }

        return GtpMove.Play(new BoardCoordinate(x, y));
    }

    public static string Format(BoardCoordinate coordinate, int boardSize)
    {
        ValidateBoardSize(boardSize);

        if (coordinate.X < 0 || coordinate.X >= boardSize || coordinate.Y < 0 || coordinate.Y >= boardSize)
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate), "Coordinate is outside board.");
        }

        var columnIndex = coordinate.X >= 8 ? coordinate.X + 1 : coordinate.X;
        var column = (char)('A' + columnIndex);
        var row = boardSize - coordinate.Y;
        return $"{column}{row}";
    }

    private static void ValidateBoardSize(int boardSize)
    {
        if (boardSize <= 0 || boardSize > 25)
        {
            throw new ArgumentOutOfRangeException(nameof(boardSize), "Board size must be between 1 and 25.");
        }
    }
}
