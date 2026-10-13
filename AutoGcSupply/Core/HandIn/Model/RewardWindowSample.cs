namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct RewardWindowSample(bool Readable, string ItemName, int Seals)
{
    public static RewardWindowSample Unreadable { get; } = new(false, string.Empty, 0);
}
