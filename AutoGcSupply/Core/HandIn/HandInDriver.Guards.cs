using AutoGcSupply.Core.Game.Ops;
using AutoGcSupply.Core.HandIn.Model;
using System.Threading.Tasks;

namespace AutoGcSupply.Core.HandIn;

internal sealed partial class HandInDriver
{
    private const int ClickWaitMs = 3_000;

    private async Task<Halt> HaltOnUnexpected(bool talkUnexpected)
    {
        if (UnexpectedAddons.CombatOrDutyPop())
        {
            return new Halt(HandInPauseReason.InCombatOrDuty, "combat or a duty finder pop during the hand-in");
        }

        if (NpcInteraction.SelectYesnoOpen())
        {
            var text = NpcInteraction.SelectYesnoText();
            await DeclinePrompt();
            return new Halt(HandInPauseReason.UnexpectedPrompt, text);
        }

        if (!UnexpectedAddons.TryFind(talkUnexpected, out var addonName))
        {
            return Halt.None;
        }

        if (addonName != UnexpectedAddons.TalkAddonName)
        {
            await CloseAddon(addonName);
        }

        return new Halt(HandInPauseReason.UnexpectedAddon, addonName);
    }

    private async Task DeclinePrompt()
    {
        Warn($"Hand-in: answering No to '{NpcInteraction.SelectYesnoText()}'");
        if (!await RepeatUntil(NpcInteraction.AnswerNo, () => !NpcInteraction.SelectYesnoOpen(), ClickWaitMs))
        {
            Warn("Hand-in: the prompt did not close after No");
        }
    }

    private async Task CloseAddon(string addonName)
    {
        if (!NpcInteraction.IsAddonReady(addonName))
        {
            return;
        }

        Diag($"Hand-in: closing {addonName}");
        if (!await RepeatUntil(() => NpcInteraction.Close(addonName), () => !NpcInteraction.IsAddonReady(addonName), ClickWaitMs))
        {
            Warn($"Hand-in: {addonName} did not close");
        }
    }

    private async Task<bool> ClickOnce(Func<bool> click)
    {
        var deadline = Environment.TickCount64 + ClickWaitMs;
        while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
        {
            if (click())
            {
                return true;
            }

            await NextFrame();
        }

        return false;
    }

    private async Task<bool> RepeatUntil(Func<bool> click, Func<bool> done, int timeoutMs)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
        {
            if (done())
            {
                return true;
            }

            click();
            await DelayMs(PollMs);
        }

        return done();
    }
}
