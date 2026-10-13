using AutoGcSupply.Core.Requests.Model;

namespace AutoGcSupply.Core.Value.Model;

public readonly record struct DecisionSettings(
    bool IncludeMaxLevelClasses,
    bool UseOwnedItems,
    SupplyQualityPolicy SupplyQuality,
    int PriceTolerancePercent,
    ClassMask ExcludedClasses);
