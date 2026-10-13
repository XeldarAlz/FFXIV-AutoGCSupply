namespace AutoGcSupply.Core.Localization;

internal static partial class L
{
    internal static class Automate
    {
        public static readonly LocString SubBuyAndHandIn = new("automate.subBuyAndHandIn", "buy {0}, hand in {1}");
        public static readonly LocString SubHandIn = new("automate.subHandIn", "hand in {0}");
        public static readonly LocString BlockRunning = new("automate.block.running", "a run is under way");
        public static readonly LocString BlockSetup = new("automate.block.setup", "install the required plugins");
        public static readonly LocString BlockLoggedOut = new("automate.block.loggedOut", "log in first");
        public static readonly LocString BlockNoGrandCompany = new("automate.block.noGrandCompany", "join a Grand Company first");
        public static readonly LocString BlockNotOnHomeWorld = new("automate.block.notOnHomeWorld", "return to your home world first");
        public static readonly LocString BlockInDuty = new("automate.block.inDuty", "leave the duty first");
        public static readonly LocString BlockNotLoaded = new("automate.block.notLoaded", "load today's requests first");
        public static readonly LocString BlockNothingSelected = new("automate.block.nothingSelected", "choose at least one request");
        public static readonly LocString BlockNothingToDo = new("automate.block.nothingToDo", "nothing to hand in today");
        public static readonly LocString BlockPricesPending = new("automate.block.pricesPending", "waiting for prices");
        public static readonly LocString BlockPricesFailed = new("automate.block.pricesFailed", "prices failed: retry them or leave out the items to buy");
        public static readonly LocString BlockNeedsSpendCap = new("automate.block.needsSpendCap", "set a run spend cap");
        public static readonly LocString BlockShopperNeeded = new("automate.block.shopperNeeded", "Auto Shopper is needed to buy");
        public static readonly LocString BlockShopperOutdated = new("automate.block.shopperOutdated", "update Auto Shopper to 1.1.0.0 or later");
        public static readonly LocString BlockUpdateNeeded = new("automate.block.updateNeeded", "update Auto GC Supply to work with this Auto Shopper");
        public static readonly LocString DryRun = new("automate.dryRun", "Dry run");
        public static readonly LocString DryRunOnCaption = new("automate.dryRunOnCaption", "buys nothing");
        public static readonly LocString DryRunOffCaption = new("automate.dryRunOffCaption", "spends real gil");
        public static readonly LocString DryRunTileHelp = new("automate.dryRunTileHelp", "Click to change it in Auto Shopper.");
        public static readonly LocString PauseInShopper = new("automate.pauseInShopper", "in Auto Shopper");
        public static readonly LocString PauseInShopperHelp = new("automate.pauseInShopperHelp", "Auto Shopper owns pausing while it buys. Pause it there, or stop the run.");
        public static readonly LocString StateStopping = new("automate.stateStopping", "stopping");
    }
}
