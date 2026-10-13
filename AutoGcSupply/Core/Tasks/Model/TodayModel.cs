using System.Collections.Immutable;
using AutoGcSupply.Core.Value.Model;

namespace AutoGcSupply.Core.Tasks.Model;

internal enum TodayState : byte { NotLoggedIn, NoGrandCompany, Reading, NotLoaded, Ready, AllDone }

internal enum ShopperState : byte { Missing, Outdated, Newer, Available }

internal enum PricingState : byte { NotNeeded, Pending, Ready, Failed, Unavailable }

internal enum StartBlock : byte
{
    None,
    Running,
    SetupNeeded,
    NotLoggedIn,
    NoGrandCompany,
    NotOnHomeWorld,
    InDuty,
    NotLoaded,
    NothingSelected,
    NothingToDo,
    PricesPending,
    PricesFailed,
    NeedsSpendCap,
    ShopperNeeded,
    UpdateNeeded,
}

internal readonly record struct TodayHeader(byte GrandCompany, byte Rank, uint Seals, uint SealCap, DateTime NextResetUtc, string DayLabel);

internal readonly record struct TodayRow(RowDecision Decision, string ClassName, byte Level, string ItemName, uint ItemIconId);

internal readonly record struct TodayTotals(int Selected, int ToBuy, int ToHandIn, long EstimatedSpend, long Experience, long Seals, long SealsLost);

internal sealed class TodayModel
{
    public static readonly TodayModel Empty = new()
    {
        Revision = 0,
        State = TodayState.NotLoggedIn,
        Header = default,
        Rows = [],
        Totals = default,
        Shopper = ShopperState.Missing,
        ShopperDryRun = false,
        Pricing = PricingState.NotNeeded,
        SpendCap = 0,
        SuggestedSpendCap = 0,
        StartBlock = StartBlock.NotLoggedIn,
    };

    public required int Revision { get; init; }
    public required TodayState State { get; init; }
    public required TodayHeader Header { get; init; }
    public required ImmutableArray<TodayRow> Rows { get; init; }
    public required TodayTotals Totals { get; init; }
    public required ShopperState Shopper { get; init; }
    public required bool ShopperDryRun { get; init; }
    public required PricingState Pricing { get; init; }
    public required long SpendCap { get; init; }
    public required long SuggestedSpendCap { get; init; }
    public required StartBlock StartBlock { get; init; }
}
