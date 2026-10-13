using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Tasks;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoGcSupply.Windows.Sections;

internal static class TodayStates
{
    private const float ReadingHeight = 110f;
    private const float RingRadius = 13f;
    private const float RingThickness = 3f;
    private const float PadX = 24f;
    private const float PadY = 20f;
    private const float LineGap = 8f;
    private const float AllDoneHeight = 150f;
    private const float TrophySize = 34f;
    private const long MinutesKeyStride = 4096;

    private static CachedText allDoneText;

    public static void DrawReading()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, ReadingHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var drawList = ImGui.GetWindowDrawList();
        Paint.Surface(drawList, origin, end, Styling.CardRounding * scale, Styling.WithAlpha(Styling.Surface0, 0.6f), Styling.WithAlpha(Styling.BorderDim, 0.5f), topLight: false);

        var center = new Vector2((origin.X + end.X) * 0.5f, origin.Y + size.Y * 0.40f);
        var radius = RingRadius * scale;
        ProgressRing.Track(center, radius, RingThickness * scale, Styling.WithAlpha(Styling.BorderDim, 0.7f));
        ProgressRing.Sweep(center, radius, RingThickness * scale, Styling.AccentBlue, Styling.PulseCalm, MathF.PI * 0.9f, 1f);
        TextDraw.Center(Loc.T(L.Requests.Reading), center.X, center.Y + radius + 12f * scale, Styling.TextDim);
        ImGui.Dummy(size);
    }

    public static void DrawNotLoaded(GcRunController controller)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var padX = PadX * scale;
        var padY = PadY * scale;
        var iconWidth = TextDraw.IconSize(FontAwesomeIcon.Hourglass).X + 12f * scale;
        var body = Loc.T(L.Requests.NotLoadedBody);
        var wrapWidth = MathF.Max(1f, width - padX * 2f - iconWidth);
        var bodyHeight = TextDraw.MeasureWrapped(body, wrapWidth).Y;
        var buttonHeight = Layout.ActionPillHeight * scale;
        var height = padY * 2f + bodyHeight + LineGap * 2f * scale + buttonHeight;
        var end = origin + new Vector2(width, height);
        var drawList = ImGui.GetWindowDrawList();
        Paint.Surface(drawList, origin, end, Styling.CardRounding * scale, Styling.Tint(Styling.WithAlpha(Styling.Surface1, 0.6f), Styling.AccentBlue, 0.08f), Styling.WithAlpha(Styling.AccentBlue, 0.4f), topLight: false);
        TextDraw.Icon(FontAwesomeIcon.Hourglass, new Vector2(origin.X + padX, origin.Y + padY), Styling.AccentBlueSoft);

        var x = origin.X + padX + iconWidth;
        var y = origin.Y + padY;
        TextDraw.Wrapped(body, new Vector2(x, y), wrapWidth, Styling.TextSecondary);
        y += bodyHeight + LineGap * 2f * scale;
        ImGui.SetCursorScreenPos(new Vector2(x, y));
        if (PillButton.Draw("##agcs_today_load", Loc.T(L.Requests.Load), Styling.AccentGlow, PillButton.Emphasis.Filled, FontAwesomeIcon.Download, height: Layout.ActionPillHeight))
        {
            controller.Refresh();
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, height));
    }

    public static void DrawAllDone(in TodayHeader header)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, AllDoneHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var drawList = ImGui.GetWindowDrawList();
        Paint.Glass(drawList, origin, end, Styling.PanelRounding * scale, Styling.AccentMint, 0.08f, 0f, elevated: true);

        var trophyCenter = new Vector2((origin.X + end.X) * 0.5f, origin.Y + size.Y * 0.32f);
        var glow = 0.6f + 0.4f * Styling.Pulse(Styling.PulseBreath);
        ProgressRing.Glow(trophyCenter, TrophySize * scale, Styling.AccentMint, glow);
        ProgressRing.CenterIcon(trophyCenter, FontAwesomeIcon.Trophy, Styling.AccentMintSoft, TrophySize * scale);

        var title = Loc.T(L.Requests.AllDoneTitle);
        float titleHeight;
        using (Fonts.PushHeadline())
        {
            titleHeight = TextDraw.Measure(title).Y;
            TextDraw.Center(title, trophyCenter.X, trophyCenter.Y + TrophySize * scale * 0.5f + 14f * scale, Styling.TextStrong);
        }

        var nextReset = Formatting.NextReset(header.NextResetUtc);
        var minutes = Formatting.MinutesUntilReset(nextReset);
        var key = nextReset.Ticks / TimeSpan.TicksPerMinute * MinutesKeyStride + minutes;
        if (!allDoneText.TryGet(key, out var body))
        {
            body = allDoneText.Set(key, Loc.T(L.Requests.AllDoneBody, Formatting.ResetTime(nextReset), Formatting.Countdown(TimeSpan.FromMinutes(minutes))));
        }

        TextDraw.Center(body, trophyCenter.X, trophyCenter.Y + TrophySize * scale * 0.5f + 14f * scale + titleHeight + 6f * scale, Styling.TextDim);
        ImGui.Dummy(size);
    }
}
