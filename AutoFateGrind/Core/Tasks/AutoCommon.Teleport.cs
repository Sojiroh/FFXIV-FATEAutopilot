using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using AutoFateGrind.Core.Game.Watchers;
using AutoFateGrind.Core.Ipc;
using AutoFateGrind.Core.Zones;
using Dalamud.Game.ClientState.Conditions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using clib.TaskSystem;

namespace AutoFateGrind.Core.Tasks;

public abstract partial class AutoCommon
{
    private readonly Vector3[] landingSpots = new Vector3[LandingCandidateCount];

    private const int TeleportCombatClearMs = 30000;

    private const int UnstickMoveMs = 20000;

    private const int ZoneLoadSettleMs = 5000;

    private const int ReturnHomePollMs = 250;

    private const int TeleportRetryBackoffMs = 2000;

    private static readonly ConditionFlag[] ActionBlockingFlags =
    [
        ConditionFlag.Occupied,
        ConditionFlag.Occupied30,
        ConditionFlag.Occupied33,
        ConditionFlag.Occupied38,
        ConditionFlag.Occupied39,
        ConditionFlag.OccupiedInEvent,
        ConditionFlag.OccupiedInQuestEvent,
        ConditionFlag.OccupiedInCutSceneEvent,
        ConditionFlag.OccupiedSummoningBell,
        ConditionFlag.WatchingCutscene,
        ConditionFlag.WatchingCutscene78,
        ConditionFlag.InThatPosition,
        ConditionFlag.TradeOpen,
        ConditionFlag.Unconscious,
        ConditionFlag.Mounting,
        ConditionFlag.Mounting71,
        ConditionFlag.Fishing,
        ConditionFlag.Gathering,
        ConditionFlag.PreparingToCraft,
        ConditionFlag.Crafting,
        ConditionFlag.ExecutingCraftingAction,
        ConditionFlag.MeldingMateria,
        ConditionFlag.CarryingItem,
        ConditionFlag.CarryingObject,
        ConditionFlag.OperatingSiegeMachine,
        ConditionFlag.ParticipatingInCustomMatch,
        ConditionFlag.PlayingLordOfVerminion,
        ConditionFlag.ChocoboRacing,
        ConditionFlag.PlayingMiniGame,
        ConditionFlag.Performing,
        ConditionFlag.Transformed,
        ConditionFlag.UsingHousingFunctions
    ];

    internal static string DescribeRefusal(TeleportRefusal? refusal)
    {
        if (refusal is not null)
        {
            return $"\"{refusal.Text}\" (LogMessage {refusal.LogMessageId})";
        }
        return "no refusal message";
    }

    internal static string ConditionTag()
    {
        var condition = ECommons.DalamudServices.Svc.Condition;
        var list = new List<string>(6);
        if (condition[ConditionFlag.InCombat])
        {
            list.Add("combat");
        }
        if (condition[ConditionFlag.Casting] || condition[ConditionFlag.Casting87])
        {
            list.Add("cast");
        }
        if (condition[ConditionFlag.Mounted] || condition[ConditionFlag.RidingPillion])
        {
            list.Add("mount");
        }
        if (condition[ConditionFlag.InFlight])
        {
            list.Add("flight");
        }
        if (condition[ConditionFlag.Diving])
        {
            list.Add("dive");
        }
        if (condition[ConditionFlag.Swimming])
        {
            list.Add("swim");
        }
        if (condition[ConditionFlag.Jumping] || condition[ConditionFlag.Jumping61])
        {
            list.Add("jump");
        }
        if (condition[ConditionFlag.BeingMoved])
        {
            list.Add("moved");
        }
        if (condition[ConditionFlag.BetweenAreas] || condition[ConditionFlag.BetweenAreas51])
        {
            list.Add("zoning");
        }
        for (var i = 0; i < ActionBlockingFlags.Length; i++)
        {
            if (condition[ActionBlockingFlags[i]])
            {
                list.Add(ActionBlockingFlags[i].ToString());
            }
        }
        if (Player.IsAnimationLocked)
        {
            list.Add("animlock");
        }
        if (!Player.Interactable)
        {
            list.Add("untargetable");
        }
        if (list.Count != 0)
        {
            return string.Join(",", list);
        }
        return "grounded";
    }

    private static bool InWater() => ECommons.DalamudServices.Svc.Condition[ConditionFlag.Diving] || ECommons.DalamudServices.Svc.Condition[ConditionFlag.Swimming];

    internal async Task PrepareForTeleport(string scope)
    {
        NavmeshIPC.Instance.Stop();
        if (ECommons.DalamudServices.Svc.Condition[ConditionFlag.InCombat] || ECommons.DalamudServices.Svc.Condition[ConditionFlag.Casting])
        {
            Status = "Waiting for combat to clear before teleport";
            Diag($"{scope}: combat/casting ({ConditionTag()}), waiting up to {TeleportCombatClearMs / 1000}s for a castable window");
            await WaitUntilTimed(() => !ECommons.DalamudServices.Svc.Condition[ConditionFlag.InCombat] && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.Casting], TeleportCombatClearMs, scope + "-wait-teleportable");
        }
        if (CancelToken.IsCancellationRequested)
        {
            return;
        }
        if (InWater())
        {
            await LeaveWaterForTeleport(scope);
        }
    }

    private async Task LeaveWaterForTeleport(string scope)
    {
        var playerPosition = ECommons.DalamudServices.Svc.Objects.LocalPlayer?.Position;
        if (playerPosition is not { } start)
        {
            return;
        }
        var reachablePoint = NavmeshIPC.Instance.NearestPointReachable(start, 30f, 30f);
        if (reachablePoint is not { } dest)
        {
            Warn(scope + ": in the water (" + ConditionTag() + ") with no reachable mesh point to climb out at; teleport may fail");
            return;
        }
        if (Vector3.Distance(start, dest) < 2f)
        {
            return;
        }
        Status = "Leaving the water before teleport";
        Diag($"{scope}: in the water ({ConditionTag()}); moving ~{Vector3.Distance(start, dest):F0}m to a reachable point before teleport");
        var territory = ECommons.DalamudServices.Svc.ClientState.TerritoryType;
        var op = new MoveOp((MoveOp o) => o.Move(territory, dest, MovementConfig.Everything.WithTolerance(3f), null));
        await RunCancellable(op, UnstickMoveMs, scope + "-water", StuckDetector.MoveStallAbort(scope + "-water"));
    }

    internal async Task<TeleportOutcome> RunTeleport(uint territoryId, Vector3 destination, bool allowSameZoneTeleport, int timeoutMs, string scope)
    {
        var outcome = await RunTeleportOnce(territoryId, destination, allowSameZoneTeleport, timeoutMs, scope);
        if (outcome.Succeeded || CancelToken.IsCancellationRequested || !ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounted])
        {
            return outcome;
        }
        if (outcome.Refusal is { IsPermanent: true } or { IsAlreadyUnderway: true })
        {
            return outcome;
        }
        Diag(scope + ": teleport did not go through while mounted (" + ConditionTag() + "); dismounting and casting once more");
        if (!(await SafeDismount(scope + "-dismount")))
        {
            return outcome;
        }
        return await RunTeleportOnce(territoryId, destination, allowSameZoneTeleport, timeoutMs, scope + "-dismounted");
    }

    private async Task<TeleportOutcome> RunTeleportOnce(uint territoryId, Vector3 destination, bool allowSameZoneTeleport, int timeoutMs, string scope)
    {
        var startedAt = Environment.TickCount64;
        var operation = new MoveOp((MoveOp move) => move.Teleport(territoryId, destination, allowSameZoneTeleport));
        var completed = await RunCancellable(operation, timeoutMs, scope, StuckDetector.IdleStallAbort(StuckDetector.IdleStallTimeoutMs));
        var teleportRefusal = TeleportRefusals.Since(startedAt);
        if (teleportRefusal is not null)
        {
            LastTeleportRefusal = teleportRefusal;
            Diag(scope + ": the game refused the teleport: " + DescribeRefusal(teleportRefusal));
        }
        return new TeleportOutcome(completed, operation.Fault, teleportRefusal);
    }

    private const int ReturnHomeWaitMs = 30000;

    private const int ReturnHomeReissueMs = 3000;

    private const uint ReturnGeneralActionId = 8u;

    internal async Task<bool> TryReturnHome(string scope)
    {
        var startTerr = ECommons.DalamudServices.Svc.ClientState.TerritoryType;
        Diag(scope + ": teleport not starting (" + ConditionTag() + "); casting Return to home aetheryte to clear a stuck teleport");
        Status = "Returning home to clear a stuck teleport";
        var deadline = Environment.TickCount64 + ReturnHomeWaitMs;
        var nextCastAt = 0L;
        while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
        {
            if (ECommons.DalamudServices.Svc.ClientState.TerritoryType != startTerr)
            {
                await DelayMs(ZoneLoadSettleMs);
                return true;
            }
            if (Environment.TickCount64 >= nextCastAt && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.Casting] && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.BetweenAreas] && !ECommons.DalamudServices.Svc.Condition[ConditionFlag.BetweenAreas51])
            {
                UseGeneralAction(ReturnGeneralActionId);
                nextCastAt = Environment.TickCount64 + ReturnHomeReissueMs;
            }
            await DelayMs(ReturnHomePollMs);
        }
        if (ECommons.DalamudServices.Svc.ClientState.TerritoryType != startTerr)
        {
            return true;
        }
        Diag($"{scope}: Return home did not complete within {ReturnHomeWaitMs / 1000}s");
        return false;
    }

    internal unsafe static bool UseGeneralAction(uint generalActionId)
    {
        ActionManager* ptr = ActionManager.Instance();
        if (ptr == null)
        {
            return false;
        }
        return ptr->UseAction(ActionType.GeneralAction, generalActionId, 3758096384uL, 0u, ActionManager.UseActionMode.None, 0u, null);
    }

    protected unsafe static uint? GeneralActionStatus(uint generalActionId)
    {
        ActionManager* ptr = ActionManager.Instance();
        if (ptr == null)
        {
            return null;
        }
        return ptr->GetActionStatus(ActionType.GeneralAction, generalActionId, 3758096384uL, checkRecastActive: true, checkCastingActive: true, null);
    }

    private const int MaxTeleportFaults = 2;

    private const int StallsBeforeReturnHome = 2;

    internal async Task<TerritoryTeleportResult> AttemptTerritoryTeleport(uint territoryId, Vector3 dest, string label, int perAttemptTimeoutMs, int attempts = 4)
    {
        if (ECommons.DalamudServices.Svc.ClientState.TerritoryType == territoryId)
        {
            return TerritoryTeleportResult.Reached;
        }
        var viaGateway = ZoneAetherytes.TryFindGateway(territoryId, out var gateway);
        var hopTerritoryId = (viaGateway ? gateway.TerritoryId : territoryId);
        var hopDest = (viaGateway ? gateway.Position : dest);
        var maxAttempts = (viaGateway ? Math.Min(attempts, GatewayAttempts) : attempts);
        if (ZoneAetherytes.AttunableIdsIn(hopTerritoryId).Length == 0)
        {
            Warn($"{label}: territory {hopTerritoryId} has no aetheryte to teleport to; giving up");
            return TerritoryTeleportResult.Unreachable;
        }
        LastTeleportRefusal = null;
        var returnedHome = false;
        var lockReported = false;
        var casts = 0;
        var stalls = 0;
        var faults = 0;
        for (var attempt = 1; attempt <= maxAttempts && !CancelToken.IsCancellationRequested; attempt++)
        {
            if (ECommons.DalamudServices.Svc.ClientState.TerritoryType == territoryId)
            {
                return TerritoryTeleportResult.Reached;
            }
            var scope = $"{label}#{attempt}";
            await PrepareForTeleport(scope);
            if (CancelToken.IsCancellationRequested)
            {
                break;
            }
            if (ECommons.DalamudServices.Svc.ClientState.TerritoryType != hopTerritoryId)
            {
                casts++;
                var teleportOutcome = await RunTeleport(hopTerritoryId, hopDest, allowSameZoneTeleport: false, perAttemptTimeoutMs, scope);
                var fault = teleportOutcome.Fault;
                if (fault != null)
                {
                    faults++;
                    Diag($"{scope} teleport faulted ({faults}/{MaxTeleportFaults}): {fault.Message}");
                }
                else if (!teleportOutcome.Completed)
                {
                    stalls++;
                }
                if (teleportOutcome.Refusal is { IsInsufficientGil: true })
                {
                    throw new UnrecoverableRunException("Not enough gil to teleport. Restock gil, then start again.");
                }
                if (teleportOutcome.Refusal is { IsPermanent: true })
                {
                    Warn(label + ": teleport refused for good: " + DescribeRefusal(teleportOutcome.Refusal) + "; giving up");
                    return TerritoryTeleportResult.Unreachable;
                }
                lockReported |= teleportOutcome.Refusal is { IsAlreadyUnderway: true };
            }
            if (viaGateway && ECommons.DalamudServices.Svc.ClientState.TerritoryType == hopTerritoryId)
            {
                await RideAethernetInto(territoryId, dest, gateway, scope);
            }
            if (ECommons.DalamudServices.Svc.ClientState.TerritoryType == territoryId)
            {
                return TerritoryTeleportResult.Reached;
            }
            if (faults >= MaxTeleportFaults)
            {
                Diag($"{label}: teleport rejected {faults} times, not a stuck cast; giving up without Return");
                break;
            }
            if (!returnedHome && (lockReported || stalls >= StallsBeforeReturnHome))
            {
                returnedHome = true;
                await TryReturnHome(label);
            }
            else
            {
                await DelayMs(TeleportRetryBackoffMs);
            }
        }
        if (ECommons.DalamudServices.Svc.ClientState.TerritoryType == territoryId)
        {
            return TerritoryTeleportResult.Reached;
        }
        return (faults == 0 && stalls > 0 && stalls == casts) ? TerritoryTeleportResult.Blocked : TerritoryTeleportResult.Unreachable;
    }

    internal async Task<bool> TeleportToTerritory(uint territoryId, Vector3 dest, string label, int perAttemptTimeoutMs, int attempts = 4)
    {
        return await AttemptTerritoryTeleport(territoryId, dest, label, perAttemptTimeoutMs, attempts) == TerritoryTeleportResult.Reached;
    }

    private const int GatewayAttempts = 3;

    private const int AethernetLegMs = 90000;

    private const int AethernetNavmeshWaitMs = 60000;

    private async Task RideAethernetInto(uint territoryId, Vector3 dest, ZoneGateway gateway, string scope)
    {
        Status = "Riding the aethernet from " + gateway.Name;
        await WaitForNavmeshReady(AethernetNavmeshWaitMs, 60);
        if (CancelToken.IsCancellationRequested)
        {
            return;
        }
        var localPlayer = ECommons.DalamudServices.Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return;
        }
        if (!CityAethernet.TryPlanEntry(ECommons.DalamudServices.Svc.ClientState.TerritoryType, localPlayer.Position, territoryId, dest, gateway.AethernetGroup, out var hop))
        {
            Warn($"{scope}: no attuned aethernet shard of {gateway.Name} leads into territory {territoryId}; attune one there first");
            return;
        }
        Diag($"{scope}: reached {gateway.Name}; aethernet {CityAethernet.ShardName(hop.Source.Id)} → {CityAethernet.ShardName(hop.Destination.Id)} into territory {territoryId}");
        var op = new MoveOp((MoveOp o) => o.RideAethernet(hop));
        await RunCancellable(op, AethernetLegMs, scope + "-aethernet");
        var fault = op.Fault;
        if (fault != null)
        {
            Diag(scope + ": aethernet leg faulted: " + fault.Message);
        }
    }

    internal static bool WithinReach(Vector3 destination, float tolerance)
    {
        var localPlayer = ECommons.DalamudServices.Svc.Objects.LocalPlayer;
        return localPlayer != null && Vector3.Distance(localPlayer.Position, destination) <= tolerance + WalkArrivalSlackMeters;
    }
}

internal enum TerritoryTeleportResult : byte
{
    Reached,
    Unreachable,
    Blocked
}
