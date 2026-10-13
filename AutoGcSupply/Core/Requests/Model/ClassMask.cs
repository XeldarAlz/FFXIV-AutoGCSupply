namespace AutoGcSupply.Core.Requests.Model;

public readonly record struct ClassMask(ushort Bits)
{
    public static readonly ClassMask None = new(0);

    public bool Contains(byte classJobId)
        => SupplyClasses.Contains(classJobId) && (Bits & BitOf(classJobId)) != 0;

    public ClassMask With(byte classJobId, bool present)
    {
        if (!SupplyClasses.Contains(classJobId))
        {
            throw new ArgumentOutOfRangeException(nameof(classJobId), classJobId, "Not a supply or provisioning class.");
        }

        var bit = BitOf(classJobId);
        return new ClassMask((ushort)(present ? Bits | bit : Bits & ~bit));
    }

    private static int BitOf(byte classJobId) => 1 << SupplyClasses.DisplayIndex(classJobId);
}
