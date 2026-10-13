using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace AutoGcSupply.Core.Requests.Model;

public readonly record struct RequestSnapshot(
    ulong ContentId,
    DayKey Day,
    DateTime ReadAtUtc,
    SnapshotLoadState LoadState,
    ImmutableArray<SupplyRequest> Requests)
{
    public bool Loaded => LoadState == SnapshotLoadState.Loaded;

    public bool AllDone => Loaded && OpenCount == 0;

    public int OpenCount
    {
        get
        {
            var open = 0;
            for (var index = 0; index < Requests.Length; index++)
            {
                if (Requests[index].Open)
                {
                    open++;
                }
            }

            return open;
        }
    }

    public static RequestSnapshot Create(ulong contentId, DateTime readAtUtc, SupplyRequest[] requests, Func<uint, bool> itemExists)
    {
        var loadState = SnapshotValidator.Validate(requests, itemExists);
        return new RequestSnapshot(contentId, DayKey.For(readAtUtc), readAtUtc, loadState, ImmutableCollectionsMarshal.AsImmutableArray(requests));
    }

    public static RequestSnapshot Unread(DateTime readAtUtc)
        => new(0, DayKey.For(readAtUtc), readAtUtc, SnapshotLoadState.NotLoaded, ImmutableArray<SupplyRequest>.Empty);

    public bool TryFind(byte classJobId, out SupplyRequest request)
    {
        for (var index = 0; index < Requests.Length; index++)
        {
            if (Requests[index].ClassJobId == classJobId)
            {
                request = Requests[index];
                return true;
            }
        }

        request = default;
        return false;
    }
}
