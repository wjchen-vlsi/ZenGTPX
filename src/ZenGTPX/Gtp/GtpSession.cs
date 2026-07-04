using System.Globalization;
using ZenGTPX.Board;

namespace ZenGTPX.Gtp;

public sealed class GtpSession
{
    private const int AnalysisCandidateCount = 10;
    private const int AnalysisIntervalMilliseconds = 1000;

    private static readonly string[] Commands =
    [
        "protocol_version",
        "name",
        "version",
        "list_commands",
        "known_command",
        "boardsize",
        "clear_board",
        "komi",
        "play",
        "genmove",
        "undo",
        "fixed_handicap",
        "place_free_handicap",
        "time_settings",
        "time_left",
        "showboard",
        "final_score",
        "stop",
        "lz-analyze",
        "kata-analyze",
        "kata-set-param",
        "kata-get-param",
        "kata-list-params",
        "kata-get-rules",
        "kata-time_settings",
        "zengtp_last_search_info",
        "quit",
    ];

    private static readonly HashSet<string> KnownCommands = new(Commands, StringComparer.Ordinal);
    private readonly Dictionary<string, string> _kataParameters = new(StringComparer.Ordinal)
    {
        ["analysisWideRootNoise"] = "0.04",
        ["maxTime"] = "2",
        ["maxVisits"] = "500",
        ["numSearchThreads"] = "1",
        ["playoutDoublingAdvantage"] = "0.0",
    };

    private readonly IGtpEngine _engine;
    private readonly BoardState _board;
    private readonly object _engineLock = new();
    private readonly Action<string>? _writeAnalysisOutput;
    private CancellationTokenSource? _analysisCancellation;
    private Thread? _analysisThread;

    public GtpSession(IGtpEngine engine, Action<string>? writeAnalysisOutput = null)
    {
        _engine = engine;
        _board = new BoardState(engine.BoardSize);
        _writeAnalysisOutput = writeAnalysisOutput;
    }

    public GtpExecutionResult Execute(GtpCommand command)
    {
        try
        {
            return command.Name switch
            {
                "protocol_version" => Success(command, "2"),
                "name" => Success(command, _engine.GtpName),
                "version" => Success(command, "0.1.0"),
                "list_commands" => Success(command, string.Join('\n', Commands)),
                "known_command" => KnownCommand(command),
                "boardsize" => BoardSize(command),
                "clear_board" => ClearBoard(command),
                "komi" => Komi(command),
                "play" => Play(command),
                "genmove" => GenMove(command),
                "undo" => Undo(command),
                "fixed_handicap" => FixedHandicap(command),
                "place_free_handicap" => PlaceFreeHandicap(command),
                "time_settings" => TimeSettings(command),
                "time_left" => TimeLeft(command),
                "showboard" => ShowBoard(command),
                "final_score" => FinalScore(command),
                "stop" => Stop(command),
                "lz-analyze" => LzAnalyze(command),
                "kata-analyze" => KataAnalyze(command),
                "kata-set-param" => KataSetParam(command),
                "kata-get-param" => KataGetParam(command),
                "kata-list-params" => KataListParams(command),
                "kata-get-rules" => KataGetRules(command),
                "kata-time_settings" => KataTimeSettings(command),
                "zengtp_last_search_info" => LastSearchInfo(command),
                "quit" => Quit(command),
                _ => Error(command, "unknown command"),
            };
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or InvalidOperationException)
        {
            return Error(command, ex.Message);
        }
    }

    private static GtpExecutionResult KnownCommand(GtpCommand command)
    {
        if (command.Arguments.Count != 1)
        {
            return Error(command, "known_command requires one argument");
        }

        var known = KnownCommands.Contains(command.Arguments[0].ToLowerInvariant());
        return Success(command, known ? "true" : "false");
    }

    private GtpExecutionResult BoardSize(GtpCommand command)
    {
        if (command.Arguments.Count != 1)
        {
            return Error(command, "boardsize requires one argument");
        }

        var boardSize = int.Parse(command.Arguments[0], CultureInfo.InvariantCulture);
        StopAnalysis();
        lock (_engineLock)
        {
            _engine.SetBoardSize(boardSize);
            _engine.ClearBoard();
        }

        _board.SetBoardSize(boardSize);
        return Success(command, "");
    }

    private GtpExecutionResult ClearBoard(GtpCommand command)
    {
        if (command.Arguments.Count != 0)
        {
            return Error(command, "clear_board does not accept arguments");
        }

        StopAnalysis();
        lock (_engineLock)
        {
            _engine.ClearBoard();
        }

        _board.Clear();
        return Success(command, "");
    }

    private GtpExecutionResult Komi(GtpCommand command)
    {
        if (command.Arguments.Count != 1)
        {
            return Error(command, "komi requires one argument");
        }

        var komi = double.Parse(command.Arguments[0], CultureInfo.InvariantCulture);
        lock (_engineLock)
        {
            _engine.SetKomi(komi);
        }

        return Success(command, "");
    }

    private GtpExecutionResult Play(GtpCommand command)
    {
        if (command.Arguments.Count != 2)
        {
            return Error(command, "play requires color and vertex");
        }

        var color = ParseColor(command.Arguments[0]);
        var move = GtpVertex.Parse(command.Arguments[1], _engine.BoardSize);
        if (move.IsResign)
        {
            return Error(command, "resign is not valid for play");
        }

        StopAnalysis();
        bool played;
        lock (_engineLock)
        {
            played = _engine.Play(color, move);
        }

        if (!played)
        {
            return Error(command, "illegal move");
        }

        _board.Play(color, move);
        return Success(command, "");
    }

    private GtpExecutionResult GenMove(GtpCommand command)
    {
        if (command.Arguments.Count != 1)
        {
            return Error(command, "genmove requires one color argument");
        }

        var color = ParseColor(command.Arguments[0]);
        StopAnalysis();
        GtpMove move;
        lock (_engineLock)
        {
            move = _engine.GenMove(color);
        }

        _board.Play(color, move);
        return Success(command, FormatMove(move));
    }

    private GtpExecutionResult Undo(GtpCommand command)
    {
        if (command.Arguments.Count > 1)
        {
            return Error(command, "undo accepts at most one argument");
        }

        var count = command.Arguments.Count == 0
            ? 1
            : int.Parse(command.Arguments[0], CultureInfo.InvariantCulture);

        StopAnalysis();
        bool undone;
        lock (_engineLock)
        {
            undone = _engine.Undo(count);
        }

        if (!undone)
        {
            return Error(command, "cannot undo");
        }

        _board.Undo(count);
        return Success(command, "");
    }

    private GtpExecutionResult FixedHandicap(GtpCommand command)
    {
        if (command.Arguments.Count != 1)
        {
            return Error(command, "fixed_handicap requires one argument");
        }

        var count = int.Parse(command.Arguments[0], CultureInfo.InvariantCulture);
        var coordinates = FixedHandicapCoordinates(_engine.BoardSize, count);

        foreach (var coordinate in coordinates)
        {
            var move = GtpMove.Play(coordinate);
            bool played;
            StopAnalysis();
            lock (_engineLock)
            {
                played = _engine.Play(StoneColor.Black, move);
            }

            if (!played)
            {
                return Error(
                    command,
                    $"illegal handicap stone at {GtpVertex.Format(coordinate, _engine.BoardSize)}");
            }

            _board.Play(StoneColor.Black, move);
        }

        return Success(
            command,
            string.Join(
                ' ',
                coordinates.Select(coordinate => GtpVertex.Format(coordinate, _engine.BoardSize))));
    }

    private GtpExecutionResult PlaceFreeHandicap(GtpCommand command)
    {
        if (command.Arguments.Count == 0)
        {
            return Error(command, "place_free_handicap requires at least one vertex");
        }

        var coordinates = new List<BoardCoordinate>(command.Arguments.Count);
        foreach (var argument in command.Arguments)
        {
            var move = GtpVertex.Parse(argument, _engine.BoardSize);
            if (move.IsPass || move.IsResign || move.Coordinate is not { } coordinate)
            {
                return Error(command, $"invalid handicap vertex: {argument}");
            }

            coordinates.Add(coordinate);
        }

        foreach (var coordinate in coordinates)
        {
            var move = GtpMove.Play(coordinate);
            bool played;
            StopAnalysis();
            lock (_engineLock)
            {
                played = _engine.Play(StoneColor.Black, move);
            }

            if (!played)
            {
                return Error(
                    command,
                    $"illegal handicap stone at {GtpVertex.Format(coordinate, _engine.BoardSize)}");
            }

            _board.Play(StoneColor.Black, move);
        }

        return Success(
            command,
            string.Join(
                ' ',
                coordinates.Select(coordinate => GtpVertex.Format(coordinate, _engine.BoardSize))));
    }

    private GtpExecutionResult TimeSettings(GtpCommand command)
    {
        if (command.Arguments.Count != 3)
        {
            return Error(command, "time_settings requires main time, byoyomi time, and periods");
        }

        var mainTime = double.Parse(command.Arguments[0], CultureInfo.InvariantCulture);
        var byoyomiTime = double.Parse(command.Arguments[1], CultureInfo.InvariantCulture);
        var periods = int.Parse(command.Arguments[2], CultureInfo.InvariantCulture);

        if (mainTime < 0 || byoyomiTime < 0 || periods < 0)
        {
            return Error(command, "time_settings values must not be negative");
        }

        lock (_engineLock)
        {
            _engine.SetTimeSettings(mainTime, byoyomiTime, periods);
        }

        return Success(command, "");
    }

    private GtpExecutionResult TimeLeft(GtpCommand command)
    {
        if (command.Arguments.Count != 3)
        {
            return Error(command, "time_left requires color, time, and stones");
        }

        var color = ParseColor(command.Arguments[0]);
        var time = double.Parse(command.Arguments[1], CultureInfo.InvariantCulture);
        var stones = int.Parse(command.Arguments[2], CultureInfo.InvariantCulture);

        if (time < 0)
        {
            return Error(command, "time_left time must not be negative");
        }

        lock (_engineLock)
        {
            _engine.SetTimeLeft(color, time, stones);
        }

        return Success(command, "");
    }

    private GtpExecutionResult ShowBoard(GtpCommand command)
    {
        if (command.Arguments.Count != 0)
        {
            return Error(command, "showboard does not accept arguments");
        }

        return Success(command, "\n" + _board.FormatShowBoard());
    }

    private GtpExecutionResult FinalScore(GtpCommand command)
    {
        if (command.Arguments.Count != 0)
        {
            return Error(command, "final_score does not accept arguments");
        }

        lock (_engineLock)
        {
            return Success(command, _engine.EstimateFinalScore());
        }
    }

    private GtpExecutionResult LastSearchInfo(GtpCommand command)
    {
        if (command.Arguments.Count != 0)
        {
            return Error(command, "zengtp_last_search_info does not accept arguments");
        }

        if (_engine.LastSearchInfo is not { } searchInfo)
        {
            return Error(command, "no search info available");
        }

        return Success(
            command,
            string.Create(
                CultureInfo.InvariantCulture,
                $"move {FormatMove(searchInfo.Move)} playouts {searchInfo.Playouts} winrate {searchInfo.Winrate:0.0000} time {searchInfo.TimeSeconds:0.000}"));
    }

    private GtpExecutionResult Stop(GtpCommand command)
    {
        if (command.Arguments.Count != 0)
        {
            return Error(command, "stop does not accept arguments");
        }

        StopAnalysis();
        return Success(command, "");
    }

    private GtpExecutionResult LzAnalyze(GtpCommand command)
    {
        if (command.Arguments.Count > 1)
        {
            return Error(command, "lz-analyze accepts at most one visits argument");
        }

        var color = _board.NextColor;
        if (_writeAnalysisOutput is null)
        {
            IReadOnlyList<GtpAnalysisMove> moves;
            lock (_engineLock)
            {
                moves = _engine.Analyze(color, AnalysisCandidateCount, CancellationToken.None);
            }

            return AnalysisSuccess(command, FormatLzAnalysis(moves));
        }

        StartAnalysisStream(color, FormatLzAnalysis);
        return Success(command, "");
    }

    private GtpExecutionResult KataAnalyze(GtpCommand command)
    {
        if (command.Arguments.Count is < 1 or > 2)
        {
            return Error(command, "kata-analyze requires color and optional visits argument");
        }

        var color = ParseColor(command.Arguments[0]);
        if (_writeAnalysisOutput is null)
        {
            IReadOnlyList<GtpAnalysisMove> moves;
            lock (_engineLock)
            {
                moves = _engine.Analyze(color, AnalysisCandidateCount, CancellationToken.None);
            }

            return AnalysisSuccess(command, FormatKataAnalysis(moves));
        }

        StartAnalysisStream(color, FormatKataAnalysis);
        return Success(command, "");
    }

    private GtpExecutionResult KataSetParam(GtpCommand command)
    {
        if (command.Arguments.Count < 2)
        {
            return Error(command, "kata-set-param requires name and value");
        }

        var name = command.Arguments[0];
        var value = command.Arguments[1];
        _kataParameters[name] = value;
        if (name.Equals("maxTime", StringComparison.Ordinal) && double.TryParse(value, CultureInfo.InvariantCulture, out var maxTime))
        {
            lock (_engineLock)
            {
                _engine.SetMaxTime(maxTime);
            }
        }

        return Success(command, "");
    }

    private GtpExecutionResult KataGetParam(GtpCommand command)
    {
        if (command.Arguments.Count != 1)
        {
            return Error(command, "kata-get-param requires one parameter name");
        }

        return Success(command, _kataParameters.GetValueOrDefault(command.Arguments[0], ""));
    }

    private GtpExecutionResult KataListParams(GtpCommand command)
    {
        if (command.Arguments.Count != 0)
        {
            return Error(command, "kata-list-params does not accept arguments");
        }

        return Success(command, string.Join('\n', _kataParameters.Keys.Order(StringComparer.Ordinal)));
    }

    private static GtpExecutionResult KataGetRules(GtpCommand command)
    {
        if (command.Arguments.Count != 0)
        {
            return Error(command, "kata-get-rules does not accept arguments");
        }

        return Success(
            command,
            "{\"ko\":\"SIMPLE\",\"scoring\":\"AREA\",\"tax\":\"NONE\",\"multiStoneSuicideLegal\":false,\"hasButton\":false,\"whiteHandicapBonus\":\"N\",\"friendlyPassOk\":false}");
    }

    private static GtpExecutionResult KataTimeSettings(GtpCommand command)
    {
        if (command.Arguments.Count == 0)
        {
            return Error(command, "kata-time_settings requires arguments");
        }

        return Success(command, "");
    }

    private GtpExecutionResult Quit(GtpCommand command)
    {
        StopAnalysis();
        return new GtpExecutionResult(GtpResponse.Success(command.Id), ShouldQuit: true);
    }

    private void StartAnalysisStream(StoneColor color, Func<IReadOnlyList<GtpAnalysisMove>, string> format)
    {
        StopAnalysis();
        if (_writeAnalysisOutput is null)
        {
            return;
        }

        var cancellation = new CancellationTokenSource();
        _analysisCancellation = cancellation;
        _analysisThread = new Thread(() => RunAnalysisStream(color, format, cancellation.Token))
        {
            IsBackground = true,
            Name = "ZenGTPX analysis stream",
        };
        _analysisThread.Start();
    }

    private void RunAnalysisStream(
        StoneColor color,
        Func<IReadOnlyList<GtpAnalysisMove>, string> format,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            IReadOnlyList<GtpAnalysisMove> moves;
            lock (_engineLock)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                moves = _engine.Analyze(color, AnalysisCandidateCount, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var output = format(moves);
            if (output.Length > 0)
            {
                _writeAnalysisOutput?.Invoke(output + "\n");
            }

            if (cancellationToken.WaitHandle.WaitOne(AnalysisIntervalMilliseconds))
            {
                return;
            }
        }
    }

    private void StopAnalysis()
    {
        var cancellation = _analysisCancellation;
        var thread = _analysisThread;
        _analysisCancellation = null;
        _analysisThread = null;

        if (cancellation is null)
        {
            return;
        }

        cancellation.Cancel();
        var joined = thread is null || !thread.IsAlive || thread.Join(TimeSpan.FromSeconds(5));
        if (joined)
        {
            cancellation.Dispose();
        }
    }

    private static StoneColor ParseColor(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "b" or "black" => StoneColor.Black,
            "w" or "white" => StoneColor.White,
            _ => throw new FormatException($"Invalid color: {value}"),
        };
    }

    private static IReadOnlyList<BoardCoordinate> FixedHandicapCoordinates(int boardSize, int count)
    {
        if (count is < 2 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Handicap count must be between 2 and 9.");
        }

        if (boardSize < 7 || boardSize % 2 == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(boardSize),
                "Handicap is supported only on odd board sizes 7 or larger.");
        }

        var starOffset = boardSize < 13 ? 2 : 3;
        var low = starOffset;
        var high = boardSize - 1 - starOffset;
        var middle = boardSize / 2;

        BoardCoordinate lowerLeft = new(low, high);
        BoardCoordinate upperRight = new(high, low);
        BoardCoordinate lowerRight = new(high, high);
        BoardCoordinate upperLeft = new(low, low);
        BoardCoordinate center = new(middle, middle);
        BoardCoordinate left = new(low, middle);
        BoardCoordinate right = new(high, middle);
        BoardCoordinate lower = new(middle, high);
        BoardCoordinate upper = new(middle, low);

        return count switch
        {
            2 => [lowerLeft, upperRight],
            3 => [lowerLeft, upperRight, lowerRight],
            4 => [lowerLeft, upperRight, lowerRight, upperLeft],
            5 => [lowerLeft, upperRight, lowerRight, upperLeft, center],
            6 => [lowerLeft, upperRight, lowerRight, upperLeft, left, right],
            7 => [lowerLeft, upperRight, lowerRight, upperLeft, left, right, center],
            8 => [lowerLeft, upperRight, lowerRight, upperLeft, left, right, lower, upper],
            9 => [lowerLeft, upperRight, lowerRight, upperLeft, left, right, lower, upper, center],
            _ => throw new ArgumentOutOfRangeException(nameof(count), "Handicap count must be between 2 and 9."),
        };
    }

    private string FormatMove(GtpMove move)
    {
        if (move.IsResign)
        {
            return "resign";
        }

        if (move.IsPass || move.Coordinate is not { } coordinate)
        {
            return "pass";
        }

        return GtpVertex.Format(coordinate, _engine.BoardSize);
    }

    private string FormatKataAnalysis(IReadOnlyList<GtpAnalysisMove> moves)
    {
        return string.Join(
            " ",
            moves.Select(
                (move, index) => string.Create(
                    CultureInfo.InvariantCulture,
                    $"info move {FormatMove(move.Move)} visits {move.Playouts} winrate {move.Winrate:0.0000} scoreLead {KataScoreLead(move.Winrate):0.0} scoreMean {KataScoreLead(move.Winrate):0.0} prior {KataPrior(index):0.000} order {index} pv {FormatPrincipalVariation(move)}")));
    }

    private string FormatLzAnalysis(IReadOnlyList<GtpAnalysisMove> moves)
    {
        return string.Join(
            '\n',
            moves.Select(
                move => string.Create(
                    CultureInfo.InvariantCulture,
                    $"info move {FormatMove(move.Move)} visits {move.Playouts} winrate {ToLeelaWinrate(move.Winrate)} pv {FormatPrincipalVariation(move)}")));
    }

    private string FormatPrincipalVariation(GtpAnalysisMove move)
    {
        return string.IsNullOrWhiteSpace(move.PrincipalVariation)
            ? FormatMove(move.Move)
            : move.PrincipalVariation.Trim();
    }

    private static int ToLeelaWinrate(double winrate)
    {
        return Math.Clamp((int)Math.Round(winrate * 10000, MidpointRounding.AwayFromZero), 0, 10000);
    }

    private static double KataScoreLead(double winrate)
    {
        return (winrate - 0.5) * 30.0;
    }

    private static double KataPrior(int order)
    {
        return order switch
        {
            0 => 0.100,
            1 => 0.080,
            2 => 0.050,
            _ => Math.Max(0.010, 0.050 - (0.005 * (order - 2))),
        };
    }

    private static GtpExecutionResult Success(GtpCommand command, string body)
    {
        return new GtpExecutionResult(GtpResponse.Success(command.Id, body), ShouldQuit: false);
    }

    private static GtpExecutionResult AnalysisSuccess(GtpCommand command, string analysisOutput)
    {
        var output = analysisOutput.Length == 0 ? "" : analysisOutput + "\n";
        return new GtpExecutionResult(GtpResponse.Success(command.Id), ShouldQuit: false, OutputBeforeResponse: output);
    }

    private static GtpExecutionResult Error(GtpCommand command, string body)
    {
        return new GtpExecutionResult(GtpResponse.Error(command.Id, body), ShouldQuit: false);
    }
}
