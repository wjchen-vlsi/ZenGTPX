using ZenGTPX.Board;

namespace ZenGTPX.Gtp;

public interface IGtpEngine
{
    int BoardSize { get; }

    void SetBoardSize(int boardSize);

    void ClearBoard();

    void SetKomi(double komi);

    void SetMaxTime(double seconds);

    bool Play(StoneColor color, GtpMove move);

    GtpMove GenMove(StoneColor color);

    bool Undo(int count);
}
