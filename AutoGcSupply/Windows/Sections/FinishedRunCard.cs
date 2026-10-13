using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Tasks;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoGcSupply.Windows.Sections;

internal static class FinishedRunCard
{
    private const float CardHeight = 76f;
    private const float PadX = 18f;
    private const float IconBox = 38f;
    private const float BadgeGap = 8f;
    private const float LineGap = 6f;
    private const int TileCount = 5;
    private const float TileGap = 8f;

    private static int textsRevision = -1;
    private static LanguageInfo? textsLanguage;
    private static string detail = string.Empty;
    private static string delivered = string.Empty;
    private static string experience = string.Empty;
    private static string seals = string.Empty;
    private static string? sealsLost;
    private static string spent = string.Empty;
    private static string duration = string.Empty;

    public static void Draw(GcRunController controller, RunModel run)
    {
        Refresh(run);
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, CardHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var drawList = ImGui.GetWindowDrawList();
        var accent = RunLabels.OutcomeColor(run.End);
        Paint.Glass(drawList, origin, end, Styling.PanelRounding * scale, accent, 0.06f, 0f, elevated: true);

        var padX = PadX * scale;
        var midY = origin.Y + size.Y * 0.5f;
        var iconBox = IconBox * scale;
        var iconCenter = new Vector2(origin.X + padX + iconBox * 0.5f, midY);
        drawList.AddCircleFilled(iconCenter, iconBox * 0.5f, Paint.Col(Styling.WithAlpha(accent, 0.16f)));
        TextDraw.IconCentered(FontAwesomeIcon.FlagCheckered, iconCenter, Styling.Lighten(accent, 0.3f));

        var dismiss = Loc.T(L.Running.Dismiss);
        var dismissWidth = PillButton.Width(dismiss, FontAwesomeIcon.Check);
        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - dismissWidth, midY - Layout.ActionPillHeight * scale * 0.5f));
        if (PillButton.Draw("##agcs_run_dismiss", dismiss, Styling.AccentGlow, PillButton.Emphasis.Tinted, FontAwesomeIcon.Check,
                height: Layout.ActionPillHeight, tooltip: Loc.T(L.Running.DismissHelp)))
        {
            controller.Dismiss();
        }

        var x = iconCenter.X + iconBox * 0.5f + 14f * scale;
        var width = MathF.Max(0f, end.X - padX - dismissWidth - 14f * scale - x);
        var lineHeight = ImGui.GetTextLineHeight();
        float titleHeight;
        using (Fonts.PushHeadline())
        {
            titleHeight = ImGui.GetTextLineHeight();
        }

        var top = midY - (titleHeight + LineGap * scale + lineHeight) * 0.5f;
        var title = Loc.T(run.Summary.DryRun ? L.Running.FinishedTitleDry : L.Running.FinishedTitle);
        float titleWidth;
        using (Fonts.PushHeadline())
        {
            var shown = TextDraw.Truncate(title, width);
            titleWidth = TextDraw.Measure(shown).X;
            TextDraw.At(shown, new Vector2(x, top), Styling.TextStrong);
        }

        var badgeX = x + titleWidth + BadgeGap * scale;
        if (badgeX < x + width)
        {
            Badge.DrawLeft(drawList, RunLabels.OutcomeLabel(run.End), accent, badgeX, top + titleHeight * 0.5f);
        }

        TextDraw.At(TextDraw.Truncate(detail, width), new Vector2(x, top + titleHeight + LineGap * scale), Styling.TextDim);
        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);

        DrawTiles(run);
    }

    private static void DrawTiles(RunModel run)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var gap = TileGap * scale;
        var tileWidth = (ImGui.GetContentRegionAvail().X - gap * (TileCount - 1)) / TileCount;
        StatTile.Draw(Loc.T(L.Running.TileDelivered), delivered, null, Styling.AccentMint, tileWidth);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.Requests.TileExperience), experience, null, Styling.AccentNebula, tileWidth);
        ImGui.SameLine(0f, gap);
        var sealsOrigin = ImGui.GetCursorScreenPos();
        StatTile.Draw(Loc.T(L.Requests.TileSeals), seals, sealsLost, sealsLost is null ? Styling.AccentGlowSoft : Styling.AccentRose, tileWidth);
        if (sealsLost is not null && Hit.HoveringRect(sealsOrigin, sealsOrigin + new Vector2(tileWidth, Layout.StatTileHeight * scale)))
        {
            Tooltip.Show(Loc.T(L.Requests.TileSealsLostHelp));
        }

        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(run.Summary.DryRun ? L.Running.TileWouldSpend : L.Running.TileSpent), spent, null, Styling.AccentAmber, tileWidth);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.Running.TileTime), duration, null, Styling.AccentBlue, tileWidth);
    }

    private static void Refresh(RunModel run)
    {
        if (run.Revision == textsRevision && ReferenceEquals(textsLanguage, Loc.Current))
        {
            return;
        }

        textsRevision = run.Revision;
        textsLanguage = Loc.Current;
        var summary = run.Summary;
        var culture = Loc.Culture;
        delivered = string.Concat(summary.Delivered.ToString("N0", culture), " / ", summary.Planned.ToString("N0", culture));
        experience = summary.Experience.ToString("N0", culture);
        seals = summary.Seals.ToString("N0", culture);
        sealsLost = summary.SealsLost > 0 ? Loc.T(L.Requests.TileSealsLost, summary.SealsLost.ToString("N0", culture)) : null;
        spent = Formatting.Gil(summary.GilSpent);
        duration = Formatting.Elapsed(summary.Duration);
        detail = string.Concat(Loc.T(L.Runs.Delivered, summary.Delivered, summary.Planned), TextDraw.Separator, duration);
    }
}
