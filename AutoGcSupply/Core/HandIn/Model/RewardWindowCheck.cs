namespace AutoGcSupply.Core.HandIn.Model;

internal static class RewardWindowCheck
{
    public static RewardWindowVerdict Judge(in RewardWindowSample shown, string expectedItemName, in HandInTarget target)
    {
        if (!shown.Readable || PromptText.Normalize(shown.ItemName).Length == 0 || PromptText.Normalize(expectedItemName).Length == 0)
        {
            return RewardWindowVerdict.Unreadable;
        }

        if (!PromptText.Same(shown.ItemName, expectedItemName))
        {
            return RewardWindowVerdict.OtherItem;
        }

        return shown.Seals == target.SealReward ? RewardWindowVerdict.Matches : RewardWindowVerdict.OtherSeals;
    }
}
