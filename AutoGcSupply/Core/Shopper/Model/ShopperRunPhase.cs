namespace AutoGcSupply.Core.Shopper.Model;

internal enum ShopperRunPhase : byte
{
    Unknown,
    Idle,
    Preparing,
    Travelling,
    Finishing,
    Paused,
    Shopping,
    Stopping,
}
