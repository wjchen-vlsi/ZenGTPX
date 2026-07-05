using System.Globalization;
using ZenGTPX.Config;

namespace ZenGTPX.Gtp;

public sealed record GtpFinalScoreEstimate(
    int Threshold,
    double Komi,
    int BlackAlive,
    int BlackCapture,
    int BlackTerritory,
    int WhiteAlive,
    int WhiteCapture,
    int WhiteTerritory,
    int CapturedBlackPrisoners,
    int CapturedWhitePrisoners,
    string Rule = "japanese")
{
    public int BlackArea => BlackAlive + BlackCapture + BlackTerritory;

    public int WhiteArea => WhiteAlive + WhiteCapture + WhiteTerritory;

    public double AreaMargin => BlackArea - WhiteArea - Komi;

    public double CaptureAdjustedMargin => AreaMargin + CapturedWhitePrisoners - CapturedBlackPrisoners;

    public int BlackTerritoryScore => BlackTerritory + (2 * BlackCapture) + CapturedBlackPrisoners;

    public int WhiteTerritoryScore => WhiteTerritory + (2 * WhiteCapture) + CapturedWhitePrisoners;

    public double TerritoryMargin => BlackTerritoryScore - WhiteTerritoryScore - Komi;

    public string FormatConfiguredResult()
    {
        return ZenGtpOptions.IsTerritoryScoringRule(Rule)
            ? FormatTerritoryResult()
            : FormatAreaResult();
    }

    public string FormatAreaResult() => FormatMargin(AreaMargin);

    public string FormatCaptureAdjustedResult() => FormatMargin(CaptureAdjustedMargin);

    public string FormatTerritoryResult() => FormatMargin(TerritoryMargin);

    private static string FormatMargin(double margin)
    {
        var winner = margin > 0 ? "B" : "W";
        return string.Create(CultureInfo.InvariantCulture, $"{winner}+{Math.Abs(margin):0.0}");
    }
}
