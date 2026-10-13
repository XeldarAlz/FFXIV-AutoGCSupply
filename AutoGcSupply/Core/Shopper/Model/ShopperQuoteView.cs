using System.Collections.Immutable;

namespace AutoGcSupply.Core.Shopper.Model;

internal readonly record struct ShopperQuoteView(
    ShopperReply Reply,
    string QuoteId,
    ShopperQuoteState State,
    ShopperRefusal Refusal,
    string RefusalName,
    DateTime FetchedAtUtc,
    ImmutableArray<ShopperQuoteItem> Items)
{
    public static ShopperQuoteView Failed(ShopperReply reply)
    {
        return new ShopperQuoteView(reply, string.Empty, ShopperQuoteState.Unknown, ShopperRefusal.None, string.Empty, default, ImmutableArray<ShopperQuoteItem>.Empty);
    }
}
