namespace AutoGcSupply.Core;

internal static class GcSupplyConstants
{
    public const string PrimaryCommand = "/gcsupply";
    public const string AliasCommand = "/agcs";

    public const string LogPrefix = "[AGCS]";

    public const int SaveThrottleMs = 500;

    // Some plugin commands misbehave when several arrive in the same frame, so post-run commands are spaced out.
    public const int PostRunCommandSpacingMs = 500;

    internal static class ThrottleKeys
    {
        public const string Save = "AutoGcSupply.Save";
    }
}
