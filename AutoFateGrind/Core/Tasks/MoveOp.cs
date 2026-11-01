using System;
using System.Numerics;
using System.Threading.Tasks;
using AutoFateGrind.Core.Game.Ops;
using AutoFateGrind.Core.Game.Player;
using AutoFateGrind.Core.Ipc;
using AutoFateGrind.Core.Zones;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Lumina.Excel.Sheets;
using clib.Enums;
using clib.Extensions;
using clib.Services;
using clib.TaskSystem;
using clib.Utils;

namespace AutoFateGrind.Core.Tasks;

internal sealed class MoveOp(Func<MoveOp, Task> body) : AutoTask
{
    public Exception? Fault { get; private set; }

    protected override async Task Execute()
    {
        try
        {
            await body(this);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception fault)
        {
            Fault = fault;
        }
    }

    public async Task Move(uint territoryId, Vector3 dest, MovementConfig config, Func<bool>? stopCondition)
    {
        await TeleportToTerritory(territoryId, dest, allowSameZoneTeleport: false);
        await MountPreferred(dest, config);
        await Navigate(dest, config, stopCondition);
    }

    public async Task MoveInZone(Vector3 dest, MovementConfig config, Func<bool>? stopCondition)
    {
        await MountPreferred(dest, config);
        await Navigate(dest, config, stopCondition);
    }

    private const int PreferredMountWaitMs = 5000;

    private const int PreferredMountRetryMs = 500;

    private async Task MountPreferred(Vector3 dest, MovementConfig config)
    {
        if ((config.Movement & (MovementOptions.Mount | MovementOptions.Fly)) == 0 || !MountOps.HasPreferred || !MountOps.TerritoryAllowsMount())
        {
            return;
        }
        var localPlayer = ECommons.DalamudServices.Svc.Objects.LocalPlayer;
        if (localPlayer == null || Vector3.Distance(localPlayer.Position, dest) < Math.Max(config.Tolerance.GetValueOrDefault(), NavmeshIPC.Instance.GetTolerance()))
        {
            return;
        }
        var deadline = Environment.TickCount64 + PreferredMountWaitMs;
        var nextAttemptAtMs = 0L;
        while (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounted] && Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested || ECommons.DalamudServices.Svc.Condition[ConditionFlag.InCombat])
            {
                return;
            }
            if (Environment.TickCount64 >= nextAttemptAtMs && MountOps.TrySummonPreferred())
            {
                nextAttemptAtMs = Environment.TickCount64 + PreferredMountRetryMs;
            }
            await NextFrame();
        }
    }

    public async Task MoveInZoneWithFlightRecovery(Vector3 dest, MovementConfig config, Func<bool>? stopCondition, FlightReplanPolicy policy, Action<string> diag)
    {
        if (!config.Movement.HasFlag(MovementOptions.Fly) || config.Pathing == PathingStrategy.Direct)
        {
            await MoveInZone(dest, config, stopCondition);
            return;
        }
        var budgetExhaustionLogged = false;
        var replans = 0;
        for (; ; replans++)
        {
            if (CancelToken.IsCancellationRequested || (stopCondition?.Invoke() ?? false))
            {
                return;
            }
            var groundSinceMs = 0L;
            var replanForFlight = false;
            var callerStopped = false;
            bool? canFlyWhenPlanned = null;
            await MoveInZone(dest, config, ShouldStop);
            if (callerStopped || !replanForFlight || CancelToken.IsCancellationRequested)
            {
                return;
            }
            diag($"Flight became available during a ground route; replanning ({replans + 1}/{policy.MaxReplans})");
            bool ShouldStop()
            {
                canFlyWhenPlanned ??= Control.CanFly;
                callerStopped |= CancelToken.IsCancellationRequested || (stopCondition?.Invoke() ?? false);
                if (callerStopped | replanForFlight)
                {
                    return true;
                }
                if (canFlyWhenPlanned.Value || !ReadyForFlight() || ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight] || !NavmeshIPC.Instance.IsRunning() || ECommons.DalamudServices.Svc.Objects.LocalPlayer is not { } localPlayer || Vector3.Distance(localPlayer.Position, dest) <= Math.Max(policy.MinDistanceMeters, config.Tolerance.GetValueOrDefault()))
                {
                    groundSinceMs = 0L;
                    return false;
                }
                var tickCount = Environment.TickCount64;
                if (groundSinceMs == 0L)
                {
                    groundSinceMs = tickCount;
                }
                if (tickCount - groundSinceMs < policy.GroundTravelGraceMs)
                {
                    return false;
                }
                if (replans >= policy.MaxReplans)
                {
                    if (!budgetExhaustionLogged)
                    {
                        diag("Flight replan budget spent; continuing on the ground route");
                        budgetExhaustionLogged = true;
                    }
                    return false;
                }
                replanForFlight = true;
                return true;
            }
        }
    }

    private static bool ReadyForFlight() => ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounted] && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounting] && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounting71] && ECommons.DalamudServices.Svc.Objects.LocalPlayer is { IsDead: false, IsCasting: false } && Control.CanFly;

    public Task Teleport(uint territoryId, Vector3 dest, bool allowSameZoneTeleport) => TeleportToTerritory(territoryId, dest, allowSameZoneTeleport);

    public async Task RideAethernet(AethernetHop hop)
    {
        var shard = CityAethernet.FindShardObject(hop.Source.Id);
        ErrorIf(shard == null, $"Aethernet shard {hop.Source.Id} is not loaded");
        await Navigate(shard!.Position, MovementConfig.Default.WithTolerance(InteractRange.Aetheryte), null);
        await Dismount();
        var menu = (hop.Source.IsAetheryte ? AfgConstants.AddonNames.SelectString : AfgConstants.AddonNames.TelepotTown);
        await InteractWith(shard, () => NpcInteraction.AddonOpen(menu), UiSkipOptions.Talk);
        PacketDispatcherExtensions.TeleportToAethernet(hop.Source.Id, hop.Destination.Id);
        await WaitUntilThenFalse(() => ECommons.DalamudServices.Svc.Condition[ConditionFlag.BetweenAreas], "AethernetHop");
        await WaitUntil(() => ECommons.GameHelpers.Player.Interactable, "AethernetArrival");
    }

    public Task Interact(IGameObject obj, Func<bool>? waitUntil, UiSkipOptions skip) => InteractWith(obj, waitUntil, skip);

    public Task DismountNow() => Dismount();

    private static NavmeshIPC Navmesh => NavmeshIPC.Instance;

    private static IPlayerCharacter? Player => ECommons.DalamudServices.Svc.Objects.LocalPlayer;

    private const float DefaultArriveMeters = 3f;

    private const float TeleportCastSeconds = 5f;

    private const uint FlyingMountRouletteGeneralActionId = 24u;

    private const int InteractAttempts = 5;

    private async Task Navigate(Vector3 destination, MovementConfig config, Func<bool>? stopCondition)
    {
        await WaitUntil(() => IPlayerCharacterExtensions.get_Available(Player), "WaitingForPlayer");
        var tolerance = Math.Max(config.Tolerance.GetValueOrDefault(), Navmesh.GetTolerance());
        if (Player.WithinRange(destination, tolerance))
        {
            return;
        }
        if (config.Movement.HasFlag(MovementOptions.Mount) || config.Movement.HasFlag(MovementOptions.Fly))
        {
            await Mount();
        }
        Status = "Waiting for Navmesh";
        await WaitUntil(() => Navmesh.IsReady() || Navmesh.BuildProgress() >= 0f, "WaitForBuildStart");
        await WaitWhile(() => Navmesh.BuildProgress() >= 0f, "BuildMesh");
        ErrorIf(!Navmesh.IsReady(), $"Failed to build navmesh for zone #{ECommons.DalamudServices.Svc.ClientState.TerritoryType}");
        await WaitWhile(Navmesh.IsPathfinding, "WaitingForInProgressCalls");
        CancelToken.ThrowIfCancellationRequested();
        var fly = IPlayerCharacterExtensions.get_InFlight(Player) || (config.Movement.HasFlag(MovementOptions.Fly) && Control.CanFly);
        ErrorIf(!Navmesh.PathfindAndMoveCloseTo(destination, fly, config.Tolerance ?? DefaultArriveMeters), $"Failed to start pathfinding to destination [#{ECommons.DalamudServices.Svc.ClientState.TerritoryType} - {destination}]");
        Status = $"Moving to {destination}";
        using (new OnDispose(Navmesh.Stop))
        {
            await NextFrame();
            await WaitWhile(() =>
            {
                if (!Player.WithinRange(destination, tolerance) && stopCondition?.Invoke() != true)
                {
                    if (!Navmesh.IsMovePending())
                    {
                        return Navmesh.IsRunning();
                    }
                    return true;
                }
                return false;
            }, "Navigate");
            if (stopCondition?.Invoke() ?? false)
            {
                await StopPathfinding();
            }
            if (config.Movement.HasFlag(MovementOptions.Dismount) && Player.WithinRange(destination, tolerance))
            {
                await Dismount();
            }
        }
    }

    private async Task TeleportToTerritory(uint territoryId, Vector3 destination, bool allowSameZoneTeleport)
    {
        if (!allowSameZoneTeleport && ECommons.DalamudServices.Svc.ClientState.TerritoryType == territoryId)
        {
            return;
        }
        await StopPathfinding();
        await WaitWhile(() => IPlayerCharacterExtensions.get_IsUiFading(Player), "WaitForUiUnfade");
        var primaryAetheryteId = Coords.FindPrimaryAetheryte(Coords.FindClosestAetheryte(territoryId, destination).GetValueOrDefault());
        ErrorIf(primaryAetheryteId == 0, $"Failed to find an aetheryte in territory {territoryId}");
        var row = ECommons.DalamudServices.Svc.Data.GetExcelSheet<Aetheryte>().GetRow(primaryAetheryteId);
        if ((ECommons.DalamudServices.Svc.ClientState.TerritoryType != row.Territory.RowId) | allowSameZoneTeleport)
        {
            Status = $"Teleporting to {row.PlaceName.Value.Name}";
            await CastTeleport(primaryAetheryteId);
        }
        if (ECommons.DalamudServices.Svc.ClientState.TerritoryType != territoryId)
        {
            await UseAethernet(territoryId, destination);
        }
        ErrorIf(ECommons.DalamudServices.Svc.ClientState.TerritoryType != territoryId, $"Failed to reach territory (exp: {territoryId}, act: {ECommons.DalamudServices.Svc.ClientState.TerritoryType})");
    }

    private async Task CastTeleport(uint aetheryteId)
    {
        var teleported = false;
        while (!teleported)
        {
            await StopPathfinding();
            await WaitWhile(() => IPlayerCharacterExtensions.get_IsBusy(Player), "WaitForTeleportReady");
            ErrorIf(!ActionManagerExtensions.Teleport(aetheryteId, 0), $"Failed to teleport to {aetheryteId}");
            teleported = await WaitForTeleportOutcome();
        }
    }

    private async Task<bool> WaitForTeleportOutcome()
    {
        var sawCast = false;
        var sawUiFade = false;
        while (true)
        {
            var uiFading = IPlayerCharacterExtensions.get_IsUiFading(Player);
            var casting = Player?.IsCasting ?? false;
            sawCast |= casting;
            sawUiFade |= uiFading;
            if (sawUiFade && !uiFading)
            {
                await WaitUntil(() => GameMainExtensions.get_IsTerritoryLoaded() && IPlayerCharacterExtensions.get_Interactable(Player), "WaitTransportFinish");
                return true;
            }
            if (sawCast && !casting && !sawUiFade && ActionManagerExtensions.GetCastAction().Elapsed != TeleportCastSeconds)
            {
                return false;
            }
            await NextFrame();
        }
    }

    private async Task UseAethernet(uint territoryId, Vector3 destination)
    {
        var sourceId = Coords.FindClosestAetheryte(ECommons.DalamudServices.Svc.ClientState.TerritoryType, Player!.Position).GetValueOrDefault();
        var destinationId = Coords.FindClosestAetheryte(territoryId, destination).GetValueOrDefault();
        if (sourceId == 0 || destinationId == 0 || sourceId == destinationId)
        {
            return;
        }
        var primaryAetheryteId = Coords.FindPrimaryAetheryte(sourceId);
        if (primaryAetheryteId == 0 || primaryAetheryteId != Coords.FindPrimaryAetheryte(destinationId))
        {
            return;
        }
        var (objectId, destination2) = Coords.FindAetheryte(sourceId);
        if (objectId == 0L)
        {
            (objectId, destination2) = Coords.FindAetheryte(primaryAetheryteId);
        }
        if (objectId == 0L)
        {
            return;
        }
        Status = $"Interacting with aethernet to get to [{territoryId}]";
        await Navigate(destination2, MovementConfig.Default.WithTolerance(InteractRange.Aetheryte), null);
        if (IPlayerCharacterExtensions.get_Mounted(Player))
        {
            await Dismount();
        }
        ErrorIf(!TargetSystemExtensions.InteractWith(objectId), "Failed to interact with aetheryte");
        var isAetheryte = ECommons.DalamudServices.Svc.Data.GetExcelSheet<Aetheryte>().GetRow(sourceId).IsAetheryte;
        var menu = (isAetheryte ? AfgConstants.AddonNames.SelectString : AfgConstants.AddonNames.TelepotTown);
        await WaitUntilSkipping(() => NpcInteraction.AddonOpen(menu), isAetheryte ? UiSkipOptions.Talk : UiSkipOptions.None);
        PacketDispatcherExtensions.TeleportToAethernet(sourceId, destinationId);
        await WaitUntilThenFalse(() => ECommons.DalamudServices.Svc.Condition[ConditionFlag.BetweenAreas], "TeleportStart");
        await WaitUntil(() => ECommons.DalamudServices.Svc.ClientState.TerritoryType == territoryId && GameMainExtensions.get_IsTerritoryLoaded() && IPlayerCharacterExtensions.get_Interactable(Player), "TeleportFinish");
    }

    private async Task InteractWith(IGameObject target, Func<bool>? waitUntil, UiSkipOptions skip)
    {
        ErrorIf(waitUntil == null && skip != UiSkipOptions.None, "Skip arguments provided but no wait condition");
        if (!target.IsInInteractRange())
        {
            await Navigate(target.Position, MovementConfig.Default.WithTolerance(InteractRange.EventObj), target.IsInInteractRange);
        }
        Status = $"Interacting with {target.Name}";
        await WaitWhile(() => IPlayerCharacterExtensions.get_IsJumping(Player), "WaitForAbleToInteract");
        for (var attempt = 0; attempt < InteractAttempts; attempt++)
        {
            if (TargetSystemExtensions.InteractWith(target.GameObjectId))
            {
                if (waitUntil != null)
                {
                    await WaitUntilSkipping(waitUntil, skip);
                }
                return;
            }
            await NextFrame();
        }
        Error($"Failed to interact with object after {InteractAttempts} tries");
    }

    private async Task WaitUntilSkipping(Func<bool> condition, UiSkipOptions skip)
    {
        while (!condition())
        {
            if (skip.HasFlag(UiSkipOptions.Talk) && NpcInteraction.AddonOpen(AfgConstants.AddonNames.Talk))
            {
                AddonTalkExtensions.Progress();
            }
            if (skip.HasFlag(UiSkipOptions.YesNo) && NpcInteraction.AddonOpen(AfgConstants.AddonNames.SelectYesno))
            {
                AddonSelectYesnoExtensions.Yes();
            }
            await NextFrame();
        }
    }

    private async Task Mount()
    {
        if (!IPlayerCharacterExtensions.get_CanMount(Player))
        {
            return;
        }
        Status = "Mounting";
        while (!IPlayerCharacterExtensions.get_Mounted(Player))
        {
            if (!IPlayerCharacterExtensions.get_IsBusy(Player) && !ActionManagerExtensions.IsActionInUse(ActionType.GeneralAction, FlyingMountRouletteGeneralActionId))
            {
                ActionManagerExtensions.UseAction(ActionType.GeneralAction, FlyingMountRouletteGeneralActionId);
            }
            await NextFrame();
        }
    }

    private async Task Dismount()
    {
        if (Player == null || !IPlayerCharacterExtensions.get_Mounted(Player))
        {
            return;
        }
        if (IPlayerCharacterExtensions.get_InFlight(Player))
        {
            if (Navmesh.NearestPointReachable(Player.Position) is { } landingPoint)
            {
                await Navigate(landingPoint, MovementConfig.Everything, null);
            }
            else
            {
                Warning($"No nearest landable point found from {Player.Position}. Dismounting may fail");
            }
        }
        Status = "Dismounting";
        while (IPlayerCharacterExtensions.get_Mounted(Player))
        {
            if (IPlayerCharacterExtensions.get_InFlight(Player) && !IPlayerCharacterExtensions.get_IsAirDismountable(Player))
            {
                ActionManagerExtensions.UseAction(ActionType.GeneralAction, AutoCommon.DismountGeneralActionId);
            }
            else if (IPlayerCharacterExtensions.get_InFlight(Player))
            {
                GameMainExtensions.ExecuteLocationCommand(LocationCommandFlag.Dismount, Player.Position, (int)IPlayerCharacterExtensions.get_PackedRotation(Player));
            }
            else
            {
                GameMainExtensions.ExecuteCommand(CommandFlag.Dismount, 1);
            }
            await NextFrame();
        }
    }

    private async Task StopPathfinding()
    {
        Navmesh.Stop();
        if (!Navmesh.IsMovePending())
        {
            return;
        }
        await WaitWhile(Navmesh.IsMovePending, "WaitForPathfindingStop");
        Navmesh.Stop();
    }
}
