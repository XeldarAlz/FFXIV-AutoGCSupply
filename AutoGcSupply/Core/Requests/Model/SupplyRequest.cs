namespace AutoGcSupply.Core.Requests.Model;

public readonly record struct SupplyRequest(
    byte Slot,
    byte ClassJobId,
    uint ItemId,
    uint ExperienceReward,
    uint SealReward,
    byte Requested,
    byte RemainingToHandIn,
    bool Starred)
{
    public byte NoHighQualityFlag { get; init; }

    public bool Open => RemainingToHandIn > 0;

    public SupplyMissionKind Kind => SupplyClasses.KindOf(ClassJobId);
}
