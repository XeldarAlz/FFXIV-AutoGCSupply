using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;

namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct HandInTarget(uint ItemId, byte ClassJobId, int Requested, ItemQuality Quality, int SealReward)
{
    public SupplyListTab Tab => SupplyClasses.KindOf(ClassJobId) == SupplyMissionKind.Provisioning ? SupplyListTab.Provisioning : SupplyListTab.Supply;

    public bool IsValid
        => ItemId != 0
        && Requested > 0
        && SealReward >= 0
        && SupplyClasses.Contains(ClassJobId)
        && !(Tab == SupplyListTab.Provisioning && Quality == ItemQuality.High);
}
