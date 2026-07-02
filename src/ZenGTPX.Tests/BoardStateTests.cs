using ZenGTPX.Board;

namespace ZenGTPX.Tests;

[TestClass]
public sealed class BoardStateTests
{
    [TestMethod]
    public void FormatShowBoard_IncludesPlayedStones()
    {
        var board = new BoardState(9);

        board.Play(StoneColor.Black, GtpVertex.Parse("D4", 9));
        board.Play(StoneColor.White, GtpVertex.Parse("E5", 9));

        var text = board.FormatShowBoard();

        StringAssert.Contains(text, "A B C D E F G H J");
        StringAssert.Contains(text, " 4 . . . X . . . . . 4");
    }

    [TestMethod]
    public void Undo_RemovesLatestCoordinateMove()
    {
        var board = new BoardState(9);
        board.Play(StoneColor.Black, GtpVertex.Parse("D4", 9));

        board.Undo(1);

        var text = board.FormatShowBoard();
        Assert.IsFalse(text.Contains('X'));
    }
}
