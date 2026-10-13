namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct SupplyListRow(int RowIndex, uint ItemId, byte ClassJobId, int Requested, bool Starred);
