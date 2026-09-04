using System.Globalization;
using System.Text;
using System.Text.Json;
using ZenGTPX.Board;
using ZenGTPX.Config;

namespace ZenGTPX.Gtp;

public sealed class GtpSession
{
    private const int AnalysisCandidateCount = 10;
    private const int DefaultAnalysisIntervalCentiseconds = 100;

    private static readonly string[] BaseCommands =
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
        "set_free_handicap",
        "place_free_handicap",
        "time_settings",
        "time_left",
        "showboard",
        "loadsgf",
        "final_score",
        "final_status_list",
        "zengtp_final_score_detail",
        "stop",
        "lz-analyze",
        "lz-genmove_analyze",
        "kata-analyze",
        "kata-genmove_analyze",
        "analyze",
        "genmove_analyze",
        "kata-set-param",
        "kata-get-param",
        "kata-list-params",
        "kata-get-rules",
        "kata-set-rules",
        "kata-time_settings",
        "clear_cache",
        "zengtp_last_search_info",
        "zengtp_policy",
        "territory",
        "zengtp_territory",
        "quit",
    ];

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
    private readonly ZenConfigurationService? _configuration;
    private readonly string[] _commands;
    private readonly HashSet<string> _knownCommands;
    private CancellationTokenSource? _analysisCancellation;
    private Thread? _analysisThread;
    private bool _ignoreKataMaxTime;

    public GtpSession(
        IGtpEngine engine,
        Action<string>? writeAnalysisOutput = null,
        ZenConfigurationService? configuration = null)
    {
        _engine = engine;
        _board = new BoardState(engine.BoardSize);
        _writeAnalysisOutput = writeAnalysisOutput;
        _configuration = configuration;
        _commands = configuration is null
            ? BaseCommands
            : [.. BaseCommands[..^1], .. ZenConfigurationService.Commands, BaseCommands[^1]];
        _knownCommands = new HashSet<string>(_commands, StringComparer.Ordinal);
    }

    public GtpExecutionResult Execute(GtpCommand command)
    {
        try
        {
            StopAnalysis();
            return command.Name switch
            {
                "protocol_version" => Success(command, "2"),
                "name" => Success(command, _engine.GtpName),
                "version" => Success(command, "0.96"),
                "list_commands" => Success(command, string.Join('\n', _commands)),
                "known_command" => KnownCommand(command),
                "boardsize" => BoardSize(command),
                "clear_board" => ClearBoard(command),
                "komi" => Komi(command),
                "play" => Play(command),
                "genmove" => GenMove(command),
                "undo" => Undo(command),
                "fixed_handicap" => FixedHandicap(command),
                "set_free_handicap" => FreeHandicap(command, "set_free_handicap"),
                "place_free_handicap" => PlaceFreeHandicap(command),
                "time_settings" => TimeSettings(command),
                "time_left" => TimeLeft(command),
                "showboard" => ShowBoard(command),
                "loadsgf" => LoadSgf(command),
                "final_score" => FinalScore(command),
                "final_status_list" => FinalStatusList(command),
                "zengtp_final_score_detail" => FinalScoreDetail(command),
                "stop" => Stop(command),
                "lz-analyze" => LzAnalyze(command),
                "lz-genmove_analyze" => LzGenMoveAnalyze(command),
                "kata-analyze" => KataAnalyze(command),
                "kata-genmove_analyze" => KataGenMoveAnalyze(command),
                "analyze" => KataAnalyze(command),
                "genmove_analyze" => GenMoveAnalyze(command),
                "kata-set-param" => KataSetParam(command),
                "kata-get-param" => KataGetParam(command),
                "kata-list-params" => KataListParams(command),
                "kata-get-rules" => KataGetRules(command),
                "kata-set-rules" => KataSetRules(command),
                "kata-time_settings" => KataTimeSettings(command),
                "clear_cache" => ClearCache(command),
                "zengtp_last_search_info" => LastSearchInfo(command),
                "zengtp_policy" => Policy(command),
                "territory" => LegacyTerritory(command),
                "zengtp_territory" => Territory(command),
                "zengtp_config_version" when _configuration is not null => ConfigurationVersion(command),
                "zengtp_config_schema" when _configuration is not null => ConfigurationSchema(command),
                "zengtp_config_get" when _configuration is not null => ConfigurationGet(command),
                "zengtp_config_set" when _configuration is not null => ConfigurationSet(command),
                "zengtp_config_save" when _configuration is not null => ConfigurationSave(command),
                "zengtp_config_reset" when _configuration is not null => ConfigurationReset(command),
                "quit" => Quit(command),
                _ => Error(command, "unknown command"),
            };
        }
        catch (ZenConfigurationException ex)
        {
            return Error(command, ZenConfigurationService.FormatError(ex));
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or InvalidOperationException)
        {
            return Error(command, ex.Message);
        }
    }

    public void InterruptAnalysis()
    {
        StopAnalysis();
    }

    private GtpExecutionResult KnownCommand(GtpCommand command)
    {
        if (command.Arguments.Count != 1)
        {
            return Error(command, "known_command requires one argument");
        }

        var known = _knownCommands.Contains(command.Arguments[0].ToLowerInvariant());
        return Success(command, known ? "true" : "false");
    }

    private GtpExecutionResult BoardSize(GtpCommand command)
    {
        if (command.Arguments.Count != 1)
        {
            return Error(command, "boardsize requires one argument");
        }

        if (!TryParseInteger(command.Arguments[0], out var boardSize))
        {
            return Error(command, "boardsize must be an integer");
        }

        if (boardSize <= 0 || boardSize > 25)
        {
            return Error(command, "boardsize must be between 1 and 25");
        }

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

    private GtpExecutionResult LoadSgf(GtpCommand command)
    {
        if (command.Arguments.Count is < 1 or > 2)
        {
            return Error(command, "loadsgf requires filename and optional move number");
        }

        if (command.Arguments.Count == 2 && !TryParseInteger(command.Arguments[1], out _))
        {
            return Error(command, "loadsgf move number must be an integer");
        }

        var path = command.Arguments[0];
        if (!File.Exists(path))
        {
            return Error(command, "loadsgf file not found");
        }

        SgfGame game;
        try
        {
            game = ParseSgf(File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or IOException)
        {
            return Error(command, $"loadsgf failed: {ex.Message}");
        }

        var moves = game.Moves;
        if (command.Arguments.Count == 2 && int.TryParse(command.Arguments[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var moveNumber))
        {
            if (moveNumber < 0)
            {
                return Error(command, "loadsgf move number must be non-negative");
            }

            moves = moves.Take(moveNumber).ToArray();
        }

        StopAnalysis();
        lock (_engineLock)
        {
            _engine.SetBoardSize(game.BoardSize);
            _engine.ClearBoard();
            if (game.Komi is { } komi)
            {
                _engine.SetKomi(komi);
            }

            foreach (var stone in game.SetupStones)
            {
                if (!_engine.Play(stone.Color, stone.Move))
                {
                    return Error(command, "loadsgf failed: illegal setup stone");
                }
            }

            foreach (var move in moves)
            {
                if (!_engine.Play(move.Color, move.Move))
                {
                    return Error(command, "loadsgf failed: illegal move");
                }
            }
        }

        _board.SetBoardSize(game.BoardSize);
        foreach (var stone in game.SetupStones)
        {
            _board.Play(stone.Color, stone.Move);
        }

        foreach (var move in moves)
        {
            _board.Play(move.Color, move.Move);
        }

        return Success(command, "");
    }

    private GtpExecutionResult Komi(GtpCommand command)
    {
        if (command.Arguments.Count != 1)
        {
            return Error(command, "komi requires one argument");
        }

        if (!TryParseDouble(command.Arguments[0], out var komi))
        {
            return Error(command, "komi must be numeric");
        }

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

        if (move.Coordinate is { } coordinate && _board.IsOccupied(coordinate))
        {
            return Error(command, $"point {GtpVertex.Format(coordinate, _engine.BoardSize)} is already occupied");
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
        var move = GenerateMove(color);
        return Success(command, FormatMove(move));
    }

    private GtpExecutionResult Undo(GtpCommand command)
    {
        if (command.Arguments.Count > 1)
        {
            return Error(command, "undo accepts at most one argument");
        }

        var count = 1;
        if (command.Arguments.Count == 1 &&
            !TryParseInteger(command.Arguments[0], out count))
        {
            return Error(command, "undo count must be an integer");
        }

        if (count <= 0)
        {
            return Error(command, "undo count must be positive");
        }

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

        if (!TryParseInteger(command.Arguments[0], out var count))
        {
            return Error(command, "fixed_handicap count must be an integer");
        }

        var coordinates = FixedHandicapCoordinates(_engine.BoardSize, count);
        foreach (var coordinate in coordinates)
        {
            if (_board.IsOccupied(coordinate))
            {
                return Error(command, $"point {GtpVertex.Format(coordinate, _engine.BoardSize)} is already occupied");
            }
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

    private GtpExecutionResult PlaceFreeHandicap(GtpCommand command)
    {
        return FreeHandicap(command, "place_free_handicap");
    }

    private GtpExecutionResult FreeHandicap(GtpCommand command, string commandName)
    {
        if (command.Arguments.Count == 0)
        {
            return Error(command, $"{commandName} requires at least one vertex");
        }

        var coordinates = new List<BoardCoordinate>(command.Arguments.Count);
        foreach (var argument in command.Arguments)
        {
            var move = GtpVertex.Parse(argument, _engine.BoardSize);
            if (move.IsPass || move.IsResign || move.Coordinate is not { } coordinate)
            {
                return Error(command, $"invalid handicap vertex: {argument}");
            }

            if (_board.IsOccupied(coordinate) || coordinates.Contains(coordinate))
            {
                return Error(command, $"point {GtpVertex.Format(coordinate, _engine.BoardSize)} is already occupied");
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

        if (!TryParseDouble(command.Arguments[0], out var mainTime) ||
            !TryParseDouble(command.Arguments[1], out var byoyomiTime))
        {
            return Error(command, "time_settings times must be numeric");
        }

        if (!TryParseInteger(command.Arguments[2], out var periods))
        {
            return Error(command, "time_settings periods must be an integer");
        }

        if (mainTime < 0 || byoyomiTime < 0 || periods < 0)
        {
            return Error(command, "time_settings values must not be negative");
        }

        lock (_engineLock)
        {
            _engine.SetTimeSettings(mainTime, byoyomiTime, periods);
        }

        _ignoreKataMaxTime = false;
        return Success(command, "");
    }

    private GtpExecutionResult TimeLeft(GtpCommand command)
    {
        if (command.Arguments.Count != 3)
        {
            return Error(command, "time_left requires color, time, and stones");
        }

        var color = ParseColor(command.Arguments[0]);
        if (!TryParseDouble(command.Arguments[1], out var time))
        {
            return Error(command, "time_left time must be numeric");
        }

        if (!TryParseInteger(command.Arguments[2], out var stones))
        {
            return Error(command, "time_left stones must be an integer");
        }

        if (time < 0 || stones < 0)
        {
            return Error(command, "time_left values must not be negative");
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

    private GtpExecutionResult FinalStatusList(GtpCommand command)
    {
        if (command.Arguments.Count != 1)
        {
            return Error(command, "final_status_list requires one status argument");
        }

        return command.Arguments[0].ToLowerInvariant() switch
        {
            "alive" or "dead" or "seki" => Success(command, ""),
            _ => Error(command, "final_status_list status must be alive, dead, or seki"),
        };
    }

    private GtpExecutionResult FinalScoreDetail(GtpCommand command)
    {
        if (command.Arguments.Count != 0)
        {
            return Error(command, "zengtp_final_score_detail does not accept arguments");
        }

        StopAnalysis();
        lock (_engineLock)
        {
            return Success(command, FormatFinalScoreDetail(_engine.GetFinalScoreEstimate()));
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

    private GtpExecutionResult Policy(GtpCommand command)
    {
        if (command.Arguments.Count > 1)
        {
            return Error(command, "zengtp_policy accepts at most one count argument");
        }

        var count = 20;
        if (command.Arguments.Count == 1 &&
            !int.TryParse(command.Arguments[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
        {
            return Error(command, "zengtp_policy count must be an integer");
        }

        if (count <= 0)
        {
            return Error(command, "zengtp_policy count must be positive");
        }

        StopAnalysis();
        IReadOnlyList<GtpPolicyPoint> points;
        lock (_engineLock)
        {
            points = _engine.GetPolicy(count);
        }

        return Success(command, FormatPolicy(points));
    }

    private GtpExecutionResult Territory(GtpCommand command)
    {
        if (command.Arguments.Count != 0)
        {
            return Error(command, "zengtp_territory does not accept arguments");
        }

        StopAnalysis();
        int[,] territory;
        lock (_engineLock)
        {
            territory = _engine.GetTerritoryStatistics();
        }

        return Success(command, FormatTerritory(territory));
    }

    private GtpExecutionResult LegacyTerritory(GtpCommand command)
    {
        if (command.Arguments.Count != 0)
        {
            return Error(command, "territory does not accept arguments");
        }

        StopAnalysis();
        int[,] territory;
        lock (_engineLock)
        {
            territory = _engine.GetTerritoryStatistics();
        }

        return Success(command, "\n" + FormatLegacyTerritory(territory));
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
            return Error(command, "lz-analyze accepts at most one interval argument");
        }

        var interval = DefaultAnalysisIntervalCentiseconds;
        if (command.Arguments.Count == 1)
        {
            if (!TryParseInteger(command.Arguments[0], out interval))
            {
                return Error(command, "lz-analyze interval must be an integer");
            }

            if (interval <= 0)
            {
                return Error(command, "lz-analyze interval must be positive");
            }
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

        StartAnalysisStream(color, FormatLzAnalysis, interval);
        return Success(command, "", IsEndResponse: false);
    }

    private GtpExecutionResult KataAnalyze(GtpCommand command)
    {
        if (command.Arguments.Count > 2)
        {
            return Error(command, "kata-analyze accepts optional color and optional interval argument");
        }

        var color = _board.NextColor;
        var intervalIndex = 0;
        if (command.Arguments.Count > 0 && TryParseColor(command.Arguments[0], out var explicitColor))
        {
            color = explicitColor;
            intervalIndex = 1;
        }

        var interval = DefaultAnalysisIntervalCentiseconds;
        if (command.Arguments.Count > intervalIndex)
        {
            if (!TryParseInteger(command.Arguments[intervalIndex], out interval))
            {
                return Error(command, "kata-analyze interval must be an integer");
            }

            if (interval <= 0)
            {
                return Error(command, "kata-analyze interval must be positive");
            }
        }

        if (_writeAnalysisOutput is null)
        {
            IReadOnlyList<GtpAnalysisMove> moves;
            lock (_engineLock)
            {
                moves = _engine.Analyze(color, AnalysisCandidateCount, CancellationToken.None);
            }

            return AnalysisSuccess(command, FormatKataAnalysis(moves));
        }

        StartAnalysisStream(color, FormatKataAnalysis, interval);
        return Success(command, "", IsEndResponse: false);
    }

    private GtpExecutionResult LzGenMoveAnalyze(GtpCommand command)
    {
        var parsed = ParseGenMoveAnalyzeArguments(command, "lz-genmove_analyze");
        if (parsed.Error is { } error)
        {
            return error;
        }

        if (_writeAnalysisOutput is not null)
        {
            _writeAnalysisOutput(GtpResponse.Success(command.Id, IsEndResponse: false).Format());
        }

        var move = GenerateMoveAnalyze(parsed.Color, parsed.Interval, FormatLzAnalysis);
        if (_writeAnalysisOutput is not null)
        {
            _writeAnalysisOutput($"play {FormatMove(move)}\n\n");
            return new GtpExecutionResult(GtpResponse.Success(command.Id), ShouldQuit: false, SuppressResponse: true);
        }

        return GenMoveAnalyzeSuccess(command, _writeAnalysisOutput is null ? FormatLzAnalysis(SearchInfoAsAnalysisMove(move)) : "", move);
    }

    private GtpExecutionResult KataGenMoveAnalyze(GtpCommand command)
    {
        var parsed = ParseGenMoveAnalyzeArguments(command, "kata-genmove_analyze");
        if (parsed.Error is { } error)
        {
            return error;
        }

        if (_writeAnalysisOutput is not null)
        {
            _writeAnalysisOutput(GtpResponse.Success(command.Id, IsEndResponse: false).Format());
        }

        var move = GenerateMoveAnalyze(parsed.Color, parsed.Interval, FormatKataAnalysis);
        if (_writeAnalysisOutput is not null)
        {
            _writeAnalysisOutput($"play {FormatMove(move)}\n\n");
            return new GtpExecutionResult(GtpResponse.Success(command.Id), ShouldQuit: false, SuppressResponse: true);
        }

        return GenMoveAnalyzeSuccess(command, _writeAnalysisOutput is null ? FormatKataAnalysis(SearchInfoAsAnalysisMove(move)) : "", move);
    }

    private GtpExecutionResult GenMoveAnalyze(GtpCommand command)
    {
        var parsed = ParseGenMoveAnalyzeArguments(command, "genmove_analyze");
        if (parsed.Error is { } error)
        {
            return error;
        }

        if (_writeAnalysisOutput is not null)
        {
            _writeAnalysisOutput(GtpResponse.Success(command.Id, IsEndResponse: false).Format());
        }

        var move = GenerateMoveAnalyze(parsed.Color, parsed.Interval, FormatKataAnalysis);
        if (_writeAnalysisOutput is not null)
        {
            _writeAnalysisOutput($"play {FormatMove(move)}\n\n");
            return new GtpExecutionResult(GtpResponse.Success(command.Id), ShouldQuit: false, SuppressResponse: true);
        }

        return GenMoveAnalyzeSuccess(command, _writeAnalysisOutput is null ? FormatKataAnalysis(SearchInfoAsAnalysisMove(move)) : "", move);
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
        if (name.Equals("maxTime", StringComparison.Ordinal) &&
            double.TryParse(value, CultureInfo.InvariantCulture, out var maxTime))
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

    private GtpExecutionResult ConfigurationVersion(GtpCommand command)
    {
        EnsureConfigurationArgumentCount(command, 0, "zengtp_config_version does not accept arguments");
        return Success(command, _configuration!.GetVersionJson());
    }

    private GtpExecutionResult ConfigurationSchema(GtpCommand command)
    {
        EnsureConfigurationArgumentCount(command, 0, "zengtp_config_schema does not accept arguments");
        lock (_engineLock)
        {
            return Success(command, _configuration!.GetSchemaJson());
        }
    }

    private GtpExecutionResult ConfigurationGet(GtpCommand command)
    {
        EnsureConfigurationArgumentCount(command, 0, "zengtp_config_get does not accept arguments");
        lock (_engineLock)
        {
            return Success(command, _configuration!.GetStateJson());
        }
    }

    private GtpExecutionResult ConfigurationSet(GtpCommand command)
    {
        if (command.Arguments.Count == 0)
        {
            throw ZenConfigurationService.InvalidArguments("zengtp_config_set requires one JSON object");
        }

        var json = string.Join(' ', command.Arguments);
        lock (_engineLock)
        {
            return Success(command, _configuration!.SetJson(json));
        }
    }

    private GtpExecutionResult ConfigurationSave(GtpCommand command)
    {
        EnsureConfigurationArgumentCount(command, 0, "zengtp_config_save does not accept arguments");
        lock (_engineLock)
        {
            return Success(command, _configuration!.SaveJson());
        }
    }

    private GtpExecutionResult ConfigurationReset(GtpCommand command)
    {
        if (command.Arguments.Count > 1)
        {
            throw ZenConfigurationService.InvalidArguments("zengtp_config_reset accepts at most one target");
        }

        var target = command.Arguments.Count == 0 ? "saved" : command.Arguments[0];
        lock (_engineLock)
        {
            return Success(command, _configuration!.ResetJson(target));
        }
    }

    private static void EnsureConfigurationArgumentCount(GtpCommand command, int expected, string message)
    {
        if (command.Arguments.Count != expected)
        {
            throw ZenConfigurationService.InvalidArguments(message);
        }
    }

    private GtpExecutionResult KataGetRules(GtpCommand command)
    {
        if (command.Arguments.Count != 0)
        {
            return Error(command, "kata-get-rules does not accept arguments");
        }

        return Success(command, FormatKataRules(_engine.FinalScoreRule));
    }

    private GtpExecutionResult KataSetRules(GtpCommand command)
    {
        if (command.Arguments.Count == 0)
        {
            return Error(command, "kata-set-rules requires rules JSON");
        }

        var json = string.Join(' ', command.Arguments);
        if (!TryMapKataRules(json, out var rule, out var error))
        {
            return Error(command, error);
        }

        lock (_engineLock)
        {
            _engine.SetFinalScoreRule(rule);
        }

        return Success(command, "");
    }

    private GtpExecutionResult KataTimeSettings(GtpCommand command)
    {
        if (command.Arguments.Count == 0)
        {
            return Error(command, "kata-time_settings requires arguments");
        }

        _ignoreKataMaxTime = command.Arguments[0].Equals("none", StringComparison.OrdinalIgnoreCase);
        return Success(command, "");
    }

    private static GtpExecutionResult ClearCache(GtpCommand command)
    {
        if (command.Arguments.Count != 0)
        {
            return Error(command, "clear_cache does not accept arguments");
        }

        return Success(command, "");
    }

    private GtpExecutionResult Quit(GtpCommand command)
    {
        StopAnalysis();
        return new GtpExecutionResult(GtpResponse.Success(command.Id), ShouldQuit: true);
    }

    private void StartAnalysisStream(
        StoneColor color,
        Func<IReadOnlyList<GtpAnalysisMove>, string> format,
        int intervalCentiseconds)
    {
        StopAnalysis();
        if (_writeAnalysisOutput is null)
        {
            return;
        }

        var cancellation = new CancellationTokenSource();
        _analysisCancellation = cancellation;
        _analysisThread = new Thread(() => RunAnalysisStream(
            color,
            format,
            TimeSpan.FromMilliseconds(intervalCentiseconds * 10),
            cancellation.Token))
        {
            IsBackground = true,
            Name = "ZenGTPX analysis stream",
        };
        _analysisThread.Start();
    }

    private void RunAnalysisStream(
        StoneColor color,
        Func<IReadOnlyList<GtpAnalysisMove>, string> format,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        lock (_engineLock)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _engine.RunAnalysis(
                color,
                AnalysisCandidateCount,
                interval,
                moves =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var output = format(moves);
                    if (output.Length > 0)
                    {
                        _writeAnalysisOutput?.Invoke(output + "\n");
                    }
                },
                cancellationToken);
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

    private GtpMove GenerateMove(StoneColor color)
    {
        GtpMove move;
        lock (_engineLock)
        {
            move = _engine.GenMove(color);
        }

        _board.Play(color, move);
        return move;
    }

    private GtpMove GenerateMoveAnalyze(
        StoneColor color,
        int intervalCentiseconds,
        Func<IReadOnlyList<GtpAnalysisMove>, string> format)
    {
        if (_writeAnalysisOutput is null)
        {
            return GenerateMove(color);
        }

        GtpMove move;
        lock (_engineLock)
        {
            move = _engine.GenMoveAnalyze(
                color,
                AnalysisCandidateCount,
                TimeSpan.FromMilliseconds(intervalCentiseconds * 10),
                moves =>
                {
                    var output = format(moves);
                    if (output.Length > 0)
                    {
                        _writeAnalysisOutput(output + "\n");
                    }
                },
                CancellationToken.None);
        }

        _board.Play(color, move);
        return move;
    }

    private (StoneColor Color, int Interval, GtpExecutionResult? Error) ParseGenMoveAnalyzeArguments(
        GtpCommand command,
        string commandName)
    {
        var color = _board.NextColor;
        var index = 0;
        if (command.Arguments.Count > 0 && TryParseColor(command.Arguments[0], out var explicitColor))
        {
            color = explicitColor;
            index = 1;
        }

        var interval = DefaultAnalysisIntervalCentiseconds;
        if (command.Arguments.Count > index)
        {
            if (!TryParseInteger(command.Arguments[index], out interval))
            {
                return (color, interval, Error(command, $"{commandName} interval must be an integer"));
            }

            if (interval <= 0)
            {
                return (color, interval, Error(command, $"{commandName} interval must be positive"));
            }
        }

        return (color, interval, null);
    }

    private IReadOnlyList<GtpAnalysisMove> SearchInfoAsAnalysisMove(GtpMove move)
    {
        var searchInfo = _engine.LastSearchInfo;
        return
        [
            new GtpAnalysisMove(
                move,
                searchInfo?.Playouts ?? 0,
                searchInfo?.Winrate ?? 0.5,
                FormatMove(move))
        ];
    }

    private static bool TryParseInteger(string value, out int result)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryParseDouble(string value, out double result)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    private static StoneColor ParseColor(string value)
    {
        if (TryParseColor(value, out var color))
        {
            return color;
        }

        throw new FormatException($"Invalid color: {value}");
    }

    private static bool TryParseColor(string value, out StoneColor color)
    {
        switch (value.ToLowerInvariant())
        {
            case "b":
            case "black":
                color = StoneColor.Black;
                return true;
            case "w":
            case "white":
                color = StoneColor.White;
                return true;
            default:
                color = default;
                return false;
        }
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
                    $"info move {FormatMove(move.Move)} visits {move.Playouts} winrate {move.Winrate:0.0000} scoreLead {KataScoreLead(move.Winrate):0.0} scoreMean {KataScoreLead(move.Winrate):0.0} prior {KataPrior(move, index):0.000} order {index} pv {FormatPrincipalVariation(move)}")));
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

    private string FormatPolicy(IReadOnlyList<GtpPolicyPoint> points)
    {
        var max = points.Count == 0 ? 0 : points.Max(point => point.Value);
        var selectedSum = points.Sum(point => point.Value);
        var lines = new List<string>
        {
            string.Create(CultureInfo.InvariantCulture, $"boardSize {_engine.BoardSize} count {points.Count} max {max} selectedSum {selectedSum}"),
        };
        lines.AddRange(
            points.Select(point => string.Create(
                CultureInfo.InvariantCulture,
                $"{GtpVertex.Format(point.Coordinate, _engine.BoardSize)} {point.Value} {point.Normalized:0.0000}")));
        return string.Join('\n', lines);
    }

    private string FormatTerritory(int[,] territory)
    {
        var lines = new List<string>
        {
            string.Create(CultureInfo.InvariantCulture, $"boardSize {_engine.BoardSize}"),
        };

        for (var y = 0; y < _engine.BoardSize; y++)
        {
            var row = new string[_engine.BoardSize];
            for (var x = 0; x < _engine.BoardSize; x++)
            {
                row[x] = territory[y, x].ToString(CultureInfo.InvariantCulture);
            }

            lines.Add(string.Join(' ', row));
        }

        return string.Join('\n', lines);
    }

    private string FormatLegacyTerritory(int[,] territory)
    {
        var lines = new List<string>(_engine.BoardSize + 1);
        for (var y = 0; y < _engine.BoardSize; y++)
        {
            var row = new string[_engine.BoardSize + 1];
            row[0] = "#";
            for (var x = 0; x < _engine.BoardSize; x++)
            {
                row[x + 1] = territory[y, x].ToString(CultureInfo.InvariantCulture);
            }

            lines.Add(string.Join(' ', row));
        }

        lines.Add("territory");
        return string.Join('\n', lines);
    }

    private static string FormatFinalScoreDetail(GtpFinalScoreEstimate estimate)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"rule {estimate.Rule} " +
            $"configuredEstimate {estimate.FormatConfiguredResult()} " +
            $"areaEstimate {estimate.FormatAreaResult()} " +
            $"areaMargin {estimate.AreaMargin:0.0} " +
            $"territoryEstimate {estimate.FormatTerritoryResult()} " +
            $"territoryMargin {estimate.TerritoryMargin:0.0} " +
            $"captureAdjustedEstimate {estimate.FormatCaptureAdjustedResult()} " +
            $"captureAdjustedMargin {estimate.CaptureAdjustedMargin:0.0} " +
            $"threshold {estimate.Threshold} " +
            $"komi {estimate.Komi:0.0} " +
            $"blackArea {estimate.BlackArea} " +
            $"whiteArea {estimate.WhiteArea} " +
            $"blackTerritoryScore {estimate.BlackTerritoryScore} " +
            $"whiteTerritoryScore {estimate.WhiteTerritoryScore} " +
            $"blackAlive {estimate.BlackAlive} " +
            $"blackCapture {estimate.BlackCapture} " +
            $"blackTerritory {estimate.BlackTerritory} " +
            $"whiteAlive {estimate.WhiteAlive} " +
            $"whiteCapture {estimate.WhiteCapture} " +
            $"whiteTerritory {estimate.WhiteTerritory} " +
            $"capturedBlackPrisoners {estimate.CapturedBlackPrisoners} " +
            $"capturedWhitePrisoners {estimate.CapturedWhitePrisoners}");
    }

    private static string FormatKataRules(string rule)
    {
        var scoring = ZenGtpOptions.IsTerritoryScoringRule(rule) ? "TERRITORY" : "AREA";
        var tax = scoring == "TERRITORY" ? "SEKI" : "NONE";
        var whiteHandicapBonus = scoring == "TERRITORY" ? "0" : "N";
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{{\"ko\":\"SIMPLE\",\"scoring\":\"{scoring}\",\"tax\":\"{tax}\",\"multiStoneSuicideLegal\":false,\"hasButton\":false,\"whiteHandicapBonus\":\"{whiteHandicapBonus}\",\"friendlyPassOk\":false}}");
    }

    private static bool TryMapKataRules(string json, out string rule, out string error)
    {
        rule = "japanese";
        error = "";

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (ContainsStringValue(root, "korean"))
            {
                rule = "japanese";
                return true;
            }

            if (!TryGetStringProperty(root, "scoring", out var scoring))
            {
                error = "kata-set-rules requires scoring";
                return false;
            }

            rule = scoring.ToUpperInvariant() switch
            {
                "AREA" or "CHINESE" => "area",
                "TERRITORY" or "JAPANESE" or "KOREAN" => "japanese",
                _ => "",
            };

            if (rule.Length == 0)
            {
                error = "kata-set-rules unsupported scoring";
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            error = "kata-set-rules requires valid JSON";
            return false;
        }
    }

    private static bool TryGetStringProperty(JsonElement element, string name, out string value)
    {
        value = "";
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(name, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? "";
        return true;
    }

    private static bool ContainsStringValue(JsonElement element, string value)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString()?.Equals(value, StringComparison.OrdinalIgnoreCase) == true;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name.Equals(value, StringComparison.OrdinalIgnoreCase) ||
                        ContainsStringValue(property.Value, value))
                    {
                        return true;
                    }
                }

                return false;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (ContainsStringValue(item, value))
                    {
                        return true;
                    }
                }

                return false;
            default:
                return false;
        }
    }

    private static SgfGame ParseSgf(string sgf)
    {
        var properties = ReadSgfProperties(sgf);
        var boardSize = 19;
        double? komi = null;
        var setupStones = new List<SgfMove>();
        var moves = new List<SgfMove>();

        foreach (var property in properties)
        {
            if (property.Name == "SZ" && property.Values.Count > 0)
            {
                if (!TryParseInteger(property.Values[0], out boardSize))
                {
                    throw new FormatException("invalid SZ property");
                }

                if (boardSize <= 0 || boardSize > 25)
                {
                    throw new FormatException("SZ property must be between 1 and 25");
                }
            }
        }

        foreach (var property in properties)
        {
            switch (property.Name)
            {
                case "KM" when property.Values.Count > 0:
                    if (!TryParseDouble(property.Values[0], out var parsedKomi))
                    {
                        throw new FormatException("invalid KM property");
                    }

                    komi = parsedKomi;
                    break;
                case "AB":
                    foreach (var value in property.Values)
                    {
                        setupStones.Add(new SgfMove(StoneColor.Black, SgfCoordinateToMove(value, boardSize)));
                    }

                    break;
                case "AW":
                    foreach (var value in property.Values)
                    {
                        setupStones.Add(new SgfMove(StoneColor.White, SgfCoordinateToMove(value, boardSize)));
                    }

                    break;
                case "B" when property.Values.Count > 0:
                    moves.Add(new SgfMove(StoneColor.Black, SgfCoordinateToMove(property.Values[0], boardSize)));
                    break;
                case "W" when property.Values.Count > 0:
                    moves.Add(new SgfMove(StoneColor.White, SgfCoordinateToMove(property.Values[0], boardSize)));
                    break;
            }
        }

        return new SgfGame(boardSize, komi, setupStones, moves);
    }

    private static List<SgfProperty> ReadSgfProperties(string sgf)
    {
        var properties = new List<SgfProperty>();
        var index = 0;
        while (index < sgf.Length)
        {
            if (!IsSgfPropertyNameStart(sgf[index]))
            {
                index++;
                continue;
            }

            var nameStart = index;
            while (index < sgf.Length && sgf[index] is >= 'A' and <= 'Z')
            {
                index++;
            }

            if (index >= sgf.Length || sgf[index] != '[')
            {
                continue;
            }

            var name = sgf[nameStart..index];
            var values = new List<string>();
            while (index < sgf.Length && sgf[index] == '[')
            {
                index++;
                var value = new StringBuilder();
                while (index < sgf.Length)
                {
                    var current = sgf[index++];
                    if (current == '\\' && index < sgf.Length)
                    {
                        value.Append(sgf[index++]);
                        continue;
                    }

                    if (current == ']')
                    {
                        break;
                    }

                    value.Append(current);
                }

                values.Add(value.ToString());
            }

            properties.Add(new SgfProperty(name, values));
        }

        return properties;
    }

    private static bool IsSgfPropertyNameStart(char value)
    {
        return value is >= 'A' and <= 'Z';
    }

    private static GtpMove SgfCoordinateToMove(string value, int boardSize)
    {
        if (string.IsNullOrEmpty(value))
        {
            return GtpMove.Pass;
        }

        if (value.Length != 2)
        {
            throw new FormatException($"invalid SGF coordinate: {value}");
        }

        var x = value[0] - 'a';
        var y = value[1] - 'a';
        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize)
        {
            throw new FormatException($"SGF coordinate is outside board: {value}");
        }

        return GtpMove.Play(new BoardCoordinate(x, y));
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

    private static double KataPrior(GtpAnalysisMove move, int order)
    {
        if (move.Prior is { } prior)
        {
            return Math.Clamp(prior, 0.0, 1.0);
        }

        return order switch
        {
            0 => 0.100,
            1 => 0.080,
            2 => 0.050,
            _ => Math.Max(0.010, 0.050 - (0.005 * (order - 2))),
        };
    }

    private static GtpExecutionResult Success(GtpCommand command, string body, bool IsEndResponse = true)
    {
        return new GtpExecutionResult(GtpResponse.Success(command.Id, body, IsEndResponse), ShouldQuit: false);
    }

    private static GtpExecutionResult AnalysisSuccess(GtpCommand command, string analysisOutput)
    {
        var output = analysisOutput.Length == 0 ? "" : analysisOutput + "\n";
        return new GtpExecutionResult(GtpResponse.Success(command.Id), ShouldQuit: false, OutputBeforeResponse: output);
    }

    private GtpExecutionResult GenMoveAnalyzeSuccess(GtpCommand command, string analysisOutput, GtpMove move)
    {
        var body = "\n";
        if (analysisOutput.Length > 0)
        {
            body += analysisOutput + "\n";
        }

        body += "play " + FormatMove(move);
        return Success(command, body);
    }

    private static GtpExecutionResult Error(GtpCommand command, string body)
    {
        return new GtpExecutionResult(GtpResponse.Error(command.Id, body), ShouldQuit: false);
    }

    private sealed record SgfGame(
        int BoardSize,
        double? Komi,
        IReadOnlyList<SgfMove> SetupStones,
        IReadOnlyList<SgfMove> Moves);

    private sealed record SgfProperty(string Name, IReadOnlyList<string> Values);

    private readonly record struct SgfMove(StoneColor Color, GtpMove Move);
}
