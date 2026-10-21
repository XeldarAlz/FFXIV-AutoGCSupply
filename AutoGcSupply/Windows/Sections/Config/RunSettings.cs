using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Value.Model;
using AutoGcSupply.Windows.Components;

namespace AutoGcSupply.Windows.Sections.Config;

internal static class RunSettings
{
    private const float GilFieldWidth = 150f;
    private const string QualityId = "##agcs_run_quality";
    private const string SealCapId = "##agcs_run_seal_cap";

    private static readonly SupplyQualityPolicy[] qualityOrder = [SupplyQualityPolicy.Automatic, SupplyQualityPolicy.AlwaysHigh, SupplyQualityPolicy.AlwaysNormal];
    private static readonly SealCapPolicy[] sealCapOrder = [SealCapPolicy.DeliverAnyway, SealCapPolicy.SkipRow];

    private static readonly SettingsControls.Choices.Choice[] qualityChoices =
    [
        new(L.RunSettings.QualityAutomatic, L.RunSettings.QualityAutomaticDetail),
        new(L.RunSettings.QualityHigh, L.RunSettings.QualityHighDetail),
        new(L.RunSettings.QualityNormal, L.RunSettings.QualityNormalDetail),
    ];

    private static readonly SettingsControls.Choices.Choice[] sealCapChoices =
    [
        new(L.RunSettings.SealCapDeliver, L.RunSettings.SealCapDeliverDetail),
        new(L.RunSettings.SealCapSkip, L.RunSettings.SealCapSkipDetail),
    ];

    private static readonly GilField.Buffer spendCapBuffer = new();

    public static void Draw(Configuration configuration)
    {
        DrawRequestsGroup(configuration);
        DrawBuyingGroup(configuration);
        DrawHandInGroup(configuration);
        PostRunSettings.Draw(configuration);
    }

    private static void DrawRequestsGroup(Configuration configuration)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.RunSettings.GroupRequests));

        SettingsRow.Draw(Loc.T(L.RunSettings.IncludeMaxLevel),
            Loc.T(L.RunSettings.IncludeMaxLevelHelp),
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(configuration, () => configuration.IncludeMaxLevelClasses, value => configuration.IncludeMaxLevelClasses = value, "##agcs_run_max_level"),
            SettingsRow.ToggleHeight);

        SettingsRow.Draw(Loc.T(L.RunSettings.UseOwned),
            Loc.T(L.RunSettings.UseOwnedHelp),
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(configuration, () => configuration.UseOwnedItems, value => configuration.UseOwnedItems = value, "##agcs_run_use_owned"),
            SettingsRow.ToggleHeight);
    }

    private static void DrawBuyingGroup(Configuration configuration)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.RunSettings.GroupBuying));

        var quality = Index(qualityOrder, configuration.SupplyQuality);
        SettingsRow.Draw(Loc.T(L.RunSettings.Quality),
            Loc.T(L.RunSettings.QualityHelp),
            SettingsControls.RowComboWidth,
            () => SettingsControls.Choices.DrawCombo(QualityId, qualityChoices, quality, choice =>
            {
                configuration.SupplyQuality = qualityOrder[choice];
                configuration.SaveDebounced();
            }));
        SettingsRow.Caption(Loc.T(qualityChoices[quality].Detail));

        SettingsRow.Draw(Loc.T(L.RunSettings.Tolerance),
            Loc.T(L.RunSettings.ToleranceHelp),
            SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(configuration, "##agcs_run_tolerance",
                () => configuration.PriceTolerancePercent, value => configuration.PriceTolerancePercent = value,
                Configuration.MinimumPriceTolerancePercent, Configuration.MaximumPriceTolerancePercent, Loc.T(L.RunSettings.ToleranceFormat)));

        SettingsRow.Draw(Loc.T(L.RunSettings.DefaultSpendCap),
            Loc.T(L.RunSettings.DefaultSpendCapHelp),
            GilFieldWidth,
            () =>
            {
                if (GilField.Draw("##agcs_run_default_cap", spendCapBuffer, configuration.RunSpendCap, Configuration.MaximumRunSpendCap,
                        Loc.T(L.RunSettings.DefaultSpendCapHint), GilFieldWidth, out var value))
                {
                    configuration.RunSpendCap = value;
                    configuration.SaveDebounced();
                }
            });
    }

    private static void DrawHandInGroup(Configuration configuration)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.RunSettings.GroupHandIn));

        var sealCap = Index(sealCapOrder, configuration.SealCapPolicy);
        SettingsRow.Draw(Loc.T(L.RunSettings.SealCap),
            Loc.T(L.RunSettings.SealCapHelp),
            SettingsControls.RowComboWidth,
            () => SettingsControls.Choices.DrawCombo(SealCapId, sealCapChoices, sealCap, choice =>
            {
                configuration.SealCapPolicy = sealCapOrder[choice];
                configuration.SaveDebounced();
            }));
        SettingsRow.Caption(Loc.T(sealCapChoices[sealCap].Detail));
    }

    private static int Index<T>(T[] order, T value) where T : struct, Enum
    {
        for (var index = 0; index < order.Length; index++)
        {
            if (EqualityComparer<T>.Default.Equals(order[index], value))
            {
                return index;
            }
        }

        return 0;
    }
}
