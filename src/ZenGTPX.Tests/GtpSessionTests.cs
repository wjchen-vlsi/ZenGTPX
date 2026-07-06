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
    public void Execute_KnownCommand_SetFreeHandicap()
    {
        var result = Execute("known_command set_free_handicap");

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
    public void Execute_KnownCommand_FinalStatusList()
    {
        var result = Execute("known_command final_status_list");

        Assert.AreEqual("= true\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_KnownCommand_LastSearchInfo()
    {
        var result = Execute("known_command zengtp_last_search_info");

        Assert.AreEqual("= true\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_KnownCommand_Policy()
    {
        var result = Execute("known_command zengtp_policy");

        Assert.AreEqual("= true\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_KnownCommand_Territory()
    {
        var result = Execute("known_command zengtp_territory");

        Assert.AreEqual("= true\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_KnownCommand_LegacyTerritory()
    {
        var result = Execute("known_command territory");

        Assert.AreEqual("= true\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_KnownCommand_FinalScoreDetail()
    {
        var result = Execute("known_command zengtp_final_score_detail");

        Assert.AreEqual("= true\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_KnownCommand_KataAnalyze()
    {
        var result = Execute("known_command kata-analyze");

        Assert.AreEqual("= true\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_KnownCommand_KataGenMoveAnalyze()
    {
        var result = Execute("known_command kata-genmove_analyze");

        Assert.AreEqual("= true\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_KnownCommand_GenMoveAnalyze()
    {
        var result = Execute("known_command genmove_analyze");

        Assert.AreEqual("= true\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_KnownCommand_Analyze()
    {
        var result = Execute("known_command analyze");

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
                "set_free_handicap",
                "place_free_handicap",
                "time_settings",
                "time_left",
                "showboard",
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
                "kata-time_settings",
                "clear_cache",
                "zengtp_last_search_info",
                "zengtp_policy",
                "territory",
                "zengtp_territory",
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

    [DataTestMethod]
    [DataRow("boardsize abc", "? boardsize must be an integer\n\n")]
    [DataRow("boardsize 0", "? boardsize must be between 1 and 25\n\n")]
    [DataRow("komi abc", "? komi must be numeric\n\n")]
    [DataRow("undo abc", "? undo count must be an integer\n\n")]
    [DataRow("undo 0", "? undo count must be positive\n\n")]
    [DataRow("fixed_handicap abc", "? fixed_handicap count must be an integer\n\n")]
    public void Execute_InvalidNumericArgument_ReturnsCommandSpecificError(string command, string expected)
    {
        var engine = new FakeGtpEngine();
        var result = Execute(command, engine);

        Assert.AreEqual(expected, result.Response.Format());
        CollectionAssert.AreEqual(Array.Empty<string>(), engine.Calls);
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
    public void Execute_PlayPass_DoesNotOccupyPoint()
    {
        var engine = new FakeGtpEngine();
        var session = new GtpSession(engine);

        Execute("play b pass", session);
        var result = Execute("play w D4", session);
        var showBoard = Execute("showboard", session);

        Assert.AreEqual("=\n\n", result.Response.Format());
        StringAssert.Contains(showBoard.Response.Body, " 4 . . . O . . . . . . . . . . . . . . . 4");
    }

    [TestMethod]
    public void Execute_PlayOccupiedPoint_ReturnsErrorBeforeEngineCall()
    {
        var engine = new FakeGtpEngine();
        var session = new GtpSession(engine);

        Execute("play b D4", session);
        var result = Execute("play w D4", session);

        Assert.AreEqual("? point D4 is already occupied\n\n", result.Response.Format());
        CollectionAssert.AreEqual(new[] { "Play:Black:D4" }, engine.Calls);
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
    public void Execute_GenMoveCoordinate_UpdatesBoardState()
    {
        var engine = new FakeGtpEngine
        {
            NextGeneratedMove = GtpMove.Play(new BoardCoordinate(15, 3)),
        };
        var session = new GtpSession(engine);

        Execute("genmove w", session);
        var result = Execute("play b Q16", session);

        Assert.AreEqual("? point Q16 is already occupied\n\n", result.Response.Format());
        CollectionAssert.AreEqual(new[] { "GenMove:White" }, engine.Calls);
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
    public void Execute_KataGenMoveAnalyze_ReturnsPartialResponseWithPlay()
    {
        var engine = new FakeGtpEngine
        {
            NextGeneratedMove = GtpMove.Play(new BoardCoordinate(15, 3)),
            NextSearchInfo = new GtpSearchInfo(GtpMove.Play(new BoardCoordinate(15, 3)), 6000, 0.53421, 1.23456),
        };

        var result = Execute("kata-genmove_analyze b 10", engine);

        Assert.AreEqual(
            "=\ninfo move Q16 visits 6000 winrate 0.5342 scoreLead 1.0 scoreMean 1.0 prior 0.100 order 0 pv Q16\nplay Q16\n\n",
            result.Response.Format());
        Assert.AreEqual(StoneColor.Black, engine.LastGenMoveColor);
    }

    [TestMethod]
    public void Execute_GenMoveAnalyze_ReturnsKataStylePartialResponseWithPlay()
    {
        var engine = new FakeGtpEngine
        {
            NextGeneratedMove = GtpMove.Play(new BoardCoordinate(15, 3)),
            NextSearchInfo = new GtpSearchInfo(GtpMove.Play(new BoardCoordinate(15, 3)), 6000, 0.53421, 1.23456),
        };

        var result = Execute("genmove_analyze b 10", engine);

        Assert.AreEqual(
            "=\ninfo move Q16 visits 6000 winrate 0.5342 scoreLead 1.0 scoreMean 1.0 prior 0.100 order 0 pv Q16\nplay Q16\n\n",
            result.Response.Format());
        Assert.AreEqual(StoneColor.Black, engine.LastGenMoveColor);
    }

    [TestMethod]
    public void Execute_LzGenMoveAnalyze_UsesBoardNextColor()
    {
        var engine = new FakeGtpEngine
        {
            NextGeneratedMove = GtpMove.Play(new BoardCoordinate(15, 3)),
            NextSearchInfo = new GtpSearchInfo(GtpMove.Play(new BoardCoordinate(15, 3)), 6000, 0.53421, 1.23456),
        };
        var session = new GtpSession(engine);

        Execute("play b D16", session);
        var result = Execute("lz-genmove_analyze 10", session);

        Assert.AreEqual(
            "=\ninfo move Q16 visits 6000 winrate 5342 pv Q16\nplay Q16\n\n",
            result.Response.Format());
        Assert.AreEqual(StoneColor.White, engine.LastGenMoveColor);
    }

    [TestMethod]
    public void Execute_KataGenMoveAnalyze_UpdatesBoardState()
    {
        var engine = new FakeGtpEngine
        {
            NextGeneratedMove = GtpMove.Play(new BoardCoordinate(15, 3)),
        };
        var session = new GtpSession(engine);

        Execute("kata-genmove_analyze b 10", session);
        var result = Execute("play w Q16", session);

        Assert.AreEqual("? point Q16 is already occupied\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_KataGenMoveAnalyze_InvalidInterval_ReturnsError()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("kata-genmove_analyze b 0", engine);

        Assert.AreEqual("? kata-genmove_analyze interval must be positive\n\n", result.Response.Format());
        CollectionAssert.AreEqual(Array.Empty<string>(), engine.Calls);
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
        Assert.IsNull(engine.LastAnalyzeMaxTimeSeconds);
    }

    [TestMethod]
    public void Execute_KataAnalyze_VisitsOnlyUsesBoardNextColor()
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
        var result = Execute("kata-analyze 10", session);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual(
            "info move Q16 visits 1700 winrate 0.5342 scoreLead 1.0 scoreMean 1.0 prior 0.100 order 0 pv Q16 D4\n",
            result.OutputBeforeResponse);
        Assert.AreEqual(StoneColor.White, engine.LastAnalyzeColor);
    }

    [TestMethod]
    public void Execute_Analyze_EmitsRawKataInfoBeforeSuccess()
    {
        var engine = new FakeGtpEngine
        {
            AnalysisMoves =
            [
                new GtpAnalysisMove(GtpMove.Play(new BoardCoordinate(15, 3)), 1700, 0.53421, "Q16 D4"),
            ],
        };

        var result = Execute("analyze b 10", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual(
            "info move Q16 visits 1700 winrate 0.5342 scoreLead 1.0 scoreMean 1.0 prior 0.100 order 0 pv Q16 D4\n",
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

    [DataTestMethod]
    [DataRow("lz-analyze abc", "? lz-analyze interval must be an integer\n\n")]
    [DataRow("lz-analyze 0", "? lz-analyze interval must be positive\n\n")]
    [DataRow("kata-analyze b abc", "? kata-analyze interval must be an integer\n\n")]
    [DataRow("kata-analyze b 0", "? kata-analyze interval must be positive\n\n")]
    public void Execute_Analyze_InvalidInterval_ReturnsCommandSpecificError(string command, string expected)
    {
        var engine = new FakeGtpEngine();
        var result = Execute(command, engine);

        Assert.AreEqual(expected, result.Response.Format());
        CollectionAssert.AreEqual(Array.Empty<string>(), engine.Calls);
    }

    [TestMethod]
    public void Execute_Stop_CancelsBackgroundAnalysisBeforeReturning()
    {
        var engine = new FakeGtpEngine { BlockAnalyzeUntilCanceled = true };
        var session = new GtpSession(engine, _ => { });

        Execute("kata-analyze b 10", session);
        Assert.IsTrue(engine.WaitForAnalyzeStarted());
        Assert.AreEqual(TimeSpan.FromMilliseconds(100), engine.LastAnalysisInterval);

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
        CollectionAssert.AreEqual(new[] { "RunAnalysis:Black:10:100", "Play:Black:D16" }, engine.Calls);
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
    public void Execute_ClearCache_ReturnsSuccessNoOp()
    {
        var result = Execute("clear_cache");

        Assert.AreEqual("=\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_Policy_ReturnsDefaultTopPolicyPoints()
    {
        var engine = new FakeGtpEngine
        {
            PolicyPoints =
            [
                new GtpPolicyPoint(new BoardCoordinate(15, 3), 1000, 0.5),
                new GtpPolicyPoint(new BoardCoordinate(3, 15), 500, 0.25),
            ],
        };

        var result = Execute("zengtp_policy", engine);

        Assert.AreEqual("= boardSize 19 count 2 max 1000 selectedSum 1500\nQ16 1000 0.5000\nD4 500 0.2500\n\n", result.Response.Format());
        Assert.AreEqual(20, engine.LastPolicyCount);
    }

    [TestMethod]
    public void Execute_Policy_UsesRequestedCount()
    {
        var engine = new FakeGtpEngine
        {
            PolicyPoints =
            [
                new GtpPolicyPoint(new BoardCoordinate(15, 3), 1000, 0.5),
            ],
        };

        var result = Execute("zengtp_policy 1", engine);

        Assert.AreEqual("= boardSize 19 count 1 max 1000 selectedSum 1000\nQ16 1000 0.5000\n\n", result.Response.Format());
        Assert.AreEqual(1, engine.LastPolicyCount);
    }

    [TestMethod]
    public void Execute_Policy_RejectsInvalidCount()
    {
        var result = Execute("zengtp_policy 0");

        Assert.AreEqual("? zengtp_policy count must be positive\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_Policy_RejectsNonIntegerCount()
    {
        var result = Execute("zengtp_policy abc");

        Assert.AreEqual("? zengtp_policy count must be an integer\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_Policy_ReturnsBoardSizeLimitError()
    {
        var engine = new FakeGtpEngine { ThrowPolicyBoardSizeLimit = true };
        var result = Execute("zengtp_policy", engine);

        Assert.AreEqual("? policy diagnostics are supported only up to board size 19.\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_Territory_ReturnsMatrix()
    {
        var engine = new FakeGtpEngine { BoardSizeOverride = 3 };
        engine.Territory[0, 0] = 1;
        engine.Territory[1, 1] = -2;
        engine.Territory[2, 2] = 3;

        var result = Execute("zengtp_territory", engine);

        Assert.AreEqual("= boardSize 3\n1 0 0\n0 -2 0\n0 0 3\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_LegacyTerritory_ReturnsZenEstimateMatrix()
    {
        var engine = new FakeGtpEngine { BoardSizeOverride = 3 };
        engine.Territory[0, 0] = 1;
        engine.Territory[1, 1] = -2;
        engine.Territory[2, 2] = 3;

        var result = Execute("territory", engine);

        Assert.AreEqual("=\n# 1 0 0\n# 0 -2 0\n# 0 0 3\nterritory\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_Territory_RejectsArguments()
    {
        var result = Execute("zengtp_territory 1");

        Assert.AreEqual("? zengtp_territory does not accept arguments\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_LegacyTerritory_RejectsArguments()
    {
        var result = Execute("territory 1");

        Assert.AreEqual("? territory does not accept arguments\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_Territory_ReturnsBoardSizeLimitError()
    {
        var engine = new FakeGtpEngine { ThrowTerritoryBoardSizeLimit = true };
        var result = Execute("zengtp_territory", engine);

        Assert.AreEqual("? territory diagnostics are supported only up to board size 19.\n\n", result.Response.Format());
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
    public void Execute_SetFreeHandicap_PlacesProvidedVertices()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("set_free_handicap D16 Q16 D4", engine);

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
    public void Execute_SetFreeHandicap_RejectsMissingVertices()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("set_free_handicap", engine);

        Assert.AreEqual("? set_free_handicap requires at least one vertex\n\n", result.Response.Format());
        CollectionAssert.AreEqual(Array.Empty<string>(), engine.Calls);
    }

    [TestMethod]
    public void Execute_SetFreeHandicap_RejectsDuplicateVertexBeforeEngineCall()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("set_free_handicap D16 D16", engine);

        Assert.AreEqual("? point D16 is already occupied\n\n", result.Response.Format());
        CollectionAssert.AreEqual(Array.Empty<string>(), engine.Calls);
    }

    [TestMethod]
    public void Execute_SetFreeHandicap_RejectsPassVertex()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("set_free_handicap D16 pass", engine);

        Assert.AreEqual("? invalid handicap vertex: pass\n\n", result.Response.Format());
        CollectionAssert.AreEqual(Array.Empty<string>(), engine.Calls);
    }

    [TestMethod]
    public void Execute_PlaceFreeHandicap_RejectsDuplicateVertexBeforeEngineCall()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("place_free_handicap D16 D16", engine);

        Assert.AreEqual("? point D16 is already occupied\n\n", result.Response.Format());
        CollectionAssert.AreEqual(Array.Empty<string>(), engine.Calls);
    }

    [TestMethod]
    public void Execute_FixedHandicap_RejectsOccupiedPointBeforeEngineCall()
    {
        var engine = new FakeGtpEngine();
        var session = new GtpSession(engine);

        Execute("play b D4", session);
        var result = Execute("fixed_handicap 2", session);

        Assert.AreEqual("? point D4 is already occupied\n\n", result.Response.Format());
        CollectionAssert.AreEqual(new[] { "Play:Black:D4" }, engine.Calls);
    }

    [TestMethod]
    public void Execute_SetFreeHandicapUndoAndClearBoard_UpdateBoardState()
    {
        var engine = new FakeGtpEngine();
        var session = new GtpSession(engine);

        Execute("set_free_handicap D16 Q16 D4", session);
        var withHandicap = Execute("showboard", session);
        Execute("undo 3", session);
        var afterUndo = Execute("showboard", session);
        Execute("set_free_handicap D16 Q16 D4", session);
        Execute("clear_board", session);
        var afterClear = Execute("showboard", session);

        StringAssert.Contains(withHandicap.Response.Body, "16 . . . X . . . . . . . . . . . X . . . 16");
        StringAssert.Contains(withHandicap.Response.Body, " 4 . . . X . . . . . . . . . . . . . . . 4");
        Assert.IsFalse(afterUndo.Response.Body.Contains('X'));
        Assert.IsFalse(afterClear.Response.Body.Contains('X'));
    }

    [TestMethod]
    public void Execute_HandicapUndoAndClearBoard_UpdateBoardState()
    {
        var engine = new FakeGtpEngine();
        var session = new GtpSession(engine);

        Execute("fixed_handicap 2", session);
        var withHandicap = Execute("showboard", session);
        Execute("undo 2", session);
        var afterUndo = Execute("showboard", session);
        Execute("fixed_handicap 2", session);
        Execute("clear_board", session);
        var afterClear = Execute("showboard", session);

        StringAssert.Contains(withHandicap.Response.Body, " 4 . . . X . . . . . . . . . . . . . . . 4");
        StringAssert.Contains(withHandicap.Response.Body, "16 . . . . . . . . . . . . . . . X . . . 16");
        Assert.IsFalse(afterUndo.Response.Body.Contains('X'));
        Assert.IsFalse(afterClear.Response.Body.Contains('X'));
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
    public void Execute_TimeSettings_AllowsZeroSettings()
    {
        var engine = new FakeGtpEngine { ConfiguredMaxTime = 60 };
        Execute("kata-set-param maxTime 15", engine);
        var result = Execute("time_settings 0 0 0", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual(60.0, engine.MaxTime);
        CollectionAssert.Contains(engine.Calls, "SetTimeSettings:0:0:0");
    }

    [TestMethod]
    public void Execute_KataSetParam_MaxTime_OverridesRuntimeMaxTime()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("kata-set-param maxTime 15", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual(15.0, engine.MaxTime);
        CollectionAssert.Contains(engine.Calls, "SetMaxTime:15");
    }

    [TestMethod]
    public void Execute_KataSetParam_MaxTimeZero_RestoresConfiguredMaxTime()
    {
        var engine = new FakeGtpEngine { ConfiguredMaxTime = 60 };
        Execute("kata-set-param maxTime 15", engine);
        var result = Execute("kata-set-param maxTime 0", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        Assert.AreEqual(60.0, engine.MaxTime);
        CollectionAssert.Contains(engine.Calls, "ResetMaxTime");
    }

    [DataTestMethod]
    [DataRow("kata-set-param maxTime -1", "? kata-set-param maxTime must not be negative\n\n")]
    [DataRow("kata-set-param maxTime abc", "? kata-set-param maxTime must be numeric\n\n")]
    public void Execute_KataSetParam_InvalidMaxTime_ReturnsCommandSpecificError(string command, string expected)
    {
        var engine = new FakeGtpEngine();
        var result = Execute(command, engine);

        Assert.AreEqual(expected, result.Response.Format());
        CollectionAssert.AreEqual(Array.Empty<string>(), engine.Calls);
    }

    [TestMethod]
    public void Execute_KataSetParam_MaxTime_IgnoresRuntimeTimeWhenDisabled()
    {
        var engine = new FakeGtpEngine { RuntimeTimeOverrideEnabled = false };
        var result = Execute("kata-set-param maxTime 999", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        CollectionAssert.AreEqual(Array.Empty<string>(), engine.Calls);
    }

    [TestMethod]
    public void Execute_TimeSettings_RejectsNegativeValuesBeforeEngineCall()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("time_settings 60 -1 3", engine);

        Assert.AreEqual("? time_settings values must not be negative\n\n", result.Response.Format());
        CollectionAssert.AreEqual(Array.Empty<string>(), engine.Calls);
    }

    [TestMethod]
    public void Execute_TimeSettings_IgnoresRuntimeTimeWhenDisabled()
    {
        var engine = new FakeGtpEngine { RuntimeTimeOverrideEnabled = false };
        var result = Execute("time_settings 60 10 3", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        CollectionAssert.AreEqual(Array.Empty<string>(), engine.Calls);
    }

    [DataTestMethod]
    [DataRow("time_settings abc 10 3", "? time_settings times must be numeric\n\n")]
    [DataRow("time_settings 60 abc 3", "? time_settings times must be numeric\n\n")]
    [DataRow("time_settings 60 10 abc", "? time_settings periods must be an integer\n\n")]
    public void Execute_TimeSettings_InvalidNumericArgument_ReturnsCommandSpecificError(string command, string expected)
    {
        var engine = new FakeGtpEngine();
        var result = Execute(command, engine);

        Assert.AreEqual(expected, result.Response.Format());
        CollectionAssert.AreEqual(Array.Empty<string>(), engine.Calls);
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
    public void Execute_TimeLeft_IgnoresRuntimeTimeWhenDisabled()
    {
        var engine = new FakeGtpEngine { RuntimeTimeOverrideEnabled = false };
        var result = Execute("time_left b 30 10", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        CollectionAssert.AreEqual(Array.Empty<string>(), engine.Calls);
    }

    [TestMethod]
    public void Execute_TimeLeft_AllowsZeroStones()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("time_left w 0 0", engine);

        Assert.AreEqual("=\n\n", result.Response.Format());
        CollectionAssert.Contains(engine.Calls, "SetTimeLeft:White:0:0");
    }

    [TestMethod]
    public void Execute_TimeLeft_RejectsNegativeValuesBeforeEngineCall()
    {
        var engine = new FakeGtpEngine();
        var result = Execute("time_left b 10 -1", engine);

        Assert.AreEqual("? time_left values must not be negative\n\n", result.Response.Format());
        CollectionAssert.AreEqual(Array.Empty<string>(), engine.Calls);
    }

    [DataTestMethod]
    [DataRow("time_left b abc 5", "? time_left time must be numeric\n\n")]
    [DataRow("time_left b 10 abc", "? time_left stones must be an integer\n\n")]
    public void Execute_TimeLeft_InvalidNumericArgument_ReturnsCommandSpecificError(string command, string expected)
    {
        var engine = new FakeGtpEngine();
        var result = Execute(command, engine);

        Assert.AreEqual(expected, result.Response.Format());
        CollectionAssert.AreEqual(Array.Empty<string>(), engine.Calls);
    }

    [TestMethod]
    public void Execute_TimeLeft_CanBeUpdatedMultipleTimes()
    {
        var engine = new FakeGtpEngine();
        var session = new GtpSession(engine);

        Execute("time_left b 30 5", session);
        var result = Execute("time_left b 20 4", session);

        Assert.AreEqual("=\n\n", result.Response.Format());
        CollectionAssert.AreEqual(
            new[]
            {
                "SetTimeLeft:Black:30:5",
                "SetTimeLeft:Black:20:4",
            },
            engine.Calls);
    }

    [TestMethod]
    public void Execute_TimeSettingsThenGenMove_ReturnsMove()
    {
        var engine = new FakeGtpEngine
        {
            NextGeneratedMove = GtpMove.Play(new BoardCoordinate(3, 3)),
        };
        var session = new GtpSession(engine);

        Execute("time_settings 60 10 3", session);
        Execute("time_left b 10 5", session);
        var result = Execute("genmove b", session);

        Assert.AreEqual("= D16\n\n", result.Response.Format());
        CollectionAssert.AreEqual(
            new[]
            {
                "SetTimeSettings:60:10:3",
                "SetTimeLeft:Black:10:5",
                "GenMove:Black",
            },
            engine.Calls);
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

    [DataTestMethod]
    [DataRow("alive")]
    [DataRow("dead")]
    [DataRow("seki")]
    public void Execute_FinalStatusList_ReturnsEmptyCompatibilityList(string status)
    {
        var result = Execute($"final_status_list {status}");

        Assert.AreEqual("=\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_FinalStatusList_RejectsMissingStatus()
    {
        var result = Execute("final_status_list");

        Assert.AreEqual("? final_status_list requires one status argument\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_FinalStatusList_RejectsUnsupportedStatus()
    {
        var result = Execute("final_status_list dame");

        Assert.AreEqual("? final_status_list status must be alive, dead, or seki\n\n", result.Response.Format());
    }

    [TestMethod]
    public void Execute_FinalScoreDetail_ReturnsEstimateBreakdown()
    {
        var engine = new FakeGtpEngine
        {
            FinalScoreEstimate = new GtpFinalScoreEstimate(
                300,
                6.5,
                10,
                1,
                20,
                8,
                2,
                18,
                3,
                4),
        };

        var result = Execute("zengtp_final_score_detail", engine);

        Assert.AreEqual(
            "= rule japanese configuredEstimate W+7.5 areaEstimate W+3.5 areaMargin -3.5 territoryEstimate W+7.5 territoryMargin -7.5 captureAdjustedEstimate W+2.5 captureAdjustedMargin -2.5 threshold 300 komi 6.5 blackArea 31 whiteArea 28 blackTerritoryScore 25 whiteTerritoryScore 26 blackAlive 10 blackCapture 1 blackTerritory 20 whiteAlive 8 whiteCapture 2 whiteTerritory 18 capturedBlackPrisoners 3 capturedWhitePrisoners 4\n\n",
            result.Response.Format());
    }

    [TestMethod]
    public void FinalScoreEstimate_UsesConfiguredAreaRule()
    {
        var estimate = new GtpFinalScoreEstimate(
            300,
            6.5,
            10,
            1,
            20,
            8,
            2,
            18,
            3,
            4,
            "area");

        Assert.AreEqual("W+3.5", estimate.FormatConfiguredResult());
        Assert.AreEqual("W+7.5", estimate.FormatTerritoryResult());
    }

    [TestMethod]
    public void Execute_FinalScoreDetail_RejectsArguments()
    {
        var result = Execute("zengtp_final_score_detail extra");

        Assert.AreEqual("? zengtp_final_score_detail does not accept arguments\n\n", result.Response.Format());
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
        public int BoardSize => BoardSizeOverride ?? _boardSize;

        public int? BoardSizeOverride { get; init; }

        private int _boardSize = 19;

        public string GtpName { get; init; } = "ZenGTPX";

        public GtpSearchInfo? LastSearchInfo { get; private set; }

        public bool RuntimeTimeOverrideEnabled { get; init; } = true;

        public double Komi { get; private set; }

        public double MaxTime { get; private set; }

        public double ConfiguredMaxTime { get; init; } = 60.0;

        public string FinalScore { get; init; } = "W+0.5";

        public GtpFinalScoreEstimate FinalScoreEstimate { get; init; } = new(
            300,
            7.5,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);

        public StoneColor LastColor { get; private set; }

        public GtpMove LastMove { get; private set; }

        public int LastUndoCount { get; private set; }

        public StoneColor LastGenMoveColor { get; private set; }

        public StoneColor LastAnalyzeColor { get; private set; }

        public GtpMove NextGeneratedMove { get; init; } = GtpMove.Pass;

        public GtpSearchInfo? NextSearchInfo { get; init; }

        public IReadOnlyList<GtpAnalysisMove> AnalysisMoves { get; init; } = [];

        public double? LastAnalyzeMaxTimeSeconds { get; private set; }

        public TimeSpan? LastAnalysisInterval { get; private set; }

        public IReadOnlyList<GtpPolicyPoint> PolicyPoints { get; init; } = [];

        public int LastPolicyCount { get; private set; }

        public int[,] Territory { get; } = new int[19, 19];

        public bool ThrowPolicyBoardSizeLimit { get; init; }

        public bool ThrowTerritoryBoardSizeLimit { get; init; }

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
            _boardSize = boardSize;
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

        public void ResetMaxTime()
        {
            MaxTime = ConfiguredMaxTime;
            AddCall("ResetMaxTime");
        }

        public void SetTimeSettings(double mainTime, double byoyomiTime, int periods)
        {
            var maxTime = byoyomiTime > 0 ? byoyomiTime : mainTime;
            if (maxTime > 0)
            {
                MaxTime = maxTime;
            }
            else
            {
                ResetMaxTime();
            }

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
            CancellationToken cancellationToken,
            double? maxTimeSeconds = null)
        {
            LastAnalyzeColor = color;
            LastAnalyzeMaxTimeSeconds = maxTimeSeconds;
            AddCall($"Analyze:{color}:{maxCandidates}");
            _analyzeStarted.Set();
            if (BlockAnalyzeUntilCanceled)
            {
                cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));
                LastAnalyzeCancellationRequested = cancellationToken.IsCancellationRequested;
            }

            return AnalysisMoves.Take(maxCandidates).ToArray();
        }

        public void RunAnalysis(
            StoneColor color,
            int maxCandidates,
            TimeSpan interval,
            Action<IReadOnlyList<GtpAnalysisMove>> onMoves,
            CancellationToken cancellationToken)
        {
            LastAnalyzeColor = color;
            LastAnalysisInterval = interval;
            AddCall($"RunAnalysis:{color}:{maxCandidates}:{interval.TotalMilliseconds}");
            _analyzeStarted.Set();
            if (BlockAnalyzeUntilCanceled)
            {
                cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));
                LastAnalyzeCancellationRequested = cancellationToken.IsCancellationRequested;
                return;
            }

            onMoves(AnalysisMoves.Take(maxCandidates).ToArray());
        }

        public IReadOnlyList<GtpPolicyPoint> GetPolicy(int count)
        {
            if (ThrowPolicyBoardSizeLimit)
            {
                throw new InvalidOperationException("policy diagnostics are supported only up to board size 19.");
            }

            LastPolicyCount = count;
            AddCall($"GetPolicy:{count}");
            return PolicyPoints.Take(count).ToArray();
        }

        public int[,] GetTerritoryStatistics()
        {
            if (ThrowTerritoryBoardSizeLimit)
            {
                throw new InvalidOperationException("territory diagnostics are supported only up to board size 19.");
            }

            AddCall("GetTerritoryStatistics");
            return Territory;
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

        public GtpFinalScoreEstimate GetFinalScoreEstimate()
        {
            AddCall("GetFinalScoreEstimate");
            return FinalScoreEstimate;
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
