using System.Text.Json.Serialization;

namespace AutoGcSupply.Core.Shopper.Model;

internal readonly struct QuoteStartRequestJson
{
    public required int Schema { get; init; }
    public required string Caller { get; init; }
    public required string CallerDisplayName { get; init; }
    public required QuoteStartItemJson[] Items { get; init; }
}

internal readonly struct QuoteStartItemJson
{
    public required uint ItemId { get; init; }
    public required int Quantity { get; init; }
}

internal readonly struct QuoteStartResponseJson
{
    public int? Schema { get; init; }
    public bool? Accepted { get; init; }
    public string? QuoteId { get; init; }

    [JsonConverter(typeof(ShopperNameConverter))]
    public string? Refusal { get; init; }

    public string? Message { get; init; }
}
