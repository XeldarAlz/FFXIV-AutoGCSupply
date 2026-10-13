using System.Text.Json.Serialization;

namespace AutoGcSupply.Core.Shopper.Model;

internal readonly struct StartRunRequestJson
{
    public required int Schema { get; init; }
    public required string Caller { get; init; }
    public required string CallerDisplayName { get; init; }
    public required string CallerRunId { get; init; }
    public required string Label { get; init; }
    public required long MaxSpend { get; init; }
    public required bool ReturnHome { get; init; }
    public required StartRunLineJson[] Lines { get; init; }
}

internal readonly struct StartRunLineJson
{
    public required uint ItemId { get; init; }
    public required ShopperQuality Quality { get; init; }
    public required int Quantity { get; init; }
    public required long MaxUnitPrice { get; init; }
    public required int ExcessAllowance { get; init; }
}

internal readonly struct StartRunResponseJson
{
    public int? Schema { get; init; }
    public bool? Accepted { get; init; }
    public string? RunId { get; init; }

    [JsonConverter(typeof(ShopperNameConverter))]
    public string? Refusal { get; init; }

    public string? Message { get; init; }
}
