using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Tasks;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;
using AutoGcSupply.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoGcSupply.Windows.Sections;

internal static class RequestTable
{
    private const float RowHeight = 54f;
    private const float CardHeight = 144f;
    private const float RowGap = 4f;
    private const float HeaderRowHeight = 24f;
    private const float GroupGap = 18f;
    private const float GroupTitleGap = 6f;
    private const float PadX = 12f;
    private const float CheckSize = 18f;
    private const float ClassIconSize = 26f;
    private const float ClassTextWidth = 104f;
    private const float ItemIconSize = 30f;
    private const float StatusWidth = 96f;
    private const float CostWidth = 104f;
    private const float ExperienceWidth = 100f;
    private const float SealsWidth = 64f;
    private const float ValueWidth = 80f;
    private const float CellGap = 10f;
    private const float RailWidth = 3f;
    private const float CaptionGap = 2f;
    private const float BadgeGap = 8f;
    private const float MinimumNameWidth = 140f;
    private const float PillHeight = 24f;
    private const float MutedOverlay = 0.55f;
    private const string NoValue = "-";
    private const string RowIdPrefix = "##agcs_request_";

    private static readonly string[] rowIds = BuildRowIds();
    private static readonly int[] supplyRows = new int[SupplyClasses.Count];
    private static readonly int[] provisioningRows = new int[SupplyClasses.Count];
    private static RowText[] texts = [];
    private static int supplyCount;
    private static int provisioningCount;
    private static TodayModel? textsModel;
    private static LanguageInfo? textsLanguage;

    private readonly record struct RowText(string Level, string Quantity, string Owned, string Status, string Cost, string Experience, string Share, string Seals, string Value, string Tooltip);

    private readonly record struct Columns(
        float Left, float Check, float ClassIcon, float ClassText, float ItemIcon, float Name, float NameRight,
        float Status, float CostRight, float ExperienceRight, float SealsRight, float ValueRight, bool ShowValue, float Right)
    {
        public bool Stacked => NameRight - Name < MinimumNameWidth * ImGuiHelpers.GlobalScale;
    }

    public static void Draw(GcRunController controller, TodayModel today)
    {
        Refresh(today);
        if (today.Rows.Length == 0)
        {
            EmptyState.Draw(FontAwesomeIcon.ClipboardList, Loc.T(L.Today.Empty));
            return;
        }

        var origin = ImGui.GetCursorScreenPos();
        var columns = ColumnsFor(origin.X, ImGui.GetContentRegionAvail().X);
        DrawGroup(controller, today, SupplyMissionKind.Supply, supplyRows.AsSpan(0, supplyCount), columns);
        if (provisioningCount == 0)
        {
            return;
        }

        Styling.VSpace(GroupGap);
        DrawGroup(controller, today, SupplyMissionKind.Provisioning, provisioningRows.AsSpan(0, provisioningCount), columns);
    }

    private static void DrawGroup(GcRunController controller, TodayModel today, SupplyMissionKind kind, ReadOnlySpan<int> indices, in Columns columns)
    {
        if (indices.Length == 0)
        {
            return;
        }

        DrawGroupTitle(controller, kind, columns);
        Styling.VSpace(GroupTitleGap);
        if (!columns.Stacked) DrawColumnHeader(columns);

        var scale = ImGuiHelpers.GlobalScale;
        var top = ImGui.GetCursorScreenPos();
        var height = (columns.Stacked ? CardHeight : RowHeight) * scale;
        var stride = height + RowGap * scale;
        var rows = today.Rows;
        for (var position = 0; position < indices.Length; position++)
        {
            var index = indices[position];
            var min = new Vector2(columns.Left, top.Y + position * stride);
            var max = new Vector2(columns.Right, min.Y + height);
            if (!ImGui.IsRectVisible(min, max))
            {
                continue;
            }

            DrawRow(controller, rows.ItemRef(index), texts[index], min, max, columns);
        }

        ImGui.SetCursorScreenPos(top);
        ImGui.Dummy(new Vector2(columns.Right - columns.Left, indices.Length * stride - RowGap * scale));
    }

    private static void DrawGroupTitle(GcRunController controller, SupplyMissionKind kind, in Columns columns)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var height = PillHeight * scale;
        var title = Loc.T(kind == SupplyMissionKind.Supply ? L.Requests.GroupSupply : L.Requests.GroupProvisioning);
        var titleSize = TextDraw.SectionTitleSize(title);
        TextDraw.SectionTitle(title, new Vector2(origin.X, origin.Y + (height - titleSize.Y) * 0.5f), Styling.TextStrong);

        var none = Loc.T(L.Requests.SelectNone);
        var all = Loc.T(L.Requests.SelectAll);
        var noneX = columns.Right - PillButton.Width(none);
        var allX = noneX - 6f * scale - PillButton.Width(all);
        var kindId = kind == SupplyMissionKind.Supply ? 0 : 1;
        ImGui.PushID(kindId);
        ImGui.SetCursorScreenPos(new Vector2(allX, origin.Y));
        if (PillButton.Draw("##all", all, Styling.AccentGlow, PillButton.Emphasis.Ghost, height: PillHeight, tooltip: Loc.T(L.Requests.SelectAllHelp)))
        {
            controller.SetAllIncluded(kind, true);
        }

        ImGui.SetCursorScreenPos(new Vector2(noneX, origin.Y));
        if (PillButton.Draw("##none", none, Styling.AccentGlow, PillButton.Emphasis.Ghost, height: PillHeight, tooltip: Loc.T(L.Requests.SelectNoneHelp)))
        {
            controller.SetAllIncluded(kind, false);
        }

        ImGui.PopID();
        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(columns.Right - columns.Left, height));
    }

    private static void DrawColumnHeader(in Columns columns)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var height = HeaderRowHeight * scale;
        HeaderLeft(L.Requests.ColumnClass, columns.ClassIcon, origin.Y, columns.ItemIcon - CellGap * scale - columns.ClassIcon, height);
        HeaderLeft(L.Requests.ColumnRequest, columns.ItemIcon, origin.Y, columns.NameRight - columns.ItemIcon, height);
        HeaderLeft(L.Requests.ColumnStatus, columns.Status, origin.Y, StatusWidth * scale, height);
        HeaderRight(L.Requests.ColumnCost, L.Requests.CostHelp, columns.CostRight, origin.Y, CostWidth * scale, height);
        HeaderRight(L.Requests.ColumnExperience, L.Requests.ExperienceHelp, columns.ExperienceRight, origin.Y, ExperienceWidth * scale, height);
        HeaderRight(L.Requests.ColumnSeals, L.Requests.SealsColumnHelp, columns.SealsRight, origin.Y, SealsWidth * scale, height);
        if (columns.ShowValue)
        {
            HeaderRight(L.Requests.ColumnValue, L.Requests.ValueHelp, columns.ValueRight, origin.Y, ValueWidth * scale, height);
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(columns.Right - columns.Left, height));
    }

    private static void HeaderLeft(LocString label, float x, float y, float width, float height)
    {
        using (Fonts.PushCaption())
        {
            var shown = TextDraw.Truncate(TextDraw.Upper(Loc.T(label)), width);
            TextDraw.At(shown, new Vector2(x, y + (height - ImGui.GetTextLineHeight()) * 0.5f), Styling.TextMuted);
        }
    }

    private static void HeaderRight(LocString label, LocString help, float right, float y, float width, float height)
    {
        using (Fonts.PushCaption())
        {
            var shown = TextDraw.Truncate(TextDraw.Upper(Loc.T(label)), width);
            TextDraw.Right(shown, right, y + (height - ImGui.GetTextLineHeight()) * 0.5f, Styling.TextMuted);
        }

        if (Hit.HoveringRect(new Vector2(right - width, y), new Vector2(right, y + height)))
        {
            Tooltip.Show(Loc.T(help));
        }
    }

    private static void DrawRow(GcRunController controller, in TodayRow row, in RowText text, Vector2 min, Vector2 max, in Columns columns)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var decision = row.Decision;
        var request = decision.Request;
        var coverage = decision.Coverage;
        var included = decision.Included;
        var clickable = coverage is not (RowCoverage.Delivered or RowCoverage.MaxLevel);
        var muted = coverage is RowCoverage.Delivered or RowCoverage.MaxLevel or RowCoverage.Excluded;
        var rowId = rowIds[request.ClassJobId];
        var drawList = ImGui.GetWindowDrawList();

        ImGui.SetCursorScreenPos(min);
        var hit = Hit.Area(rowId, max - min, clickable);
        var hovered = clickable ? hit.Hovered : Hit.HoveringRect(min, max);
        var hover = Motion.Hover(Motion.Key(rowId), hovered && clickable);
        var on = Motion.Approach(Motion.Key(rowId, 1), included ? 1f : 0f, 16f);
        if (hit.Clicked)
        {
            controller.SetIncluded(request.ClassJobId, !included);
        }

        DrawSurface(drawList, min, max, hover, on, hit.Held);
        if (columns.Stacked)
        {
            DrawCard(drawList, row, text, min, max, on, muted);
            if (hovered) Tooltip.Show(text.Tooltip);
            return;
        }

        var midY = (min.Y + max.Y) * 0.5f;
        DrawCheck(drawList, coverage, on, new Vector2(columns.Check, midY));

        var classIconSize = ClassIconSize * scale;
        var classIconMin = new Vector2(columns.ClassIcon, midY - classIconSize * 0.5f);
        ItemIcon.Draw(drawList, SupplyClasses.IconId(request.ClassJobId), classIconMin, classIconSize);
        var itemIconSize = ItemIconSize * scale;
        var itemIconMin = new Vector2(columns.ItemIcon, midY - itemIconSize * 0.5f);
        var highQuality = request.Kind == SupplyMissionKind.Supply && decision.ChosenQuality == ItemQuality.High && coverage != RowCoverage.Delivered;
        ItemIcon.Draw(drawList, row.ItemIconId, itemIconMin, itemIconSize, highQuality);
        if (muted)
        {
            Mute(drawList, classIconMin, classIconSize);
            Mute(drawList, itemIconMin, itemIconSize);
        }

        var strong = muted ? Styling.TextDim : Styling.TextStrong;
        var secondary = muted ? Styling.TextMuted : Styling.TextDim;
        TwoLines(row.ClassName, text.Level, columns.ClassText, ClassTextWidth * scale, midY, strong, secondary);
        DrawRequest(drawList, row, text, columns, midY, strong, secondary, muted);

        var chipColor = RunLabels.CoverageColor(coverage);
        using (Fonts.PushCaption())
        {
            Badge.DrawLeft(drawList, TextDraw.Truncate(text.Status, (StatusWidth - 14f) * scale), chipColor, columns.Status, midY);
        }

        var lineY = midY - ImGui.GetTextLineHeight() * 0.5f;
        TextDraw.Right(text.Cost, columns.CostRight, lineY, coverage == RowCoverage.ToBuy ? Styling.TextSecondary : Styling.TextMuted);
        RightTwoLines(text.Experience, text.Share, columns.ExperienceRight, midY, muted ? Styling.TextMuted : Styling.TextSecondary, secondary);
        TextDraw.Right(text.Seals, columns.SealsRight, lineY, muted ? Styling.TextMuted : Styling.TextSecondary);
        if (columns.ShowValue)
        {
            TextDraw.Right(text.Value, columns.ValueRight, lineY, muted ? Styling.TextMuted : Styling.TextDim);
        }

        if (hovered)
        {
            Tooltip.Show(text.Tooltip);
        }
    }

    private static void DrawCard(ImDrawListPtr drawList, in TodayRow row, in RowText text, Vector2 min, Vector2 max, float on, bool muted)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var decision = row.Decision;
        var strong = muted ? Styling.TextDim : Styling.TextStrong;
        var secondary = muted ? Styling.TextMuted : Styling.TextDim;
        var left = min.X + (PadX + RailWidth) * scale;
        var right = max.X - PadX * scale;
        var midY = min.Y + 24f * scale;
        DrawCheck(drawList, decision.Coverage, on, new Vector2(left, midY));
        var classIconMin = new Vector2(left + (CheckSize + CellGap) * scale, midY - ClassIconSize * scale * 0.5f);
        ItemIcon.Draw(drawList, SupplyClasses.IconId(decision.Request.ClassJobId), classIconMin, ClassIconSize * scale);
        if (muted) Mute(drawList, classIconMin, ClassIconSize * scale);

        float badgeWidth;
        using (Fonts.PushCaption())
        {
            var status = TextDraw.Truncate(text.Status, (right - left) * 0.35f - 14f * scale);
            badgeWidth = Badge.Draw(drawList, status, RunLabels.CoverageColor(decision.Coverage), right, midY);
        }

        var classX = classIconMin.X + (ClassIconSize + 8f) * scale;
        TwoLines(row.ClassName, text.Level, classX, right - badgeWidth - CellGap * scale - classX, midY, strong, secondary);

        var itemMidY = min.Y + 66f * scale;
        var itemMin = new Vector2(left, itemMidY - ItemIconSize * scale * 0.5f);
        var highQuality = decision.Request.Kind == SupplyMissionKind.Supply && decision.ChosenQuality == ItemQuality.High && decision.Coverage != RowCoverage.Delivered;
        ItemIcon.Draw(drawList, row.ItemIconId, itemMin, ItemIconSize * scale, highQuality);
        if (muted) Mute(drawList, itemMin, ItemIconSize * scale);
        var requestColumns = default(Columns) with { Name = left + (ItemIconSize + CellGap) * scale, NameRight = right };
        DrawRequest(drawList, row, text, requestColumns, itemMidY, strong, secondary, muted);

        var gap = CellGap * scale;
        var cellWidth = (right - left - 2f * gap) / 3f;
        var top = min.Y + 92f * scale;
        CardMetric(L.Requests.ColumnCost, text.Cost, string.Empty, left, top, cellWidth, secondary);
        CardMetric(L.Requests.ColumnExperience, text.Experience, text.Share, left + cellWidth + gap, top, cellWidth, secondary);
        CardMetric(L.Requests.ColumnSeals, text.Seals, string.Empty, left + 2f * (cellWidth + gap), top, cellWidth, secondary);
    }

    private static void CardMetric(LocString label, string value, string detail, float x, float top, float width, Vector4 color)
    {
        using (Fonts.PushCaption())
        {
            TextDraw.At(TextDraw.Truncate(TextDraw.Upper(Loc.T(label)), width), new Vector2(x, top), Styling.TextMuted);
        }

        top += TextDraw.CaptionLineHeight() + 3f * ImGuiHelpers.GlobalScale;
        TextDraw.At(TextDraw.Truncate(value, width), new Vector2(x, top), color);
        if (detail.Length > 0)
        {
            top += ImGui.GetTextLineHeight() + ImGuiHelpers.GlobalScale;
            using (Fonts.PushCaption())
            {
                TextDraw.At(TextDraw.Truncate(detail, width), new Vector2(x, top), Styling.TextMuted);
            }
        }
    }

    private static void DrawSurface(ImDrawListPtr drawList, Vector2 min, Vector2 max, float hover, float on, bool held)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var rounding = 8f * scale;
        var fill = Vector4.Lerp(Styling.WithAlpha(Styling.Surface1, 0.45f), Styling.WithAlpha(Styling.Surface2, 0.75f), hover);
        fill = Styling.Tint(fill, Styling.AccentGlow, 0.05f * on + (held ? 0.04f : 0f));
        Paint.Fill(drawList, min, max, fill, rounding);
        Paint.Stroke(drawList, min, max, Vector4.Lerp(Styling.WithAlpha(Styling.BorderDim, 0.25f + 0.3f * hover), Styling.WithAlpha(Styling.AccentGlow, 0.35f + 0.2f * hover), on), rounding);
        if (on <= 0.01f)
        {
            return;
        }

        var inset = 8f * scale;
        Paint.Fill(drawList, new Vector2(min.X + 2f * scale, min.Y + inset), new Vector2(min.X + (2f + RailWidth) * scale, max.Y - inset),
            Styling.WithAlpha(Styling.AccentGlow, on), RailWidth * scale * 0.5f);
    }

    private static void DrawCheck(ImDrawListPtr drawList, RowCoverage coverage, float on, Vector2 leftMid)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = CheckSize * scale;
        var center = new Vector2(leftMid.X + size * 0.5f, leftMid.Y);
        if (coverage == RowCoverage.Delivered)
        {
            TextDraw.IconCentered(FontAwesomeIcon.CheckCircle, center, Styling.AccentMint);
            return;
        }

        if (coverage == RowCoverage.MaxLevel)
        {
            TextDraw.IconCentered(FontAwesomeIcon.Medal, center, Styling.TextMuted);
            return;
        }

        var min = new Vector2(leftMid.X, leftMid.Y - size * 0.5f);
        var max = min + new Vector2(size, size);
        var rounding = 5f * scale;
        Paint.Fill(drawList, min, max, Vector4.Lerp(Styling.WithAlpha(Styling.Surface0, 0.9f), Styling.AccentGlow, on), rounding);
        Paint.Stroke(drawList, min, max, Vector4.Lerp(Styling.WithAlpha(Styling.BorderDim, 0.9f), Styling.Lighten(Styling.AccentGlow, 0.3f), on), rounding, 1.2f);
        if (on > 0.05f)
        {
            Paint.Check(drawList, center, size * 0.62f, Styling.WithAlpha(Styling.InkOnGlow, on), 2f * scale);
        }
    }

    private static void Mute(ImDrawListPtr drawList, Vector2 min, float size)
        => Paint.Fill(drawList, min, min + new Vector2(size, size), Styling.WithAlpha(Styling.Surface0, MutedOverlay), 5f * ImGuiHelpers.GlobalScale);

    private static void DrawRequest(ImDrawListPtr drawList, in TodayRow row, in RowText text, in Columns columns, float midY, Vector4 strong, Vector4 secondary, bool muted)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var decision = row.Decision;
        var lineHeight = ImGui.GetTextLineHeight();
        var captionHeight = TextDraw.CaptionLineHeight();

        var top = midY - (lineHeight + CaptionGap * scale + captionHeight) * 0.5f;
        var x = columns.Name;
        var width = columns.NameRight - x;
        var quantityWidth = TextDraw.Measure(text.Quantity).X;
        var quality = decision.Request.Kind == SupplyMissionKind.Supply && decision.Coverage != RowCoverage.Delivered
            ? Loc.T(decision.ChosenQuality == ItemQuality.High ? L.Requests.QualityHigh : L.Requests.QualityNormal)
            : null;
        var extras = quantityWidth + BadgeGap * scale;
        if (quality is not null)
        {
            using (Fonts.PushCaption())
            {
                extras += TextDraw.Measure(quality).X + 14f * scale + BadgeGap * scale;
            }
        }

        if (decision.Request.Starred)
        {
            extras += TextDraw.IconSize(FontAwesomeIcon.Star).X + BadgeGap * scale;
        }

        var name = TextDraw.Truncate(row.ItemName, MathF.Max(0f, width - extras));
        TextDraw.At(name, new Vector2(x, top), strong);
        x += TextDraw.Measure(name).X + 6f * scale;
        TextDraw.At(text.Quantity, new Vector2(x, top), muted ? Styling.TextMuted : Styling.AccentGlowSoft);
        x += quantityWidth + BadgeGap * scale;
        var nameMid = top + lineHeight * 0.5f;
        if (quality is not null)
        {
            var qualityColor = decision.ChosenQuality == ItemQuality.High ? Styling.AccentAmber : Styling.TextDim;
            x += Badge.DrawLeft(drawList, quality, muted ? Styling.TextMuted : qualityColor, x, nameMid) + BadgeGap * scale;
        }

        if (decision.Request.Starred)
        {
            var starSize = TextDraw.IconSize(FontAwesomeIcon.Star);
            TextDraw.Icon(FontAwesomeIcon.Star, new Vector2(x, nameMid - starSize.Y * 0.5f), muted ? Styling.TextMuted : Styling.AccentCoffee);
        }

        using (Fonts.PushCaption())
        {
            TextDraw.At(TextDraw.Truncate(text.Owned, width), new Vector2(columns.Name, top + lineHeight + CaptionGap * scale), secondary);
        }
    }

    private static void TwoLines(string first, string second, float x, float width, float midY, Vector4 firstColor, Vector4 secondColor)
    {
        var lineHeight = ImGui.GetTextLineHeight();
        var top = midY - (lineHeight + CaptionGap * ImGuiHelpers.GlobalScale + TextDraw.CaptionLineHeight()) * 0.5f;
        TextDraw.At(TextDraw.Truncate(first, width), new Vector2(x, top), firstColor);
        using (Fonts.PushCaption())
        {
            TextDraw.At(TextDraw.Truncate(second, width), new Vector2(x, top + lineHeight + CaptionGap * ImGuiHelpers.GlobalScale), secondColor);
        }
    }

    private static void RightTwoLines(string first, string second, float right, float midY, Vector4 firstColor, Vector4 secondColor)
    {
        var lineHeight = ImGui.GetTextLineHeight();
        if (second.Length == 0)
        {
            TextDraw.Right(first, right, midY - lineHeight * 0.5f, firstColor);
            return;
        }

        var top = midY - (lineHeight + CaptionGap * ImGuiHelpers.GlobalScale + TextDraw.CaptionLineHeight()) * 0.5f;
        TextDraw.Right(first, right, top, firstColor);
        using (Fonts.PushCaption())
        {
            TextDraw.Right(second, right, top + lineHeight + CaptionGap * ImGuiHelpers.GlobalScale, secondColor);
        }
    }


    private static Columns ColumnsFor(float left, float width)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var gap = CellGap * scale;
        var right = left + width;
        var inner = right - PadX * scale;
        var check = left + (PadX + RailWidth) * scale;
        var classIcon = check + CheckSize * scale + gap;
        var classText = classIcon + ClassIconSize * scale + 8f * scale;
        var itemIcon = classText + ClassTextWidth * scale + gap;
        var name = itemIcon + ItemIconSize * scale + gap;
        var fixedRight = (StatusWidth + CostWidth + ExperienceWidth + SealsWidth) * scale + gap * 4f;
        var showValue = inner - name - fixedRight - (ValueWidth * scale + gap) >= MinimumNameWidth * scale;
        var valueRight = inner;
        var sealsRight = showValue ? valueRight - ValueWidth * scale - gap : inner;
        var experienceRight = sealsRight - SealsWidth * scale - gap;
        var costRight = experienceRight - ExperienceWidth * scale - gap;
        var status = costRight - CostWidth * scale - gap - StatusWidth * scale;
        return new Columns(left, check, classIcon, classText, itemIcon, name, status - gap, status, costRight, experienceRight, sealsRight, valueRight, showValue, right);
    }

    private static void Refresh(TodayModel today)
    {
        if (ReferenceEquals(today, textsModel) && ReferenceEquals(textsLanguage, Loc.Current))
        {
            return;
        }

        textsModel = today;
        textsLanguage = Loc.Current;
        var rows = today.Rows;
        if (texts.Length != rows.Length)
        {
            texts = new RowText[rows.Length];
        }

        supplyCount = 0;
        provisioningCount = 0;
        var shopperAvailable = today.Shopper == ShopperState.Available;
        for (var index = 0; index < rows.Length; index++)
        {
            texts[index] = Build(rows[index], shopperAvailable);
            if (rows[index].Decision.Request.Kind == SupplyMissionKind.Supply)
            {
                supplyRows[supplyCount++] = index;
            }
            else
            {
                provisioningRows[provisioningCount++] = index;
            }
        }
    }

    private static RowText Build(in TodayRow row, bool shopperAvailable)
    {
        var culture = Loc.Culture;
        var decision = row.Decision;
        var owned = decision.Owned;
        var experience = decision.Experience;
        return new RowText(
            Loc.T(L.Requests.Level, row.Level),
            Loc.T(L.Requests.Quantity, decision.Request.Requested),
            owned.Normal == 0 && owned.High == 0 ? Loc.T(L.Requests.OwnedNone) : Loc.T(L.Requests.Owned, owned.Normal.ToString("N0", culture), owned.High.ToString("N0", culture)),
            RunLabels.CoverageLabel(decision.Coverage, shopperAvailable),
            decision.Coverage == RowCoverage.ToBuy ? Formatting.Gil(decision.EstimatedTotal) : NoValue,
            experience > 0 ? experience.ToString("N0", culture) : NoValue,
            experience > 0 && decision.LevelShare > 0d ? Loc.T(L.Requests.LevelShare, (decision.LevelShare * 100d).ToString("0.#", culture)) : string.Empty,
            decision.Seals > 0 ? decision.Seals.ToString("N0", culture) : NoValue,
            decision.GilPerThousandExperience > 0 ? decision.GilPerThousandExperience.ToString("N0", culture) : NoValue,
            string.Concat(row.ClassName, "\n", row.ItemName, " ", Loc.T(L.Requests.Quantity, decision.Request.Requested), "\n", RowTooltip(decision, shopperAvailable)));
    }

    private static string RowTooltip(in RowDecision decision, bool shopperAvailable)
    {
        var coverage = decision.Coverage;
        var text = coverage switch
        {
            RowCoverage.Owned => Loc.T(L.Requests.CoverageOwnedHelp),
            RowCoverage.ToBuy => Loc.T(L.Requests.CoverageToBuyHelp, decision.BuyQuantity.ToString("N0", Loc.Culture), Formatting.Gil(decision.EstimatedTotal), Formatting.Gil(decision.MaxUnitPrice)),
            RowCoverage.Unpriced => Loc.T(shopperAvailable ? L.Requests.CoverageUnpricedHelp : L.Requests.CoverageNoShopperHelp),
            RowCoverage.NoListings => Loc.T(L.Requests.CoverageNoListingsHelp),
            RowCoverage.Delivered => Loc.T(L.Requests.CoverageDeliveredHelp),
            RowCoverage.Excluded => Loc.T(L.Requests.CoverageExcludedHelp),
            _ => Loc.T(L.Requests.CoverageMaxLevelHelp),
        };

        if (decision.Request.Starred)
        {
            text = string.Concat(text, "\n", Loc.T(L.Requests.StarHelp));
        }

        if (decision.Request.Kind == SupplyMissionKind.Supply && coverage != RowCoverage.Delivered)
        {
            text = string.Concat(text, "\n", Loc.T(decision.ChosenQuality == ItemQuality.High ? L.Requests.QualityHighHelp : L.Requests.QualityNormalHelp));
        }

        if (coverage is RowCoverage.Delivered or RowCoverage.MaxLevel)
        {
            return text;
        }

        return string.Concat(text, "\n", Loc.T(decision.Included ? L.Requests.ClickToExclude : L.Requests.ClickToInclude));
    }

    private static string[] BuildRowIds()
    {
        var ids = new string[SupplyClasses.Last + 1];
        for (var classJobId = 0; classJobId < ids.Length; classJobId++)
        {
            ids[classJobId] = string.Concat(RowIdPrefix, classJobId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return ids;
    }
}
