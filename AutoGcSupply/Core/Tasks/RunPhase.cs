namespace AutoGcSupply.Core.Tasks;

internal enum RunPhase : byte { Idle, Preparing, Buying, Travelling, HandingIn, Finishing, Paused, Stopping }
