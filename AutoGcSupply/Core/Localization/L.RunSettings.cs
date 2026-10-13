namespace AutoGcSupply.Core.Localization;

internal static partial class L
{
    internal static class RunSettings
    {
        public static readonly LocString Tab = new("runSettings.tab", "Run");
        public static readonly LocString TabSub = new("runSettings.tabSub", "What a run delivers, what it buys and how it hands in.");
        public static readonly LocString GroupRequests = new("runSettings.group.requests", "Requests");
        public static readonly LocString IncludeMaxLevel = new("runSettings.includeMaxLevel", "Include max-level classes (seals only)");
        public static readonly LocString IncludeMaxLevelHelp = new("runSettings.includeMaxLevelHelp", "Deliver for classes at the level cap too. They earn seals only, no experience.");
        public static readonly LocString UseOwned = new("runSettings.useOwned", "Use items I already own");
        public static readonly LocString UseOwnedHelp = new("runSettings.useOwnedHelp", "Hand in matching items from your bags before buying any. Equipped gear, the armoury chest, retainers and saddlebags are never used.");
        public static readonly LocString GroupBuying = new("runSettings.group.buying", "Buying");
        public static readonly LocString Quality = new("runSettings.quality", "Supply quality");
        public static readonly LocString QualityHelp = new("runSettings.qualityHelp", "Which quality to buy and hand in for crafter supply requests. Provisioning takes any quality.");
        public static readonly LocString QualityAutomatic = new("runSettings.quality.automatic", "Automatic");
        public static readonly LocString QualityAutomaticDetail = new("runSettings.quality.automaticDetail", "HQ when it costs at most twice NQ; HQ supply pays double.");
        public static readonly LocString QualityHigh = new("runSettings.quality.high", "Always HQ");
        public static readonly LocString QualityHighDetail = new("runSettings.quality.highDetail", "Buy and hand in high quality only.");
        public static readonly LocString QualityNormal = new("runSettings.quality.normal", "Always NQ");
        public static readonly LocString QualityNormalDetail = new("runSettings.quality.normalDetail", "Buy and hand in normal quality only.");
        public static readonly LocString Tolerance = new("runSettings.tolerance", "Price tolerance over the estimate");
        public static readonly LocString ToleranceHelp = new("runSettings.toleranceHelp", "How far above the estimated unit price a purchase may go. Higher buys more reliably when prices move; lower spends less.");
        public static readonly LocString ToleranceFormat = new("runSettings.toleranceFormat", "%d%%");
        public static readonly LocString DefaultSpendCap = new("runSettings.defaultSpendCap", "Default run spend cap");
        public static readonly LocString DefaultSpendCapHelp = new("runSettings.defaultSpendCapHelp", "The spend cap the Today page starts with. Leave it empty to set one before each run that buys.");
        public static readonly LocString DefaultSpendCapHint = new("runSettings.defaultSpendCapHint", "Not set");
        public static readonly LocString GroupHandIn = new("runSettings.group.handIn", "Hand-in");
        public static readonly LocString SealCap = new("runSettings.sealCap", "When seals would pass the cap");
        public static readonly LocString SealCapHelp = new("runSettings.sealCapHelp", "Your rank caps how many seals you can hold. Seals over the cap are lost.");
        public static readonly LocString SealCapDeliver = new("runSettings.sealCap.deliver", "Deliver anyway");
        public static readonly LocString SealCapDeliverDetail = new("runSettings.sealCap.deliverDetail", "Hand in for the experience; seals over the cap are lost.");
        public static readonly LocString SealCapSkip = new("runSettings.sealCap.skip", "Skip the row");
        public static readonly LocString SealCapSkipDetail = new("runSettings.sealCap.skipDetail", "Keep the item and skip the request until its seals fit.");
    }
}
