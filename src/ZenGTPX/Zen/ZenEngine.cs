using ZenGTPX.Config;
using ZenGTPX.Board;
using ZenGTPX.Gtp;

namespace ZenGTPX.Zen;

public sealed class ZenEngine : IGtpEngine, IDisposable
{
    private readonly ZenNative _native;
    private readonly ZenGtpOptions _options;
    private int _boardSize;
    private double _maxTime;

    private ZenEngine(ZenNative native, ZenGtpOptions options)
    {
        _native = native;
        _options = options;
        _boardSize = options.BoardSize;
        _maxTime = options.MaxTimeSeconds;
    }

    public static ZenEngine CreateInitialized(ZenGtpOptions options, string baseDirectory)
    {
        var dllPath = options.ResolveZenDllPath(baseDirectory);
        var native = ZenNative.Load(dllPath);

        try
        {
            using var emptyString = new AnsiString("");
            native.Initialize(emptyString.Pointer);

            if (!native.IsInitialized)
            {
                throw new InvalidOperationException("ZenInitialize completed, but ZenIsInitialized returned false.");
            }

            native.SetNumberOfThreads(options.Threads);
            native.SetNumberOfSimulations(options.MaxSimulations);
            native.SetMaxTime((float)options.MaxTimeSeconds);
            native.SetBoardSize(options.BoardSize);
            native.SetKomi((float)options.Komi);
            native.SetPnLevel(options.PnLevel);
            native.SetPnWeight((float)options.PnWeight);
            native.SetVnMixRate((float)options.VnMixRate);
            native.ClearBoard();

            return new ZenEngine(native, options);
        }
        catch
        {
            native.Dispose();
            throw;
        }
    }

    public int BoardSize => _boardSize;

    public void SetBoardSize(int boardSize)
    {
        if (boardSize <= 0 || boardSize > 25)
        {
            throw new ArgumentOutOfRangeException(nameof(boardSize), "Board size must be between 1 and 25.");
        }

        _native.SetBoardSize(boardSize);
        _boardSize = boardSize;
    }

    public void ClearBoard()
    {
        _native.ClearBoard();
    }

    public void SetKomi(double komi)
    {
        _native.SetKomi((float)komi);
    }

    public void SetMaxTime(double seconds)
    {
        if (seconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), "Max time must not be negative.");
        }

        _native.SetMaxTime((float)seconds);
        _maxTime = seconds;
    }

    public bool Play(StoneColor color, GtpMove move)
    {
        var zenColor = (int)color;
        if (move.IsPass)
        {
            _native.Pass(zenColor);
            return true;
        }

        if (move.IsResign || move.Coordinate is not { } coordinate)
        {
            return false;
        }

        return _native.Play(coordinate.X, coordinate.Y, zenColor);
    }

    public GtpMove GenMove(StoneColor color)
    {
        var zenColor = (int)color;
        var topMove = ThinkUntilTopMove(zenColor);

        if (topMove.Playouts <= 0 || !IsOnBoard(topMove.X, topMove.Y))
        {
            _native.Pass(zenColor);
            return GtpMove.Pass;
        }

        if (topMove.Winrate < _options.ResignThreshold)
        {
            return GtpMove.Resign;
        }

        var coordinate = new BoardCoordinate(topMove.X, topMove.Y);
        return _native.Play(coordinate.X, coordinate.Y, zenColor)
            ? GtpMove.Play(coordinate)
            : Pass(zenColor);
    }

    public bool Undo(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Undo count must be positive.");
        }

        return _native.Undo(count);
    }

    private ZenTopMove ThinkUntilTopMove(int zenColor)
    {
        ZenTopMove topMove = default;
        _native.StartThinking(zenColor);

        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(Math.Max(0.1, _maxTime) + 0.5);
            while (DateTime.UtcNow < deadline)
            {
                Thread.Sleep(100);
                topMove = _native.GetTopMoveInfo(0);
                if (topMove.Playouts >= _options.MaxSimulations || !_native.IsThinking)
                {
                    break;
                }
            }
        }
        finally
        {
            _native.StopThinking();
        }

        return topMove.Playouts > 0 ? topMove : _native.GetTopMoveInfo(0);
    }

    private GtpMove Pass(int zenColor)
    {
        _native.Pass(zenColor);
        return GtpMove.Pass;
    }

    private bool IsOnBoard(int x, int y)
    {
        return x >= 0 && x < _boardSize && y >= 0 && y < _boardSize;
    }

    public void Dispose()
    {
        _native.Dispose();
    }
}
