using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;

namespace AutoGcSupply.Core.Tasks;

internal static class RunText
{
    public static string Detail(in RowProgress progress, in RowDecision row) => progress.Note switch
    {
        RowNote.InBags => Loc.T(L.Run.RowInBags),
        RowNote.ToBuy => Loc.T(L.Run.RowToBuy),
        RowNote.Buying => Loc.T(L.Run.RowBuying, progress.Bought, row.BuyQuantity),
        RowNote.Bought => Loc.T(L.Run.RowBought),
        RowNote.NoPrice => Loc.T(L.Run.RowNoPrice),
        RowNote.NoListings => Loc.T(L.Run.RowNoListings),
        RowNote.ShopperMissing => Loc.T(L.Run.RowShopperMissing),
        RowNote.ShopperOutdated => Loc.T(L.Run.RowShopperOutdated),
        RowNote.ShopperNewer => Loc.T(L.Run.RowShopperNewer),
        RowNote.PriceCapExceeded => Loc.T(L.Run.RowPriceCap),
        RowNote.NoStock => Loc.T(L.Run.RowNoStock),
        RowNote.InventoryFull => Loc.T(L.Run.RowInventoryFull),
        RowNote.InsufficientGil => Loc.T(L.Run.RowInsufficientGil),
        RowNote.BudgetReached => Loc.T(L.Run.RowBudgetReached),
        RowNote.NotMarketable => Loc.T(L.Run.RowNotMarketable),
        RowNote.ShopperIssue => Loc.T(L.Run.RowShopperIssue),
        RowNote.RequestChanged => Loc.T(L.Run.RowRequestChanged),
        RowNote.DayChanged => Loc.T(L.Run.RowDayChanged),
        RowNote.AlreadyHandedIn => Loc.T(L.Run.RowAlreadyHandedIn),
        RowNote.NotInBags => Loc.T(L.Run.RowNotInBags),
        RowNote.SealCap => Loc.T(L.Run.RowSealCap),
        RowNote.NotOffered => Loc.T(L.Run.RowNotOffered),
        RowNote.HandingIn => Loc.T(L.Run.RowHandingIn),
        RowNote.HandedIn => Loc.T(L.Run.RowHandedIn),
        RowNote.WouldHandIn => Loc.T(L.Run.RowWouldHandIn),
        RowNote.Unconfirmed => Loc.T(L.Run.RowUnconfirmed),
        _ => string.Empty,
    };

    public static string Refusal(NeedsPlayerReason reason, in ShopperAcceptance acceptance)
    {
        if (acceptance.Reply != ShopperReply.Valid)
        {
            return Loc.T(L.Run.NeedsNoAnswer);
        }

        return ShopperNeeds(reason, acceptance.Refusal, acceptance.Message.Length > 0 ? acceptance.Message : Loc.T(L.Run.NeedsRefused, acceptance.RefusalName));
    }

    public static string ShopperNeeds(NeedsPlayerReason reason, in ShopperRunView view)
        => ShopperNeeds(reason, view.Refusal, view.Status.Length > 0 ? view.Status : Loc.T(L.Run.NeedsRefused, view.RefusalName));

    public static string ShopperPaused(ShopperPauseReason reason)
        => Loc.T(reason == ShopperPauseReason.InContent ? L.Run.StatusShopperPausedInContent : L.Run.StatusShopperPaused);

    public static string HandInNeeds(NeedsPlayerReason reason, in HandInResult result)
    {
        if (reason == NeedsPlayerReason.DeliveryUnconfirmed)
        {
            return Loc.T(L.Run.NeedsUnconfirmed);
        }

        if (result.PauseReason == HandInPauseReason.RewardWindowMismatch)
        {
            return WithTryAgain(RewardWindow(result.RewardWindow));
        }

        return WithTryAgain(HandInPause(result.PauseReason, result.Detail));
    }

    public static string HandInTimeout() => WithTryAgain(Loc.T(L.Run.NeedsHandInTimeout));

    public static string Ended(RunEnd end, in RunSummary summary, string failure) => end switch
    {
        RunEnd.Completed => Loc.T(summary.DryRun ? L.Run.StatusCompletedDry : L.Run.StatusCompleted),
        RunEnd.CompletedPartial => Loc.T(summary.DryRun ? L.Run.StatusPartialDry : L.Run.StatusPartial, summary.Delivered, summary.Planned),
        RunEnd.NeedsPlayer => Loc.T(L.Run.StatusNeedsPlayer),
        RunEnd.Failed => failure,
        _ => Loc.T(L.Run.StatusStopped),
    };

    private static string ShopperNeeds(NeedsPlayerReason reason, ShopperRefusal refusal, string shopperText) => reason switch
    {
        NeedsPlayerReason.ShopperConsent => Loc.T(refusal == ShopperRefusal.ConsentDenied ? L.Run.NeedsConsentDenied : L.Run.NeedsConsent),
        NeedsPlayerReason.ShopperUncertainPurchase => Loc.T(L.Run.NeedsUncertain),
        NeedsPlayerReason.ShopperInterrupted => Loc.T(L.Run.NeedsInterrupted),
        NeedsPlayerReason.ShopperNotReady => string.Concat(shopperText, " ", Loc.T(L.Run.NeedsInShopper)),
        NeedsPlayerReason.ShopperStopped => Loc.T(L.Run.NeedsShopperStopped),
        NeedsPlayerReason.ShopperLostTrack => Loc.T(L.Run.NeedsLostTrack),
        NeedsPlayerReason.ShopperLostTrackStopped => Loc.T(L.Run.NeedsLostTrackStopped),
        _ => WithTryAgain(shopperText),
    };

    private static string HandInPause(HandInPauseReason reason, string detail) => reason switch
    {
        HandInPauseReason.NotInGrandCompany => Loc.T(L.Run.NeedsNotInGrandCompany),
        HandInPauseReason.TravelFailed => Loc.T(L.Run.NeedsTravelFailed),
        HandInPauseReason.OfficerNotFound => Loc.T(L.Run.NeedsOfficerNotFound),
        HandInPauseReason.OfficerUnreachable => Loc.T(L.Run.NeedsOfficerUnreachable),
        HandInPauseReason.MenuTextUnavailable or HandInPauseReason.MenuOptionNotFound => Loc.T(L.Run.NeedsMenu),
        HandInPauseReason.ListDidNotOpen or HandInPauseReason.ListClosed or HandInPauseReason.TabDidNotSwitch => Loc.T(L.Run.NeedsList),
        HandInPauseReason.ListNotRefreshed => Loc.T(L.Run.NeedsListNotRefreshed),
        HandInPauseReason.UnexpectedPrompt => Loc.T(L.Run.NeedsPrompt, detail),
        HandInPauseReason.UnexpectedAddon => Loc.T(L.Run.NeedsAddon, detail),
        HandInPauseReason.HighQualityPromptForNormalRow => Loc.T(L.Run.NeedsHighQualityPrompt),
        HandInPauseReason.RewardWindowDidNotOpen or HandInPauseReason.DeliverButtonDisabled => Loc.T(L.Run.NeedsReward),
        HandInPauseReason.InCombatOrDuty => Loc.T(L.Run.NeedsCombat),
        _ => Loc.T(L.Run.NeedsHandInFault),
    };

    private static string RewardWindow(in RewardWindowMismatch mismatch) => mismatch.Verdict switch
    {
        RewardWindowVerdict.OtherItem => Loc.T(L.Run.NeedsRewardMismatch, Loc.T(L.Run.NeedsRewardOtherItem, mismatch.ShownItemName, mismatch.ExpectedItemName)),
        RewardWindowVerdict.OtherSeals => Loc.T(
            L.Run.NeedsRewardMismatch,
            Loc.T(L.Run.NeedsRewardOtherSeals, Seals(mismatch.ShownSeals), mismatch.ExpectedItemName, Seals(mismatch.LowestSeals), Seals(mismatch.HighestSeals))),
        _ => Loc.T(L.Run.NeedsRewardUnreadable),
    };

    private static string Seals(int seals) => seals.ToString("N0", Loc.Culture);

    private static string WithTryAgain(string reason) => string.Concat(reason, " ", Loc.T(L.Run.NeedsTryAgain));
}
