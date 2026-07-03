using ZenGTPX.Board;

namespace ZenGTPX.Gtp;

public interface IGtpEngine
{
    int BoardSize { get; }

    GtpSearchInfo? LastSearchInfo { get; }

    void SetBoardSize(int boardSize);

    void ClearBoard();

    void SetKomi(double komi);

    void SetNextColor(StoneColor color);

    void SetMaxTime(double seconds);

    void SetTimeSettings(double mainTime, double byoyomiTime, int periods);

    void SetTimeLeft(StoneColor color, double time, int stones);

    bool Play(StoneColor color, GtpMove move);

    GtpMove GenMove(StoneColor color);

    bool Undo(int count);

    string EstimateFinalScore();
}
