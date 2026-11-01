using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AutoFateGrind.Core.Game.Fates;
using AutoFateGrind.Core.Ipc;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using clib.Extensions;
using clib.Utils;

namespace AutoFateGrind.Core.Tasks;

public sealed partial class AutoFate
{
    private async Task Revive()
    {
        DisableTextAdvance();
        try
        {
            BossModIPC.Instance.ClearActive();
        }
        catch
        {
        }
        uint startZoneId = Svc.ClientState.TerritoryType;
        Vector3? startPos = Svc.Objects.LocalPlayer?.Position;
        bool solo = Svc.Party.Length == 0;
        Status = (solo ? "KO — releasing" : "KO — waiting for raise");
        AutoCommon.Diag(solo ? "Solo KO: returning home." : "Party KO: waiting up to 30s for a raise.");
        bool returningHome = solo;
        if (solo)
        {
            await DelayMs(Pacing.ReviveDelayMs());
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            TriggerReturnHome();
        }
        else
        {
            long raiseDeadline = Environment.TickCount64 + 30000;
            bool accepted = false;
            while (Environment.TickCount64 < raiseDeadline)
            {
                if (CancelToken.IsCancellationRequested)
                {
                    return;
                }
                if (!IsPlayerKO())
                {
                    AutoCommon.Diag("Raised by another player.");
                    accepted = true;
                    break;
                }
                if (TryAcceptRaisePrompt())
                {
                    AutoCommon.Diag("Accepted raise prompt programmatically.");
                    accepted = true;
                    break;
                }
                await NextFrame(30);
            }
            if (!accepted)
            {
                AutoCommon.Diag("No raise within window; falling back to return-home.");
                TriggerReturnHome();
                returningHome = true;
            }
        }
        await WaitForReviveOrTransition(returningHome);
        if (IsPlayerKO())
        {
            AutoCommon.Diag("Still KO after revive window; outer loop will retry.");
            return;
        }
        if (returnToFateId is { } returnId && PublicEvent.GetFateById(returnId) is { } fateById && fateById.Progress < 100 && Svc.ClientState.TerritoryType == zone.TerritoryId)
        {
            Status = "Returning to FATE";
            AutoCommon.Diag($"Re-engaging FATE {returnId} after revive.");
            await TryTeleportShortcut(fateById.Position, returnId, fateById.Name);
        }
        else if (Svc.ClientState.TerritoryType != startZoneId && startPos.HasValue)
        {
            AutoCommon.Diag($"Revived in {Svc.ClientState.TerritoryType}, expected {startZoneId}; outer loop will retarget.");
        }
    }

    private void TriggerReturnHome()
    {
        if (!TryExecuteReviveCommand(8))
        {
            AutoCommon.Diag("Return-home command dispatch failed.");
        }
    }

    private static bool TryExecuteReviveCommand(int param)
    {
        try
        {
            GameMain.ExecuteCommand(200, param);
            return true;
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "GameMain.ExecuteCommand revive failed");
            return false;
        }
    }

    private static bool TryAcceptRaisePrompt()
    {
        return TryExecuteReviveCommand(5);
    }

    private async Task WaitForReviveOrTransition(bool reissueReturn)
    {
        long deadline = Environment.TickCount64 + 60000;
        long nextReissueAt = Environment.TickCount64 + 1500;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            bool knockedOut = IsPlayerKO();
            bool betweenAreas = Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51];
            if (!knockedOut && !betweenAreas)
            {
                await DelayMs(1000);
                return;
            }
            if ((reissueReturn & knockedOut) && !betweenAreas && Environment.TickCount64 >= nextReissueAt)
            {
                TriggerReturnHome();
                nextReissueAt = Environment.TickCount64 + 1500;
            }
            await DelayMs(250);
        }
        AutoCommon.Diag("Revive transition timed out; outer loop will retry.");
    }

    private static bool IsPlayerKO()
    {
        return Svc.Condition[ConditionFlag.Unconscious];
    }

    private void StartFollowUpWatch(uint parentFateId)
    {
        Fate? fate = Svc.Data.GetExcelSheet<Fate>()?.GetRowOrDefault(parentFateId);
        if (fate?.HasFollowUp != true)
        {
            return;
        }
        if (followUpFateId != parentFateId)
        {
            followUpWatchMs = Pacing.FollowUpWatchMs(15000);
            AutoCommon.Diag($"Watching for follow-up to FATE {parentFateId} for {followUpWatchMs / 1000}s");
        }
        followUpFateId = parentFateId;
        followUpWatchUntilMs = Environment.TickCount64 + followUpWatchMs;
    }

    private bool ShouldWaitForFollowUp()
    {
        if (followUpFateId is not { } fateId)
        {
            return false;
        }
        ExcelSheet<Fate> sheet = Svc.Data.GetExcelSheet<Fate>();
        Fate? fate = sheet?.GetRowOrDefault(fateId);
        if (!fate.HasValue)
        {
            followUpFateId = null;
            return false;
        }
        uint locationId = fate.Value.Location;
        if (locationId != 0 && PublicEvent.Fates is { } fates && fates.Any(f => f.Id > fateId && sheet?.GetRowOrDefault(f.Id)?.Location == locationId))
        {
            AutoCommon.Diag($"Follow-up FATE detected for parent {fateId}; resuming");
            followUpFateId = null;
            return false;
        }
        if (Environment.TickCount64 >= followUpWatchUntilMs)
        {
            followUpFateId = null;
            return false;
        }
        return true;
    }

    private async Task EnsureObstacleMapForEngage(PublicEvent fate)
    {
        if (!BossModIPC.Instance.IsAvailable)
        {
            return;
        }
        if (!FateClock.IsOnMap(fate))
        {
            return;
        }
        if (BossModIPC.Instance.HasTempObstacleMap())
        {
            return;
        }
        await GenerateObstacleMap(fate);
    }

    private int pullStatusCandidates = -1;

    private async Task GenerateObstacleMap(PublicEvent fate)
    {
        if (obstacleMapBlacklist.Contains(fate.Id))
        {
            return;
        }
        if (Plugin.Cfg.RuntimeBadObstacleMaps.Contains(fate.Id))
        {
            return;
        }
        if (!BossModIPC.Instance.IsAvailable)
        {
            return;
        }
        if (!NavmeshIPC.Instance.IsReady())
        {
            return;
        }
        Vector3? projected = FateGround.Project(fate.Position);
        if (!projected.HasValue)
        {
            AutoCommon.Diag($"FATE {fate.Id} centre {fate.Position} has no navmesh nearby; BossMod keeps its own obstacle map");
            return;
        }
        Vector3 centre = projected.Value;
        float projectionOffset = FateGround.HorizontalDistance(fate.Position, centre);
        float radius = Math.Max(fate.Radius + projectionOffset, 10f);
        if (!BossModIPC.Instance.GenerateObstacleMap(centre, radius))
        {
            AutoCommon.Diag($"Obstacle map generate IPC returned false for FATE {fate.Id}");
            return;
        }
        long deadline = Environment.TickCount64 + 5000;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            TaskStatus? obstacleMapStatus = BossModIPC.Instance.GetObstacleMapStatus();
            if (!obstacleMapStatus.HasValue)
            {
                return;
            }
            if (obstacleMapStatus == TaskStatus.RanToCompletion)
            {
                break;
            }
            if (obstacleMapStatus == TaskStatus.Faulted || obstacleMapStatus == TaskStatus.Canceled)
            {
                AutoCommon.Diag($"Obstacle map generation {obstacleMapStatus} for FATE {fate.Id}");
                return;
            }
            await NextFrame();
        }
        if (BossModIPC.Instance.EvaluateTempMapQualityIsBad())
        {
            AutoCommon.Diag($"Obstacle map quality too poor for FATE {fate.Id}; clearing and adding to runtime blacklist");
            Plugin.Cfg.RuntimeBadObstacleMaps.Add(fate.Id);
            BossModIPC.Instance.ClearTempObstacleMap();
        }
    }

    private bool SoftStopReady()
    {
        PublicEvent? currentFate = PublicEvent.CurrentFate;
        return currentFate is not { State: FateState.Running, Progress: < 100 };
    }
}
