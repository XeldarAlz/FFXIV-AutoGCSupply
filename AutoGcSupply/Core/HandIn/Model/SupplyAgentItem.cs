using AutoGcSupply.Core.Requests.Model;

namespace AutoGcSupply.Core.HandIn.Model;

internal readonly record struct SupplyAgentItem(uint ItemId, int Position, int NumPossessed, int NumRequested, bool RequestOpen)
{
    public byte RawTurnInFlag { get; init; }

    public static bool TryCreate(uint itemId, int position, int possessed, int requested, byte rawTurnInFlag,
        in RequestSnapshot requests, out SupplyAgentItem item)
    {
        item = default;
        if (!requests.Loaded || position is < 0 or >= SupplyClasses.Count
            || !requests.TryFind((byte)(SupplyClasses.First + position), out var request)
            || (request.Open && (request.ItemId != itemId || request.Requested != requested)))
        {
            return false;
        }

        // The agent's availability flag is not a completion marker for daily missions.
        // Read completion from the same live daily data used by the Timers request list.
        item = new SupplyAgentItem(itemId, position, possessed, requested, request.Open) { RawTurnInFlag = rawTurnInFlag };
        return true;
    }
}
