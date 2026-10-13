using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Shared;
using AutoGcSupply.Core.Value.Model;

namespace AutoGcSupply.Core.Tasks.Model;

internal readonly record struct RunVerdict(RunEnd End, NeedsPlayerReason Reason)
{
    public static RunVerdict NeedsPlayer(NeedsPlayerReason reason) => new(RunEnd.NeedsPlayer, reason);
}

internal static class RunOutcomeRule
{
    public static RunVerdict AfterHandIn(in HandInResult result, ReadOnlySpan<RowProgress> progress, bool dryRun)
    {
        if (result.PauseReason == HandInPauseReason.DeliveryNotConfirmed || Any(progress, RowRunState.Unconfirmed))
        {
            return RunVerdict.NeedsPlayer(NeedsPlayerReason.DeliveryUnconfirmed);
        }

        return result.Outcome switch
        {
            HandInOutcome.Paused => RunVerdict.NeedsPlayer(NeedsPlayerReason.HandInPaused),
            HandInOutcome.Cancelled => new RunVerdict(RunEnd.Stopped, NeedsPlayerReason.None),
            _ => Completion(progress, dryRun),
        };
    }

    public static RunVerdict Completion(ReadOnlySpan<RowProgress> progress, bool dryRun)
    {
        var settled = 0;
        for (var index = 0; index < progress.Length; index++)
        {
            if (Counts(progress[index], dryRun))
            {
                settled++;
            }
        }

        var end = progress.Length > 0 && settled == progress.Length ? RunEnd.Completed : RunEnd.CompletedPartial;
        return new RunVerdict(end, NeedsPlayerReason.None);
    }

    public static RunSummary Summarize(ReadOnlySpan<RowDecision> authorized, ReadOnlySpan<RowProgress> progress, long gilSpent, bool drySpend, TimeSpan duration, bool dryRun)
    {
        var delivered = 0;
        var experience = 0L;
        var seals = 0L;
        var sealsLost = 0L;
        for (var index = 0; index < progress.Length; index++)
        {
            if (!Counts(progress[index], dryRun))
            {
                continue;
            }

            delivered++;
            experience = Gil.SaturatingAdd(experience, authorized[index].Experience);
            seals = Gil.SaturatingAdd(seals, authorized[index].Seals);
            sealsLost = Gil.SaturatingAdd(sealsLost, progress[index].SealsLost);
        }

        return new RunSummary(delivered, authorized.Length, experience, seals, sealsLost, gilSpent, drySpend, duration, dryRun);
    }

    public static RunSummary ForHistory(ReadOnlySpan<RowDecision> authorized, ReadOnlySpan<RowProgress> progress, in RunSummary shown)
    {
        if (!shown.DryRun)
        {
            return shown;
        }

        var realSpend = shown.DrySpend ? 0 : shown.GilSpent;
        if (realSpend == 0 && !Any(progress, RowRunState.Delivered))
        {
            return shown;
        }

        return Summarize(authorized, progress, realSpend, drySpend: false, shown.Duration, dryRun: false);
    }

    private static bool Counts(in RowProgress progress, bool dryRun)
        => progress.State == RowRunState.Delivered || (dryRun && progress.State == RowRunState.WouldDeliver);

    private static bool Any(ReadOnlySpan<RowProgress> progress, RowRunState state)
    {
        for (var index = 0; index < progress.Length; index++)
        {
            if (progress[index].State == state)
            {
                return true;
            }
        }

        return false;
    }
}
