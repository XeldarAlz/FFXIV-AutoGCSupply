using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Tasks;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoGcSupply.Windows.Sections;

internal static class RunPanel
{
    private const float PadX = 18f;
    private const float CardPadY = 16f;
    private const float RingInset = 16f;
    private const float RingGap = 20f;
    private const float RingThickness = 6f;
    private const float ChipPadX = 9f;
    private const float ChipPadY = 3f;
    private const float ChipGap = 10f;
    private const float StatusGap = 12f;
    private const float BarHeight = 8f;
    private const float HeaderFooterGap = 16f;
    private const float SectionGap = 14f;
    private const float RailHeight = 66f;
    private const float StepRadius = 11f;
    private const float StepLineThickness = 2f;
    private const float StepLabelGap = 8f;
    private const float ShopperCardHeight = 64f;
    private const float ShopperIconBox = 34f;
    private const float RowHeight = 38f;
    private const float RowGap = 4f;
    private const float RowPadX = 14f;
    private const float RowClassIconSize = 22f;
    private const float RowItemIconSize = 24f;
    private const float RowStateColumn = 18f;
    private const float NoteGap = 6f;
    private const float QuantityGap = 6f;
    private const int StepCount = 6;
    private const int FinishingStep = 5;
    private const int ProgressKeyShift = 16;

    private static readonly LocString[] stepLabels =
    [
        L.Running.StepReading,
        L.Running.StepPricing,
        L.Running.StepBuying,
        L.Running.StepTravelling,
        L.Running.StepHandingIn,
        L.Running.StepDone,
    ];

    private static readonly CachedText[] quantityTexts = new CachedText[SupplyClasses.Count];

    private static CachedText progressText;
    private static CachedText spentText;

    public static void Draw(Plugin plugin)
    {
        var controller = plugin.Controller;
        var run = controller.Run;
        var running = controller.Running;
        var (accent, accentSoft, label) = PhasePalette(controller);

        DrawHeaderStrip(controller, run, controller.Today, accent, accentSoft);
        Styling.VSpace(6f);
        if (running)
        {
            DrawHeroCard(controller, run, accent, accentSoft, label);
        }
        else
        {
            FinishedRunCard.Draw(controller, run);
        }

        Styling.VSpace(SectionGap);
        DrawStepRail(run, running);
        if (run.DryRun)
        {
            DrawNote(FontAwesomeIcon.Flask, Loc.T(L.Running.DryRunBanner), Styling.AccentBlue, Styling.AccentBlueSoft);
        }

        if (run.NeedsPlayer != NeedsPlayerReason.None)
        {
            Styling.VSpace(SectionGap);
            NeedsPlayerCard.Draw(controller, run);
        }

        if (running && run.Step == RunStep.Buying)
        {
            Styling.VSpace(SectionGap);
            DrawShopperCard(run);
        }

        DrawRows(run, controller.Labels);
        Styling.VSpace(12f);
    }

    private static (Vector4 Accent, Vector4 AccentSoft, string Label) PhasePalette(GcRunController controller)
    {
        if (!controller.Running)
        {
            var end = controller.Run.End;
            var color = RunLabels.OutcomeColor(end);
            return (color, Styling.Lighten(color, 0.3f), RunLabels.OutcomeLabel(end));
        }

        return controller.Phase switch
        {
            RunPhase.Stopping => (Styling.AccentAmber, Styling.AccentAmberSoft, ReadyState.PhaseLabel(RunPhase.Stopping)),
            RunPhase.Paused => (Styling.AccentAmber, Styling.AccentAmberSoft, ReadyState.PhaseLabel(RunPhase.Paused)),
            RunPhase.Finishing => (Styling.AccentMint, Styling.AccentMintSoft, ReadyState.PhaseLabel(RunPhase.Finishing)),
            var phase => (Styling.AccentBlue, Styling.AccentBlueSoft, ReadyState.PhaseLabel(phase)),
        };
    }

    private static void DrawHeaderStrip(GcRunController controller, RunModel run, TodayModel today, Vector4 accent, Vector4 accentSoft)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var drawList = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        var available = ImGui.GetContentRegionAvail().X;
        var lineHeight = ImGui.GetTextLineHeight();
        var midY = origin.Y + lineHeight * 0.5f;
        var active = controller.Running && controller.Phase != RunPhase.Paused;

        var dot = active ? Styling.PulseColor(accent, accentSoft, Styling.PulseMedium) : accent;
        var radius = 4f * scale;
        Paint.Dot(drawList, new Vector2(origin.X + radius + 3f * scale, midY), radius, dot);

        var status = !controller.Running ? Loc.T(L.Running.StatusFinished)
            : controller.Phase == RunPhase.Paused ? Loc.T(L.Shell.StatusPaused)
            : Loc.T(L.Shell.StatusRunning);
        var statusSize = TextDraw.SmallCapsSize(status);
        var statusX = origin.X + radius * 2f + 12f * scale;
        TextDraw.SmallCaps(status, new Vector2(statusX, midY - statusSize.Y * 0.5f), Styling.TextSecondary);
        var statusEnd = statusX + statusSize.X;
        if (run.DryRun)
        {
            statusEnd += HeaderFooterGap * scale * 0.5f;
            statusEnd += Badge.DrawLeft(drawList, Loc.T(L.Running.DryRunBadge), Styling.AccentBlue, statusEnd, midY);
        }

        if (today.Header.GrandCompany != 0)
        {
            using (Fonts.PushCaption())
            {
                var footerLeft = statusEnd + HeaderFooterGap * scale;
                var footer = TextDraw.Truncate(GrandCompanyText.CompanyName(today.Header.GrandCompany), origin.X + available - footerLeft);
                var footerSize = TextDraw.Measure(footer);
                TextDraw.At(footer, new Vector2(origin.X + available - footerSize.X, midY - footerSize.Y * 0.5f), Styling.TextMuted);
            }
        }

        ImGui.Dummy(new Vector2(available, lineHeight));
    }

    private static void DrawHeroCard(GcRunController controller, RunModel run, Vector4 accent, Vector4 accentSoft, string label)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, HeroCardHeight());
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var drawList = ImGui.GetWindowDrawList();
        var active = controller.Phase is not (RunPhase.Paused or RunPhase.Stopping);
        var rounding = Styling.PanelRounding * scale;

        Paint.Glass(drawList, origin, end, rounding, accent, active ? 0.10f : 0.03f, 0f, elevated: true);
        if (active)
        {
            Paint.Stroke(drawList, origin, end, Styling.PulseColor(Styling.WithAlpha(accent, 0.5f), accentSoft, Styling.PulseMedium), rounding, 1.6f);
        }

        var padX = PadX * scale;
        var ringRadius = size.Y * 0.5f - RingInset * scale;
        var ringCenter = new Vector2(origin.X + padX + ringRadius, origin.Y + size.Y * 0.5f);
        DrawProgressRing(run, ringCenter, ringRadius, accent, active);

        var columnX = ringCenter.X + ringRadius + RingGap * scale;
        var columnWidth = end.X - padX - columnX;
        var y = origin.Y + CardPadY * scale;
        y += DrawPhaseChip(columnX, y, columnWidth, label, accent, accentSoft) + ChipGap * scale;
        var status = run.Status.Length > 0 ? run.Status : controller.Status;
        var line = TextDraw.Truncate(string.IsNullOrWhiteSpace(status) ? Loc.T(L.Running.Working) : status, columnWidth);
        TextDraw.At(line, new Vector2(columnX, y), Styling.TextSecondary);
        y += ImGui.GetTextLineHeight() + StatusGap * scale;

        var barOrigin = new Vector2(columnX, y);
        if (active)
        {
            Paint.IndeterminateBar(drawList, barOrigin, columnWidth, BarHeight * scale, accent);
        }
        else
        {
            Paint.Bar(drawList, barOrigin, columnWidth, BarHeight * scale, 0f, accent);
        }

        ImGui.Dummy(size);
    }

    private static float HeroCardHeight()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var bodyLine = ImGui.GetTextLineHeight();
        var captionLine = TextDraw.CaptionLineHeight();

        var chip = captionLine + ChipPadY * 2f * scale;
        var gaps = (CardPadY * 2f + ChipGap + StatusGap + BarHeight) * scale;
        return MathF.Max(gaps + chip + bodyLine, 96f * scale);
    }

    private static void DrawProgressRing(RunModel run, Vector2 center, float radius, Vector4 accent, bool active)
    {
        var thickness = RingThickness * ImGuiHelpers.GlobalScale;
        CountRows(run, out var delivered, out var total);
        ProgressRing.Track(center, radius, thickness, Styling.WithAlpha(Styling.BorderDim, 0.7f));
        if (total > 0)
        {
            ProgressRing.Fill(center, radius, thickness, (float)delivered / total, Styling.AccentMint);
        }

        if (active)
        {
            ProgressRing.Sweep(center, radius, thickness, accent, Styling.PulseOrbit, MathF.PI * 0.6f, 0.9f);
        }

        if (total == 0)
        {
            ProgressRing.CenterIcon(center, FontAwesomeIcon.BoxOpen, Styling.TextDim, radius * 0.55f);
            return;
        }

        var value = progressText.Get(((long)delivered << ProgressKeyShift) | (uint)total, static packed => string.Concat(
            ((int)(packed >> ProgressKeyShift)).ToString(Loc.Culture), "/", ((int)(packed & 0xFFFF)).ToString(Loc.Culture)));
        ProgressRing.CenterValue(center, value, Loc.T(L.Running.Delivered), Styling.TextStrong, Styling.TextDim);
    }

    private static void CountRows(RunModel run, out int delivered, out int total)
    {
        delivered = 0;
        total = run.Rows.Length;
        for (var index = 0; index < run.Rows.Length; index++)
        {
            if (RunLabels.IsDelivered(run.Rows[index].State))
            {
                delivered++;
            }
        }
    }

    private static float DrawPhaseChip(float x, float y, float maxWidth, string text, Vector4 accent, Vector4 accentSoft)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var drawList = ImGui.GetWindowDrawList();
        var padX = ChipPadX * scale;
        var padY = ChipPadY * scale;
        using (Fonts.PushCaption())
        {
            var label = TextDraw.Truncate(TextDraw.Upper(text), maxWidth - padX * 2f);
            var textSize = new Vector2(TextDraw.Measure(label).X, ImGui.GetTextLineHeight());
            var chipMin = new Vector2(x, y);
            var chipMax = chipMin + new Vector2(padX * 2f + textSize.X, textSize.Y + padY * 2f);
            Paint.Pill(drawList, chipMin, chipMax, Styling.WithAlpha(accent, 0.28f), Styling.WithAlpha(accent, 0.65f));
            TextDraw.At(label, new Vector2(x + padX, y + padY), accentSoft);
            return chipMax.Y - chipMin.Y;
        }
    }

    private static int StepIndex(RunStep step) => step switch
    {
        RunStep.Reading => 0,
        RunStep.Pricing => 1,
        RunStep.Buying => 2,
        RunStep.Travelling => 3,
        RunStep.HandingIn => 4,
        RunStep.Finishing => FinishingStep,
        RunStep.Done => StepCount,
        _ => -1,
    };

    private static void DrawStepRail(RunModel run, bool running)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, RailHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var drawList = ImGui.GetWindowDrawList();
        Paint.Surface(drawList, origin, end, Styling.CardRounding * scale, Styling.WithAlpha(Styling.Surface1, 0.45f), Styling.WithAlpha(Styling.BorderDim, 0.5f), topLight: false);

        var completed = !running && run.End == RunEnd.Completed;
        var current = completed ? StepCount : StepIndex(running ? run.Step : run.LastStep);
        var halted = running ? run.NeedsPlayer != NeedsPlayerReason.None : !completed;
        var haltColor = running ? Styling.AccentAmber : RunLabels.OutcomeColor(run.End);
        var radius = StepRadius * scale;
        var padX = PadX * scale + radius;
        var spacing = (size.X - padX * 2f) / (StepCount - 1);
        var captionHeight = TextDraw.CaptionLineHeight();

        var circleY = origin.Y + (size.Y - (radius * 2f + StepLabelGap * scale + captionHeight)) * 0.5f + radius;
        for (var step = 0; step < StepCount - 1; step++)
        {
            var from = new Vector2(origin.X + padX + spacing * step + radius, circleY);
            var to = new Vector2(origin.X + padX + spacing * (step + 1) - radius, circleY);
            var color = step < current ? Styling.WithAlpha(Styling.AccentMint, 0.8f) : Styling.WithAlpha(Styling.BorderDim, 0.8f);
            drawList.AddLine(from, to, Paint.Col(color), StepLineThickness * scale);
        }

        for (var step = 0; step < StepCount; step++)
        {
            var center = new Vector2(origin.X + padX + spacing * step, circleY);
            var done = step < current;
            var now = step == current;
            DrawStepCircle(drawList, center, radius, done, now, halted, haltColor);
            var labelColor = now ? (halted ? Styling.Lighten(haltColor, 0.3f) : Styling.AccentBlueSoft) : done ? Styling.TextSecondary : Styling.TextMuted;
            using (Fonts.PushCaption())
            {
                var text = TextDraw.Truncate(Loc.T(stepLabels[step]), spacing - 6f * scale);
                TextDraw.Center(text, center.X, circleY + radius + StepLabelGap * scale, labelColor);
            }
        }

        ImGui.Dummy(size);
    }

    private static void DrawStepCircle(ImDrawListPtr drawList, Vector2 center, float radius, bool done, bool now, bool halted, Vector4 haltColor)
    {
        var scale = ImGuiHelpers.GlobalScale;
        if (done)
        {
            drawList.AddCircleFilled(center, radius, Paint.Col(Styling.AccentMint));
            Paint.Check(drawList, center, radius * 1.1f, Styling.ForegroundOn(Styling.AccentMint), 2f * scale);
            return;
        }

        if (now)
        {
            var color = halted ? haltColor : Styling.AccentBlue;
            var border = halted ? color : Styling.PulseColor(Styling.WithAlpha(color, 0.5f), Styling.Lighten(color, 0.3f), Styling.PulseMedium);
            drawList.AddCircleFilled(center, radius, Paint.Col(Styling.WithAlpha(color, 0.22f)));
            drawList.AddCircle(center, radius, Paint.Col(border), 0, 2f * scale);
            drawList.AddCircleFilled(center, radius * 0.38f, Paint.Col(color));
            return;
        }

        drawList.AddCircleFilled(center, radius, Paint.Col(Styling.WithAlpha(Styling.Surface0, 0.9f)));
        drawList.AddCircle(center, radius, Paint.Col(Styling.WithAlpha(Styling.BorderDim, 0.9f)), 0, 1.4f * scale);
    }

    private static void DrawNote(FontAwesomeIcon icon, string text, Vector4 iconColor, Vector4 textColor)
    {
        var scale = ImGuiHelpers.GlobalScale;
        Styling.VSpace(NoteGap);
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var padX = PadX * scale;
        using (Fonts.PushCaption())
        {
            var lineHeight = ImGui.GetTextLineHeight();
            var iconSize = TextDraw.IconSize(icon);
            TextDraw.Icon(icon, new Vector2(origin.X + padX, origin.Y + (lineHeight - iconSize.Y) * 0.5f), iconColor);
            var textX = origin.X + padX + iconSize.X + 8f * scale;
            var shown = TextDraw.Truncate(text, origin.X + width - textX);
            TextDraw.At(shown, new Vector2(textX, origin.Y), textColor);
            if (!ReferenceEquals(shown, text) && Hit.HoveringRect(origin, origin + new Vector2(width, lineHeight)))
            {
                Tooltip.Show(text);
            }

            ImGui.Dummy(new Vector2(width, lineHeight));
        }
    }

    private static void DrawShopperCard(RunModel run)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, ShopperCardHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var drawList = ImGui.GetWindowDrawList();
        Paint.Glass(drawList, origin, end, Styling.CardRounding * scale, Styling.AccentBlue, 0.05f);

        var padX = PadX * scale;
        var midY = origin.Y + size.Y * 0.5f;
        var iconBox = ShopperIconBox * scale;
        var iconCenter = new Vector2(origin.X + padX + iconBox * 0.5f, midY);
        drawList.AddCircleFilled(iconCenter, iconBox * 0.5f, Paint.Col(Styling.WithAlpha(Styling.AccentBlue, 0.16f)));
        TextDraw.IconCentered(FontAwesomeIcon.ShoppingCart, iconCenter, Styling.AccentBlueSoft);

        var spent = spentText.Get(run.DryRun ? -run.ConfirmedSpend - 1 : run.ConfirmedSpend, static key => key < 0
            ? Loc.T(L.Running.ShopperWouldSpend, Formatting.Gil(-key - 1))
            : Loc.T(L.Running.ShopperSpent, Formatting.Gil(key)));
        var spentWidth = TextDraw.Measure(spent).X;
        TextDraw.At(spent, new Vector2(end.X - padX - spentWidth, midY - ImGui.GetTextLineHeight() * 0.5f), Styling.AccentMintSoft);

        var x = iconCenter.X + iconBox * 0.5f + 14f * scale;
        var width = MathF.Max(0f, end.X - padX - spentWidth - 14f * scale - x);
        var lineHeight = ImGui.GetTextLineHeight();
        var captionHeight = TextDraw.CaptionLineHeight();

        var top = midY - (captionHeight + 3f * scale + lineHeight) * 0.5f;
        TextDraw.SmallCaps(Loc.T(L.Running.ShopperTitle), new Vector2(x, top), Styling.TextMuted);
        var status = run.ShopperStatus.Length > 0 ? run.ShopperStatus : Loc.T(L.Running.ShopperWaiting);
        TextDraw.At(TextDraw.Truncate(status, width), new Vector2(x, top + captionHeight + 3f * scale), Styling.TextStrong);
        ImGui.Dummy(size);
    }

    private static void DrawRows(RunModel run, IRowLabels labels)
    {
        var rows = run.Rows;
        if (rows.Length == 0)
        {
            return;
        }

        GroupLabel.Draw(Loc.T(L.Running.Requests), SectionGap + 4f, 6f);
        var scale = ImGuiHelpers.GlobalScale;
        var width = ImGui.GetContentRegionAvail().X;
        var top = ImGui.GetCursorScreenPos();
        var height = RowHeight * scale;
        var stride = height + RowGap * scale;
        for (var index = 0; index < rows.Length; index++)
        {
            var min = new Vector2(top.X, top.Y + index * stride);
            var max = min + new Vector2(width, height);
            if (!ImGui.IsRectVisible(min, max))
            {
                continue;
            }

            DrawRow(rows.ItemRef(index), index, labels, min, max);
        }

        ImGui.SetCursorScreenPos(top);
        ImGui.Dummy(new Vector2(width, rows.Length * stride - RowGap * scale));
    }

    private static void DrawRow(in RunRow row, int rowIndex, IRowLabels labels, Vector2 min, Vector2 max)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var drawList = ImGui.GetWindowDrawList();
        var active = RunLabels.IsActive(row.State);
        var color = RunLabels.RowStateColor(row.State);
        var hovered = Hit.HoveringRect(min, max);
        var rounding = 8f * scale;
        var fill = Styling.WithAlpha(hovered ? Styling.Surface2 : Styling.Surface1, hovered ? 0.7f : 0.45f);
        Paint.Fill(drawList, min, max, active ? Styling.Tint(fill, color, 0.10f) : fill, rounding);
        if (active)
        {
            Paint.Stroke(drawList, min, max, Styling.PulseColor(Styling.WithAlpha(color, 0.35f), Styling.WithAlpha(color, 0.8f), Styling.PulseMedium), rounding);
        }

        var padX = RowPadX * scale;
        var midY = (min.Y + max.Y) * 0.5f;
        var stateColumn = RowStateColumn * scale;
        var icon = RunLabels.RowStateIcon(row.State);
        var iconSize = TextDraw.IconSize(icon);
        TextDraw.Icon(icon, new Vector2(min.X + padX + (stateColumn - iconSize.X) * 0.5f, midY - iconSize.Y * 0.5f), active ? Styling.PulseColor(color, Styling.Lighten(color, 0.4f)) : color);

        var x = min.X + padX + stateColumn + 10f * scale;
        var classSize = RowClassIconSize * scale;
        ItemIcon.Draw(drawList, SupplyClasses.IconId(row.ClassJobId), new Vector2(x, midY - classSize * 0.5f), classSize);
        x += classSize + 8f * scale;

        var itemSize = RowItemIconSize * scale;
        ItemIcon.Draw(drawList, labels.ItemIconId(row.ItemId), new Vector2(x, midY - itemSize * 0.5f), itemSize, row.Quality == ItemQuality.High);
        x += itemSize + 10f * scale;

        var itemName = labels.ItemName(row.ItemId);
        var name = itemName.Length > 0 ? itemName : SupplyClasses.Abbreviation(row.ClassJobId);
        var stateLabel = RunLabels.RowStateLabel(row.State);
        float badgeWidth;
        using (Fonts.PushCaption())
        {
            badgeWidth = TextDraw.Measure(stateLabel).X + 14f * scale;
        }

        var right = max.X - padX;
        Badge.Draw(drawList, stateLabel, color, right, midY);
        var textRight = right - badgeWidth - 12f * scale;
        var lineY = midY - ImGui.GetTextLineHeight() * 0.5f;
        var quantity = quantityTexts[rowIndex].Get(row.Requested, static requested => Loc.T(L.Requests.Quantity, requested));
        var quantityGap = QuantityGap * scale;
        var quantityWidth = TextDraw.Measure(quantity).X;
        var shown = TextDraw.Truncate(name, MathF.Max(0f, textRight - x - quantityGap - quantityWidth));
        TextDraw.At(shown, new Vector2(x, lineY), RunLabels.IsDelivered(row.State) ? Styling.AccentMintSoft : Styling.TextStrong);
        var quantityX = x + TextDraw.Measure(shown).X + quantityGap;
        TextDraw.At(quantity, new Vector2(quantityX, lineY), Styling.AccentGlowSoft);
        using (Fonts.PushCaption())
        {
            TextDraw.Trailing(labels.ClassName(row.ClassJobId), quantityX + quantityWidth, textRight, midY - ImGui.GetTextLineHeight() * 0.5f, Styling.TextMuted);
        }

        if (hovered && row.Detail.Length > 0)
        {
            Tooltip.Show(row.Detail);
        }
    }
}
