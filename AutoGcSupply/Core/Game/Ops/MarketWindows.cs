using ECommons.DalamudServices;
using System.Threading.Tasks;

namespace AutoGcSupply.Core.Game.Ops;

internal static class MarketWindows
{
    private const string SearchAddonName = "ItemSearch";
    private const string ResultAddonName = "ItemSearchResult";
    private const string PromptAddonName = "SelectYesno";
    private const int CancelCallback = -1;
    private const int CloseWaitMs = 5_000;
    private const int PollFrames = 2;

    public static bool AnyOpen()
        => NpcInteraction.IsAddonReady(SearchAddonName) || NpcInteraction.IsAddonReady(ResultAddonName);

    public static async Task<bool> CloseAll(Func<bool> stopped)
    {
        if (!AnyOpen())
        {
            return true;
        }

        RunLog.Info("Market windows: closing the market board windows left open");
        var deadline = Environment.TickCount64 + CloseWaitMs;
        while (Environment.TickCount64 < deadline && !stopped())
        {
            CloseOne();
            await Svc.Framework.DelayTicks(PollFrames);
            if (!AnyOpen())
            {
                return true;
            }
        }

        if (!stopped())
        {
            RunLog.Warning($"Market windows: the market board windows did not close within {CloseWaitMs / TimeUnits.MillisecondsPerSecond}s");
        }

        return false;
    }

    private static void CloseOne()
    {
        if (NpcInteraction.SelectYesnoOpen())
        {
            NpcInteraction.FireCallback(PromptAddonName, CancelCallback);
            return;
        }

        if (NpcInteraction.IsAddonReady(ResultAddonName))
        {
            NpcInteraction.FireCallback(ResultAddonName, CancelCallback);
            return;
        }

        NpcInteraction.Close(SearchAddonName);
    }
}
