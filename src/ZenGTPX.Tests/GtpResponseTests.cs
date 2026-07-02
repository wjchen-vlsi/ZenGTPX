using ZenGTPX.Gtp;

namespace ZenGTPX.Tests;

[TestClass]
public sealed class GtpResponseTests
{
    [TestMethod]
    public void Format_SuccessWithoutId()
    {
        Assert.AreEqual("= 2\n\n", GtpResponse.Success(id: null, "2").Format());
    }

    [TestMethod]
    public void Format_SuccessWithId()
    {
        Assert.AreEqual("=123 true\n\n", GtpResponse.Success("123", "true").Format());
    }

    [TestMethod]
    public void Format_ErrorWithoutId()
    {
        Assert.AreEqual("? unknown command\n\n", GtpResponse.Error(id: null, "unknown command").Format());
    }

    [TestMethod]
    public void Format_EmptySuccess()
    {
        Assert.AreEqual("=\n\n", GtpResponse.Success(id: null).Format());
    }
}
