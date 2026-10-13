namespace AutoGcSupply.Core.Localization;

internal static partial class L
{
    internal static class Requests
    {
        public static readonly LocString GreetingMorning = new("requests.greeting.morning", "Good morning");
        public static readonly LocString GreetingAfternoon = new("requests.greeting.afternoon", "Good afternoon");
        public static readonly LocString GreetingEvening = new("requests.greeting.evening", "Good evening");
        public static readonly LocString GreetingNight = new("requests.greeting.night", "Late night");

        public static readonly LocString TitleNotLoggedIn = new("requests.title.notLoggedIn", "Not logged in.");
        public static readonly LocString TitleNoGrandCompany = new("requests.title.noGrandCompany", "No Grand Company yet.");
        public static readonly LocString TitleReading = new("requests.title.reading", "Reading today's requests.");
        public static readonly LocString TitleNotLoaded = new("requests.title.notLoaded", "Requests not loaded yet.");
        public static readonly LocString TitleAllDone = new("requests.title.allDone", "All done for today.");
        public static readonly LocString TitleReady = new("requests.title.ready", "Ready to deliver.");
        public static readonly LocString TitleBlocked = new("requests.title.blocked", "Not ready yet.");
        public static readonly LocString DetailNotLoggedIn = new("requests.detail.notLoggedIn", "Today's requests are read from your character once you are in the game.");
        public static readonly LocString DetailNoGrandCompany = new("requests.detail.noGrandCompany", "Enlist with a Grand Company to take its supply and provisioning missions.");
        public static readonly LocString DetailReading = new("requests.detail.reading", "This takes a moment.");
        public static readonly LocString DetailNotLoaded = new("requests.detail.notLoaded", "The game sends them when the Timers window opens.");
        public static readonly LocString DetailAllDone = new("requests.detail.allDone", "Every request is handed in. New ones arrive at {0}.");
        public static readonly LocPlural DetailReady = new("requests.detail.ready", "Automate buys what is missing and hands in {0} request.", "Automate buys what is missing and hands in {0} requests.");
        public static readonly LocString DetailBlocked = new("requests.detail.blocked", "Automate is off: {0}.");
        public static readonly LocString OpenPlugins = new("requests.openPlugins", "Open Plugins");
        public static readonly LocString LastRun = new("requests.lastRun", "Last run");
        public static readonly LocString LastRunDetail = new("requests.lastRunDetail", "{0}  ·  {1} of {2} delivered");
        public static readonly LocString NoRunsYet = new("requests.noRunsYet", "No runs yet");
        public static readonly LocString ResultsAppearHere = new("requests.resultsAppearHere", "Your results appear here");

        public static readonly LocString Rank = new("requests.rank", "Rank {0}");
        public static readonly LocString Seals = new("requests.seals", "Seals");
        public static readonly LocString SealsValue = new("requests.sealsValue", "{0} / {1}");
        public static readonly LocString SealsHelp = new("requests.sealsHelp", "The Grand Company seals you hold and the most your rank allows. Seals over the cap are lost.");
        public static readonly LocString ResetsIn = new("requests.resetsIn", "Resets in");
        public static readonly LocString ResetHelp = new("requests.resetHelp", "New requests arrive every day at {0} your time (20:00 UTC).");
        public static readonly LocString Refresh = new("requests.refresh", "Read today's requests again");

        public static readonly LocString ShopperLabel = new("requests.shopper.label", "Auto Shopper");
        public static readonly LocString ShopperMissing = new("requests.shopper.missing", "Not installed. Items you already own can still be handed in.");
        public static readonly LocString ShopperInstall = new("requests.shopper.install", "Install Auto Shopper to buy missing items");
        public static readonly LocString ShopperOutdated = new("requests.shopper.outdated", "This version cannot take orders. Update it to buy missing items.");
        public static readonly LocString ShopperUpdate = new("requests.shopper.update", "Update");
        public static readonly LocString ShopperUpdateHelp = new("requests.shopper.updateHelp", "Update Auto Shopper from /xlplugins, then come back here.");
        public static readonly LocString ShopperReady = new("requests.shopper.ready", "Buys what is missing, within your caps.");
        public static readonly LocString DryRunOn = new("requests.dryRun.on", "Dry run on");
        public static readonly LocString DryRunOff = new("requests.dryRun.off", "Dry run off");
        public static readonly LocString DryRunOnHelp = new("requests.dryRun.onHelp", "Dry run follows Auto Shopper's switch. While it is on, a run buys nothing and hands nothing in; it shows what it would do.");
        public static readonly LocString DryRunOffHelp = new("requests.dryRun.offHelp", "Dry run is off in Auto Shopper: a run spends real gil, up to the run spend cap, and hands the items in.");
        public static readonly LocString ChangeInShopper = new("requests.changeInShopper", "Change in Auto Shopper");
        public static readonly LocString PricingPending = new("requests.pricing.pending", "Pricing…");
        public static readonly LocString PricingPendingHelp = new("requests.pricing.pendingHelp", "Auto Shopper is fetching market prices for today's items.");
        public static readonly LocString PricingReady = new("requests.pricing.ready", "Priced");
        public static readonly LocString PricingReadyHelp = new("requests.pricing.readyHelp", "Costs come from Auto Shopper's market prices for the worlds it is allowed to visit.");
        public static readonly LocString PricingFailed = new("requests.pricing.failed", "Prices failed");
        public static readonly LocString PricingFailedHelp = new("requests.pricing.failedHelp", "Auto Shopper could not price today's items. Retry, or hand in only what you own.");
        public static readonly LocString PricingRetry = new("requests.pricing.retry", "Retry");

        public static readonly LocString NotLoadedBody = new("requests.notLoaded.body", "Today's requests have not reached the game yet. They load when the Timers window opens; Auto GC Supply can open it for a moment and read them.");
        public static readonly LocString Load = new("requests.notLoaded.load", "Load today's requests");
        public static readonly LocString EmptyNotLoggedIn = new("requests.empty.notLoggedIn", "Log in to see today's requests.");
        public static readonly LocString EmptyNoGrandCompany = new("requests.empty.noGrandCompany", "Supply and provisioning missions open once you join a Grand Company.");
        public static readonly LocString Reading = new("requests.reading", "Reading today's requests…");
        public static readonly LocString AllDoneTitle = new("requests.allDone.title", "Every request delivered!");
        public static readonly LocString AllDoneBody = new("requests.allDone.body", "New requests arrive at {0}, in {1}.");

        public static readonly LocString TileSelected = new("requests.tile.selected", "Selected");
        public static readonly LocString TileSelectedSub = new("requests.tile.selectedSub", "of {0}");
        public static readonly LocString TileToBuy = new("requests.tile.toBuy", "To buy");
        public static readonly LocString TileToHandIn = new("requests.tile.toHandIn", "To hand in");
        public static readonly LocString TileSpend = new("requests.tile.spend", "Est. spend");
        public static readonly LocString TileExperience = new("requests.tile.experience", "Experience");
        public static readonly LocString TileSeals = new("requests.tile.seals", "Seals");
        public static readonly LocString TileSealsLost = new("requests.tile.sealsLost", "{0} lost");
        public static readonly LocString TileSealsLostHelp = new("requests.tile.sealsLostHelp", "Seals over your rank's cap are lost when these requests are handed in.");

        public static readonly LocString SpendCap = new("requests.spendCap", "Run spend cap");
        public static readonly LocString SpendCapHelp = new("requests.spendCapHelp", "The most gil this run may spend in Auto Shopper. Buying stops before it would go over.");
        public static readonly LocString SpendCapHint = new("requests.spendCapHint", "Set a cap");
        public static readonly LocString UseSuggested = new("requests.useSuggested", "Use estimate + 20%");
        public static readonly LocString UseSuggestedHelp = new("requests.useSuggestedHelp", "Sets the cap to {0}: the estimate plus room for prices that move.");
        public static readonly LocString SpendCapCovers = new("requests.spendCapCovers", "Covers the estimated {0}.");
        public static readonly LocString SpendCapBelow = new("requests.spendCapBelow", "Below the estimated {0}; some items may stay unbought.");
        public static readonly LocString SpendCapMissing = new("requests.spendCapMissing", "Needed before anything can be bought.");

        public static readonly LocString GroupSupply = new("requests.group.supply", "Supply");
        public static readonly LocString GroupProvisioning = new("requests.group.provisioning", "Provisioning");
        public static readonly LocString SelectAll = new("requests.selectAll", "All");
        public static readonly LocString SelectNone = new("requests.selectNone", "None");
        public static readonly LocString SelectAllHelp = new("requests.selectAllHelp", "Include every open request in this group.");
        public static readonly LocString SelectNoneHelp = new("requests.selectNoneHelp", "Leave out every request in this group.");
        public static readonly LocString ColumnClass = new("requests.column.class", "Class");
        public static readonly LocString ColumnRequest = new("requests.column.request", "Request");
        public static readonly LocString ColumnStatus = new("requests.column.status", "Status");
        public static readonly LocString ColumnCost = new("requests.column.cost", "Cost");
        public static readonly LocString ColumnExperience = new("requests.column.experience", "XP");
        public static readonly LocString ColumnSeals = new("requests.column.seals", "Seals");
        public static readonly LocString ColumnValue = new("requests.column.value", "Gil / 1k XP");
        public static readonly LocString CostHelp = new("requests.costHelp", "Estimated gil to buy what is missing.");
        public static readonly LocString ExperienceHelp = new("requests.experienceHelp", "Experience this hand-in pays, and its share of your current level.");
        public static readonly LocString SealsColumnHelp = new("requests.sealsColumnHelp", "Grand Company seals this hand-in pays.");
        public static readonly LocString ValueHelp = new("requests.valueHelp", "Gil spent for every 1,000 experience. Lower is better value.");
        public static readonly LocString Level = new("requests.level", "Lv. {0}");
        public static readonly LocString Quantity = new("requests.quantity", "× {0}");
        public static readonly LocString Owned = new("requests.owned", "Own {0} NQ  ·  {1} HQ");
        public static readonly LocString OwnedNone = new("requests.ownedNone", "None owned");
        public static readonly LocString LevelShare = new("requests.levelShare", "{0}% of a level");
        public static readonly LocString StarHelp = new("requests.starHelp", "Starred: this request pays a bonus today.");
        public static readonly LocString QualityHigh = new("requests.quality.high", "HQ");
        public static readonly LocString QualityNormal = new("requests.quality.normal", "NQ");
        public static readonly LocString QualityHighHelp = new("requests.quality.highHelp", "Hands in high quality. HQ supply pays double experience and seals.");
        public static readonly LocString QualityNormalHelp = new("requests.quality.normalHelp", "Hands in normal quality.");

        public static readonly LocString CoverageOwned = new("requests.coverage.owned", "Owned");
        public static readonly LocString CoverageToBuy = new("requests.coverage.toBuy", "To buy");
        public static readonly LocString CoverageUnpriced = new("requests.coverage.unpriced", "Unpriced");
        public static readonly LocString CoverageNoShopper = new("requests.coverage.noShopper", "Not buyable");
        public static readonly LocString CoverageNoListings = new("requests.coverage.noListings", "No listings");
        public static readonly LocString CoverageDelivered = new("requests.coverage.delivered", "Delivered");
        public static readonly LocString CoverageExcluded = new("requests.coverage.excluded", "Excluded");
        public static readonly LocString CoverageMaxLevel = new("requests.coverage.maxLevel", "Max level");
        public static readonly LocString CoverageOwnedHelp = new("requests.coverage.ownedHelp", "Your bags hold enough. It is handed in without buying.");
        public static readonly LocString CoverageToBuyHelp = new("requests.coverage.toBuyHelp", "Auto Shopper buys {0} for about {1}, at most {2} each.");
        public static readonly LocString CoverageUnpricedHelp = new("requests.coverage.unpricedHelp", "No price yet for this item. Without one it is not bought.");
        public static readonly LocString CoverageNoShopperHelp = new("requests.coverage.noShopperHelp", "Auto Shopper cannot buy right now, so this item is not bought. The Auto Shopper line above says why.");
        public static readonly LocString CoverageNoListingsHelp = new("requests.coverage.noListingsHelp", "No listing on your worlds covers this request right now.");
        public static readonly LocString CoverageDeliveredHelp = new("requests.coverage.deliveredHelp", "Already handed in today.");
        public static readonly LocString CoverageExcludedHelp = new("requests.coverage.excludedHelp", "Left out of this run.");
        public static readonly LocString CoverageMaxLevelHelp = new("requests.coverage.maxLevelHelp", "This class is at the level cap and earns seals only. Turn on Include max-level classes in Settings to deliver it.");
        public static readonly LocString ClickToInclude = new("requests.clickToInclude", "Click to include it.");
        public static readonly LocString ClickToExclude = new("requests.clickToExclude", "Click to leave it out.");
    }
}
