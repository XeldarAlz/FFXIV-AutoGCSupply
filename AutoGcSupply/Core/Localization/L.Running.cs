namespace AutoGcSupply.Core.Localization;

internal static partial class L
{
    internal static class Running
    {
        public static readonly LocString PhaseBuying = new("running.phase.buying", "Buying");
        public static readonly LocString PhaseTravelling = new("running.phase.travelling", "Travelling");
        public static readonly LocString PhaseHandingIn = new("running.phase.handingIn", "Handing in");
        public static readonly LocString PhaseFinishing = new("running.phase.finishing", "Finishing");
        public static readonly LocString PhaseStopping = new("running.phase.stopping", "Stopping");
        public static readonly LocString StepReading = new("running.step.reading", "Reading");
        public static readonly LocString StepPricing = new("running.step.pricing", "Pricing");
        public static readonly LocString StepBuying = new("running.step.buying", "Buying");
        public static readonly LocString StepTravelling = new("running.step.travelling", "Travelling");
        public static readonly LocString StepHandingIn = new("running.step.handingIn", "Handing in");
        public static readonly LocString StepDone = new("running.step.done", "Done");
        public static readonly LocString StatusFinished = new("running.statusFinished", "Finished");
        public static readonly LocString DryRunBadge = new("running.dryRunBadge", "DRY RUN");
        public static readonly LocString DryRunBanner = new("running.dryRunBanner", "Dry run: nothing is bought and nothing is handed in. Each step shows what it would do.");
        public static readonly LocString Working = new("running.working", "Working…");
        public static readonly LocString Delivered = new("running.delivered", "delivered");

        public static readonly LocString ShopperTitle = new("running.shopper.title", "Auto Shopper");
        public static readonly LocString ShopperSpent = new("running.shopper.spent", "{0} spent so far");
        public static readonly LocString ShopperWouldSpend = new("running.shopper.wouldSpend", "{0} would be spent so far");
        public static readonly LocString ShopperWaiting = new("running.shopper.waiting", "Waiting for Auto Shopper…");

        public static readonly LocString Requests = new("running.requests", "Requests");
        public static readonly LocString RowWaiting = new("running.row.waiting", "Waiting");
        public static readonly LocString RowBuying = new("running.row.buying", "Buying");
        public static readonly LocString RowBought = new("running.row.bought", "Bought");
        public static readonly LocString RowNotBought = new("running.row.notBought", "Not bought");
        public static readonly LocString RowHandingIn = new("running.row.handingIn", "Handing in");
        public static readonly LocString RowDelivered = new("running.row.delivered", "Delivered");
        public static readonly LocString RowWouldDeliver = new("running.row.wouldDeliver", "Would deliver");
        public static readonly LocString RowSkipped = new("running.row.skipped", "Skipped");
        public static readonly LocString RowUnconfirmed = new("running.row.unconfirmed", "Unconfirmed");

        public static readonly LocString ConsentTitle = new("running.consent.title", "Allow Auto GC Supply in Auto Shopper");
        public static readonly LocString ConsentBody = new("running.consent.body", "Auto Shopper asks before another plugin may buy for you. Open it and allow Auto GC Supply; the run carries on once you do.");
        public static readonly LocString UncertainTitle = new("running.uncertain.title", "A purchase needs your answer");
        public static readonly LocString UncertainBody = new("running.uncertain.body", "Auto Shopper could not confirm a purchase. Answer its question in Auto Shopper; the run waits until you do.");
        public static readonly LocString InterruptedTitle = new("running.interrupted.title", "An Auto Shopper run was interrupted");
        public static readonly LocString InterruptedBody = new("running.interrupted.body", "An earlier Auto Shopper run stopped part way. Resume or discard it in Auto Shopper first.");
        public static readonly LocString RefusedTitle = new("running.refused.title", "Auto Shopper turned the order down");
        public static readonly LocString ShopperStoppedTitle = new("running.shopperStopped.title", "Auto Shopper stopped the run");
        public static readonly LocString HandInPausedTitle = new("running.handInPaused.title", "The hand-in paused");
        public static readonly LocString UnconfirmedTitle = new("running.unconfirmed.title", "A delivery could not be confirmed");
        public static readonly LocString NeedsPlayerFallback = new("running.needsPlayerFallback", "Check the game, then resume or stop the run.");
        public static readonly LocString OpenShopper = new("running.openShopper", "Open Auto Shopper");

        public static readonly LocString FinishedTitle = new("running.finished.title", "Run finished");
        public static readonly LocString FinishedTitleDry = new("running.finished.titleDry", "Dry run finished");
        public static readonly LocString TileDelivered = new("running.tile.delivered", "Delivered");
        public static readonly LocString TileSpent = new("running.tile.spent", "Gil spent");
        public static readonly LocString TileWouldSpend = new("running.tile.wouldSpend", "Would spend");
        public static readonly LocString TileTime = new("running.tile.time", "Time");
        public static readonly LocString Dismiss = new("running.dismiss", "Dismiss");
        public static readonly LocString DismissHelp = new("running.dismissHelp", "Back to today's requests.");
    }
}
