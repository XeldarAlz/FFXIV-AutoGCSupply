using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;
using Dalamud.Interface;
using System.Numerics;

namespace AutoGcSupply.Windows.Sections;

internal static class RunLabels
{
    public static string BlockReason(StartBlock block) => block switch
    {
        StartBlock.Running => Loc.T(L.Automate.BlockRunning),
        StartBlock.SetupNeeded => Loc.T(L.Automate.BlockSetup),
        StartBlock.ShopperOutdated => Loc.T(L.Automate.BlockShopperOutdated),
        StartBlock.UpdateNeeded => Loc.T(L.Automate.BlockUpdateNeeded),
        StartBlock.NotLoggedIn => Loc.T(L.Automate.BlockLoggedOut),
        StartBlock.NoGrandCompany => Loc.T(L.Automate.BlockNoGrandCompany),
        StartBlock.NotOnHomeWorld => Loc.T(L.Automate.BlockNotOnHomeWorld),
        StartBlock.InDuty => Loc.T(L.Automate.BlockInDuty),
        StartBlock.NotLoaded => Loc.T(L.Automate.BlockNotLoaded),
        StartBlock.NothingSelected => Loc.T(L.Automate.BlockNothingSelected),
        StartBlock.NothingToDo => Loc.T(L.Automate.BlockNothingToDo),
        StartBlock.PricesPending => Loc.T(L.Automate.BlockPricesPending),
        StartBlock.PricesFailed => Loc.T(L.Automate.BlockPricesFailed),
        StartBlock.NeedsSpendCap => Loc.T(L.Automate.BlockNeedsSpendCap),
        StartBlock.ShopperNeeded => Loc.T(L.Automate.BlockShopperNeeded),
        _ => string.Empty,
    };

    public static string OutcomeLabel(RunEnd end) => end switch
    {
        RunEnd.Completed => Loc.T(L.Runs.OutcomeCompleted),
        RunEnd.CompletedPartial => Loc.T(L.Runs.OutcomePartial),
        RunEnd.Stopped => Loc.T(L.Runs.OutcomeStopped),
        RunEnd.NeedsPlayer => Loc.T(L.Runs.OutcomeNeedsPlayer),
        _ => Loc.T(L.Runs.OutcomeFailed),
    };

    public static Vector4 OutcomeColor(RunEnd end) => end switch
    {
        RunEnd.Completed => Styling.AccentMint,
        RunEnd.CompletedPartial => Styling.AccentAmber,
        RunEnd.Stopped => Styling.TextDim,
        RunEnd.NeedsPlayer => Styling.AccentAmber,
        _ => Styling.AccentRose,
    };

    public static string CoverageLabel(RowCoverage coverage, bool shopperAvailable) => coverage switch
    {
        RowCoverage.Owned => Loc.T(L.Requests.CoverageOwned),
        RowCoverage.ToBuy => Loc.T(L.Requests.CoverageToBuy),
        RowCoverage.Unpriced => Loc.T(shopperAvailable ? L.Requests.CoverageUnpriced : L.Requests.CoverageNoShopper),
        RowCoverage.NoListings => Loc.T(L.Requests.CoverageNoListings),
        RowCoverage.Delivered => Loc.T(L.Requests.CoverageDelivered),
        RowCoverage.Excluded => Loc.T(L.Requests.CoverageExcluded),
        _ => Loc.T(L.Requests.CoverageMaxLevel),
    };

    public static Vector4 CoverageColor(RowCoverage coverage) => coverage switch
    {
        RowCoverage.Owned => Styling.AccentMint,
        RowCoverage.ToBuy => Styling.AccentBlue,
        RowCoverage.Unpriced => Styling.AccentAmber,
        RowCoverage.NoListings => Styling.AccentRose,
        RowCoverage.Delivered => Styling.AccentMint,
        _ => Styling.TextDim,
    };

    public static string RowStateLabel(RowRunState state) => state switch
    {
        RowRunState.Waiting => Loc.T(L.Running.RowWaiting),
        RowRunState.Buying => Loc.T(L.Running.RowBuying),
        RowRunState.Bought => Loc.T(L.Running.RowBought),
        RowRunState.NotBought => Loc.T(L.Running.RowNotBought),
        RowRunState.HandingIn => Loc.T(L.Running.RowHandingIn),
        RowRunState.Delivered => Loc.T(L.Running.RowDelivered),
        RowRunState.Skipped => Loc.T(L.Running.RowSkipped),
        _ => Loc.T(L.Running.RowUnconfirmed),
    };

    public static Vector4 RowStateColor(RowRunState state) => state switch
    {
        RowRunState.Buying or RowRunState.HandingIn => Styling.AccentBlue,
        RowRunState.Bought => Styling.AccentGlow,
        RowRunState.Delivered => Styling.AccentMint,
        RowRunState.NotBought or RowRunState.Skipped => Styling.AccentAmber,
        RowRunState.Unconfirmed => Styling.AccentRose,
        _ => Styling.TextDim,
    };

    public static FontAwesomeIcon RowStateIcon(RowRunState state) => state switch
    {
        RowRunState.Buying => FontAwesomeIcon.ShoppingCart,
        RowRunState.Bought => FontAwesomeIcon.BoxOpen,
        RowRunState.NotBought => FontAwesomeIcon.Ban,
        RowRunState.HandingIn => FontAwesomeIcon.HandHolding,
        RowRunState.Delivered => FontAwesomeIcon.CheckCircle,
        RowRunState.Skipped => FontAwesomeIcon.Forward,
        RowRunState.Unconfirmed => FontAwesomeIcon.QuestionCircle,
        _ => FontAwesomeIcon.EllipsisH,
    };

    public static bool IsActive(RowRunState state) => state is RowRunState.Buying or RowRunState.HandingIn;

    public static bool IsDelivered(RowRunState state) => state == RowRunState.Delivered;
}
