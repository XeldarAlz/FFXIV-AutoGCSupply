using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;

namespace AutoGcSupply;

public sealed partial class Configuration
{
    public const int MinimumPriceTolerancePercent = 0;
    public const int MaximumPriceTolerancePercent = 200;
    public const long MaximumRunSpendCap = SpendCapRule.Maximum;

    public bool IncludeMaxLevelClasses { get; set; } = false;

    public bool UseOwnedItems { get; set; } = true;

    public SupplyQualityPolicy SupplyQuality { get; set; } = SupplyQualityPolicy.Automatic;

    public int PriceTolerancePercent { get; set; } = 25;

    public SealCapPolicy SealCapPolicy { get; set; } = SealCapPolicy.DeliverAnyway;

    public long RunSpendCap { get; set; } = 0;

    public Dictionary<ulong, ushort> ExcludedClassesByCharacter { get; set; } = [];

    private bool RepairRunSettings()
    {
        var changed = false;
        if (ExcludedClassesByCharacter is null)
        {
            ExcludedClassesByCharacter = [];
            changed = true;
        }

        var tolerance = Math.Clamp(PriceTolerancePercent, MinimumPriceTolerancePercent, MaximumPriceTolerancePercent);
        if (tolerance != PriceTolerancePercent)
        {
            PriceTolerancePercent = tolerance;
            changed = true;
        }

        var cap = Math.Clamp(RunSpendCap, 0, MaximumRunSpendCap);
        if (cap != RunSpendCap)
        {
            RunSpendCap = cap;
            changed = true;
        }

        return changed;
    }
}
