using ZenGTPX.Config;

namespace ZenGTPX.Tests;

[TestClass]
public sealed class ZenGtpOptionsTests
{
    [TestMethod]
    public void Load_UsesDefaultsWithoutConfig()
    {
        var options = ZenGtpOptionsLoader.Load([], Environment.CurrentDirectory);

        Assert.AreEqual(19, options.BoardSize);
        Assert.AreEqual(7.5, options.Komi);
        Assert.AreEqual("rank", options.Mode);
        Assert.AreEqual("9d", options.RankPreset);
        Assert.AreEqual("ZenGTPX", options.GtpName);
        Assert.AreEqual("", options.TracePath);
        Assert.AreEqual("japanese", options.FinalScoreRule);
        Assert.AreEqual(4, options.Threads);
        Assert.AreEqual(60.0, options.MaxTimeSeconds);
        Assert.AreEqual(6000, options.MaxSimulations);
        Assert.AreEqual(3, options.PnLevel);
        Assert.AreEqual(0.75, options.PnWeight);
        Assert.AreEqual(1.0, options.VnMixRate);
    }

    [TestMethod]
    public void Load_AppliesCommandLineOverrides()
    {
        var options = ZenGtpOptionsLoader.Load(["--boardSize", "9", "--komi", "6.5", "--threads", "2", "--maxTimeSeconds", "3.5", "--gtpName", "KataGo", "--tracePath", "gtp_logs/test.log", "--finalScoreRule", "area"], Environment.CurrentDirectory);

        Assert.AreEqual(9, options.BoardSize);
        Assert.AreEqual(6.5, options.Komi);
        Assert.AreEqual("KataGo", options.GtpName);
        Assert.AreEqual("gtp_logs/test.log", options.TracePath);
        Assert.AreEqual("area", options.FinalScoreRule);
        Assert.AreEqual(2, options.Threads);
        Assert.AreEqual(3.5, options.MaxTimeSeconds);
    }

    [TestMethod]
    public void Load_UsesZen7CfgFromDefaultConfigDirectory()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"zengtpx-default-config-{Guid.NewGuid():N}"));
        var configPath = Path.Combine(directory.FullName, "zen7.cfg");
        File.WriteAllText(
            configPath,
            """
            mode = advanced
            zenDll = Zen.dll
            boardSize = 13
            komi = 6.5
            finalScoreRule = territory
            threads = 2
            maxTimeSeconds = 3.5
            maxSimulations = 200
            resignThreshold = 0.05
            pnLevel = 3
            pnWeight = 1.5
            vnMixRate = 0.75
            """);

        var options = ZenGtpOptionsLoader.Load([], Environment.CurrentDirectory, directory.FullName);

        Assert.AreEqual(13, options.BoardSize);
        Assert.AreEqual(6.5, options.Komi);
        Assert.AreEqual("territory", options.FinalScoreRule);
        Assert.AreEqual(2, options.Threads);
        Assert.AreEqual(3.5, options.MaxTimeSeconds);
    }

    [TestMethod]
    public void Load_AcceptsLegacyMaxTimeCommandLineAlias()
    {
        var options = ZenGtpOptionsLoader.Load(["--maxTime", "2.5"], Environment.CurrentDirectory);

        Assert.AreEqual(2.5, options.MaxTimeSeconds);
    }

    [TestMethod]
    public void Load_AcceptsLegacyMaxTimeJsonAlias()
    {
        var configPath = WriteTempConfig("""
            {
              "mode": "advanced",
              "maxTime": 4.5
            }
            """);

        var options = ZenGtpOptionsLoader.Load(["--config", configPath], Environment.CurrentDirectory);

        Assert.AreEqual(4.5, options.MaxTimeSeconds);
    }

    [TestMethod]
    public void Load_CfgConfig()
    {
        var configPath = WriteTempConfig(
            """
            mode = advanced
            # comment
            zenDll = Zen.dll
            gtpName = KataGo
            tracePath = gtp_logs/test-{timestamp}.log
            boardSize = 13
            komi = 6.5
            finalScoreRule = territory
            threads = 2
            maxTimeSeconds = 3.5
            maxSimulations = 200
            resignThreshold = 0.05
            pnLevel = 3
            pnWeight = 1.5
            vnMixRate = 0.75
            """,
            ".cfg");

        var options = ZenGtpOptionsLoader.Load(["--config", configPath], Environment.CurrentDirectory);

        Assert.AreEqual("Zen.dll", options.ZenDll);
        Assert.AreEqual("KataGo", options.GtpName);
        Assert.AreEqual("gtp_logs/test-{timestamp}.log", options.TracePath);
        Assert.AreEqual("advanced", options.Mode);
        Assert.AreEqual(13, options.BoardSize);
        Assert.AreEqual(6.5, options.Komi);
        Assert.AreEqual("territory", options.FinalScoreRule);
        Assert.AreEqual(2, options.Threads);
        Assert.AreEqual(3.5, options.MaxTimeSeconds);
        Assert.AreEqual(200, options.MaxSimulations);
        Assert.AreEqual(0.05, options.ResignThreshold);
        Assert.AreEqual(3, options.PnLevel);
        Assert.AreEqual(1.5, options.PnWeight);
        Assert.AreEqual(0.75, options.VnMixRate);
    }

    [TestMethod]
    public void Load_DefaultCfgTemplate()
    {
        var repoRoot = FindRepoRoot();
        var configPath = Path.Combine(repoRoot, "config", "zen7.cfg");

        var options = ZenGtpOptionsLoader.Load(["--config", configPath], repoRoot);

        Assert.AreEqual("Zen.dll", options.ZenDll);
        Assert.AreEqual("KataGo", options.GtpName);
        Assert.AreEqual("gtp_logs/zengtpx-trace-{timestamp}-{pid}.log", options.TracePath);
        Assert.AreEqual("rank", options.Mode);
        Assert.AreEqual("9d", options.RankPreset);
        Assert.AreEqual(19, options.BoardSize);
        Assert.AreEqual(7.5, options.Komi);
        Assert.AreEqual(0, options.Handicap);
        Assert.AreEqual("japanese", options.FinalScoreRule);
        Assert.AreEqual(4, options.Threads);
        Assert.AreEqual(60.0, options.MaxTimeSeconds);
        Assert.AreEqual(6000, options.MaxSimulations);
        Assert.AreEqual(0.1, options.ResignThreshold);
        Assert.AreEqual(3, options.PnLevel);
        Assert.AreEqual(0.75, options.PnWeight);
        Assert.AreEqual(1.0, options.VnMixRate);
    }

    [TestMethod]
    public void Load_ZhTwCfgTemplate()
    {
        var repoRoot = FindRepoRoot();
        var configPath = Path.Combine(repoRoot, "config", "zen7_zh-TW.cfg");

        var options = ZenGtpOptionsLoader.Load(["--config", configPath], repoRoot);

        Assert.AreEqual("Zen.dll", options.ZenDll);
        Assert.AreEqual("KataGo", options.GtpName);
        Assert.AreEqual("gtp_logs/zengtpx-trace-{timestamp}-{pid}.log", options.TracePath);
        Assert.AreEqual("rank", options.Mode);
        Assert.AreEqual("9d", options.RankPreset);
        Assert.AreEqual(19, options.BoardSize);
        Assert.AreEqual(7.5, options.Komi);
        Assert.AreEqual(0, options.Handicap);
        Assert.AreEqual("japanese", options.FinalScoreRule);
        Assert.AreEqual(4, options.Threads);
        Assert.AreEqual(60.0, options.MaxTimeSeconds);
        Assert.AreEqual(6000, options.MaxSimulations);
        Assert.AreEqual(0.1, options.ResignThreshold);
        Assert.AreEqual(3, options.PnLevel);
        Assert.AreEqual(0.75, options.PnWeight);
        Assert.AreEqual(1.0, options.VnMixRate);
    }

    [TestMethod]
    public void Load_CfgRankPresetAppliesNativeGuiTable()
    {
        var configPath = WriteTempConfig(
            """
            mode = rank
            rankPreset = 5d
            threads = 12
            maxTimeSeconds = 3
            maxSimulations = 1
            pnLevel = 0
            pnWeight = 9
            vnMixRate = 9
            """,
            ".cfg");

        var options = ZenGtpOptionsLoader.Load(["--config", configPath], Environment.CurrentDirectory);

        Assert.AreEqual("rank", options.Mode);
        Assert.AreEqual("5d", options.RankPreset);
        Assert.AreEqual(4, options.Threads);
        Assert.AreEqual(60.0, options.MaxTimeSeconds);
        Assert.AreEqual(2700, options.MaxSimulations);
        Assert.AreEqual(2, options.PnLevel);
        Assert.AreEqual(0.55, options.PnWeight);
        Assert.AreEqual(1.0, options.VnMixRate);
    }

    [TestMethod]
    public void Load_CfgFixedTimeAppliesFullStrengthBaseline()
    {
        var configPath = WriteTempConfig(
            """
            mode = fixed-time
            threads = 8
            maxTimeSeconds = 5
            maxSimulations = 1
            pnLevel = 0
            pnWeight = 9
            vnMixRate = 9
            """,
            ".cfg");

        var options = ZenGtpOptionsLoader.Load(["--config", configPath], Environment.CurrentDirectory);

        Assert.AreEqual("fixed-time", options.Mode);
        Assert.AreEqual(8, options.Threads);
        Assert.AreEqual(5.0, options.MaxTimeSeconds);
        Assert.AreEqual(1_000_000, options.MaxSimulations);
        Assert.AreEqual(3, options.PnLevel);
        Assert.AreEqual(1.0, options.PnWeight);
        Assert.AreEqual(0.75, options.VnMixRate);
    }

    [TestMethod]
    public void Load_CfgConfigRejectsUnknownKey()
    {
        var configPath = WriteTempConfig("unknownKey = 1", ".cfg");

        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => ZenGtpOptionsLoader.Load(["--config", configPath], Environment.CurrentDirectory));
        Assert.AreEqual("Unknown config key on line 1: unknownKey", exception.Message);
    }

    [TestMethod]
    public void Load_AcceptsPassiveHandicapParameter()
    {
        var configPath = WriteTempConfig("""
            {
              "handicap": 2
            }
            """);

        var options = ZenGtpOptionsLoader.Load(["--config", configPath], Environment.CurrentDirectory);

        Assert.AreEqual(2, options.Handicap);
    }

    [TestMethod]
    public void ResolveZenDllPath_ResolvesRelativePathFromBaseDirectory()
    {
        var options = new ZenGtpOptions { ZenDll = "native/Zen.dll" };

        var resolved = options.ResolveZenDllPath(Path.GetFullPath("base"));

        Assert.AreEqual(Path.GetFullPath(Path.Combine("base", "native", "Zen.dll")), resolved);
    }

    [TestMethod]
    public void Validate_RejectsNegativeHandicap()
    {
        var options = new ZenGtpOptions { Handicap = -1 };

        var exception = Assert.ThrowsException<InvalidOperationException>(options.Validate);
        Assert.AreEqual("handicap must not be negative.", exception.Message);
    }

    [TestMethod]
    public void Validate_RejectsInvalidFinalScoreRule()
    {
        var options = new ZenGtpOptions { FinalScoreRule = "aga" };

        var exception = Assert.ThrowsException<InvalidOperationException>(options.Validate);
        Assert.AreEqual("finalScoreRule must be one of: japanese, territory, chinese, area.", exception.Message);
    }

    [TestMethod]
    public void Validate_RejectsInvalidBoardSize()
    {
        var options = new ZenGtpOptions { BoardSize = 26 };

        var exception = Assert.ThrowsException<InvalidOperationException>(options.Validate);
        Assert.AreEqual("boardSize must be between 1 and 25.", exception.Message);
    }

    private static string WriteTempConfig(string text, string extension = ".json")
    {
        var path = Path.Combine(Path.GetTempPath(), $"zengtpx-test-{Guid.NewGuid():N}{extension}");
        File.WriteAllText(path, text);
        return path;
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "config", "zen7_zh-TW.cfg")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
