using ZenGTPX.Board;

namespace ZenGTPX.Gtp;

public interface IGtpEngine
{
    int BoardSize { get; }

    string GtpName { get; }

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

    IReadOnlyList<GtpAnalysisMove> Analyze(StoneColor color, int maxCandidates, CancellationToken cancellationToken);

    IReadOnlyList<GtpPolicyPoint> GetPolicy(int count);

    int[,] GetTerritoryStatistics();

    bool Undo(int count);

    string EstimateFinalScore();

    GtpFinalScoreEstimate GetFinalScoreEstimate();
}
