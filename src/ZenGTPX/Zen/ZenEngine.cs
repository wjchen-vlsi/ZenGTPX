using ZenGTPX.Config;
using ZenGTPX.Board;
using ZenGTPX.Gtp;
using System.Diagnostics;
using System.Globalization;

namespace ZenGTPX.Zen;

public sealed class ZenEngine : IGtpEngine, IDisposable
{
    private readonly ZenNative _native;
    private readonly ZenGtpOptions _options;
    private readonly double _configuredMaxTime;
    private int _boardSize;
    private double _komi;
    private double _maxTime;

    private ZenEngine(ZenNative native, ZenGtpOptions options)
    {
        _native = native;
        _options = options;
        _configuredMaxTime = options.MaxTimeSeconds;
        _boardSize = options.BoardSize;
        _komi = options.Komi;
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

    public string GtpName => _options.GtpName;

    public GtpSearchInfo? LastSearchInfo { get; private set; }

    public bool RuntimeTimeOverrideEnabled => _options.RuntimeTimeOverrideEnabled;

    public void SetBoardSize(int boardSize)
    {
        if (boardSize <= 0 || boardSize > 25)
        {
            throw new ArgumentOutOfRangeException(nameof(boardSize), "Board size must be between 1 and 25.");
        }

        _native.SetBoardSize(boardSize);
        _boardSize = boardSize;
        LastSearchInfo = null;
    }

    public void ClearBoard()
    {
        _native.ClearBoard();
        LastSearchInfo = null;
    }

    public void SetKomi(double komi)
    {
        _native.SetKomi((float)komi);
        _komi = komi;
    }

    public void SetNextColor(StoneColor color)
    {
        _native.SetNextColor((int)color);
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

    public void ResetMaxTime()
    {
        SetMaxTime(_configuredMaxTime);
    }

    public void SetTimeSettings(double mainTime, double byoyomiTime, int periods)
    {
        if (mainTime < 0 || byoyomiTime < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mainTime), "Time settings must not be negative.");
        }

        if (periods < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(periods), "Time periods must not be negative.");
        }

        _native.TimeSettings(ToNativeSeconds(mainTime), ToNativeSeconds(byoyomiTime), periods);

        var maxTime = byoyomiTime > 0 ? byoyomiTime : mainTime;
        if (maxTime > 0)
        {
            SetMaxTime(maxTime);
        }
        else
        {
            ResetMaxTime();
        }
    }

    public void SetTimeLeft(StoneColor color, double time, int stones)
    {
        if (time < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(time), "Time left must not be negative.");
        }

        _native.TimeLeft((int)color, ToNativeSeconds(time), stones);
    }

    public bool Play(StoneColor color, GtpMove move)
    {
        LastSearchInfo = null;
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
        _native.SetNextColor(zenColor);
        var stopwatch = Stopwatch.StartNew();
        var topMove = ThinkUntilTopMove(zenColor);
        stopwatch.Stop();

        if (topMove.Playouts <= 0 || !IsOnBoard(topMove.X, topMove.Y))
        {
            _native.Pass(zenColor);
            LastSearchInfo = new GtpSearchInfo(GtpMove.Pass, topMove.Playouts, topMove.Winrate, stopwatch.Elapsed.TotalSeconds);
            return GtpMove.Pass;
        }

        if (topMove.Winrate < _options.ResignThreshold)
        {
            LastSearchInfo = new GtpSearchInfo(GtpMove.Resign, topMove.Playouts, topMove.Winrate, stopwatch.Elapsed.TotalSeconds);
            return GtpMove.Resign;
        }

        var coordinate = new BoardCoordinate(topMove.X, topMove.Y);
        var move = _native.Play(coordinate.X, coordinate.Y, zenColor)
            ? GtpMove.Play(coordinate)
            : Pass(zenColor);
        LastSearchInfo = new GtpSearchInfo(move, topMove.Playouts, topMove.Winrate, stopwatch.Elapsed.TotalSeconds);
        return move;
    }

    public IReadOnlyList<GtpAnalysisMove> Analyze(StoneColor color, int maxCandidates, CancellationToken cancellationToken)
    {
        if (maxCandidates <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCandidates), "Analysis candidate count must be positive.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var candidateCount = Math.Min(maxCandidates, 10);
        var zenColor = (int)color;
        _native.SetNextColor(zenColor);
        var moves = ThinkUntilAnalysisMoves(zenColor, candidateCount, cancellationToken);
        return WithPolicyPriors(moves.Count > 0 ? moves : ReadAnalysisMoves(candidateCount));
    }

    public bool Undo(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Undo count must be positive.");
        }

        var result = _native.Undo(count);
        if (result)
        {
            LastSearchInfo = null;
        }

        return result;
    }

    public IReadOnlyList<GtpPolicyPoint> GetPolicy(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Policy point count must be positive.");
        }

        if (_boardSize > 19)
        {
            throw new InvalidOperationException("policy diagnostics are supported only up to board size 19.");
        }

        var policy = _native.GetPolicyKnowledge();
        var positiveSum = 0;
        var points = new List<(BoardCoordinate Coordinate, int Value)>(_boardSize * _boardSize);
        for (var y = 0; y < _boardSize; y++)
        {
            for (var x = 0; x < _boardSize; x++)
            {
                var value = policy[y, x];
                if (value <= 0)
                {
                    continue;
                }

                positiveSum += value;
                points.Add((new BoardCoordinate(x, y), value));
            }
        }

        return points
            .OrderByDescending(point => point.Value)
            .ThenBy(point => point.Coordinate.Y)
            .ThenBy(point => point.Coordinate.X)
            .Take(count)
            .Select(point => new GtpPolicyPoint(
                point.Coordinate,
                point.Value,
                positiveSum > 0 ? point.Value / (double)positiveSum : 0.0))
            .ToArray();
    }

    public int[,] GetTerritoryStatistics()
    {
        if (_boardSize > 19)
        {
            throw new InvalidOperationException("territory diagnostics are supported only up to board size 19.");
        }

        return _native.GetTerritoryStatistics();
    }

    public string EstimateFinalScore()
    {
        return GetFinalScoreEstimate().FormatConfiguredResult();
    }

    public GtpFinalScoreEstimate GetFinalScoreEstimate()
    {
        if (_boardSize > 19)
        {
            throw new InvalidOperationException("final_score estimate is supported only up to board size 19.");
        }

        return EstimateAreaScore(level: 3);
    }

    private GtpFinalScoreEstimate EstimateAreaScore(int level)
    {
        var territory = _native.GetTerritoryStatistics();
        var threshold = level * 100;
        var score = CalculateTerritoryStats(threshold, territory);

        return new GtpFinalScoreEstimate(
            threshold,
            _komi,
            score.BlackAlive,
            score.BlackCapture,
            score.BlackTerritory,
            score.WhiteAlive,
            score.WhiteCapture,
            score.WhiteTerritory,
            _native.GetNumBlackPrisoners(),
            _native.GetNumWhitePrisoners(),
            _options.FinalScoreRule);
    }

    private TerritoryScore CalculateTerritoryStats(int threshold, int[,] territory)
    {
        var blackAlive = 0;
        var blackCapture = 0;
        var blackTerritory = 0;
        var whiteAlive = 0;
        var whiteCapture = 0;
        var whiteTerritory = 0;

        for (var y = 0; y < _boardSize; y++)
        {
            for (var x = 0; x < _boardSize; x++)
            {
                var boardColor = _native.GetBoardColor(x, y);
                var blackOwnsPoint = IsSurroundedByThreshold(territory, x, y, threshold, black: true);
                var whiteOwnsPoint = IsSurroundedByThreshold(territory, x, y, threshold, black: false);

                if (boardColor == 0)
                {
                    if (blackOwnsPoint)
                    {
                        blackTerritory++;
                    }

                    if (whiteOwnsPoint)
                    {
                        whiteTerritory++;
                    }
                }
                else if (boardColor == (int)StoneColor.Black)
                {
                    if (territory[y, x] >= -threshold)
                    {
                        blackAlive++;
                    }
                    else
                    {
                        whiteCapture++;
                    }
                }
                else if (boardColor == (int)StoneColor.White)
                {
                    if (territory[y, x] > threshold)
                    {
                        blackCapture++;
                    }
                    else
                    {
                        whiteAlive++;
                    }
                }
            }
        }

        return new TerritoryScore(
            blackAlive,
            blackCapture,
            blackTerritory,
            whiteAlive,
            whiteCapture,
            whiteTerritory);
    }

    private bool IsSurroundedByThreshold(int[,] territory, int x, int y, int threshold, bool black)
    {
        return MeetsThreshold(territory, x, y - 1, threshold, black)
            && MeetsThreshold(territory, x, y + 1, threshold, black)
            && MeetsThreshold(territory, x - 1, y, threshold, black)
            && MeetsThreshold(territory, x + 1, y, threshold, black);
    }

    private bool MeetsThreshold(int[,] territory, int x, int y, int threshold, bool black)
    {
        if (x < 0 || x >= _boardSize || y < 0 || y >= _boardSize)
        {
            return true;
        }

        return black
            ? territory[y, x] > threshold
            : territory[y, x] < -threshold;
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

    private IReadOnlyList<GtpAnalysisMove> ThinkUntilAnalysisMoves(
        int zenColor,
        int candidateCount,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<GtpAnalysisMove> moves = [];
        _native.StartThinking(zenColor);

        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(Math.Max(0.1, _maxTime) + 0.5);
            while (DateTime.UtcNow < deadline)
            {
                if (cancellationToken.WaitHandle.WaitOne(100))
                {
                    break;
                }

                var currentMoves = ReadAnalysisMoves(candidateCount);
                if (currentMoves.Count > 0)
                {
                    moves = currentMoves;
                }

                if (currentMoves.Count > 0 && currentMoves[0].Playouts >= _options.MaxSimulations)
                {
                    break;
                }

                if (!_native.IsThinking)
                {
                    break;
                }
            }
        }
        finally
        {
            _native.StopThinking();
        }

        return moves;
    }

    private IReadOnlyList<GtpAnalysisMove> ReadAnalysisMoves(int candidateCount)
    {
        var moves = new List<GtpAnalysisMove>(candidateCount);
        var seen = new HashSet<BoardCoordinate>();
        for (var index = 0; index < candidateCount; index++)
        {
            var topMove = _native.GetTopMoveInfo(index);
            if (topMove.Playouts <= 0 || !IsOnBoard(topMove.X, topMove.Y))
            {
                continue;
            }

            var coordinate = new BoardCoordinate(topMove.X, topMove.Y);
            if (!seen.Add(coordinate))
            {
                continue;
            }

            var move = GtpMove.Play(coordinate);
            var vertex = GtpVertex.Format(coordinate, _boardSize);
            var pv = string.IsNullOrWhiteSpace(topMove.Text) ? vertex : topMove.Text.Trim();
            moves.Add(new GtpAnalysisMove(move, topMove.Playouts, topMove.Winrate, pv));
        }

        return moves;
    }

    private IReadOnlyList<GtpAnalysisMove> WithPolicyPriors(IReadOnlyList<GtpAnalysisMove> moves)
    {
        if (moves.Count == 0 || _boardSize > 19)
        {
            return moves;
        }

        var policy = _native.GetPolicyKnowledge();
        var rawValues = moves
            .Select(move => move.Move.Coordinate is { } coordinate
                ? Math.Max(0, policy[coordinate.Y, coordinate.X])
                : 0)
            .ToArray();
        var sum = rawValues.Sum();
        if (sum <= 0)
        {
            return moves;
        }

        return moves
            .Select((move, index) => move with { Prior = rawValues[index] / (double)sum })
            .ToArray();
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

    private static int ToNativeSeconds(double seconds)
    {
        if (seconds > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), "Time value is too large.");
        }

        return (int)Math.Ceiling(seconds);
    }

    public void Dispose()
    {
        _native.Dispose();
    }

    private readonly record struct TerritoryScore(
        int BlackAlive,
        int BlackCapture,
        int BlackTerritory,
        int WhiteAlive,
        int WhiteCapture,
        int WhiteTerritory);
}
