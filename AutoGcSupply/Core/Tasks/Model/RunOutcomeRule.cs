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
    public static RunVerdict AfterHandIn(in HandInResult result, ReadOnlySpan<RowProgress> progress)
    {
        if (result.PauseReason == HandInPauseReason.DeliveryNotConfirmed || Any(progress, RowRunState.Unconfirmed))
        {
            return RunVerdict.NeedsPlayer(NeedsPlayerReason.DeliveryUnconfirmed);
        }

        return result.Outcome switch
        {
            HandInOutcome.Paused => RunVerdict.NeedsPlayer(NeedsPlayerReason.HandInPaused),
            HandInOutcome.Cancelled => new RunVerdict(RunEnd.Stopped, NeedsPlayerReason.None),
            _ => Completion(progress),
        };
    }

    public static RunVerdict Completion(ReadOnlySpan<RowProgress> progress)
    {
        var delivered = 0;
        for (var index = 0; index < progress.Length; index++)
        {
            if (progress[index].State == RowRunState.Delivered)
            {
                delivered++;
            }
        }

        var end = progress.Length > 0 && delivered == progress.Length ? RunEnd.Completed : RunEnd.CompletedPartial;
        return new RunVerdict(end, NeedsPlayerReason.None);
    }

    public static RunSummary Summarize(ReadOnlySpan<RowDecision> authorized, ReadOnlySpan<RowProgress> progress, long gilSpent, TimeSpan duration)
    {
        var delivered = 0;
        var experience = 0L;
        var seals = 0L;
        var sealsLost = 0L;
        for (var index = 0; index < progress.Length; index++)
        {
            if (progress[index].State != RowRunState.Delivered)
            {
                continue;
            }

            delivered++;
            experience = Gil.SaturatingAdd(experience, authorized[index].Experience);
            seals = Gil.SaturatingAdd(seals, authorized[index].Seals);
            sealsLost = Gil.SaturatingAdd(sealsLost, progress[index].SealsLost);
        }

        return new RunSummary(delivered, authorized.Length, experience, seals, sealsLost, gilSpent, duration);
    }

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
