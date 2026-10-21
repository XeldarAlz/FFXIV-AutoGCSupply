using AutoGcSupply.Core.Ipc;
using clib.Enums;
using clib.Extensions;
using clib.TaskSystem;
using clib.Utils;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Network;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Numerics;
using System.Threading.Tasks;
using OnDispose = clib.Services.OnDispose;

namespace AutoGcSupply.Core.Tasks;

// The library's own movement methods live on TaskBase, which installs two native movement hooks per instance for its
// straight-line walk. This is the same travel, teleport and aethernet flow on top of vnavmesh alone, so a run can
// create an operation per leg for hours without exhausting the hook allocator.
internal sealed partial class MoveOp
{
    private const float DefaultArriveMeters = 3f;
    private const float TeleportCastCompleteSeconds = 5f;
    private const uint DismountGeneralActionId = 23;
    private const uint FlyingMountRouletteGeneralActionId = 24;
    private const string SelectStringAddon = "SelectString";
    private const string AethernetAddon = "TelepotTown";

    private static NavmeshIPC Navmesh => NavmeshIPC.Instance;

    private static IPlayerCharacter? Player => Svc.Objects.LocalPlayer;

    private async Task Navigate(Vector3 destination, MovementConfig config, Func<bool>? stopCondition)
    {
        await WaitUntil(() => Player.Available, "WaitingForPlayer");
        var tolerance = Math.Max(config.Tolerance ?? 0f, Navmesh.Tolerance());
        if (Player.WithinRange(destination, tolerance) || stopCondition?.Invoke() == true)
        {
            return;
        }

        if (config.Movement.HasFlag(MovementOptions.Mount) || config.Movement.HasFlag(MovementOptions.Fly))
        {
            await Mount();
        }

        await WaitUntil(() => Navmesh.IsReady() || Navmesh.BuildProgress() >= 0f, "WaitingForNavmesh");
        await WaitWhile(() => Navmesh.BuildProgress() >= 0f, "BuildingNavmesh");
        ErrorIf(!Navmesh.IsReady(), "Could not load the navmesh for this zone");
        await WaitWhile(Navmesh.IsPathfinding, "WaitingForPathfinding");
        CancelToken.ThrowIfCancellationRequested();

        // Registered with this operation, so Cancel stops the path at once instead of leaving it running.
        using (new OnDispose(StopOwnedPath))
        {
            var fly = Player.InFlight || (config.Movement.HasFlag(MovementOptions.Fly) && Control.CanFly);
            ErrorIf(!Navmesh.PathfindAndMoveCloseTo(destination, fly, config.Tolerance ?? DefaultArriveMeters), $"Could not start pathfinding to {destination}");
            Status = $"Moving to {destination}";
            await NextFrame();
            await WaitWhile(
                () => !Player.WithinRange(destination, tolerance) && stopCondition?.Invoke() != true && Navmesh.IsBusy(),
                "Navigate");

            // A path search still in flight would start walking once it lands, so a plain stop is not enough here.
            if (stopCondition?.Invoke() == true)
            {
                await StopPathfinding();
            }
        }

        if (config.Movement.HasFlag(MovementOptions.Dismount) && Player.WithinRange(destination, tolerance))
        {
            await Dismount();
        }
    }

    private async Task TeleportToTerritory(uint territoryId, Vector3 destination, bool allowSameZoneTeleport)
    {
        if (Svc.ClientState.TerritoryType == territoryId && !allowSameZoneTeleport)
        {
            return;
        }

        await StopPathfinding();
        await WaitWhile(() => Player.IsUiFading, "WaitingForUi");
        var closest = Coords.FindClosestAetheryte(territoryId, destination).GetValueOrDefault();
        var aetheryteId = Coords.FindPrimaryAetheryte(closest);
        ErrorIf(aetheryteId == 0, $"No unlocked aetheryte for territory {territoryId}");
        var aetheryte = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Aetheryte>().GetRow(aetheryteId);
        if (Svc.ClientState.TerritoryType != aetheryte.Territory.RowId || allowSameZoneTeleport)
        {
            Status = $"Teleporting to {aetheryte.PlaceName.Value.Name}";
            await CastTeleport(aetheryteId);
        }

        if (Svc.ClientState.TerritoryType != territoryId)
        {
            await UseAethernet(territoryId, destination);
        }

        ErrorIf(Svc.ClientState.TerritoryType != territoryId, $"Failed to reach territory {territoryId}");
    }

    private async Task CastTeleport(uint aetheryteId)
    {
        var arrived = false;
        while (!arrived)
        {
            await StopPathfinding();
            await WaitWhile(() => Player.IsBusy, "WaitingForTeleport");
            CancelToken.ThrowIfCancellationRequested();
            ErrorIf(!ActionManager.Teleport(aetheryteId), $"Teleport to aetheryte {aetheryteId} was rejected");
            arrived = await WaitForTeleportOutcome();
        }
    }

    // True once the destination loaded behind a UI fade. False when the cast ended with no fade, which is a cancelled
    // cast, so the caller casts again.
    private async Task<bool> WaitForTeleportOutcome()
    {
        var sawCast = false;
        var sawFade = false;
        while (true)
        {
            var fading = Player.IsUiFading;
            var casting = Player?.IsCasting == true;
            sawCast |= casting;
            sawFade |= fading;
            if (sawFade && !fading)
            {
                await WaitUntil(() => GameMain.IsTerritoryLoaded && Player.Interactable, "TeleportArrival");
                return true;
            }

            if (sawCast && !casting && !sawFade && ActionManager.GetCastAction().Elapsed != TeleportCastCompleteSeconds)
            {
                return false;
            }

            await NextFrame();
        }
    }

    private async Task UseAethernet(uint territoryId, Vector3 destination)
    {
        var source = Coords.FindClosestAetheryte(Svc.ClientState.TerritoryType, Player!.Position).GetValueOrDefault();
        var target = Coords.FindClosestAetheryte(territoryId, destination).GetValueOrDefault();
        if (source == 0 || target == 0 || source == target)
        {
            return;
        }

        var primary = Coords.FindPrimaryAetheryte(source);
        if (primary == 0 || primary != Coords.FindPrimaryAetheryte(target))
        {
            return;
        }

        // A shard out of sight is reached through the main aetheryte, whose menu is a SelectString like any main one.
        if (Coords.FindAetheryte(source).id == 0)
        {
            source = primary;
        }

        Status = $"Riding the aethernet to territory {territoryId}";
        await ApproachAetheryte(source);
        var isMainAetheryte = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Aetheryte>().GetRow(source).IsAetheryte;
        await WaitForTravelMenu(isMainAetheryte ? SelectStringAddon : AethernetAddon);
        PacketDispatcher.TeleportToAethernet(source, target);
        await WaitUntilThenFalse(() => Svc.Condition[ConditionFlag.BetweenAreas], "AethernetTransition");
        await WaitUntil(
            () => Svc.ClientState.TerritoryType == territoryId && GameMain.IsTerritoryLoaded && Player.Interactable,
            "AethernetArrival");
    }

    private async Task ApproachAetheryte(uint aetheryteId)
    {
        var (objectId, position) = Coords.FindAetheryte(aetheryteId);
        ErrorIf(objectId == 0, $"Aetheryte {aetheryteId} is not nearby");
        if (!Player.WithinRange(position, InteractRange.Aetheryte.MaxDistance))
        {
            await Navigate(position, MovementConfig.Default.WithTolerance(InteractRange.Aetheryte), null);
        }

        if (Player.Mounted)
        {
            await Dismount();
        }

        CancelToken.ThrowIfCancellationRequested();
        ErrorIf(!TargetSystem.InteractWith(objectId), $"Could not interact with aetheryte {aetheryteId}");
    }

    private async Task WaitForTravelMenu(string addonName)
    {
        while (!AddonReady(addonName))
        {
            CancelToken.ThrowIfCancellationRequested();
            AddonTalk.Progress();
            await NextFrame();
        }
    }

    private async Task Mount()
    {
        if (!Player.CanMount)
        {
            return;
        }

        Status = "Mounting";
        while (!Player.Mounted)
        {
            CancelToken.ThrowIfCancellationRequested();
            if (!Player.IsBusy && !ActionManager.IsActionInUse(ActionType.GeneralAction, FlyingMountRouletteGeneralActionId))
            {
                ActionManager.UseAction(ActionType.GeneralAction, FlyingMountRouletteGeneralActionId);
            }

            await NextFrame();
        }
    }

    private async Task Dismount()
    {
        if (Player is null || !Player.Mounted)
        {
            return;
        }

        if (Player.InFlight)
        {
            if (Navmesh.NearestPointReachable(Player.Position) is { } ground)
            {
                await Navigate(ground, MovementConfig.Everything, null);
            }
            else
            {
                RunLog.Warning($"No landing point below {Player.Position}; dismounting may fail");
            }
        }

        Status = "Dismounting";
        while (Player.Mounted)
        {
            CancelToken.ThrowIfCancellationRequested();
            if (Player.InFlight && !Player.IsAirDismountable)
            {
                ActionManager.UseAction(ActionType.GeneralAction, DismountGeneralActionId);
            }
            else if (Player.InFlight)
            {
                GameMain.ExecuteLocationCommand(LocationCommandFlag.Dismount, Player!.Position, (int)Player.PackedRotation);
            }
            else
            {
                GameMain.ExecuteCommand(CommandFlag.Dismount, 1);
            }

            await NextFrame();
        }
    }

    private async Task StopPathfinding()
    {
        CancelToken.ThrowIfCancellationRequested();
        Navmesh.Stop();
        if (!Navmesh.IsPathfinding())
        {
            return;
        }

        await WaitWhile(Navmesh.IsPathfinding, "StopPathfinding");
        Navmesh.Stop();
    }

    private static void StopOwnedPath() => NavmeshIPC.Instance.Stop();

    private static unsafe bool AddonReady(string addonName) => AtkUnitBase.IsAddonReady(addonName);
}
