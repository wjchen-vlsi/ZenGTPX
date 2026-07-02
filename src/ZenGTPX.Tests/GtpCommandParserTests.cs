using ZenGTPX.Gtp;

namespace ZenGTPX.Tests;

[TestClass]
public sealed class GtpCommandParserTests
{
    [TestMethod]
    public void Parse_CommandWithoutId()
    {
        var command = GtpCommandParser.Parse("protocol_version");

        Assert.IsNotNull(command);
        Assert.IsNull(command.Id);
        Assert.AreEqual("protocol_version", command.Name);
        Assert.AreEqual(0, command.Arguments.Count);
    }

    [TestMethod]
    public void Parse_CommandWithIdAndArguments()
    {
        var command = GtpCommandParser.Parse("123 known_command genmove");

        Assert.IsNotNull(command);
        Assert.AreEqual("123", command.Id);
        Assert.AreEqual("known_command", command.Name);
        CollectionAssert.AreEqual(new[] { "genmove" }, command.Arguments.ToArray());
    }

    [TestMethod]
    public void Parse_StripsComment()
    {
        var command = GtpCommandParser.Parse("name # comment");

        Assert.IsNotNull(command);
        Assert.AreEqual("name", command.Name);
    }

    [TestMethod]
    public void Parse_EmptyLineReturnsNull()
    {
        Assert.IsNull(GtpCommandParser.Parse("   # comment"));
    }

    [TestMethod]
    public void Parse_IgnoresLeadingBom()
    {
        var command = GtpCommandParser.Parse("\uFEFFprotocol_version");

        Assert.IsNotNull(command);
        Assert.AreEqual("protocol_version", command.Name);
    }

    [TestMethod]
    public void Parse_IgnoresMisdecodedLeadingBom()
    {
        var command = GtpCommandParser.Parse("ï»¿protocol_version");

        Assert.IsNotNull(command);
        Assert.AreEqual("protocol_version", command.Name);
    }

    [TestMethod]
    public void Parse_IgnoresLeadingNonGtpPrefix()
    {
        var command = GtpCommandParser.Parse("\0name");

        Assert.IsNotNull(command);
        Assert.AreEqual("name", command.Name);
    }
}
