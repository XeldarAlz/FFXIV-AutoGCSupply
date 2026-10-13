namespace AutoGcSupply.Core.HandIn.Model;

internal static class SealCapRule
{
    public const int UnknownCap = 0;

    public static bool Overflows(int currentSeals, int sealReward, int sealCap)
        => sealCap != UnknownCap && (long)currentSeals + sealReward > sealCap;

    public static int SealsOverCap(int currentSeals, int sealReward, int sealCap)
    {
        if (!Overflows(currentSeals, sealReward, sealCap))
        {
            return 0;
        }

        return (int)Math.Min(sealReward, (long)currentSeals + sealReward - sealCap);
    }

    public static SealCapDecision Evaluate(int currentSeals, int sealReward, int sealCap, SealCapPolicy policy)
    {
        var overCap = SealsOverCap(currentSeals, sealReward, sealCap);
        if (overCap == 0)
        {
            return new SealCapDecision(SealCapAction.Deliver, 0);
        }

        var action = policy == SealCapPolicy.SkipRow ? SealCapAction.Skip : SealCapAction.DeliverWithLoss;
        return new SealCapDecision(action, overCap);
    }

    public static int SealsAfter(int currentSeals, int sealReward, int sealCap)
    {
        var total = (long)currentSeals + sealReward;
        return (int)(sealCap == UnknownCap ? total : Math.Min(total, Math.Max(currentSeals, sealCap)));
    }
}
