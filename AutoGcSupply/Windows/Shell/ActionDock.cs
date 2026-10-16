using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Tasks;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Windows.Components;
using AutoGcSupply.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoGcSupply.Windows.Shell;

internal static class ActionDock
{
    private const float PadX = 18f;
    private const float ButtonGap = 8f;
    private const int SublabelKeyShift = 16;

    private static CachedText sublabelText;

    public static void Draw(Plugin plugin, Vector2 size, float windowRounding)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var drawList = ImGui.GetWindowDrawList();
        Dock.Background(drawList, origin, end, windowRounding);

        var padX = PadX * scale;
        var buttonHeight = Layout.HeroButtonHeight * scale;
        var innerWidth = size.X - padX * 2f;
        ImGui.SetCursorScreenPos(new Vector2(origin.X + padX, origin.Y + (size.Y - buttonHeight) * 0.5f));

        var controller = plugin.Controller;
        if (controller.Running)
        {
            DrawRunControls(controller, innerWidth);
        }
        else
        {
            DrawAutomate(controller, innerWidth, buttonHeight);
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
    }

    private static void DrawRunControls(GcRunController controller, float innerWidth)
    {
        var gap = ButtonGap * ImGuiHelpers.GlobalScale;
        var half = (innerWidth - gap) * 0.5f;
        var origin = ImGui.GetCursorScreenPos();
        var canPause = ReadyState.CanPause(controller);
        if (PauseButton.Draw(controller.PauseReason, canPause, half))
        {
            controller.TogglePause();
        }

        if (!canPause && Hit.HoveringRect(origin, origin + new Vector2(half, Layout.HeroButtonHeight * ImGuiHelpers.GlobalScale)))
        {
            Tooltip.Show(ReadyState.PauseBlockedReason(controller));
        }

        ImGui.SameLine(0f, gap);
        var state = controller.Phase == RunPhase.Stopping ? Loc.T(L.Automate.StateStopping)
            : controller.Paused ? Loc.T(L.Dock.StatePaused)
            : Loc.T(L.Dock.StateRunning);
        if (StopButton.Draw(state, half))
        {
            controller.Stop();
        }
    }

    private static void DrawAutomate(GcRunController controller, float innerWidth, float height)
    {
        var today = controller.Today;
        var block = today.StartBlock;
        var settling = controller.Settling;
        var enabled = block == StartBlock.None && !settling;
        var origin = ImGui.GetCursorScreenPos();
        var reason = settling ? Loc.T(L.Run.StatusClosingList) : RunLabels.BlockReason(block);
        if (StartButton.Draw(Sublabel(today), enabled, reason, innerWidth))
        {
            controller.Start();
        }

        if (!enabled && Hit.HoveringRect(origin, origin + new Vector2(innerWidth, height)))
        {
            Tooltip.Show(reason);
        }
    }

    private static string Sublabel(TodayModel today)
    {
        var totals = today.Totals;
        if (totals.ToHandIn == 0 && totals.ToBuy == 0)
        {
            return Loc.T(L.Dock.AutomateSub);
        }

        var key = ((long)totals.ToBuy << SublabelKeyShift) | (uint)totals.ToHandIn;
        return sublabelText.Get(key, static packed =>
        {
            var toBuy = (int)(packed >> SublabelKeyShift);
            var toHandIn = (int)(packed & 0xFFFF);
            return toBuy > 0 ? Loc.T(L.Automate.SubBuyAndHandIn, toBuy, toHandIn) : Loc.T(L.Automate.SubHandIn, toHandIn);
        });
    }
}
