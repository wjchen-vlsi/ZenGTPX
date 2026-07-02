using System.Text;

namespace ZenGTPX.Board;

public sealed class BoardState
{
    private readonly List<MoveRecord> _moves = [];
    private StoneColor?[,] _stones;

    public BoardState(int boardSize)
    {
        ValidateBoardSize(boardSize);
        BoardSize = boardSize;
        _stones = new StoneColor?[boardSize, boardSize];
    }

    public int BoardSize { get; private set; }

    public void SetBoardSize(int boardSize)
    {
        ValidateBoardSize(boardSize);
        BoardSize = boardSize;
        _stones = new StoneColor?[boardSize, boardSize];
        _moves.Clear();
    }

    public void Clear()
    {
        _stones = new StoneColor?[BoardSize, BoardSize];
        _moves.Clear();
    }

    public void Play(StoneColor color, GtpMove move)
    {
        if (move.IsResign)
        {
            return;
        }

        if (move.IsPass || move.Coordinate is not { } coordinate)
        {
            _moves.Add(new MoveRecord(color, null));
            return;
        }

        ValidateCoordinate(coordinate);
        if (_stones[coordinate.X, coordinate.Y] is not null)
        {
            throw new InvalidOperationException($"Board state already has a stone at {GtpVertex.Format(coordinate, BoardSize)}.");
        }

        PlaceStone(color, coordinate);
        _moves.Add(new MoveRecord(color, coordinate));
    }

    public void Undo(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Undo count must be positive.");
        }

        var removeCount = Math.Min(count, _moves.Count);
        _moves.RemoveRange(_moves.Count - removeCount, removeCount);
        RebuildStones();
    }

    public string FormatShowBoard()
    {
        var builder = new StringBuilder();
        builder.Append("   ");
        for (var x = 0; x < BoardSize; x++)
        {
            builder.Append(ColumnName(x));
            if (x + 1 < BoardSize)
            {
                builder.Append(' ');
            }
        }

        for (var y = 0; y < BoardSize; y++)
        {
            var row = BoardSize - y;
            builder.AppendLine();
            builder.Append(row.ToString().PadLeft(2));
            builder.Append(' ');

            for (var x = 0; x < BoardSize; x++)
            {
                builder.Append(StoneName(_stones[x, y]));
                if (x + 1 < BoardSize)
                {
                    builder.Append(' ');
                }
            }

            builder.Append(' ');
            builder.Append(row);
        }

        builder.AppendLine();
        builder.Append("   ");
        for (var x = 0; x < BoardSize; x++)
        {
            builder.Append(ColumnName(x));
            if (x + 1 < BoardSize)
            {
                builder.Append(' ');
            }
        }

        return builder.ToString();
    }

    private void RebuildStones()
    {
        _stones = new StoneColor?[BoardSize, BoardSize];
        foreach (var move in _moves)
        {
            if (move.Coordinate is { } coordinate)
            {
                PlaceStone(move.Color, coordinate);
            }
        }
    }

    private void PlaceStone(StoneColor color, BoardCoordinate coordinate)
    {
        _stones[coordinate.X, coordinate.Y] = color;
        foreach (var neighbor in Neighbors(coordinate))
        {
            if (_stones[neighbor.X, neighbor.Y] == Opposite(color))
            {
                var group = CollectGroup(neighbor);
                if (!HasLiberty(group))
                {
                    RemoveGroup(group);
                }
            }
        }

        var ownGroup = CollectGroup(coordinate);
        if (!HasLiberty(ownGroup))
        {
            throw new InvalidOperationException($"Move has no liberties at {GtpVertex.Format(coordinate, BoardSize)}.");
        }
    }

    private List<BoardCoordinate> CollectGroup(BoardCoordinate start)
    {
        var color = _stones[start.X, start.Y];
        if (color is null)
        {
            return [];
        }

        var group = new List<BoardCoordinate>();
        var seen = new bool[BoardSize, BoardSize];
        var stack = new Stack<BoardCoordinate>();
        stack.Push(start);
        seen[start.X, start.Y] = true;

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            group.Add(current);
            foreach (var neighbor in Neighbors(current))
            {
                if (!seen[neighbor.X, neighbor.Y] && _stones[neighbor.X, neighbor.Y] == color)
                {
                    seen[neighbor.X, neighbor.Y] = true;
                    stack.Push(neighbor);
                }
            }
        }

        return group;
    }

    private bool HasLiberty(IEnumerable<BoardCoordinate> group)
    {
        foreach (var stone in group)
        {
            foreach (var neighbor in Neighbors(stone))
            {
                if (_stones[neighbor.X, neighbor.Y] is null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void RemoveGroup(IEnumerable<BoardCoordinate> group)
    {
        foreach (var stone in group)
        {
            _stones[stone.X, stone.Y] = null;
        }
    }

    private IEnumerable<BoardCoordinate> Neighbors(BoardCoordinate coordinate)
    {
        if (coordinate.X > 0)
        {
            yield return new BoardCoordinate(coordinate.X - 1, coordinate.Y);
        }

        if (coordinate.X + 1 < BoardSize)
        {
            yield return new BoardCoordinate(coordinate.X + 1, coordinate.Y);
        }

        if (coordinate.Y > 0)
        {
            yield return new BoardCoordinate(coordinate.X, coordinate.Y - 1);
        }

        if (coordinate.Y + 1 < BoardSize)
        {
            yield return new BoardCoordinate(coordinate.X, coordinate.Y + 1);
        }
    }

    private static StoneColor Opposite(StoneColor color)
    {
        return color == StoneColor.Black ? StoneColor.White : StoneColor.Black;
    }

    private void ValidateCoordinate(BoardCoordinate coordinate)
    {
        if (coordinate.X < 0 || coordinate.X >= BoardSize || coordinate.Y < 0 || coordinate.Y >= BoardSize)
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate), "Coordinate is outside board.");
        }
    }

    private static void ValidateBoardSize(int boardSize)
    {
        if (boardSize <= 0 || boardSize > 25)
        {
            throw new ArgumentOutOfRangeException(nameof(boardSize), "Board size must be between 1 and 25.");
        }
    }

    private static char ColumnName(int x)
    {
        var columnIndex = x >= 8 ? x + 1 : x;
        return (char)('A' + columnIndex);
    }

    private static char StoneName(StoneColor? color)
    {
        return color switch
        {
            StoneColor.Black => 'X',
            StoneColor.White => 'O',
            _ => '.',
        };
    }

    private readonly record struct MoveRecord(StoneColor Color, BoardCoordinate? Coordinate);
}
