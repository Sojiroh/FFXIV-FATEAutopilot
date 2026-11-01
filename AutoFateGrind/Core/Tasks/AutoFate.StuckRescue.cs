using System;
using System.Numerics;
using System.Threading.Tasks;
using AutoFateGrind.Core.Game.Yokai;
using AutoFateGrind.Core.Zones;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;

namespace AutoFateGrind.Core.Tasks;

public sealed partial class AutoFate
{
    private const float StuckRescueLeashMeters = 15f;

    private const int MaxStuckRescuesWithoutFate = 3;

    private const int StuckRescueFaultGraceMs = 60000;

    private const string StuckRescueScope = "stuck-rescue";

    private long stuckWatchSinceMs = Environment.TickCount64;

    private Vector3 stuckWatchAnchor;

    private uint stuckWatchTerritory;

    private int stuckWatchCompleted = -1;

    private string? stuckRescueFault;

    private async Task<bool> RescueIfStuck(GrindState state)
    {
        Configuration cfg = Plugin.Cfg;
        if (session.StuckRescueCompletedMark != session.CompletedCount)
        {
            session.StuckRescueCompletedMark = session.CompletedCount;
            session.StuckRescuesWithoutFate = 0;
        }
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return false;
        }
        Vector3 position = localPlayer.Position;
        uint territoryType = Svc.ClientState.TerritoryType;
        bool moved = stuckWatchCompleted != session.CompletedCount || territoryType != stuckWatchTerritory || Svc.Condition[ConditionFlag.InCombat] || Vector3.Distance(position, stuckWatchAnchor) > 15f;
        if ((!cfg.StuckRescueEnabled || !StuckWatchApplies(state) || territoryType != zone.TerritoryId) | moved)
        {
            RestartStuckWatch(position, territoryType);
            return false;
        }
        long stuckMs = Environment.TickCount64 - stuckWatchSinceMs;
        if (stuckMs < StuckRescueDelayMs(cfg))
        {
            return false;
        }
        string posText = $"({position.X:F0},{position.Y:F0},{position.Z:F0})";
        if (session.StuckRescuesWithoutFate >= 3)
        {
            GiveUpStuckRescue($"Still stuck in state {state} at {posText} after {session.StuckRescuesWithoutFate} rescue teleports with no FATE finished ({AutoCommon.ConditionTag()})");
            return true;
        }
        if (!ZoneAetherytes.TryFindNearest(zone.TerritoryId, position, out var aetheryte))
        {
            GiveUpStuckRescue($"Stuck {stuckMs / 1000}s in state {state} at {posText} ({AutoCommon.ConditionTag()}), but {zone.Name} has no aetheryte of its own to teleport to");
            return true;
        }
        RefreshPendingCollectReward();
        if (CollectRewardPending)
        {
            AutoCommon.Diag($"Stuck rescue deferred: a same-zone teleport reloads the zone while Collect FATE {pendingRewardSpawn.FateId} ({pendingRewardName}) still owes its reward; checking again in {StuckRescueDelayMs(cfg) / 60000}m");
            RestartStuckWatch(position, territoryType);
            return false;
        }
        session.StuckRescuesWithoutFate++;
        Status = "Stuck; teleporting to " + aetheryte.Name;
        AutoCommon.Warn($"Stuck rescue {session.StuckRescuesWithoutFate}/{3}: no FATE finished, no zone change, no combat and nothing beyond {15f:F0}m " + $"for {stuckMs / 1000}s in state {state} at {posText} ({AutoCommon.ConditionTag()}); teleporting to {aetheryte.Name}");
        Svc.Chat.Print($"[FTA] Stuck for {stuckMs / 60000} min; teleporting to {aetheryte.Name}.");
        if (await TeleportToZoneAetheryte(aetheryte, "stuck-rescue"))
        {
            AutoCommon.Diag("Stuck rescue reached " + aetheryte.Name);
        }
        else if (!CancelToken.IsCancellationRequested)
        {
            string fallbackNote = ((state == GrindState.BetweenFates) ? $"the no-progress fault follows in {60}s" : "trying again after another window");
            AutoCommon.Warn($"Stuck rescue teleport to {aetheryte.Name} did not go through ({AutoCommon.ConditionTag()}); {fallbackNote}");
        }
        IPlayerCharacter? localPlayer2 = Svc.Objects.LocalPlayer;
        if (localPlayer2 != null)
        {
            RestartStuckWatch(localPlayer2.Position, Svc.ClientState.TerritoryType);
        }
        return true;
    }

    private void GiveUpStuckRescue(string reason)
    {
        AutoCommon.Warn(reason + "; giving up on the stuck rescue and surfacing a fault for recovery");
        stuckRescueFault = reason + "; surfacing fault for recovery.";
    }

    private void RestartStuckWatch(Vector3 position, uint territory)
    {
        stuckWatchSinceMs = Environment.TickCount64;
        stuckWatchAnchor = position;
        stuckWatchTerritory = territory;
        stuckWatchCompleted = session.CompletedCount;
    }

    private string StuckWatchHeartbeat()
    {
        if (!Plugin.Cfg.StuckRescueEnabled || !StuckWatchApplies(lastObservedState))
        {
            return "";
        }
        return $" stuckWatch={(Environment.TickCount64 - stuckWatchSinceMs) / 1000}s rescues={session.StuckRescuesWithoutFate}";
    }

    private bool YokaiTargetChanged()
    {
        if (!ZoneSelection.IsYokaiGoal(Plugin.Cfg))
        {
            return false;
        }
        uint targetMinionId = YokaiProgress.ResolveTargetMinionId(Plugin.Cfg, session.YokaiTargetMinionId);
        if (targetMinionId != 0)
        {
            return targetMinionId != session.YokaiTargetMinionId;
        }
        return false;
    }
}
