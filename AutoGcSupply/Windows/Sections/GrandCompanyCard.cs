using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Tasks;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoGcSupply.Windows.Sections;

internal static class GrandCompanyCard
{
    private const float CardHeight = 76f;
    private const float PadX = 18f;
    private const float IconBox = 40f;
    private const float InsigniaSize = 30f;
    private const float ColumnGap = 14f;
    private const float BarWidth = 220f;
    private const float BarHeight = 6f;
    private const float LineGap = 6f;
    private const float ResetColumnWidth = 110f;
    private const float RefreshSize = 30f;
    private const float RefreshGap = 12f;
    private const float WarnShare = 0.9f;
    private const int SealKeyShift = 32;

    private static CachedText sealsText;
    private static CachedText countdownText;
    private static CachedText resetHelpText;

    public static void Draw(GcRunController controller, in TodayHeader header)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, CardHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var drawList = ImGui.GetWindowDrawList();
        Paint.Glass(drawList, origin, end, Styling.CardRounding * scale, Styling.AccentNebula, 0.05f);

        var padX = PadX * scale;
        var midY = origin.Y + size.Y * 0.5f;
        var iconBox = IconBox * scale;
        var iconCenter = new Vector2(origin.X + padX + iconBox * 0.5f, midY);
        drawList.AddCircleFilled(iconCenter, iconBox * 0.5f, Paint.Col(Styling.WithAlpha(Styling.AccentNebula, 0.18f)));
        var insignia = GrandCompanyText.RankIcon(header.GrandCompany, header.Rank);
        if (insignia != 0)
        {
            var insigniaSize = InsigniaSize * scale;
            ItemIcon.Draw(drawList, insignia, iconCenter - new Vector2(insigniaSize, insigniaSize) * 0.5f, insigniaSize);
        }
        else
        {
            TextDraw.IconCentered(FontAwesomeIcon.Flag, iconCenter, Styling.AccentGlowSoft);
        }

        var refreshSize = RefreshSize * scale;
        var refreshX = end.X - padX - refreshSize;
        ImGui.SetCursorScreenPos(new Vector2(refreshX, midY - refreshSize * 0.5f));
        if (IconButton.Draw(FontAwesomeIcon.SyncAlt, "##agcs_today_refresh", refreshSize, Styling.TextSecondary, Loc.T(L.Requests.Refresh)))
        {
            controller.Refresh();
        }

        var resetRight = refreshX - RefreshGap * scale;
        DrawReset(header, resetRight, midY);

        var contentX = iconCenter.X + iconBox * 0.5f + ColumnGap * scale;
        var contentRight = resetRight - ResetColumnWidth * scale - ColumnGap * scale;
        DrawIdentity(drawList, header, contentX, contentRight, midY);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
    }

    private static void DrawIdentity(ImDrawListPtr drawList, in TodayHeader header, float x, float right, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var width = MathF.Max(0f, right - x);
        float nameHeight;
        using (Fonts.PushHeadline())
        {
            nameHeight = ImGui.GetTextLineHeight();
        }

        float captionHeight;
        using (Fonts.PushCaption())
        {
            captionHeight = ImGui.GetTextLineHeight();
        }

        var top = midY - (nameHeight + LineGap * scale + captionHeight) * 0.5f;
        var name = GrandCompanyText.CompanyName(header.GrandCompany);
        float nameWidth;
        using (Fonts.PushHeadline())
        {
            var shown = TextDraw.Truncate(name, width * 0.6f);
            nameWidth = TextDraw.Measure(shown).X;
            TextDraw.At(shown, new Vector2(x, top), Styling.TextStrong);
        }

        var rank = GrandCompanyText.RankName(header.GrandCompany, header.Rank);
        var badgeX = x + nameWidth + 10f * scale;
        var badgeRoom = x + width - badgeX;
        if (badgeRoom > 40f * scale)
        {
            using (Fonts.PushCaption())
            {
                Badge.DrawLeft(drawList, TextDraw.Truncate(rank, badgeRoom - 16f * scale), Styling.AccentNebula, badgeX, top + nameHeight * 0.5f);
            }
        }

        var barY = top + nameHeight + LineGap * scale;
        var label = Loc.T(L.Requests.Seals);
        var labelSize = TextDraw.SmallCapsSize(label);
        TextDraw.SmallCaps(label, new Vector2(x, barY + (captionHeight - labelSize.Y) * 0.5f), Styling.TextMuted);
        var barX = x + labelSize.X + 10f * scale;
        var barWidth = MathF.Max(0f, MathF.Min(BarWidth * scale, x + width - barX - 120f * scale));
        var fraction = header.SealCap == 0 ? 0f : (float)header.Seals / header.SealCap;
        var barColor = fraction >= WarnShare ? Styling.AccentAmber : Styling.AccentMint;
        if (barWidth > 0f)
        {
            Paint.Bar(drawList, new Vector2(barX, barY + (captionHeight - BarHeight * scale) * 0.5f), barWidth, BarHeight * scale, fraction, barColor);
        }

        var value = sealsText.Get(((long)header.Seals << SealKeyShift) | header.SealCap,
            static packed => Loc.T(L.Requests.SealsValue, ((packed >> SealKeyShift) & uint.MaxValue).ToString("N0", Loc.Culture), (packed & uint.MaxValue).ToString("N0", Loc.Culture)));
        var valueX = barX + barWidth + (barWidth > 0f ? 10f * scale : 0f);
        using (Fonts.PushCaption())
        {
            TextDraw.At(TextDraw.Truncate(value, MathF.Max(0f, x + width - valueX)), new Vector2(valueX, barY), fraction >= WarnShare ? Styling.AccentAmberSoft : Styling.TextSecondary);
        }

        if (Hit.HoveringRect(new Vector2(x, barY), new Vector2(x + width, barY + captionHeight)))
        {
            Tooltip.Show(Loc.T(L.Requests.SealsHelp));
        }
    }

    private static void DrawReset(in TodayHeader header, float right, float midY)
    {
        if (header.NextResetUtc == default)
        {
            return;
        }

        var scale = ImGuiHelpers.GlobalScale;
        var remaining = header.NextResetUtc - DateTime.UtcNow;
        if (remaining < TimeSpan.Zero)
        {
            remaining = TimeSpan.Zero;
        }

        var minutes = (long)Math.Ceiling(remaining.TotalMinutes);
        var countdown = countdownText.Get(minutes, static value => Formatting.Countdown(TimeSpan.FromMinutes(value)));
        var label = Loc.T(L.Requests.ResetsIn);
        var labelSize = TextDraw.SmallCapsSize(label);
        Vector2 valueSize;
        using (Fonts.PushHeadline())
        {
            valueSize = TextDraw.Measure(countdown);
        }

        var gap = 3f * scale;
        var top = midY - (labelSize.Y + gap + valueSize.Y) * 0.5f;
        var width = MathF.Max(labelSize.X, valueSize.X);
        TextDraw.SmallCaps(label, new Vector2(right - labelSize.X, top), Styling.TextMuted);
        using (Fonts.PushHeadline())
        {
            TextDraw.At(countdown, new Vector2(right - valueSize.X, top + labelSize.Y + gap), Styling.TextStrong);
        }

        if (Hit.HoveringRect(new Vector2(right - width, top), new Vector2(right, top + labelSize.Y + gap + valueSize.Y)))
        {
            var help = resetHelpText.Get(header.NextResetUtc.Ticks, static ticks => Loc.T(L.Requests.ResetHelp, TodayHeadline.ResetTime(new DateTime(ticks, DateTimeKind.Utc))));
            Tooltip.Show(help);
        }
    }
}
