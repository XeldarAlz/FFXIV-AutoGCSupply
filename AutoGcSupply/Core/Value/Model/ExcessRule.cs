namespace AutoGcSupply.Core.Value.Model;

public static class ExcessRule
{
    private const int SingleUnit = 1;

    public static int Allowance(int requested) => requested <= SingleUnit ? 0 : requested;
}
