namespace AutoGcSupply.Core.Localization;

internal static partial class L
{
    internal static partial class Run
    {
        public static readonly LocString StatusReading = new("run.status.reading", "Reading today's requests");
        public static readonly LocString StatusPricing = new("run.status.pricing", "Pricing the items to buy");
        public static readonly LocString StatusBuying = new("run.status.buying", "Buying through Auto Shopper");
        public static readonly LocString StatusShopperPaused = new("run.status.shopperPaused", "Paused in Auto Shopper; resume it there to carry on buying");
        public static readonly LocString StatusShopperPausedInContent = new("run.status.shopperPausedInContent", "Auto Shopper paused for the duty and carries on once you leave");
        public static readonly LocString StatusStopping = new("run.status.stopping", "Stopping once Auto Shopper settles its purchase");
        public static readonly LocString StatusCounting = new("run.status.counting", "Counting the items in your bags");
        public static readonly LocString StatusTravelling = new("run.status.travelling", "Travelling to the Personnel Officer");
        public static readonly LocString StatusApproaching = new("run.status.approaching", "Walking up to the Personnel Officer");
        public static readonly LocString StatusOpeningList = new("run.status.openingList", "Opening the supply list");
        public static readonly LocString StatusHandingIn = new("run.status.handingIn", "Handing in {0} ({1} of {2})");
        public static readonly LocString StatusClosingList = new("run.status.closingList", "Closing the supply list");
        public static readonly LocString StatusFinishing = new("run.status.finishing", "Wrapping up");
        public static readonly LocString StatusPausedInContent = new("run.status.pausedInContent", "Paused while you are in a duty");
        public static readonly LocString StatusCompleted = new("run.status.completed", "Every selected request is handed in");
        public static readonly LocString StatusCompletedDry = new("run.status.completedDry", "Dry run finished: every selected request would be handed in");
        public static readonly LocString StatusPartial = new("run.status.partial", "{0} of {1} requests handed in");
        public static readonly LocString StatusPartialDry = new("run.status.partialDry", "Dry run finished: {0} of {1} requests would be handed in");
        public static readonly LocString StatusStopped = new("run.status.stopped", "Stopped");
        public static readonly LocString StatusNeedsPlayer = new("run.status.needsPlayer", "Waiting for you");
        public static readonly LocString FailedNotLoaded = new("run.failed.notLoaded", "Today's requests could not be read. Open the Timers window once, then press Refresh.");
        public static readonly LocString FailedOtherCharacter = new("run.failed.otherCharacter", "Another character is logged in than the one the run started for.");
        public static readonly LocString FailedFault = new("run.failed.fault", "The run stopped on an unexpected error. The log has the details.");

        public static readonly LocString RowInBags = new("run.row.inBags", "In your bags");
        public static readonly LocString RowToBuy = new("run.row.toBuy", "To buy");
        public static readonly LocString RowBuying = new("run.row.buying", "{0} of {1} bought");
        public static readonly LocString RowBought = new("run.row.bought", "Bought");
        public static readonly LocString RowNoPrice = new("run.row.noPrice", "No price was available");
        public static readonly LocString RowNoListings = new("run.row.noListings", "No listings were found");
        public static readonly LocString RowShopperMissing = new("run.row.shopperMissing", "Install Auto Shopper to buy it");
        public static readonly LocString RowShopperOutdated = new("run.row.shopperOutdated", "Update Auto Shopper to buy it");
        public static readonly LocString RowShopperNewer = new("run.row.shopperNewer", "Update Auto GC Supply to buy it");
        public static readonly LocString RowPriceCap = new("run.row.priceCap", "Every listing cost more than the cap");
        public static readonly LocString RowNoStock = new("run.row.noStock", "Not enough was listed");
        public static readonly LocString RowInventoryFull = new("run.row.inventoryFull", "Your inventory was full");
        public static readonly LocString RowInsufficientGil = new("run.row.insufficientGil", "You ran out of gil");
        public static readonly LocString RowBudgetReached = new("run.row.budgetReached", "The run's spend cap was reached");
        public static readonly LocString RowNotMarketable = new("run.row.notMarketable", "It is not sold on the market board");
        public static readonly LocString RowShopperIssue = new("run.row.shopperIssue", "Auto Shopper did not buy it");
        public static readonly LocString RowRequestChanged = new("run.row.requestChanged", "The request changed since the run started");
        public static readonly LocString RowDayChanged = new("run.row.dayChanged", "Not bought: a new day began after the run started");
        public static readonly LocString RowAlreadyHandedIn = new("run.row.alreadyHandedIn", "Already handed in");
        public static readonly LocString RowNotInBags = new("run.row.notInBags", "Not enough in your bags");
        public static readonly LocString RowSealCap = new("run.row.sealCap", "Skipped: the seals would pass your cap");
        public static readonly LocString RowNotOffered = new("run.row.notOffered", "The supply list did not offer it");
        public static readonly LocString RowHandingIn = new("run.row.handingIn", "Handing in");
        public static readonly LocString RowHandedIn = new("run.row.handedIn", "Handed in");
        public static readonly LocString RowWouldHandIn = new("run.row.wouldHandIn", "Would be handed in (dry run)");
        public static readonly LocString RowUnconfirmed = new("run.row.unconfirmed", "The hand-in could not be confirmed");

        public static readonly LocString NeedsConsent = new("run.needs.consent", "Auto Shopper needs your permission first. Open Auto Shopper, press Allow on its Shop page, then press Automate again.");
        public static readonly LocString NeedsConsentDenied = new("run.needs.consentDenied", "Auto GC Supply was denied in Auto Shopper. Allow it under Auto Shopper's Settings, General, Other plugins, then press Automate again.");
        public static readonly LocString NeedsUncertain = new("run.needs.uncertain", "Auto Shopper has a purchase without a clear answer. Answer it in Auto Shopper, then press Automate again.");
        public static readonly LocString NeedsInterrupted = new("run.needs.interrupted", "Auto Shopper's run was interrupted. Resume or discard it in Auto Shopper, then press Automate again.");
        public static readonly LocString NeedsLostTrackStopped = new("run.needs.lostTrackStopped", "Auto GC Supply lost track of Auto Shopper's run and asked it to stop. Press Automate to try again.");
        public static readonly LocString NeedsLostTrack = new("run.needs.lostTrack", "Auto GC Supply lost track of Auto Shopper's run, and the run did not take the stop. Check it in Auto Shopper, then press Automate again.");
        public static readonly LocString NeedsShopperStopped = new("run.needs.shopperStopped", "The run was stopped inside Auto Shopper: by its Stop button, its GM alert or a change to Auto GC Supply's permission. Anything it bought stays in your bags; press Automate to hand it in.");
        public static readonly LocString NeedsRefused = new("run.needs.refused", "Auto Shopper refused the run ({0}).");
        public static readonly LocString NeedsInShopper = new("run.needs.inShopper", "Open Auto Shopper to sort it out, then press Automate again.");
        public static readonly LocString NeedsNoAnswer = new("run.needs.noAnswer", "Auto Shopper did not answer. Check that it is installed and up to date, then press Automate again.");
        public static readonly LocString NeedsUnconfirmed = new("run.needs.unconfirmed", "A hand-in could not be confirmed. Check the supply list in game, then press Automate again.");
        public static readonly LocString NeedsNotInGrandCompany = new("run.needs.notInGrandCompany", "Your character is not in a Grand Company.");
        public static readonly LocString NeedsTravelFailed = new("run.needs.travelFailed", "The trip to your Grand Company did not arrive.");
        public static readonly LocString NeedsOfficerNotFound = new("run.needs.officerNotFound", "The Personnel Officer could not be found nearby.");
        public static readonly LocString NeedsOfficerUnreachable = new("run.needs.officerUnreachable", "The Personnel Officer could not be reached on foot.");
        public static readonly LocString NeedsMenu = new("run.needs.menu", "The supply option was not found in the Personnel Officer's menu.");
        public static readonly LocString NeedsList = new("run.needs.list", "The supply list did not open or switch tabs as expected.");
        public static readonly LocString NeedsListNotRefreshed = new("run.needs.listNotRefreshed", "A delivered request stayed open on the supply list.");
        public static readonly LocString NeedsPrompt = new("run.needs.prompt", "An unexpected question appeared and was answered No: {0}");
        public static readonly LocString NeedsAddon = new("run.needs.addon", "An unexpected window appeared: {0}");
        public static readonly LocString NeedsHighQualityPrompt = new("run.needs.highQualityPrompt", "The game asked to trade a high-quality item for a normal-quality request.");
        public static readonly LocString NeedsReward = new("run.needs.reward", "The delivery window did not respond.");
        public static readonly LocString NeedsRewardMismatch = new("run.needs.rewardMismatch", "The delivery window did not match the request and was cancelled: {0}");
        public static readonly LocString NeedsCombat = new("run.needs.combat", "Combat or a duty interrupted the hand-in.");
        public static readonly LocString NeedsHandInFault = new("run.needs.handInFault", "The hand-in stopped on an unexpected error. The log has the details.");
        public static readonly LocString NeedsHandInTimeout = new("run.needs.handInTimeout", "The hand-in took too long and was stopped.");
        public static readonly LocString NeedsTryAgain = new("run.needs.tryAgain", "Press Automate to try again.");
    }
}
