using System.Text.Json.Serialization;

namespace AutoGcSupply.Core.Shopper.Model;

internal readonly struct QuoteResultResponseJson
{
    public int? Schema { get; init; }
    public string? QuoteId { get; init; }

    [JsonConverter(typeof(ShopperNameConverter))]
    public string? State { get; init; }

    [JsonConverter(typeof(ShopperNameConverter))]
    public string? Refusal { get; init; }

    public DateTime? FetchedAtUtc { get; init; }
    public QuoteResultItemJson[]? Items { get; init; }
}

internal readonly struct QuoteResultItemJson
{
    public uint? ItemId { get; init; }
    public int? Quantity { get; init; }
    public QuotePriceJson? Normal { get; init; }
    public QuotePriceJson? High { get; init; }
}

internal readonly struct QuotePriceJson
{
    public bool? Available { get; init; }
    public int? Units { get; init; }
    public long? Total { get; init; }
    public long? UnitPrice { get; init; }
    public double? OldestListingHours { get; init; }
    public int? Worlds { get; init; }
}
