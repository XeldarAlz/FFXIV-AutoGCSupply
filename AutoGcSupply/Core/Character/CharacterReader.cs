using AutoGcSupply.Core.Character.Model;
using AutoGcSupply.Core.Game.Player;
using AutoGcSupply.Core.Requests.Model;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace AutoGcSupply.Core.Character;

internal static unsafe class CharacterReader
{
    private const byte NoRank = 0;

    public static bool TryRead(out CharacterState state)
    {
        if (!Svc.ClientState.IsLoggedIn || !Svc.PlayerState.IsLoaded)
        {
            state = default;
            return false;
        }

        var player = PlayerState.Instance();
        var grandCompany = player->GrandCompany;
        var inGrandCompany = grandCompany != GrandCompanyId.None;
        var rank = inGrandCompany ? player->GetGrandCompanyRank() : NoRank;
        var seals = inGrandCompany ? InventoryManager.Instance()->GetCompanySeals(grandCompany) : 0u;
        state = new CharacterState(
            Svc.PlayerState.ContentId,
            Svc.PlayerState.HomeWorld.RowId,
            Svc.PlayerState.CurrentWorld.RowId,
            grandCompany,
            rank,
            seals,
            SealCapFor(rank),
            player->MaxLevel,
            ReadClasses(player));
        return true;
    }

    public static uint SealCap() => SealCapFor(PlayerState.Instance()->GetGrandCompanyRank());

    private static uint SealCapFor(byte rank)
        => rank == NoRank ? 0u : Svc.Data.GetExcelSheet<GrandCompanyRank>().GetRowOrDefault(rank)?.MaxSeals ?? 0u;

    private static ImmutableArray<ClassProgress> ReadClasses(PlayerState* player)
    {
        var classJobs = Svc.Data.GetExcelSheet<ClassJob>();
        var growth = Svc.Data.GetExcelSheet<ParamGrow>();
        var order = SupplyClasses.DisplayOrder;
        var classes = new ClassProgress[order.Length];
        for (var index = 0; index < order.Length; index++)
        {
            var classJobId = order[index];
            var experienceIndex = classJobs.GetRow(classJobId).ExpArrayIndex;
            var level = (byte)player->ClassJobLevels[experienceIndex];
            var experienceToNext = growth.GetRowOrDefault(level)?.ExpToNext ?? 0;
            classes[index] = new ClassProgress(classJobId, level, player->ClassJobExperience[experienceIndex], experienceToNext);
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(classes);
    }
}
