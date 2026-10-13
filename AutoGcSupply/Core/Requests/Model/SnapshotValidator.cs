namespace AutoGcSupply.Core.Requests.Model;

public static class SnapshotValidator
{
    public const byte MinimumRequested = 1;
    public const byte MaximumRequested = 99;

    public static SnapshotLoadState Validate(ReadOnlySpan<SupplyRequest> requests, Func<uint, bool> itemExists)
    {
        if (requests.Length != SnapshotParser.EntryCount)
        {
            return SnapshotLoadState.Invalid;
        }

        if (!LooksLoaded(requests))
        {
            return SnapshotLoadState.NotLoaded;
        }

        return IsConsistent(requests, itemExists) ? SnapshotLoadState.Loaded : SnapshotLoadState.Invalid;
    }

    private static bool LooksLoaded(ReadOnlySpan<SupplyRequest> requests)
    {
        var anyItem = false;
        for (var index = 0; index < requests.Length; index++)
        {
            var request = requests[index];
            if (request.ItemId == 0 && (request.Open || request.ClassJobId == 0))
            {
                return false;
            }

            anyItem |= request.ItemId != 0;
        }

        return anyItem;
    }

    private static bool IsConsistent(ReadOnlySpan<SupplyRequest> requests, Func<uint, bool> itemExists)
    {
        var seenClasses = 0;
        for (var index = 0; index < requests.Length; index++)
        {
            var request = requests[index];
            if (!SupplyClasses.Contains(request.ClassJobId))
            {
                return false;
            }

            var classBit = 1 << SupplyClasses.DisplayIndex(request.ClassJobId);
            if ((seenClasses & classBit) != 0)
            {
                return false;
            }

            seenClasses |= classBit;
            if (request.Open && request.Requested is < MinimumRequested or > MaximumRequested)
            {
                return false;
            }

            if (request.ItemId != 0 && !itemExists(request.ItemId))
            {
                return false;
            }
        }

        return true;
    }
}
