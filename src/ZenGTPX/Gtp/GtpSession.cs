using System.Globalization;
using ZenGTPX.Board;

namespace ZenGTPX.Gtp;

public sealed class GtpSession
{
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
        "time_settings",
        "time_left",
        "showboard",
        "final_score",
        "quit",
    ];

    private static readonly HashSet<string> KnownCommands = new(Commands, StringComparer.Ordinal);
    private readonly IGtpEngine _engine;
    private readonly BoardState _board;

    public GtpSession(IGtpEngine engine)
    {
        _engine = engine;
        _board = new BoardState(engine.BoardSize);
    }

    public GtpExecutionResult Execute(GtpCommand command)
    {
        try
        {
            return command.Name switch
            {
                "protocol_version" => Success(command, "2"),
                "name" => Success(command, "ZenGTPX"),
                "version" => Success(command, "0.1.0"),
                "list_commands" => Success(command, string.Join('\n', Commands)),
                "known_command" => KnownCommand(command),
                "boardsize" => BoardSize(command),
                "clear_board" => ClearBoard(command),
                "komi" => Komi(command),
                "play" => Play(command),
                "genmove" => GenMove(command),
                "undo" => Undo(command),
                "time_settings" => TimeSettings(command),
                "time_left" => TimeLeft(command),
                "showboard" => ShowBoard(command),
                "final_score" => FinalScore(command),
                "quit" => new GtpExecutionResult(GtpResponse.Success(command.Id), ShouldQuit: true),
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
        _engine.SetBoardSize(boardSize);
        _engine.ClearBoard();
        _board.SetBoardSize(boardSize);
        return Success(command, "");
    }

    private GtpExecutionResult ClearBoard(GtpCommand command)
    {
        if (command.Arguments.Count != 0)
        {
            return Error(command, "clear_board does not accept arguments");
        }

        _engine.ClearBoard();
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
        _engine.SetKomi(komi);
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

        if (!_engine.Play(color, move))
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
        var move = _engine.GenMove(color);
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

        if (!_engine.Undo(count))
        {
            return Error(command, "cannot undo");
        }

        _board.Undo(count);
        return Success(command, "");
    }

    private GtpExecutionResult TimeSettings(GtpCommand command)
    {
        if (command.Arguments.Count != 3)
        {
            return Error(command, "time_settings requires main time, byoyomi time, and periods");
        }

        var mainTime = double.Parse(command.Arguments[0], CultureInfo.InvariantCulture);
        var byoyomiTime = double.Parse(command.Arguments[1], CultureInfo.InvariantCulture);
        _ = int.Parse(command.Arguments[2], CultureInfo.InvariantCulture);

        if (mainTime < 0 || byoyomiTime < 0)
        {
            return Error(command, "time_settings values must not be negative");
        }

        var maxTime = byoyomiTime > 0 ? byoyomiTime : mainTime;
        if (maxTime > 0)
        {
            _engine.SetMaxTime(maxTime);
        }

        return Success(command, "");
    }

    private GtpExecutionResult TimeLeft(GtpCommand command)
    {
        if (command.Arguments.Count != 3)
        {
            return Error(command, "time_left requires color, time, and stones");
        }

        _ = ParseColor(command.Arguments[0]);
        var time = double.Parse(command.Arguments[1], CultureInfo.InvariantCulture);
        _ = int.Parse(command.Arguments[2], CultureInfo.InvariantCulture);

        return time < 0
            ? Error(command, "time_left time must not be negative")
            : Success(command, "");
    }

    private GtpExecutionResult ShowBoard(GtpCommand command)
    {
        if (command.Arguments.Count != 0)
        {
            return Error(command, "showboard does not accept arguments");
        }

        return Success(command, "\n" + _board.FormatShowBoard());
    }

    private static GtpExecutionResult FinalScore(GtpCommand command)
    {
        if (command.Arguments.Count != 0)
        {
            return Error(command, "final_score does not accept arguments");
        }

        return Error(command, "final_score is not available");
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

    private static GtpExecutionResult Success(GtpCommand command, string body)
    {
        return new GtpExecutionResult(GtpResponse.Success(command.Id, body), ShouldQuit: false);
    }

    private static GtpExecutionResult Error(GtpCommand command, string body)
    {
        return new GtpExecutionResult(GtpResponse.Error(command.Id, body), ShouldQuit: false);
    }
}
