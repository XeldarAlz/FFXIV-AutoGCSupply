namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct RewardWindowMismatch(
    RewardWindowVerdict Verdict,
    string ShownItemName,
    int ShownSeals,
    string ExpectedItemName,
    int LowestSeals,
    int HighestSeals)
{
    public static RewardWindowMismatch None { get; } = new(RewardWindowVerdict.Matches, string.Empty, 0, string.Empty, 0, 0);
}
