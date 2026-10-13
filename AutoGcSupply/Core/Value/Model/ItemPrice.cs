using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Shared;

namespace AutoGcSupply.Core.Value.Model;

public readonly record struct QualityPrice(bool Available, int Units, long Total, long UnitPrice, long HighestUnitPrice = 0)
{
    public bool Usable => Available && Units > 0 && Total > 0 && UnitPrice > 0;
}

public readonly record struct ItemPrice(uint ItemId, int Quantity, QualityPrice Normal, QualityPrice High)
{
    public QualityPrice Of(ItemQuality quality) => quality == ItemQuality.High ? High : Normal;

    public bool TryEstimate(ItemQuality quality, int shortfall, out long total)
    {
        var price = Of(quality);
        if (!price.Usable || shortfall <= 0)
        {
            total = 0;
            return false;
        }

        if (shortfall == Quantity)
        {
            total = price.Total;
            return true;
        }

        return Gil.TryMultiply(price.UnitPrice, shortfall, out total);
    }

    public static bool TryFind(ReadOnlySpan<ItemPrice> prices, uint itemId, out ItemPrice price)
    {
        for (var index = 0; index < prices.Length; index++)
        {
            if (prices[index].ItemId == itemId)
            {
                price = prices[index];
                return true;
            }
        }

        price = default;
        return false;
    }
}
