using ZenGTPX.Gtp;
using ZenGTPX.Board;

namespace ZenGTPX.Tests;

[TestClass]
public sealed class GtpSessionTests
{
    [TestMethod]
    public void Execute_ProtocolVersion()
    {
        var result = Execute("protocol_version");

        Assert.AreEqual("= 2\n\n", result.Response.Format());
        Assert.IsFalse(result.ShouldQuit);
    }

    [TestMethod]
    public void Execute_KnownCommand()
    {
        var result = Execute("known_command quit");

        Assert.AreEqual("= true\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_KnownCommand_Play()
    {
        var result = Execute("known_command play");

        Assert.AreEqual("= true\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_KnownCommand_GenMove()
    {
        var result = Execute("known_command genmove");

        Assert.AreEqual("= true\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_KnownCommand_TimeSettings()
    {
        var result = Execute("known_command time_settings");

        Assert.AreEqual("= true\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_KnownCommand_ShowBoard()
    {
        var result = Execute("known_command showboard");

        Assert.AreEqual("= true\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_KnownCommand_FinalScore()
    {
        var result = Execute("known_command final_score");

        Assert.AreEqual("= true\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_ListCommands_IncludesFirstVersionCommandSurface()
    {
        var result = Execute("list_commands");

        CollectionAssert.AreEqual(
            new[]
            {
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
            },
            result.Response.Body.Split('\n'));
    }

    [TestMethod]
    public void Execute_UnknownCommand()
    {
        var result = Execute("unknown");

        Assert.AreEqual("? unknown command\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_Quit()
    {
        var result = Execute("7 quit");

        Assert.AreEqual("=7\n\n", result.Response.Format());
        Assert.IsTrue(result.ShouldQuit);
    }

    [TestMethod]
    public void Execute_BoardSize_SetsBoardSizeAndClearsBoard()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("boardsize 13", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual(13, engine.BoardSize);
        CollectionAssert.AreEqual(new[] { "SetBoardSize:13", "ClearBoard" }, engine.Calls);
    }

    [TestMethod]
    public void Execute_ClearBoard()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("clear_board", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        CollectionAssert.AreEqual(new[] { "ClearBoard" }, engine.Calls);
    }

    [TestMethod]
    public void Execute_Komi()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("komi 6.5", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual(6.5, engine.Komi);
    }

    [TestMethod]
    public void Execute_PlayCoordinate()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("play b D4", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual(StoneColor.Black, engine.LastColor);
        Assert.AreEqual(new BoardCoordinate(3, 15), engine.LastMove.Coordinate);
    }

    [TestMethod]
    public void Execute_PlayPass()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("play white pass", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual(StoneColor.White, engine.LastColor);
        Assert.IsTrue(engine.LastMove.IsPass);
    }

    [TestMethod]
    public void Execute_PlayResign_ReturnsError()
    {
        var result = Execute("play b resign");

        Assert.AreEqual("? resign is not valid for play\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_GenMoveCoordinate()
    {
        var engine = new FakeGtpEngine
        {
            NextGeneratedMove = GtpMove.Play(new BoardCoordinate(15, 3)),
        };

        var result = Execute("genmove w", engine);

        Assert.AreEqual("= Q16\n\n", result.Response.Format());
        Assert.AreEqual(StoneColor.White, engine.LastGenMoveColor);
    }

    [TestMethod]
    public void Execute_GenMovePass()
    {
        var engine = new FakeGtpEngine
        {
            NextGeneratedMove = GtpMove.Pass,
        };

        var result = Execute("genmove black", engine);

        Assert.AreEqual("= pass\n\n", result.Response.Format());
        Assert.AreEqual(StoneColor.Black, engine.LastGenMoveColor);
    }

    [TestMethod]
    public void Execute_GenMoveResign()
    {
        var engine = new FakeGtpEngine
        {
            NextGeneratedMove = GtpMove.Resign,
        };

        var result = Execute("genmove b", engine);

        Assert.AreEqual("= resign\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_Undo()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("undo", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual(1, engine.LastUndoCount);
    }

    [TestMethod]
    public void Execute_UndoWithCount()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("undo 2", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual(2, engine.LastUndoCount);
    }

    [TestMethod]
    public void Execute_TimeSettings_UsesByoyomiAsMaxTime()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("time_settings 600 30 3", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual(30.0, engine.MaxTime);
    }

    [TestMethod]
    public void Execute_TimeSettings_FallsBackToMainTime()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("time_settings 120 0 0", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual(120.0, engine.MaxTime);
    }

    [TestMethod]
    public void Execute_TimeLeft_ValidatesArguments()
    {
        var result = Execute("time_left b 10 5");

        Assert.AreEqual("=\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_ShowBoard_ReflectsPlayedMoves()
    {
        var engine = new FakeGtpEngine();
        var session = new GtpSession(engine);

        Execute("play b D4", session);
        Execute("play w Q16", session);
        var result = Execute("showboard", session);

        var body = result.Response.Body;
        StringAssert.Contains(body, "A B C D E F G H J K L M N O P Q R S T");
        StringAssert.Contains(body, "16 . . . . . . . . . . . . . . . O . . . 16");
        StringAssert.Contains(body, " 4 . . . X . . . . . . . . . . . . . . . 4");
    }

    [TestMethod]
    public void Execute_ShowBoard_ReflectsUndo()
    {
        var engine = new FakeGtpEngine();
        var session = new GtpSession(engine);

        Execute("play b D4", session);
        Execute("undo", session);
        var result = Execute("showboard", session);

        Assert.IsFalse(result.Response.Body.Contains('X'));
    }

    [TestMethod]
    public void Execute_FinalScore_ReturnsExplicitUnavailableError()
    {
        var result = Execute("final_score");

        Assert.AreEqual("? final_score is not available\n\n", result.Response.Format());
    }

    private static GtpExecutionResult Execute(string line)
    {
        return Execute(line, new FakeGtpEngine());
    }

    private static GtpExecutionResult Execute(string line, IGtpEngine engine)
    {
        var command = GtpCommandParser.Parse(line);
        Assert.IsNotNull(command);
        return new GtpSession(engine).Execute(command);
    }

    private static GtpExecutionResult Execute(string line, GtpSession session)
    {
        var command = GtpCommandParser.Parse(line);
        Assert.IsNotNull(command);
        return session.Execute(command);
    }

    private sealed class FakeGtpEngine : IGtpEngine
    {
        public int BoardSize { get; private set; } = 19;

        public double Komi { get; private set; }

        public double MaxTime { get; private set; }

        public StoneColor LastColor { get; private set; }

        public GtpMove LastMove { get; private set; }

        public int LastUndoCount { get; private set; }

        public StoneColor LastGenMoveColor { get; private set; }

        public GtpMove NextGeneratedMove { get; init; } = GtpMove.Pass;

        public string[] Calls => _calls.ToArray();

        private readonly List<string> _calls = [];

        public void SetBoardSize(int boardSize)
        {
            BoardSize = boardSize;
            _calls.Add($"SetBoardSize:{boardSize}");
        }

        public void ClearBoard()
        {
            _calls.Add("ClearBoard");
        }

        public void SetKomi(double komi)
        {
            Komi = komi;
            _calls.Add($"SetKomi:{komi}");
        }

        public void SetMaxTime(double seconds)
        {
            MaxTime = seconds;
            _calls.Add($"SetMaxTime:{seconds}");
        }

        public bool Play(StoneColor color, GtpMove move)
        {
            LastColor = color;
            LastMove = move;
            _calls.Add($"Play:{color}:{move}");
            return true;
        }

        public GtpMove GenMove(StoneColor color)
        {
            LastGenMoveColor = color;
            _calls.Add($"GenMove:{color}");
            return NextGeneratedMove;
        }

        public bool Undo(int count)
        {
            LastUndoCount = count;
            _calls.Add($"Undo:{count}");
            return true;
        }
    }
}
