namespace AutoGcSupply.Core;

internal static class GcSupplyConstants
{
    public const string PrimaryCommand = "/gcsupply";
    public const string AliasCommand = "/agcs";

    public const string LogPrefix = "[AGCS]";

    public const int SaveThrottleMs = 500;

    internal static class ThrottleKeys
    {
        public const string Save = "AutoGcSupply.Save";
    }
}
