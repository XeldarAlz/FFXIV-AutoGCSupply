namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct HandInTarget(uint ItemId, byte ClassJobId, int Requested, HandInQuality Quality, int SealReward)
{
    private const byte FirstSupplyClassJob = 8;
    private const byte FirstProvisioningClassJob = 16;
    private const byte LastProvisioningClassJob = 18;

    public SupplyListTab Tab => ClassJobId >= FirstProvisioningClassJob ? SupplyListTab.Provisioning : SupplyListTab.Supply;

    public bool IsValid
        => ItemId != 0
        && Requested > 0
        && SealReward >= 0
        && ClassJobId is >= FirstSupplyClassJob and <= LastProvisioningClassJob
        && !(Tab == SupplyListTab.Provisioning && Quality == HandInQuality.High);
}
