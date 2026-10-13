using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;

namespace AutoGcSupply.Core.Value.Model;

public readonly record struct RowDecision
{
    public required SupplyRequest Request { get; init; }

    public required bool Included { get; init; }

    public required OwnedCount Owned { get; init; }

    public required RowCoverage Coverage { get; init; }

    public required ItemQuality ChosenQuality { get; init; }

    public required int BuyQuantity { get; init; }

    public required long UnitPrice { get; init; }

    public required long EstimatedTotal { get; init; }

    public required long MaxUnitPrice { get; init; }

    public required int ExcessAllowance { get; init; }

    public required long Experience { get; init; }

    public required long Seals { get; init; }

    public required double LevelShare { get; init; }

    public required long GilPerThousandExperience { get; init; }

    public bool NeedsPurchase => Included && Coverage == RowCoverage.ToBuy;

    public bool ReadyToHandIn => Included && Coverage == RowCoverage.Owned;
}
