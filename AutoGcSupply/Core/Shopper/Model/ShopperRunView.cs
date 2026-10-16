using System.Collections.Immutable;

namespace AutoGcSupply.Core.Shopper.Model;

internal readonly record struct ShopperRunView
{
    private static readonly ShopperRunView blank = new()
    {
        RunId = string.Empty,
        CallerRunId = string.Empty,
        Status = string.Empty,
        RefusalName = string.Empty,
        Lines = ImmutableArray<ShopperRunLine>.Empty,
        Issues = ImmutableArray<ShopperRunIssue>.Empty,
    };

    public ShopperReply Reply { get; init; }
    public string RunId { get; init; }
    public bool Known { get; init; }
    public string CallerRunId { get; init; }
    public ShopperRunPhase Phase { get; init; }
    public ShopperRunState State { get; init; }
    public string Status { get; init; }
    public long Revision { get; init; }
    public bool Paused { get; init; }
    public ShopperPauseReason PauseReason { get; init; }
    public bool Terminal { get; init; }
    public ShopperRunOutcome Outcome { get; init; }
    public bool NeedsPlayer { get; init; }
    public long ConfirmedSpend { get; init; }
    public long ReservedSpend { get; init; }
    public ImmutableArray<ShopperRunLine> Lines { get; init; }
    public ImmutableArray<ShopperRunIssue> Issues { get; init; }
    public ShopperReturnHome ReturnHome { get; init; }
    public ShopperRefusal Refusal { get; init; }
    public string RefusalName { get; init; }

    public static ShopperRunView Failed(ShopperReply reply)
    {
        return blank with { Reply = reply };
    }

    public static ShopperRunView UnknownRun(string runId)
    {
        return blank with { Reply = ShopperReply.Valid, RunId = runId };
    }
}
