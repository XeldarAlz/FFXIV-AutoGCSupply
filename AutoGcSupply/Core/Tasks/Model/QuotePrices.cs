using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Value.Model;
using System.Collections.Immutable;

namespace AutoGcSupply.Core.Tasks.Model;

internal static class QuotePrices
{
    public static ItemPrice[] From(ImmutableArray<ShopperQuoteItem> items)
    {
        var prices = new ItemPrice[items.Length];
        for (var index = 0; index < items.Length; index++)
        {
            var item = items[index];
            prices[index] = new ItemPrice(item.ItemId, item.Quantity, QualityOf(item.Normal), QualityOf(item.High));
        }

        return prices;
    }

    private static QualityPrice QualityOf(in ShopperQuotePrice price)
        => new(price.Available, price.Units, price.Total, price.UnitPrice, price.HighestUnitPrice);
}
