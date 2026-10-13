namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct SupplyAgentItem(uint ItemId, int Position, int NumPossessed, int NumRequested, bool TurnInAvailable);
