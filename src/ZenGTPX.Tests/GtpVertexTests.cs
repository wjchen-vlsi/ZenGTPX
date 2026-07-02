using ZenGTPX.Board;

namespace ZenGTPX.Tests;

[TestClass]
public sealed class GtpVertexTests
{
    [TestMethod]
    public void Parse_D4On19()
    {
        var move = GtpVertex.Parse("D4", 19);

        Assert.IsFalse(move.IsPass);
        Assert.AreEqual(new BoardCoordinate(3, 15), move.Coordinate);
    }

    [TestMethod]
    public void Parse_Q16On19()
    {
        var move = GtpVertex.Parse("Q16", 19);

        Assert.AreEqual(new BoardCoordinate(15, 3), move.Coordinate);
    }

    [TestMethod]
    public void Parse_Pass()
    {
        var move = GtpVertex.Parse("pass", 19);

        Assert.IsTrue(move.IsPass);
        Assert.IsNull(move.Coordinate);
    }

    [TestMethod]
    public void Format_SkipsIColumn()
    {
        Assert.AreEqual("Q16", GtpVertex.Format(new BoardCoordinate(15, 3), 19));
    }
}
