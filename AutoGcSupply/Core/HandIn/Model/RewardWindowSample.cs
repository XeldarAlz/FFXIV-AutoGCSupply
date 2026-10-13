namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct RewardWindowSample(bool Readable, RewardWindowText[] Texts, RewardWindowInteger[] Integers)
{
    public static RewardWindowSample Unreadable { get; } = new(false, [], []);

    public bool TryFindInteger(int valueIndex, out int value)
    {
        for (var integerPosition = 0; integerPosition < Integers.Length; integerPosition++)
        {
            if (Integers[integerPosition].ValueIndex != valueIndex)
            {
                continue;
            }

            value = Integers[integerPosition].Value;
            return true;
        }

        value = 0;
        return false;
    }
}
