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
    public void Execute_KnownCommand_FixedHandicap()
    {
        var result = Execute("known_command fixed_handicap");

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
    public void Execute_KnownCommand_LastSearchInfo()
    {
        var result = Execute("known_command zengtp_last_search_info");

        Assert.AreEqual("= true\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_KnownCommand_KataAnalyze()
    {
        var result = Execute("known_command kata-analyze");

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
            NextSearchInfo = new GtpSearchInfo(GtpMove.Play(new BoardCoordinate(15, 3)), 6000, 0.53421, 1.23456),
        };

        var result = Execute("genmove w", engine);

        Assert.AreEqual("= Q16\n\n", result.Response.Format());
        Assert.AreEqual(StoneColor.White, engine.LastGenMoveColor);
    }

    [TestMethod]
    public void Execute_LastSearchInfo_AfterGenMove()
    {
        var engine = new FakeGtpEngine
        {
            NextGeneratedMove = GtpMove.Play(new BoardCoordinate(15, 3)),
            NextSearchInfo = new GtpSearchInfo(GtpMove.Play(new BoardCoordinate(15, 3)), 6000, 0.53421, 1.23456),
        };
        var session = new GtpSession(engine);

        Execute("genmove w", session);
        var result = Execute("zengtp_last_search_info", session);

        Assert.AreEqual("= move Q16 playouts 6000 winrate 0.5342 time 1.235\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_LastSearchInfo_WithoutGenMoveReturnsError()
    {
        var result = Execute("zengtp_last_search_info");

        Assert.AreEqual("? no search info available\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_LastSearchInfo_AfterClearBoardReturnsError()
    {
        var engine = new FakeGtpEngine
        {
            NextGeneratedMove = GtpMove.Play(new BoardCoordinate(15, 3)),
            NextSearchInfo = new GtpSearchInfo(GtpMove.Play(new BoardCoordinate(15, 3)), 6000, 0.53421, 1.23456),
        };
        var session = new GtpSession(engine);

        Execute("genmove w", session);
        Execute("clear_board", session);
        var result = Execute("zengtp_last_search_info", session);

        Assert.AreEqual("? no search info available\n\n", result.Response.Format());
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
    public void Execute_KataAnalyze_EmitsRawKataInfoBeforeSuccess()
    {
        var engine = new FakeGtpEngine
        {
            AnalysisMoves =
            [
                new GtpAnalysisMove(GtpMove.Play(new BoardCoordinate(15, 3)), 1700, 0.53421, "Q16 D4"),
                new GtpAnalysisMove(GtpMove.Play(new BoardCoordinate(3, 15)), 850, 0.498, "D4 Q16"),
            ],
        };

        var result = Execute("kata-analyze b 10", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual(
            "info move Q16 visits 1700 winrate 0.5342 scoreLead 1.0 scoreMean 1.0 prior 0.100 order 0 pv Q16 D4 info move D4 visits 850 winrate 0.4980 scoreLead -0.1 scoreMean -0.1 prior 0.080 order 1 pv D4 Q16\n",
            result.OutputBeforeResponse);
        Assert.AreEqual(StoneColor.Black, engine.LastAnalyzeColor);
    }

    [TestMethod]
    public void Execute_KataAnalyze_UsesMovePriorWhenAvailable()
    {
        var engine = new FakeGtpEngine
        {
            AnalysisMoves =
            [
                new GtpAnalysisMove(GtpMove.Play(new BoardCoordinate(15, 3)), 1700, 0.53421, "Q16 D4", 0.625),
                new GtpAnalysisMove(GtpMove.Play(new BoardCoordinate(3, 15)), 850, 0.498, "D4 Q16", 0.375),
            ],
        };

        var result = Execute("kata-analyze b 10", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        StringAssert.Contains(result.OutputBeforeResponse, "prior 0.625 order 0");
        StringAssert.Contains(result.OutputBeforeResponse, "prior 0.375 order 1");
    }

    [TestMethod]
    public void Execute_LzAnalyze_UsesBoardNextColorAndLeelaWinrate()
    {
        var engine = new FakeGtpEngine
        {
            AnalysisMoves =
            [
                new GtpAnalysisMove(GtpMove.Play(new BoardCoordinate(15, 3)), 1700, 0.53421, "Q16 D4"),
            ],
        };
        var session = new GtpSession(engine);

        Execute("play b D16", session);
        var result = Execute("lz-analyze 10", session);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual("info move Q16 visits 1700 winrate 5342 pv Q16 D4\n", result.OutputBeforeResponse);
        Assert.AreEqual(StoneColor.White, engine.LastAnalyzeColor);
    }

    [TestMethod]
    public void Execute_Stop_CancelsBackgroundAnalysisBeforeReturning()
    {
        var engine = new FakeGtpEngine { BlockAnalyzeUntilCanceled = true };
        var session = new GtpSession(engine, _ => { });

        Execute("kata-analyze b 10", session);
        Assert.IsTrue(engine.WaitForAnalyzeStarted());

        var result = Execute("stop", session);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.IsTrue(engine.LastAnalyzeCancellationRequested);
    }

    [TestMethod]
    public void Execute_Play_CancelsBackgroundAnalysisBeforeMove()
    {
        var engine = new FakeGtpEngine { BlockAnalyzeUntilCanceled = true };
        var session = new GtpSession(engine, _ => { });

        Execute("kata-analyze b 10", session);
        Assert.IsTrue(engine.WaitForAnalyzeStarted());

        var result = Execute("play b D16", session);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.IsTrue(engine.LastAnalyzeCancellationRequested);
        CollectionAssert.AreEqual(new[] { "Analyze:Black:10", "Play:Black:D16" }, engine.Calls);
    }

    [DataTestMethod]
    [DataRow("clear_board", "ClearBoard")]
    [DataRow("undo", "Undo:1")]
    [DataRow("quit", null)]
    public void Execute_BoardChangeOrQuit_CancelsBackgroundAnalysis(string command, string? expectedCall)
    {
        var engine = new FakeGtpEngine { BlockAnalyzeUntilCanceled = true };
        var session = new GtpSession(engine, _ => { });

        Execute("kata-analyze b 10", session);
        Assert.IsTrue(engine.WaitForAnalyzeStarted());

        var result = Execute(command, session);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.IsTrue(engine.LastAnalyzeCancellationRequested);
        if (expectedCall is not null)
        {
            CollectionAssert.Contains(engine.Calls, expectedCall);
        }
    }

    [TestMethod]
    public void Execute_KataGetParam_ReturnsCompatibilityValue()
    {
        var result = Execute("kata-get-param analysisWideRootNoise");

        Assert.AreEqual("= 0.04\n\n", result.Response.Format());
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
    public void Execute_FixedHandicap_PlacesStandardStones()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("fixed_handicap 4", engine);

        Assert.AreEqual("= D4 Q16 Q4 D16\n\n", result.Response.Format());
        CollectionAssert.AreEqual(
            new[]
            {
                "Play:Black:D4",
                "Play:Black:Q16",
                "Play:Black:Q4",
                "Play:Black:D16",
            },
            engine.Calls);
    }

    [TestMethod]
    public void Execute_PlaceFreeHandicap_PlacesProvidedVertices()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("place_free_handicap D16 Q16 D4", engine);

        Assert.AreEqual("= D16 Q16 D4\n\n", result.Response.Format());
        CollectionAssert.AreEqual(
            new[]
            {
                "Play:Black:D16",
                "Play:Black:Q16",
                "Play:Black:D4",
            },
            engine.Calls);
    }

    [TestMethod]
    public void Execute_FixedHandicap_UsesCurrentBoardSize()
    {
        var engine = new FakeGtpEngine();
        var session = new GtpSession(engine);

        Execute("boardsize 13", session);
        var result = Execute("fixed_handicap 5", session);

        Assert.AreEqual("= D4 K10 K4 D10 G7\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_FixedHandicap_RejectsUnsupportedCount()
    {
        var result = Execute("fixed_handicap 1");

        Assert.AreEqual("? Handicap count must be between 2 and 9. (Parameter 'count')\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_TimeSettings_UsesByoyomiAsMaxTime()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("time_settings 600 30 3", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual(30.0, engine.MaxTime);
        CollectionAssert.Contains(engine.Calls, "SetTimeSettings:600:30:3");
    }

    [TestMethod]
    public void Execute_TimeSettings_FallsBackToMainTime()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("time_settings 120 0 0", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual(120.0, engine.MaxTime);
        CollectionAssert.Contains(engine.Calls, "SetTimeSettings:120:0:0");
    }

    [TestMethod]
    public void Execute_TimeLeft_ForwardsToEngine()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("time_left b 10 5", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        CollectionAssert.Contains(engine.Calls, "SetTimeLeft:Black:10:5");
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
    public void Execute_FinalScore_ReturnsEngineEstimate()
    {
        var engine = new FakeGtpEngine { FinalScore = "B+3.5" };
        var result = Execute("final_score", engine);

        Assert.AreEqual("= B+3.5\n\n", result.Response.Format());
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

        public string GtpName { get; init; } = "ZenGTPX";

        public GtpSearchInfo? LastSearchInfo { get; private set; }

        public double Komi { get; private set; }

        public double MaxTime { get; private set; }

        public string FinalScore { get; init; } = "W+0.5";

        public StoneColor LastColor { get; private set; }

        public GtpMove LastMove { get; private set; }

        public int LastUndoCount { get; private set; }

        public StoneColor LastGenMoveColor { get; private set; }

        public StoneColor LastAnalyzeColor { get; private set; }

        public GtpMove NextGeneratedMove { get; init; } = GtpMove.Pass;

        public GtpSearchInfo? NextSearchInfo { get; init; }

        public IReadOnlyList<GtpAnalysisMove> AnalysisMoves { get; init; } = [];

        public bool BlockAnalyzeUntilCanceled { get; init; }

        public bool LastAnalyzeCancellationRequested { get; private set; }

        public string[] Calls
        {
            get
            {
                lock (_calls)
                {
                    return _calls.ToArray();
                }
            }
        }

        private readonly List<string> _calls = [];
        private readonly ManualResetEventSlim _analyzeStarted = new();

        public bool WaitForAnalyzeStarted()
        {
            return _analyzeStarted.Wait(TimeSpan.FromSeconds(2));
        }

        public void SetBoardSize(int boardSize)
        {
            BoardSize = boardSize;
            LastSearchInfo = null;
            AddCall($"SetBoardSize:{boardSize}");
        }

        public void ClearBoard()
        {
            LastSearchInfo = null;
            AddCall("ClearBoard");
        }

        public void SetKomi(double komi)
        {
            Komi = komi;
            AddCall($"SetKomi:{komi}");
        }

        public void SetNextColor(StoneColor color)
        {
            AddCall($"SetNextColor:{color}");
        }

        public void SetMaxTime(double seconds)
        {
            MaxTime = seconds;
            AddCall($"SetMaxTime:{seconds}");
        }

        public void SetTimeSettings(double mainTime, double byoyomiTime, int periods)
        {
            MaxTime = byoyomiTime > 0 ? byoyomiTime : mainTime;
            AddCall($"SetTimeSettings:{mainTime}:{byoyomiTime}:{periods}");
        }

        public void SetTimeLeft(StoneColor color, double time, int stones)
        {
            AddCall($"SetTimeLeft:{color}:{time}:{stones}");
        }

        public bool Play(StoneColor color, GtpMove move)
        {
            LastSearchInfo = null;
            LastColor = color;
            LastMove = move;
            var moveText = move.Coordinate is { } coordinate
                ? GtpVertex.Format(coordinate, BoardSize)
                : move.IsPass
                    ? "pass"
                    : "resign";
            AddCall($"Play:{color}:{moveText}");
            return true;
        }

        public GtpMove GenMove(StoneColor color)
        {
            LastGenMoveColor = color;
            AddCall($"GenMove:{color}");
            LastSearchInfo = NextSearchInfo;
            return NextGeneratedMove;
        }

        public IReadOnlyList<GtpAnalysisMove> Analyze(
            StoneColor color,
            int maxCandidates,
            CancellationToken cancellationToken)
        {
            LastAnalyzeColor = color;
            AddCall($"Analyze:{color}:{maxCandidates}");
            _analyzeStarted.Set();
            if (BlockAnalyzeUntilCanceled)
            {
                cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));
                LastAnalyzeCancellationRequested = cancellationToken.IsCancellationRequested;
            }

            return AnalysisMoves.Take(maxCandidates).ToArray();
        }

        public bool Undo(int count)
        {
            LastUndoCount = count;
            LastSearchInfo = null;
            AddCall($"Undo:{count}");
            return true;
        }

        public string EstimateFinalScore()
        {
            AddCall("EstimateFinalScore");
            return FinalScore;
        }

        private void AddCall(string call)
        {
            lock (_calls)
            {
                _calls.Add(call);
            }
        }
    }
}
