using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Stats;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoGcSupply.Windows.Sections;

internal static class TodayHeadline
{
    private const float GreetingGap = 8f;
    private const float DetailGap = 6f;
    private const float RightGap = 18f;
    private const float PluginsButtonHeight = 30f;
    private const int StartBlockShift = 8;
    private const int HandInShift = 16;
    private const int ResetShift = 24;
    private const int RevisionShift = 32;
    private const long LastRunRefreshMs = 60_000;

    private static CachedText detailText;
    private static CachedText lastRunText;

    public static bool Draw(TodayModel today, RunHistory history)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var y = origin.Y;

        var (icon, color, greeting) = Greeting();
        using (Fonts.PushCaption())
        {
            var greetingSize = TextDraw.Measure(greeting);
            var iconSize = TextDraw.IconSize(icon);
            TextDraw.Icon(icon, new Vector2(origin.X + 1f * scale, y + (greetingSize.Y - iconSize.Y) * 0.5f), color);
            TextDraw.At(greeting, new Vector2(origin.X + iconSize.X + 8f * scale, y), Styling.TextDim);
            y += greetingSize.Y + GreetingGap * scale;
        }

        float titleHeight;
        using (Fonts.PushTitle())
        {
            titleHeight = ImGui.GetTextLineHeight();
        }

        var detailHeight = ImGui.GetTextLineHeight();
        var blockMidY = y + (titleHeight + DetailGap * scale + detailHeight) * 0.5f;
        var stacked = width < 640f * scale;
        var openPlugins = false;
        var rightWidth = stacked ? 0f : DrawRightColumn(today, history, origin.X + width, blockMidY, out openPlugins);
        var maxTextWidth = stacked ? width : width - rightWidth - RightGap * scale;

        using (Fonts.PushTitle())
        {
            TextDraw.At(TextDraw.Truncate(Title(today), maxTextWidth), new Vector2(origin.X, y), Styling.TextStrong);
        }

        y += titleHeight + DetailGap * scale;
        var detailKey = ((Formatting.NextReset(today.Header.NextResetUtc).Ticks / TimeSpan.TicksPerMinute) << ResetShift)
            | ((long)(byte)today.Totals.ToHandIn << HandInShift) | ((long)today.StartBlock << StartBlockShift) | (long)today.State;
        if (!detailText.TryGet(detailKey, out var detail))
        {
            detail = detailText.Set(detailKey, Detail(today));
        }

        TextDraw.Wrapped(detail, new Vector2(origin.X, y), maxTextWidth, Styling.TextDim);
        y += TextDraw.MeasureWrapped(detail, maxTextWidth).Y;
        if (stacked)
        {
            y += 12f * scale;
            var rightHeight = today.StartBlock == StartBlock.SetupNeeded ? PluginsButtonHeight * scale : TextDraw.CaptionLineHeight() * 2f + 3f * scale;
            DrawRightColumn(today, history, origin.X + width, y + rightHeight * 0.5f, out openPlugins);
            y += rightHeight;
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, y - origin.Y));
        return openPlugins;
    }

    private static (FontAwesomeIcon Icon, Vector4 Color, string Greeting) Greeting() => DateTime.Now.Hour switch
    {
        >= 5 and < 12  => (FontAwesomeIcon.Sun,       Styling.AccentAmber,    Loc.T(L.Requests.GreetingMorning)),
        >= 12 and < 17 => (FontAwesomeIcon.Sun,       Styling.AccentAmber,    Loc.T(L.Requests.GreetingAfternoon)),
        >= 17 and < 22 => (FontAwesomeIcon.CloudMoon, Styling.AccentGlowSoft, Loc.T(L.Requests.GreetingEvening)),
        _              => (FontAwesomeIcon.Moon,      Styling.AccentBlue,     Loc.T(L.Requests.GreetingNight)),
    };

    private static string Title(TodayModel today) => today.State switch
    {
        TodayState.NotLoggedIn => Loc.T(L.Requests.TitleNotLoggedIn),
        TodayState.NoGrandCompany => Loc.T(L.Requests.TitleNoGrandCompany),
        TodayState.Reading => Loc.T(L.Requests.TitleReading),
        TodayState.NotLoaded => Loc.T(L.Requests.TitleNotLoaded),
        TodayState.AllDone => Loc.T(L.Requests.TitleAllDone),
        _ => today.StartBlock == StartBlock.None ? Loc.T(L.Requests.TitleReady) : Loc.T(L.Requests.TitleBlocked),
    };

    private static string Detail(TodayModel today) => today.State switch
    {
        TodayState.NotLoggedIn => Loc.T(L.Requests.DetailNotLoggedIn),
        TodayState.NoGrandCompany => Loc.T(L.Requests.DetailNoGrandCompany),
        TodayState.Reading => Loc.T(L.Requests.DetailReading),
        TodayState.NotLoaded => Loc.T(L.Requests.DetailNotLoaded),
        TodayState.AllDone => Loc.T(L.Requests.DetailAllDone, Formatting.ResetTime(today.Header.NextResetUtc)),
        _ when today.StartBlock == StartBlock.None => Loc.Plural(L.Requests.DetailReady, today.Totals.ToHandIn),
        _ => Loc.T(L.Requests.DetailBlocked, RunLabels.BlockReason(today.StartBlock)),
    };

    private static float DrawRightColumn(TodayModel today, RunHistory history, float rightX, float midY, out bool openPlugins)
    {
        var scale = ImGuiHelpers.GlobalScale;
        openPlugins = false;
        if (today.StartBlock == StartBlock.SetupNeeded)
        {
            var label = Loc.T(L.Requests.OpenPlugins);
            var buttonWidth = PillButton.Width(label, FontAwesomeIcon.Plug);
            ImGui.SetCursorScreenPos(new Vector2(rightX - buttonWidth, midY - PluginsButtonHeight * scale * 0.5f));
            openPlugins = PillButton.Draw("##agcs_today_open_plugins", label, Styling.AccentRose, PillButton.Emphasis.Tinted, FontAwesomeIcon.Plug, height: PluginsButtonHeight);
            return buttonWidth;
        }

        var latest = HistoryOrder.Latest(history);
        var title = latest is null ? Loc.T(L.Requests.NoRunsYet) : Loc.T(L.Requests.LastRun);
        var detail = latest is null ? Loc.T(L.Requests.ResultsAppearHere) : LastRunDetail(latest, history.Revision);
        using (Fonts.PushCaption())
        {
            var titleSize = TextDraw.Measure(title);
            var detailSize = TextDraw.Measure(detail);
            var gap = 3f * scale;
            var top = midY - (titleSize.Y + gap + detailSize.Y) * 0.5f;
            TextDraw.At(title, new Vector2(rightX - titleSize.X, top), Styling.TextSecondary);
            TextDraw.At(detail, new Vector2(rightX - detailSize.X, top + titleSize.Y + gap), Styling.TextDim);
            return MathF.Max(titleSize.X, detailSize.X);
        }
    }

    private static string LastRunDetail(RunRecord record, int revision)
    {
        var key = ((long)revision << RevisionShift) | ((Environment.TickCount64 / LastRunRefreshMs) & uint.MaxValue);
        if (lastRunText.TryGet(key, out var text))
        {
            return text;
        }

        return lastRunText.Set(key, Loc.T(L.Requests.LastRunDetail, Formatting.RelativeTime(record.EndedAtUtc), record.Delivered, record.Planned));
    }
}
