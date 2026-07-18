using System.Text.Json;
using ZenGTPX.Config;

namespace ZenGTPX.Tests;

[TestClass]
public sealed class ZenConfigurationServiceTests
{
    [TestMethod]
    public void Version_DeclaresProtocolAndClientPersistence()
    {
        var service = CreateService(out _);

        using var document = JsonDocument.Parse(service.GetVersionJson());
        var root = document.RootElement;
        Assert.AreEqual("zengtp-config", root.GetProperty("protocol").GetString());
        Assert.AreEqual(1, root.GetProperty("version").GetInt32());
        Assert.AreEqual("client", root.GetProperty("persistenceOwner").GetString());
        Assert.AreEqual(6, root.GetProperty("commands").GetArrayLength());
    }

    [TestMethod]
    public void Schema_DescribesEveryV1FieldAndAtomicSemantics()
    {
        var service = CreateService(out _);

        using var document = JsonDocument.Parse(service.GetSchemaJson());
        var root = document.RootElement;
        Assert.AreEqual("atomic", root.GetProperty("batchSemantics").GetString());
        var fields = root.GetProperty("fields").EnumerateArray().ToArray();
        CollectionAssert.AreEqual(
            new[]
            {
                "mode", "rankPreset", "maxTimeSeconds", "maxSimulations", "pnLevel",
                "pnWeight", "vnMixRate", "threads", "finalScoreRule", "resignThreshold",
            },
            fields.Select(field => field.GetProperty("name").GetString()).ToArray());
        Assert.IsTrue(fields.All(field => !field.GetProperty("requiresRestart").GetBoolean()));
        Assert.AreEqual("next-search", fields[0].GetProperty("apply").GetString());
    }

    [TestMethod]
    public void Set_RankProfileReturnsSelectedAndDerivedEffectiveValues()
    {
        var service = CreateService(out var applied);

        var json = service.SetJson("""{"rankPreset":"6d","threads":12}""");

        Assert.AreEqual(1, applied.Count);
        Assert.AreEqual("6d", applied[0].RankPreset);
        Assert.AreEqual(4, applied[0].Threads);
        Assert.AreEqual(3000, applied[0].MaxSimulations);
        Assert.AreEqual(3, applied[0].PnLevel);
        Assert.AreEqual(4.4, applied[0].PnWeight);
        Assert.AreEqual(0.60, applied[0].VnMixRate);
        using var document = JsonDocument.Parse(json);
        var state = document.RootElement.GetProperty("state");
        Assert.AreEqual(12, state.GetProperty("selected").GetProperty("threads").GetInt32());
        Assert.AreEqual(4, state.GetProperty("effective").GetProperty("threads").GetInt32());
        Assert.IsTrue(state.GetProperty("dirty").GetBoolean());
        Assert.IsFalse(state.GetProperty("restartRequired").GetBoolean());
    }

    [TestMethod]
    public void Set_FixedTimeDerivesFullStrengthValues()
    {
        var service = CreateService(out var applied);

        service.SetJson("""{"mode":"fixed-time","maxTimeSeconds":5,"threads":8}""");

        var options = applied.Single();
        Assert.AreEqual("fixed-time", options.Mode);
        Assert.AreEqual(5.0, options.MaxTimeSeconds);
        Assert.AreEqual(1_000_000, options.MaxSimulations);
        Assert.AreEqual(3, options.PnLevel);
        Assert.AreEqual(1.0, options.PnWeight);
        Assert.AreEqual(0.75, options.VnMixRate);
        Assert.AreEqual(8, options.Threads);
    }

    [TestMethod]
    public void Set_AdvancedAppliesLowLevelValues()
    {
        var service = CreateService(out var applied);

        service.SetJson("""{"mode":"advanced","maxTimeSeconds":2.5,"maxSimulations":250,"pnLevel":2,"pnWeight":1.5,"vnMixRate":0.55,"threads":2,"resignThreshold":0.03,"finalScoreRule":"area"}""");

        var options = applied.Single();
        Assert.AreEqual("advanced", options.Mode);
        Assert.AreEqual(2.5, options.MaxTimeSeconds);
        Assert.AreEqual(250, options.MaxSimulations);
        Assert.AreEqual(2, options.PnLevel);
        Assert.AreEqual(1.5, options.PnWeight);
        Assert.AreEqual(0.55, options.VnMixRate);
        Assert.AreEqual(2, options.Threads);
        Assert.AreEqual(0.03, options.ResignThreshold);
        Assert.AreEqual("area", options.FinalScoreRule);
    }

    [DataTestMethod]
    [DataRow("{", "invalid_json", null)]
    [DataRow("[]", "invalid_type", null)]
    [DataRow("{\"unknown\":1}", "unknown_parameter", "unknown")]
    [DataRow("{\"threads\":1.5}", "invalid_type", "threads")]
    [DataRow("{\"threads\":0}", "invalid_value", "threads")]
    [DataRow("{\"pnLevel\":4}", "invalid_value", "pnLevel")]
    [DataRow("{\"vnMixRate\":1.1}", "invalid_value", "vnMixRate")]
    public void Set_InvalidPayloadIsRejectedBeforeApply(string json, string code, string? parameter)
    {
        var service = CreateService(out var applied);

        var exception = Assert.ThrowsException<ZenConfigurationException>(() => service.SetJson(json));

        Assert.AreEqual(code, exception.Code);
        Assert.AreEqual(parameter, exception.Parameter);
        Assert.AreEqual(0, applied.Count);
        using var state = JsonDocument.Parse(service.GetStateJson());
        Assert.IsFalse(state.RootElement.GetProperty("state").GetProperty("dirty").GetBoolean());
    }

    [TestMethod]
    public void Set_DuplicateParameterIsRejectedCaseInsensitively()
    {
        var service = CreateService(out var applied);

        var exception = Assert.ThrowsException<ZenConfigurationException>(
            () => service.SetJson("""{"threads":2,"Threads":3}"""));

        Assert.AreEqual("duplicate_parameter", exception.Code);
        Assert.AreEqual(0, applied.Count);
    }

    [TestMethod]
    public void SaveAndResetSavedUseProcessLocalSnapshot()
    {
        var service = CreateService(out var applied);
        service.SetJson("""{"rankPreset":"6d"}""");

        using var saved = JsonDocument.Parse(service.SaveJson());
        Assert.AreEqual("client", saved.RootElement.GetProperty("persistenceOwner").GetString());
        Assert.AreEqual("6d", saved.RootElement.GetProperty("profile").GetProperty("rankPreset").GetString());
        Assert.IsFalse(saved.RootElement.GetProperty("state").GetProperty("dirty").GetBoolean());

        service.SetJson("""{"rankPreset":"3d"}""");
        service.ResetJson("saved");

        Assert.AreEqual("6d", applied[^1].RankPreset);
        using var state = JsonDocument.Parse(service.GetStateJson());
        Assert.AreEqual("6d", state.RootElement.GetProperty("state").GetProperty("selected").GetProperty("rankPreset").GetString());
        Assert.IsFalse(state.RootElement.GetProperty("state").GetProperty("dirty").GetBoolean());
    }

    [TestMethod]
    public void ResetStartupAndDefaultsHaveDistinctSemantics()
    {
        var startup = new ZenGtpOptions { Mode = "advanced", Threads = 2, MaxTimeSeconds = 3, MaxSimulations = 200 };
        var applied = new List<ZenGtpOptions>();
        var service = new ZenConfigurationService(startup, applied.Add);

        service.SetJson("""{"threads":6}""");
        service.ResetJson("startup");
        Assert.AreEqual("advanced", applied[^1].Mode);
        Assert.AreEqual(2, applied[^1].Threads);

        service.ResetJson("defaults");
        Assert.AreEqual("rank", applied[^1].Mode);
        Assert.AreEqual("9d", applied[^1].RankPreset);
        Assert.AreEqual(4, applied[^1].Threads);
    }

    [TestMethod]
    public void ApplyFailureRestoresPreviousSelectionAndEffectiveOptions()
    {
        var calls = new List<ZenGtpOptions>();
        var fail = true;
        var startup = DefaultOptions();
        var service = new ZenConfigurationService(
            startup,
            options =>
            {
                calls.Add(options);
                if (fail)
                {
                    fail = false;
                    throw new InvalidOperationException("native rejected update");
                }
            });

        var exception = Assert.ThrowsException<ZenConfigurationException>(
            () => service.SetJson("""{"rankPreset":"6d"}"""));

        Assert.AreEqual("apply_failed", exception.Code);
        Assert.AreEqual(2, calls.Count);
        Assert.AreEqual("6d", calls[0].RankPreset);
        Assert.AreEqual("9d", calls[1].RankPreset);
        using var state = JsonDocument.Parse(service.GetStateJson());
        Assert.AreEqual("9d", state.RootElement.GetProperty("state").GetProperty("selected").GetProperty("rankPreset").GetString());
    }

    [TestMethod]
    public void GetReportsExternalRuntimeEffectiveValueWithoutChangingSelectedProfile()
    {
        var startup = DefaultOptions();
        var runtime = startup with { MaxTimeSeconds = 10 };
        var service = new ZenConfigurationService(startup, _ => { }, () => runtime);

        using var state = JsonDocument.Parse(service.GetStateJson());
        var payload = state.RootElement.GetProperty("state");
        Assert.AreEqual(60.0, payload.GetProperty("selected").GetProperty("maxTimeSeconds").GetDouble());
        Assert.AreEqual(10.0, payload.GetProperty("effective").GetProperty("maxTimeSeconds").GetDouble());
    }

    [TestMethod]
    public void SeparateServicesDoNotShareProfiles()
    {
        var first = CreateService(out var firstApplied);
        var second = CreateService(out var secondApplied);

        first.SetJson("""{"rankPreset":"9d"}""");
        second.SetJson("""{"rankPreset":"3d"}""");

        Assert.AreEqual("9d", firstApplied.Single().RankPreset);
        Assert.AreEqual("3d", secondApplied.Single().RankPreset);
    }

    [TestMethod]
    public void FormatErrorReturnsMachineReadableGtpBody()
    {
        var json = ZenConfigurationService.FormatError(
            new ZenConfigurationException("invalid_value", "bad value", "threads"));

        using var document = JsonDocument.Parse(json);
        var error = document.RootElement.GetProperty("error");
        Assert.AreEqual("invalid_value", error.GetProperty("code").GetString());
        Assert.AreEqual("threads", error.GetProperty("parameter").GetString());
        Assert.AreEqual("bad value", error.GetProperty("message").GetString());
    }

    private static ZenConfigurationService CreateService(out List<ZenGtpOptions> applied)
    {
        applied = [];
        return new ZenConfigurationService(DefaultOptions(), applied.Add);
    }

    private static ZenGtpOptions DefaultOptions()
    {
        return ZenGtpOptionsLoader.Load([], Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
    }
}
