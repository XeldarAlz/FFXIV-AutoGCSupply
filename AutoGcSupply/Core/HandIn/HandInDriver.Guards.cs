using AutoGcSupply.Core.Game.Ops;
using AutoGcSupply.Core.HandIn.Model;
using System.Threading.Tasks;

namespace AutoGcSupply.Core.HandIn;

internal sealed partial class HandInDriver
{
    private const int ClickWaitMs = 3_000;

    private async Task<Halt> HaltOnUnexpected(bool talkUnexpected, bool requestExpected = false)
    {
        if (UnexpectedAddons.CombatOrDutyPop())
        {
            return new Halt(HandInPauseReason.InCombatOrDuty, "combat or a duty finder pop during the hand-in");
        }

        if (NpcInteraction.SelectYesnoOpen())
        {
            var text = NpcInteraction.SelectYesnoText();
            await OfficerWindows.DeclinePrompt(cancelled);
            return new Halt(HandInPauseReason.UnexpectedPrompt, text);
        }

        if (!UnexpectedAddons.TryFind(talkUnexpected, out var addonName, requestExpected))
        {
            return Halt.None;
        }

        if (addonName == RequestHandover.AddonName)
        {
            await OfficerWindows.CloseRequest(cancelled);
        }
        else if (addonName != UnexpectedAddons.TalkAddonName)
        {
            await OfficerWindows.CloseAddon(addonName, cancelled);
        }

        return new Halt(HandInPauseReason.UnexpectedAddon, addonName);
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
}
