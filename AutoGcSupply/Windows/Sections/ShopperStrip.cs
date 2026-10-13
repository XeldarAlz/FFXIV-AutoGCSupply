using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Tasks;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoGcSupply.Windows.Sections;

internal static class ShopperStrip
{
    private const float LineHeight = 26f;
    private const float DotRadius = 3.5f;
    private const float DotGap = 10f;
    private const float LabelGap = 8f;
    private const float ItemGap = 10f;
    private const float SpinnerRadius = 5f;
    private const float SpinnerThickness = 2f;
    private const float PillHeight = 24f;

    public static bool Draw(GcRunController controller, TodayModel today)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var height = LineHeight * scale;
        var midY = origin.Y + height * 0.5f;
        var drawList = ImGui.GetWindowDrawList();
        var accent = today.Shopper switch
        {
            ShopperState.Available => Styling.AccentMint,
            ShopperState.Outdated or ShopperState.Newer => Styling.AccentAmber,
            _ => Styling.AccentRose,
        };

        var radius = DotRadius * scale;
        Paint.Dot(drawList, new Vector2(origin.X + radius + 1f * scale, midY), radius, accent);
        var x = origin.X + radius * 2f + DotGap * scale;
        var label = Loc.T(L.Requests.ShopperLabel);
        var labelSize = TextDraw.SmallCapsSize(label);
        TextDraw.SmallCaps(label, new Vector2(x, midY - labelSize.Y * 0.5f), Styling.TextMuted);
        x += labelSize.X + LabelGap * scale;

        var pricingWidth = PricingWidth(today.Pricing);
        var right = origin.X + width;
        var openPlugins = false;
        var gap = ItemGap * scale;

        void Place(float needed)
        {
            if (x > origin.X && x + needed > right)
            {
                x = origin.X;
                midY += height + 6f * scale;
            }
        }

        if (today.Shopper == ShopperState.Available)
        {
            var chip = Loc.T(today.ShopperDryRun ? L.Requests.DryRunOn : L.Requests.DryRunOff);
            var chipWidth = Badge.Width(chip);
            Place(chipWidth);
            Badge.DrawLeft(drawList, chip, today.ShopperDryRun ? Styling.AccentBlue : Styling.AccentAmber, x, midY);
            if (Hit.HoveringRect(new Vector2(x, midY - height * 0.5f), new Vector2(x + chipWidth, midY + height * 0.5f)))
            {
                Tooltip.Show(Loc.T(today.ShopperDryRun ? L.Requests.DryRunOnHelp : L.Requests.DryRunOffHelp));
            }

            x += chipWidth + gap;
            var change = Loc.T(L.Requests.ChangeInShopper);
            var buttonWidth = PillButton.Width(change, FontAwesomeIcon.ExternalLinkAlt);
            Place(buttonWidth);
            if (Pill("##agcs_shopper_change", change, Styling.AccentGlow, PillButton.Emphasis.Ghost, FontAwesomeIcon.ExternalLinkAlt, x, midY, null, right - x))
            {
                controller.OpenAutoShopper();
            }

            x += MathF.Min(buttonWidth, right - x) + gap;
        }
        else
        {
            if (today.Shopper != ShopperState.Newer)
            {
                var outdated = today.Shopper == ShopperState.Outdated;
                var action = Loc.T(outdated ? L.Requests.ShopperUpdate : L.Requests.ShopperInstall);
                var buttonWidth = PillButton.Width(action, FontAwesomeIcon.Plug);
                Place(buttonWidth);
                openPlugins = Pill("##agcs_shopper_setup", action, accent, PillButton.Emphasis.Tinted, FontAwesomeIcon.Plug,
                    x, midY, outdated ? Loc.T(L.Requests.ShopperUpdateHelp) : null, right - x);
                x += MathF.Min(buttonWidth, right - x) + gap;
            }

            var detail = Loc.T(today.Shopper == ShopperState.Newer ? L.Requests.ShopperNewer
                : today.Shopper == ShopperState.Outdated ? L.Requests.ShopperOutdated : L.Requests.ShopperMissing);
            float detailWidth;
            using (Fonts.PushCaption()) detailWidth = TextDraw.Measure(detail).X;
            Place(detailWidth);
            Caption(detail, x, right, midY, Styling.TextDim);
            x += MathF.Min(detailWidth, right - x) + gap;
        }

        if (pricingWidth > 0f)
        {
            Place(pricingWidth);
            DrawPricing(controller, today.Pricing, x, midY);
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, midY - origin.Y + height * 0.5f));
        return openPlugins;
    }

    private static float PricingWidth(PricingState pricing)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var retryWidth = pricing == PricingState.Failed ? PillButton.Width(Loc.T(L.Requests.PricingRetry), FontAwesomeIcon.Redo) : 0f;
        using (Fonts.PushCaption())
        {
            return pricing switch
            {
                PricingState.Pending => (SpinnerRadius * 2f + 8f) * scale + TextDraw.Measure(Loc.T(L.Requests.PricingPending)).X,
                PricingState.Queued => (SpinnerRadius * 2f + 8f) * scale + TextDraw.Measure(Loc.T(L.Requests.PricingQueued)).X,
                PricingState.Ready => (DotRadius * 2f + 8f) * scale + TextDraw.Measure(Loc.T(L.Requests.PricingReady)).X,
                PricingState.Failed => TextDraw.Measure(Loc.T(L.Requests.PricingFailed)).X + ItemGap * scale + retryWidth,
                _ => 0f,
            };
        }
    }

    private static void DrawPricing(GcRunController controller, PricingState pricing, float x, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var drawList = ImGui.GetWindowDrawList();
        var start = x;
        switch (pricing)
        {
            case PricingState.Pending:
                DrawWaiting(L.Requests.PricingPending, L.Requests.PricingPendingHelp, Styling.AccentBlue, Styling.AccentBlueSoft, x, midY);
                return;
            case PricingState.Queued:
                DrawWaiting(L.Requests.PricingQueued, L.Requests.PricingQueuedHelp, Styling.AccentAmber, Styling.AccentAmberSoft, x, midY);
                return;
            case PricingState.Ready:
            {
                var radius = DotRadius * scale;
                Paint.Dot(drawList, new Vector2(x + radius, midY), radius, Styling.AccentMint, 0.12f);
                x += radius * 2f + 8f * scale;
                Caption(Loc.T(L.Requests.PricingReady), x, float.MaxValue, midY, Styling.TextDim);
                HoverHelp(start, midY, L.Requests.PricingReadyHelp);
                return;
            }
            case PricingState.Failed:
            {
                var failed = Loc.T(L.Requests.PricingFailed);
                Caption(failed, x, float.MaxValue, midY, Styling.AccentRoseSoft);
                HoverHelp(start, midY, L.Requests.PricingFailedHelp);
                using (Fonts.PushCaption())
                {
                    x += TextDraw.Measure(failed).X + ItemGap * scale;
                }

                if (Pill("##agcs_pricing_retry", Loc.T(L.Requests.PricingRetry), Styling.AccentRose, PillButton.Emphasis.Ghost, FontAwesomeIcon.Redo, x, midY, null))
                {
                    controller.RetryPrices();
                }

                return;
            }
        }
    }

    private static void DrawWaiting(LocString caption, LocString help, Vector4 spinner, Vector4 text, float x, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var radius = SpinnerRadius * scale;
        var center = new Vector2(x + radius, midY);
        ProgressRing.Track(center, radius, SpinnerThickness * scale, Styling.WithAlpha(Styling.BorderDim, 0.7f));
        ProgressRing.Sweep(center, radius, SpinnerThickness * scale, spinner, Styling.PulseCalm, MathF.PI * 0.9f, 1f);
        Caption(Loc.T(caption), x + radius * 2f + 8f * scale, float.MaxValue, midY, text);
        HoverHelp(x, midY, help);
    }

    private static void HoverHelp(float x, float midY, LocString help)
    {
        var half = LineHeight * ImGuiHelpers.GlobalScale * 0.5f;
        if (Hit.HoveringRect(new Vector2(x, midY - half), new Vector2(ImGui.GetWindowPos().X + ImGui.GetWindowWidth(), midY + half)))
        {
            Tooltip.Show(Loc.T(help));
        }
    }

    private static bool Pill(string id, string label, Vector4 accent, PillButton.Emphasis emphasis, FontAwesomeIcon icon, float x, float midY, string? tooltip, float maxWidth = float.MaxValue)
    {
        ImGui.SetCursorScreenPos(new Vector2(x, midY - PillHeight * ImGuiHelpers.GlobalScale * 0.5f));
        return PillButton.Draw(id, label, accent, emphasis, icon, height: PillHeight, tooltip: tooltip, maxWidth: maxWidth);
    }

    private static void Caption(string text, float x, float right, float midY, Vector4 color)
    {
        using (Fonts.PushCaption())
        {
            var shown = TextDraw.Truncate(text, right - x);
            if (shown.Length == 0)
            {
                return;
            }

            TextDraw.At(shown, new Vector2(x, midY - ImGui.GetTextLineHeight() * 0.5f), color);
        }
    }
}
