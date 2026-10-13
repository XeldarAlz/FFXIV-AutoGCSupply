using AutoGcSupply.Core.Requests.Model;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace AutoGcSupply.Core.Requests;

internal static unsafe class GcSupplyReader
{
    private static readonly Func<uint, bool> itemExists = ItemExists;

    public static RequestSnapshot Read()
    {
        var readAtUtc = DateTime.UtcNow;
        if (!Svc.ClientState.IsLoggedIn)
        {
            return RequestSnapshot.Unread(readAtUtc);
        }

        Span<byte> raw = stackalloc byte[SnapshotParser.TotalBytes];
        new ReadOnlySpan<byte>(&UIState.Instance()->GCSupply, SnapshotParser.TotalBytes).CopyTo(raw);
        return RequestSnapshot.Create(Svc.PlayerState.ContentId, readAtUtc, SnapshotParser.Parse(raw), itemExists);
    }

    public static void Log(in RequestSnapshot snapshot)
    {
        RunLog.Info($"Supply requests for day {snapshot.Day}: {snapshot.LoadState}, {snapshot.OpenCount} open of {snapshot.Requests.Length}.");
        for (var index = 0; index < snapshot.Requests.Length; index++)
        {
            var request = snapshot.Requests[index];
            RunLog.Debug(
                $"Slot {request.Slot}: {SupplyClasses.Abbreviation(request.ClassJobId)} item {request.ItemId} x{request.Requested}, " +
                $"remaining {request.RemainingToHandIn}, starred {request.Starred}, flag19 {request.NoHighQualityFlag}, " +
                $"exp {request.ExperienceReward}, seals {request.SealReward}");
        }
    }

    private static bool ItemExists(uint itemId)
        => Svc.Data.GetExcelSheet<Item>().GetRowOrDefault(itemId) is { } item && !item.Name.IsEmpty;
}
