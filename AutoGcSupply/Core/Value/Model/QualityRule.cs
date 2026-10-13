using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;

namespace AutoGcSupply.Core.Value.Model;

public static class QualityRule
{
    public static QualityChoice Choose(SupplyMissionKind kind, SupplyQualityPolicy policy, int requested, OwnedCount owned, bool priced, in ItemPrice price)
    {
        var provisioning = kind == SupplyMissionKind.Provisioning;
        var allowsNormal = provisioning || policy != SupplyQualityPolicy.AlwaysHigh;
        var allowsHigh = provisioning || policy != SupplyQualityPolicy.AlwaysNormal;
        var ownedFirst = provisioning ? ItemQuality.Normal : ItemQuality.High;
        var ownedSecond = provisioning ? ItemQuality.High : ItemQuality.Normal;
        if (Allows(ownedFirst, allowsNormal, allowsHigh) && owned.Covers(ownedFirst, requested))
        {
            return new QualityChoice(RowCoverage.Owned, ownedFirst, 0, 0, 0);
        }

        if (Allows(ownedSecond, allowsNormal, allowsHigh) && owned.Covers(ownedSecond, requested))
        {
            return new QualityChoice(RowCoverage.Owned, ownedSecond, 0, 0, 0);
        }

        var fallback = allowsNormal ? ItemQuality.Normal : ItemQuality.High;
        if (!priced)
        {
            return new QualityChoice(RowCoverage.Unpriced, fallback, owned.ShortfallOf(fallback, requested), 0, 0);
        }

        var normalPriced = TryEstimate(allowsNormal, price, ItemQuality.Normal, owned.ShortfallOf(ItemQuality.Normal, requested), out var normalTotal);
        var highPriced = TryEstimate(allowsHigh, price, ItemQuality.High, owned.ShortfallOf(ItemQuality.High, requested), out var highTotal);
        if (!normalPriced && !highPriced)
        {
            return new QualityChoice(RowCoverage.NoListings, fallback, owned.ShortfallOf(fallback, requested), 0, 0);
        }

        var buyHigh = highPriced && (!normalPriced || HighPaysOff(kind, highTotal, normalTotal));
        var quality = buyHigh ? ItemQuality.High : ItemQuality.Normal;
        return new QualityChoice(
            RowCoverage.ToBuy,
            quality,
            owned.ShortfallOf(quality, requested),
            price.Of(quality).UnitPrice,
            buyHigh ? highTotal : normalTotal);
    }

    public static bool HighPaysOff(SupplyMissionKind kind, long highTotal, long normalTotal)
        => kind == SupplyMissionKind.Supply && (Int128)highTotal <= (Int128)normalTotal * RewardMath.HighQualitySupplyFactor;

    private static bool Allows(ItemQuality quality, bool allowsNormal, bool allowsHigh)
        => quality == ItemQuality.High ? allowsHigh : allowsNormal;

    private static bool TryEstimate(bool allowed, in ItemPrice price, ItemQuality quality, int shortfall, out long total)
    {
        if (allowed)
        {
            return price.TryEstimate(quality, shortfall, out total);
        }

        total = 0;
        return false;
    }
}
