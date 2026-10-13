using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Stats;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Windows.Components;
using AutoGcSupply.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoGcSupply.Windows.Pages;

internal sealed class HistoryPage
{
    private const int TileCount = 4;
    private const float TileGap = 8f;
    private const float PadX = 14f;
    private const float IconColumn = 18f;
    private const float BadgeGap = 6f;
    private const float RowGap = 4f;
    private const float ConfirmSlide = 12f;
    private const float SectionGap = 14f;
    private const string DateFormat = "g";

    private const long RowTextRefreshMs = 15_000;

    private readonly Dictionary<RunRecord, RowText> rowTexts = new(ReferenceEqualityComparer.Instance);
    private int rowTextsRevision = -1;
    private long rowTextsStep = -1;
    private LanguageInfo? rowTextsLanguage;
    private int totalsRevision = -1;
    private LanguageInfo? totalsLanguage;
    private string subtitle = string.Empty;
    private string runs = string.Empty;
    private string? completed;
    private string experience = string.Empty;
    private string seals = string.Empty;
    private string spent = string.Empty;
    private bool confirmClear;

    private readonly record struct RowText(string When, string Detail, string Tooltip);

    public void Draw(RunHistory history)
    {
        RefreshTotals(history);
        PageHeader.Draw(Loc.T(L.History.Title), subtitle);
        DrawTotals();
        Styling.VSpace(SectionGap);
        if (history.Records.Count == 0)
        {
            EmptyState.Draw(FontAwesomeIcon.History, Loc.T(L.History.NoRuns));
            return;
        }

        Label(Loc.T(L.Runs.RecentRuns));
        DrawRows(history);
        Styling.VSpace(8f);
        DrawClearControl(history);
    }

    private static void Label(string text)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var size = TextDraw.SectionTitleSize(text);
        TextDraw.SectionTitle(text, origin, Styling.TextStrong);
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, size.Y + 8f * scale));
    }

    private void DrawTotals()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var gap = TileGap * scale;
        var tileWidth = (ImGui.GetContentRegionAvail().X - gap * (TileCount - 1)) / TileCount;
        StatTile.Draw(Loc.T(L.Runs.TileRuns), runs, completed, Styling.AccentGlow, tileWidth);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.Runs.TileExperience), experience, null, Styling.AccentNebula, tileWidth);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.Runs.TileSeals), seals, null, Styling.AccentMint, tileWidth);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.Runs.TileSpent), spent, null, Styling.AccentAmber, tileWidth);
    }

    private void RefreshTotals(RunHistory history)
    {
        if (history.Revision == totalsRevision && ReferenceEquals(totalsLanguage, Loc.Current))
        {
            return;
        }

        totalsRevision = history.Revision;
        totalsLanguage = Loc.Current;
        var records = history.Records;
        var completedRuns = 0;
        long experienceSum = 0;
        long sealsSum = 0;
        long spentSum = 0;
        for (var index = 0; index < records.Count; index++)
        {
            var record = records[index];
            completedRuns += record.End == RunEnd.Completed ? 1 : 0;
            experienceSum += record.Experience;
            sealsSum += record.Seals;
            spentSum += record.DryRun ? 0 : record.GilSpent;
        }

        var culture = Loc.Culture;
        subtitle = records.Count == 0 ? Loc.T(L.History.Empty) : Loc.Plural(L.Runs.Summary, records.Count);
        runs = records.Count.ToString("N0", culture);
        completed = records.Count == 0 ? null : Loc.T(L.Runs.TileRunsSub, completedRuns.ToString("N0", culture));
        experience = experienceSum.ToString("N0", culture);
        seals = sealsSum.ToString("N0", culture);
        spent = Formatting.Gil(spentSum);
    }

    private void DrawRows(RunHistory history)
    {
        RefreshRowTexts(history.Revision);
        var records = history.Records;
        var newest = HistoryOrder.Newest(history);
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, Layout.HistoryRowHeight * scale);
        var stride = size.Y + RowGap * scale;
        var top = ImGui.GetCursorScreenPos();
        var windowTop = ImGui.GetWindowPos().Y;
        var first = Math.Clamp((int)MathF.Floor((windowTop - top.Y) / stride), 0, newest.Length);
        var last = Math.Clamp((int)MathF.Ceiling((windowTop + ImGui.GetWindowHeight() - top.Y) / stride), first, newest.Length);
        for (var position = first; position < last; position++)
        {
            var record = records[newest[position]];
            DrawRow(record, RowTextFor(record), new Vector2(top.X, top.Y + position * stride), size);
        }

        ImGui.SetCursorScreenPos(top);
        ImGui.Dummy(new Vector2(size.X, MathF.Max(0f, newest.Length * stride - RowGap * scale)));
    }

    private void RefreshRowTexts(int revision)
    {
        var step = Environment.TickCount64 / RowTextRefreshMs;
        if (revision == rowTextsRevision && step == rowTextsStep && ReferenceEquals(rowTextsLanguage, Loc.Current))
        {
            return;
        }

        rowTexts.Clear();
        rowTextsRevision = revision;
        rowTextsStep = step;
        rowTextsLanguage = Loc.Current;
    }

    private RowText RowTextFor(RunRecord record)
    {
        if (rowTexts.TryGetValue(record, out var cached))
        {
            return cached;
        }

        var text = new RowText(Formatting.RelativeTime(record.EndedAtUtc), RowDetail(record), RowTooltip(record));
        rowTexts[record] = text;
        return text;
    }

    private static void DrawRow(RunRecord record, in RowText text, Vector2 origin, Vector2 size)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var end = origin + size;
        var drawList = ImGui.GetWindowDrawList();
        var hovered = Hit.HoveringRect(origin, end);
        var hover = Motion.Hover(Motion.Key("##agcs_hist_row", record.RunId.GetHashCode()), hovered);
        Paint.Glass(drawList, origin, end, Styling.CardRounding * scale, Styling.AccentGlow, 0.02f, hover);

        var padX = PadX * scale;
        var midY = origin.Y + size.Y * 0.5f;
        var column = IconColumn * scale;
        var iconSize = TextDraw.IconSize(FontAwesomeIcon.BoxOpen);
        TextDraw.Icon(FontAwesomeIcon.BoxOpen, new Vector2(origin.X + padX + (column - iconSize.X) * 0.5f, midY - iconSize.Y * 0.5f), Styling.TextDim);
        var textX = origin.X + padX + column + 10f * scale;

        var whenSize = TextDraw.Measure(text.When);
        float captionHeight;
        using (Fonts.PushCaption())
        {
            captionHeight = ImGui.GetTextLineHeight();
        }

        var top = midY - (whenSize.Y + 3f * scale + captionHeight) * 0.5f;
        TextDraw.At(text.When, new Vector2(textX, top), Styling.TextStrong);
        var badgeMid = top + whenSize.Y * 0.5f;
        var badgeX = textX + whenSize.X + BadgeGap * 2f * scale;
        badgeX += Badge.DrawLeft(drawList, RunLabels.OutcomeLabel(record.End), RunLabels.OutcomeColor(record.End), badgeX, badgeMid);
        if (record.DryRun)
        {
            Badge.DrawLeft(drawList, Loc.T(L.Runs.DryRun), Styling.AccentBlue, badgeX + BadgeGap * scale, badgeMid);
        }

        using (Fonts.PushCaption())
        {
            TextDraw.At(TextDraw.Truncate(text.Detail, end.X - padX - textX), new Vector2(textX, top + whenSize.Y + 3f * scale), Styling.TextDim);
        }

        if (hovered)
        {
            Tooltip.Show(text.Tooltip);
        }
    }

    private static string RowDetail(RunRecord record)
    {
        var culture = Loc.Culture;
        var detail = string.Concat(
            Loc.T(L.Runs.Delivered, record.Delivered, record.Planned), TextDraw.Separator,
            Loc.T(L.Runs.Experience, record.Experience.ToString("N0", culture)), TextDraw.Separator,
            Loc.T(L.Runs.Seals, record.Seals.ToString("N0", culture)));
        var spent = Loc.T(record.DryRun ? L.Runs.WouldSpend : L.Runs.Spent, Formatting.Gil(record.GilSpent));
        return string.Concat(detail, TextDraw.Separator, spent, TextDraw.Separator, Formatting.Elapsed(record.EndedAtUtc - record.StartedAtUtc));
    }

    private static string RowTooltip(RunRecord record)
    {
        var started = record.StartedAtUtc.ToLocalTime().ToString(DateFormat, Loc.Culture);
        var ended = record.EndedAtUtc.ToLocalTime().ToString(DateFormat, Loc.Culture);
        var text = Loc.T(L.Runs.TooltipTimes, started, ended);
        return record.SealsLost > 0
            ? string.Concat(text, "\n", Loc.T(L.Runs.TooltipSealsLost, record.SealsLost.ToString("N0", Loc.Culture)))
            : text;
    }

    private void DrawClearControl(RunHistory history)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var available = ImGui.GetContentRegionAvail().X;
        var origin = ImGui.GetCursorScreenPos();
        var reveal = Motion.Transition(Motion.Key("##agcs_hist_clear_state"), confirmClear);
        var slide = (1f - reveal) * ConfirmSlide * scale;
        using var alpha = Motion.PushAlpha(reveal);

        if (!confirmClear)
        {
            var label = Loc.T(L.Runs.Clear);
            ImGui.SetCursorScreenPos(new Vector2(origin.X + available - PillButton.Width(label, FontAwesomeIcon.Trash) - slide, origin.Y));
            if (PillButton.Draw("##agcs_hist_clear", label, Styling.AccentRose, PillButton.Emphasis.Ghost, FontAwesomeIcon.Trash))
            {
                confirmClear = true;
            }

            return;
        }

        var question = Loc.T(L.Runs.ClearQuestion);
        var yes = Loc.T(L.Runs.ClearYes);
        var no = Loc.T(L.Runs.Cancel);
        var questionSize = TextDraw.Measure(question);
        var gap = 8f * scale;
        var buttonHeight = 28f * scale;

        var x = origin.X + available - PillButton.Width(no) - slide;
        ImGui.SetCursorScreenPos(new Vector2(x, origin.Y));
        if (PillButton.Draw("##agcs_hist_clear_no", no, Styling.AccentGlow, PillButton.Emphasis.Ghost))
        {
            confirmClear = false;
        }

        x -= gap + PillButton.Width(yes);
        ImGui.SetCursorScreenPos(new Vector2(x, origin.Y));
        if (PillButton.Draw("##agcs_hist_clear_yes", yes, Styling.AccentRose, PillButton.Emphasis.Filled))
        {
            history.Clear();
            confirmClear = false;
        }

        TextDraw.At(question, new Vector2(x - gap * 1.5f - questionSize.X, origin.Y + (buttonHeight - questionSize.Y) * 0.5f), Styling.AccentRoseSoft);
    }
}
