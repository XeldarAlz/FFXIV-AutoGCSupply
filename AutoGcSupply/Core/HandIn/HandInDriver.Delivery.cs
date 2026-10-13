using AutoGcSupply.Core.Game.Ops;
using AutoGcSupply.Core.HandIn.Model;
using System.Threading.Tasks;

namespace AutoGcSupply.Core.HandIn;

internal sealed partial class HandInDriver
{
    private const int RewardOpenTimeoutMs = 10_000;
    private const int DeliverEnableTimeoutMs = 3_000;
    private const int VerifyTimeoutMs = 10_000;
    private const int SealSettleMs = 2_000;
    private const int PromptCloseTimeoutMs = 3_000;

    private DeliverySample activeBefore;
    private SealCapDecision activeDecision;
    private bool highQualityAccepted;
    private bool deliverPressed;

    private async Task<Halt> DeliverMatch(RowMatch match)
    {
        var targetIndex = match.TargetIndex;
        var target = targets[targetIndex];
        var sealsBefore = HandInReads.Seals(grandCompany);
        var decision = SealCapRule.Evaluate(sealsBefore, target.SealReward, sealCap, sealCapPolicy);
        if (decision.Action == SealCapAction.Skip)
        {
            SetRow(targetIndex, rows[targetIndex] with { Status = HandInRowStatus.Skipped, SealCap = SealCapAction.Skip });
            Warn($"Hand-in: skipping {HandInReads.Describe(target)}: it would put {decision.SealsOverCap} seals over the cap of {sealCap}");
            return Halt.None;
        }

        activeBefore = new DeliverySample(stocks[targetIndex].Of(target.Quality), sealsBefore, true);
        activeDecision = decision;
        highQualityAccepted = false;
        deliverPressed = false;
        CurrentTargetIndex = targetIndex;
        SetRow(targetIndex, rows[targetIndex] with { Status = HandInRowStatus.Delivering, SealCap = decision.Action });
        EnterStep(HandInStep.PickingRow, $"Handing in {HandInReads.ItemName(target.ItemId)}");
        Diag($"Hand-in: choosing row {match.RowIndex} for {HandInReads.Describe(target)}; bags hold {activeBefore.Owned}, seals {sealsBefore}/{sealCap}, seal cap {decision.Action}");
        if (!await ClickOnce(() => HandInClicks.ChooseRow(match.RowIndex)))
        {
            SettleActiveRow();
            return CancelToken.IsCancellationRequested
                ? Halt.None
                : new Halt(HandInPauseReason.ListClosed, $"the supply list was not ready for row {match.RowIndex}");
        }

        var halt = await AwaitRewardWindow(target);
        if (!Interrupted(halt))
        {
            halt = await PressDeliver(target);
        }

        if (!Interrupted(halt))
        {
            return await VerifyDelivery();
        }

        SettleActiveRow();
        return halt;
    }

    private async Task<Halt> AwaitRewardWindow(HandInTarget target)
    {
        EnterStep(HandInStep.Confirming, $"Waiting for the reward window for {HandInReads.ItemName(target.ItemId)}");
        var deadline = Environment.TickCount64 + RewardOpenTimeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested || HandInClicks.RewardOpen())
            {
                return Halt.None;
            }

            var halt = await HandlePrompts(target);
            if (halt.Stopped)
            {
                return halt;
            }

            await DelayMs(PollMs);
        }

        return new Halt(HandInPauseReason.RewardWindowDidNotOpen, $"no reward window within {RewardOpenTimeoutMs / TimeUnits.MillisecondsPerSecond}s for {HandInReads.Describe(target)}");
    }

    private async Task<Halt> PressDeliver(HandInTarget target)
    {
        EnterStep(HandInStep.Delivering, $"Delivering {HandInReads.ItemName(target.ItemId)}");
        var halt = await HandlePrompts(target);
        if (Interrupted(halt))
        {
            return halt;
        }

        if (!await WaitUntilTimed(HandInClicks.DeliverEnabled, DeliverEnableTimeoutMs, "hand-in-deliver-enabled", 1))
        {
            return CancelToken.IsCancellationRequested
                ? Halt.None
                : new Halt(HandInPauseReason.DeliverButtonDisabled, $"Deliver stayed disabled for {HandInReads.Describe(target)}");
        }

        if (!await ClickOnce(HandInClicks.PressDeliver))
        {
            return CancelToken.IsCancellationRequested
                ? Halt.None
                : new Halt(HandInPauseReason.DeliverButtonDisabled, $"Deliver could not be pressed for {HandInReads.Describe(target)}");
        }

        deliverPressed = true;
        return Halt.None;
    }

    private async Task<Halt> VerifyDelivery()
    {
        var target = targets[CurrentTargetIndex];
        EnterStep(HandInStep.Verifying, $"Checking the hand-in of {HandInReads.ItemName(target.ItemId)}");
        var deadline = Environment.TickCount64 + VerifyTimeoutMs;
        while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
        {
            var halt = await HandlePrompts(target);
            if (halt.Stopped)
            {
                SettleActiveRow();
                return halt;
            }

            if (!HandInClicks.RewardOpen() && SupplyListReader.IsLoaded())
            {
                var verdict = DeliveryCheck.Judge(activeBefore, Sample(target), target, sealCap, final: false);
                if (verdict == DeliveryVerdict.Confirmed)
                {
                    await WaitForSealsToSettle(target);
                    RecordDelivered(Sample(target));
                    return Halt.None;
                }

                if (verdict == DeliveryVerdict.Contradicted)
                {
                    break;
                }
            }

            await DelayMs(PollMs);
        }

        var after = Sample(target);
        if (DeliveryCheck.Judge(activeBefore, after, target, sealCap, final: true) == DeliveryVerdict.Confirmed)
        {
            RecordDelivered(after);
            return Halt.None;
        }

        var rewardState = HandInClicks.RewardOpen() ? ", the reward window is still open" : string.Empty;
        var detail = $"{HandInReads.Describe(target)}: bags {activeBefore.Owned} -> {after.Owned}, seals {activeBefore.Seals} -> {after.Seals}, row {(after.RowOpen ? "still open" : "closed")}{rewardState}";
        MarkActiveRow(HandInRowStatus.Unconfirmed);
        return CancelToken.IsCancellationRequested ? Halt.None : new Halt(HandInPauseReason.DeliveryNotConfirmed, detail);
    }

    private async Task WaitForSealsToSettle(HandInTarget target)
    {
        if (target.SealReward == 0 || SealCapRule.Overflows(activeBefore.Seals, target.SealReward, sealCap) || HandInReads.Seals(grandCompany) != activeBefore.Seals)
        {
            return;
        }

        await WaitUntilTimed(() => HandInReads.Seals(grandCompany) != activeBefore.Seals, SealSettleMs, "hand-in-seals-settle", 1);
    }

    private async Task<Halt> HandlePrompts(HandInTarget target)
    {
        if (!NpcInteraction.SelectYesnoOpen())
        {
            return await HaltOnUnexpected(talkUnexpected: true);
        }

        var text = NpcInteraction.SelectYesnoText();
        if (!PromptText.Same(text, HighQualityPrompt.Text()))
        {
            await DeclinePrompt();
            return new Halt(HandInPauseReason.UnexpectedPrompt, text);
        }

        if (target.Quality != HandInQuality.High)
        {
            await DeclinePrompt();
            return new Halt(HandInPauseReason.HighQualityPromptForNormalRow, $"{HandInReads.Describe(target)}: {text}");
        }

        if (highQualityAccepted)
        {
            await DeclinePrompt();
            return new Halt(HandInPauseReason.UnexpectedPrompt, $"a second high-quality confirmation for {HandInReads.Describe(target)}: {text}");
        }

        return await AcceptHighQualityPrompt();
    }

    private async Task<Halt> AcceptHighQualityPrompt()
    {
        if (!await ClickOnce(HandInClicks.AcceptPrompt))
        {
            return CancelToken.IsCancellationRequested
                ? Halt.None
                : new Halt(HandInPauseReason.UnexpectedPrompt, "the high-quality confirmation could not be answered");
        }

        highQualityAccepted = true;
        Diag("Hand-in: accepted the high-quality confirmation");
        if (await WaitUntilTimed(() => !NpcInteraction.SelectYesnoOpen(), PromptCloseTimeoutMs, "hand-in-prompt-closed", 1) || CancelToken.IsCancellationRequested)
        {
            return Halt.None;
        }

        var text = NpcInteraction.SelectYesnoText();
        await DeclinePrompt();
        return new Halt(HandInPauseReason.UnexpectedPrompt, $"a confirmation stayed open after the high-quality one was accepted: {text}");
    }

    private DeliverySample Sample(in HandInTarget target)
        => new(HandInReads.CountInBags(target.ItemId).Of(target.Quality), HandInReads.Seals(grandCompany), SupplyListReader.Read().RowStillOpen(target));

    private void SettleActiveRow()
    {
        if (CurrentTargetIndex == NoTarget || rows[CurrentTargetIndex].Status != HandInRowStatus.Delivering)
        {
            return;
        }

        var target = targets[CurrentTargetIndex];
        var after = Sample(target);
        if (DeliveryCheck.Judge(activeBefore, after, target, sealCap, final: true) == DeliveryVerdict.Confirmed)
        {
            RecordDelivered(after);
            return;
        }

        MarkActiveRow(deliverPressed ? HandInRowStatus.Unconfirmed : HandInRowStatus.Pending);
    }

    private void MarkActiveRow(HandInRowStatus status)
    {
        var targetIndex = CurrentTargetIndex;
        CurrentTargetIndex = NoTarget;
        SetRow(targetIndex, rows[targetIndex] with { Status = status });
    }

    private void RecordDelivered(in DeliverySample after)
    {
        var targetIndex = CurrentTargetIndex;
        var target = targets[targetIndex];
        var sealsGained = after.Seals - activeBefore.Seals;
        var sealsLost = activeDecision.Action == SealCapAction.DeliverWithLoss ? activeDecision.SealsOverCap : 0;
        CurrentTargetIndex = NoTarget;
        SetRow(targetIndex, new HandInRowResult(HandInRowStatus.Delivered, RowMatchStatus.Matched, activeDecision.Action, sealsGained, sealsLost));
        var lossText = sealsLost > 0 ? $", {sealsLost} lost to the cap" : string.Empty;
        Diag($"Hand-in: delivered {HandInReads.Describe(target)}; bags {activeBefore.Owned} -> {after.Owned}, seals {activeBefore.Seals} -> {after.Seals}{lossText}");
    }
}
