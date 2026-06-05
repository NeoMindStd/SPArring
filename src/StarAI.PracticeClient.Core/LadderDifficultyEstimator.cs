namespace StarAI.PracticeClient.Core;

public sealed record LadderDifficulty(
    int SourceElo,
    int EstimatedMmr,
    string Grade,
    string Label,
    string Disclaimer);

public static class LadderDifficultyEstimator
{
    private const string DefaultDisclaimer =
        "SCHNAIL ELO를 SCR 래더 MMR에 직접 대입한 보정 전 참고값입니다.";

    public static LadderDifficulty? EstimateFromSchnailElo(int? schnailElo)
    {
        if (schnailElo is null)
        {
            return null;
        }

        var mmr = Math.Max(0, schnailElo.Value);
        var grade = GradeForMmr(mmr);

        return new LadderDifficulty(
            SourceElo: schnailElo.Value,
            EstimatedMmr: mmr,
            Grade: grade,
            Label: $"{grade} / MMR {mmr}",
            Disclaimer: DefaultDisclaimer);
    }

    public static string GradeForMmr(int mmr)
    {
        return mmr switch
        {
            >= 2471 => "S",
            >= 2015 => "A",
            >= 1698 => "B",
            >= 1549 => "C",
            >= 1427 => "D",
            >= 1137 => "E",
            _ => "F"
        };
    }
}
