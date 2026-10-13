using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Windows.Components;
using AutoGcSupply.Windows.Sections;
using AutoGcSupply.Windows.Shell;
using Dalamud.Interface;

namespace AutoGcSupply.Windows.Pages;

internal sealed class TodayPage
{
    private const float SwitchRevealMs = 320f;
    private const float HeadlineGap = 18f;
    private const float SectionGap = 12f;
    private const float ListGap = 20f;

    public void Draw(Plugin plugin, AppWindow window)
    {
        var controller = plugin.Controller;
        var showRun = controller.Running || controller.Run.End != RunEnd.None;
        using var reveal = Motion.PushSwitch("##agcs_today_state", showRun, SwitchRevealMs);
        if (showRun)
        {
            RunPanel.Draw(plugin);
            return;
        }

        DrawRequests(plugin, window);
    }

    private static void DrawRequests(Plugin plugin, AppWindow window)
    {
        var controller = plugin.Controller;
        var today = controller.Today;
        if (TodayHeadline.Draw(today, plugin.History))
        {
            window.Show(AppWindow.Page.Plugins);
        }

        Styling.VSpace(HeadlineGap);
        using var state = Motion.PushSwitch("##agcs_today_requests_state", (int)today.State);
        switch (today.State)
        {
            case TodayState.NotLoggedIn:
                EmptyState.Draw(FontAwesomeIcon.UserClock, Loc.T(L.Requests.EmptyNotLoggedIn));
                return;
            case TodayState.NoGrandCompany:
                EmptyState.Draw(FontAwesomeIcon.Flag, Loc.T(L.Requests.EmptyNoGrandCompany));
                return;
        }

        if (today.Header.GrandCompany != 0)
        {
            GrandCompanyCard.Draw(controller, today.Header);
            Styling.VSpace(SectionGap);
        }

        switch (today.State)
        {
            case TodayState.Reading:
                TodayStates.DrawReading();
                return;
            case TodayState.NotLoaded:
                TodayStates.DrawNotLoaded(controller);
                return;
            case TodayState.AllDone:
                TodayStates.DrawAllDone(today.Header);
                return;
        }

        if (ShopperStrip.Draw(controller, today))
        {
            window.Show(AppWindow.Page.Plugins);
        }

        Styling.VSpace(SectionGap);
        TotalsTiles.Draw(today);
        if (today.Totals.ToBuy > 0 && today.Shopper == ShopperState.Available)
        {
            Styling.VSpace(SectionGap);
            SpendCapCard.Draw(controller, today);
        }

        Styling.VSpace(ListGap);
        RequestTable.Draw(controller, today);
        Styling.VSpace(SectionGap);
    }
}
