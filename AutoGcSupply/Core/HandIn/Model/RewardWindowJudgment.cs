namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct RewardWindowJudgment(RewardWindowVerdict Verdict, bool SealsUnverified)
{
    public bool BaseSealsShown { get; init; }

    public static RewardWindowJudgment Matches { get; } = new(RewardWindowVerdict.Matches, false);

    public static RewardWindowJudgment SealsNotLocated { get; } = new(RewardWindowVerdict.Matches, true);

    public static RewardWindowJudgment Refuses(RewardWindowVerdict verdict) => new(verdict, false);
}
