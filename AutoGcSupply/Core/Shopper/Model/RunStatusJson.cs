using System.Text.Json.Serialization;

namespace AutoGcSupply.Core.Shopper.Model;

internal readonly struct RunStatusResponseJson
{
    public int? Schema { get; init; }
    public string? RunId { get; init; }
    public bool? Known { get; init; }
    public string? CallerRunId { get; init; }
    public bool? DryRun { get; init; }

    [JsonConverter(typeof(ShopperNameConverter))]
    public string? Phase { get; init; }

    [JsonConverter(typeof(ShopperNameConverter))]
    public string? State { get; init; }

    public string? Status { get; init; }
    public long? Revision { get; init; }
    public bool? Paused { get; init; }

    [JsonConverter(typeof(ShopperNameConverter))]
    public string? PauseReason { get; init; }

    public bool? Terminal { get; init; }

    [JsonConverter(typeof(ShopperNameConverter))]
    public string? Outcome { get; init; }

    public bool? NeedsPlayer { get; init; }
    public long? ConfirmedSpend { get; init; }
    public long? ReservedSpend { get; init; }
    public RunStatusLineJson[]? Lines { get; init; }
    public RunStatusIssueJson[]? Issues { get; init; }

    [JsonConverter(typeof(ShopperNameConverter))]
    public string? ReturnHome { get; init; }

    [JsonConverter(typeof(ShopperNameConverter))]
    public string? Refusal { get; init; }
}

internal readonly struct RunStatusLineJson
{
    public uint? ItemId { get; init; }

    [JsonConverter(typeof(ShopperNameConverter))]
    public string? Quality { get; init; }

    public int? Requested { get; init; }
    public int? Acquired { get; init; }
    public int? Missing { get; init; }
}

internal readonly struct RunStatusIssueJson
{
    [JsonConverter(typeof(ShopperNameConverter))]
    public string? Reason { get; init; }

    public uint? ItemId { get; init; }
}
