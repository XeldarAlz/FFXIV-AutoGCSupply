using System.Collections.Immutable;

namespace AutoGcSupply.Core.Tasks.Model;

internal enum RunStep : byte { Idle, Reading, Pricing, Buying, Travelling, HandingIn, Finishing, Done }

internal enum RunEnd : byte { None, Completed, CompletedPartial, Stopped, NeedsPlayer, Failed }

internal enum NeedsPlayerReason : byte
{
    None,
    ShopperConsent,
    ShopperUncertainPurchase,
    ShopperInterrupted,
    ShopperRefused,
    HandInPaused,
    DeliveryUnconfirmed,
}

internal enum RowRunState : byte { Waiting, Buying, Bought, NotBought, HandingIn, Delivered, WouldDeliver, Skipped, Unconfirmed }

internal readonly record struct RunRow(byte ClassJobId, RowRunState State, string Detail);

internal readonly record struct RunSummary(int Delivered, int Planned, long Experience, long Seals, long SealsLost, long GilSpent, TimeSpan Duration, bool DryRun);

internal sealed class RunModel
{
    public static readonly RunModel Idle = new()
    {
        Revision = 0,
        Step = RunStep.Idle,
        Status = string.Empty,
        DryRun = false,
        ShopperStatus = string.Empty,
        ConfirmedSpend = 0,
        Rows = [],
        NeedsPlayer = NeedsPlayerReason.None,
        NeedsPlayerDetail = string.Empty,
        End = RunEnd.None,
        Summary = default,
    };

    public required int Revision { get; init; }
    public required RunStep Step { get; init; }
    public required string Status { get; init; }
    public required bool DryRun { get; init; }
    public required string ShopperStatus { get; init; }
    public required long ConfirmedSpend { get; init; }
    public required ImmutableArray<RunRow> Rows { get; init; }
    public required NeedsPlayerReason NeedsPlayer { get; init; }
    public required string NeedsPlayerDetail { get; init; }
    public required RunEnd End { get; init; }
    public required RunSummary Summary { get; init; }
}
