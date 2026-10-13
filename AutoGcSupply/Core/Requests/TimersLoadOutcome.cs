namespace AutoGcSupply.Core.Requests;

public enum TimersLoadOutcome : byte
{
    Cancelled = 0,
    Loaded = 1,
    NotLoaded = 2,
    Invalid = 3,
    PlayerBusy = 4,
}
