namespace AutoGcSupply.Core.Localization;

internal static partial class L
{
    internal static class Runs
    {
        public static readonly LocPlural Summary = new("runs.summary", "{0} recorded run", "{0} recorded runs");
        public static readonly LocString TileRuns = new("runs.tile.runs", "Runs");
        public static readonly LocString TileRunsSub = new("runs.tile.runsSub", "{0} completed");
        public static readonly LocString TileExperience = new("runs.tile.experience", "Experience");
        public static readonly LocString TileSeals = new("runs.tile.seals", "Seals");
        public static readonly LocString TileSpent = new("runs.tile.spent", "Gil spent");
        public static readonly LocString RecentRuns = new("runs.recentRuns", "Recent runs");
        public static readonly LocString Delivered = new("runs.delivered", "{0} of {1} delivered");
        public static readonly LocString Experience = new("runs.experience", "{0} XP");
        public static readonly LocString Seals = new("runs.seals", "{0} seals");
        public static readonly LocString Spent = new("runs.spent", "{0} spent");
        public static readonly LocString WouldSpend = new("runs.wouldSpend", "{0} would be spent");
        public static readonly LocString DryRun = new("runs.dryRun", "Dry run");
        public static readonly LocString OutcomeCompleted = new("runs.outcome.completed", "Completed");
        public static readonly LocString OutcomePartial = new("runs.outcome.partial", "Partly done");
        public static readonly LocString OutcomeStopped = new("runs.outcome.stopped", "Stopped");
        public static readonly LocString OutcomeNeedsPlayer = new("runs.outcome.needsPlayer", "Needs you");
        public static readonly LocString OutcomeFailed = new("runs.outcome.failed", "Error");
        public static readonly LocString TooltipTimes = new("runs.tooltipTimes", "Started {0}, ended {1}");
        public static readonly LocString TooltipSealsLost = new("runs.tooltipSealsLost", "{0} seals lost to the cap");
        public static readonly LocString Clear = new("runs.clear", "Clear history");
        public static readonly LocString ClearQuestion = new("runs.clearQuestion", "Delete all recorded runs?");
        public static readonly LocString ClearYes = new("runs.clearYes", "Yes, clear");
        public static readonly LocString Cancel = new("runs.cancel", "Cancel");
    }
}
