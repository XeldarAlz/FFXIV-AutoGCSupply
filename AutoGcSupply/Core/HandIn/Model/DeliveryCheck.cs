namespace AutoGcSupply.Core.HandIn.Model;

internal static class DeliveryCheck
{
    public static DeliveryVerdict Judge(in DeliverySample before, in DeliverySample after, in HandInTarget target, int sealCap, bool final)
    {
        var itemsHandedOver = before.Owned - after.Owned;
        var sealsGained = after.Seals - before.Seals;
        if (itemsHandedOver < 0 || sealsGained < 0)
        {
            return DeliveryVerdict.Contradicted;
        }

        var rewardSeen = sealsGained > 0
            || SealCapRule.Overflows(before.Seals, target.SealReward, sealCap)
            || !after.RowOpen;
        if (itemsHandedOver >= target.Requested && rewardSeen)
        {
            return DeliveryVerdict.Confirmed;
        }

        return final ? DeliveryVerdict.Contradicted : DeliveryVerdict.NotYet;
    }
}
