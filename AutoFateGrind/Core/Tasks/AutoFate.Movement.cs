using System;
using System.Numerics;
using System.Threading.Tasks;
using AutoFateGrind.Core.External;
using AutoFateGrind.Core.Game.Fates;
using AutoFateGrind.Core.Game.Player;
using AutoFateGrind.Core.Ipc;
using AutoFateGrind.Core.Modes;
using AutoFateGrind.Core.Zones;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using clib.TaskSystem;
using clib.Utils;

namespace AutoFateGrind.Core.Tasks;

public sealed partial class AutoFate
{
    private async Task<MoveStopReason> MoveToFate(PublicEvent fate)
    {
        await WaitForNavmeshReady(60000, 60);
        await GenerateObstacleMap(fate);
        Vector3 plannedDest = RandomPointInsideRadius(fate.Position, fate.Radius * 0.5f);
        Vector3? projectedDest = FateGround.Project(plannedDest) ?? FateGround.Project(fate.Position);
        Vector3 dest = projectedDest ?? plannedDest;
        if (!projectedDest.HasValue)
        {
            AutoCommon.Diag($"Could not project FATE {fate.Id} dest {plannedDest} or its centre onto the navmesh; vnav may struggle");
        }
        uint targetId = fate.Id;
        string fateName = fate.Name;
        bool walkingInCombat = Svc.Condition[ConditionFlag.InCombat];
        if (walkingInCombat)
        {
            AutoCommon.Diag($"In combat; walking toward FATE {targetId} ({fate.Name}) on foot until combat drops");
        }
        bool walkInstead = !Svc.Condition[ConditionFlag.Mounted] && Svc.Objects.LocalPlayer is { } localPlayer && Vector3.Distance(localPlayer.Position, dest) < 30f;
        if (walkInstead && !walkingInCombat)
        {
            AutoCommon.Diag($"FATE {targetId} ({fate.Name}) is under {30f:F0}m away; walking instead of mounting");
        }
        MovementConfig config = ((walkingInCombat | walkInstead) ? MovementConfig.Default : MovementConfig.Everything).WithTolerance(3f);
        string label = "Moving to " + fate.Name;
        await TryTeleportShortcut(fate.Position, targetId, fate.Name);
        if (CancelToken.IsCancellationRequested)
        {
            return MoveStopReason.None;
        }
        long startedAtMs = Environment.TickCount64;
        long hardDeadline = startedAtMs + 240000;
        long deadline = startedAtMs + 60000;
        float closestMeters = ((Svc.Objects.LocalPlayer is { } localPlayer2) ? Vector3.Distance(localPlayer2.Position, dest) : float.MaxValue);
        bool extensionLogged = false;
        long lastRetargetAtMs = Environment.TickCount64;
        long nextProgressLogMs = Environment.TickCount64 + 15000;
        MoveStopReason stopReason = MoveStopReason.None;
        MoveStallTracker stuck = new MoveStallTracker();
        bool StopCondition()
        {
            Status = label;
            if (stopReason != MoveStopReason.None)
            {
                return true;
            }
            if (Environment.TickCount64 >= deadline)
            {
                stopReason = DeadlineExpired();
                return true;
            }
            if (walkingInCombat && !Svc.Condition[ConditionFlag.InCombat])
            {
                stopReason = MoveStopReason.CombatDropped;
                return true;
            }
            PublicEvent? fateById2 = PublicEvent.GetFateById(targetId);
            if (fateById2 == null)
            {
                stopReason = MoveStopReason.FateInvalid;
                return true;
            }
            if (fateById2.State != FateState.Running)
            {
                if (!FateScanner.AwaitsNpcStart(fateById2))
                {
                    stopReason = MoveStopReason.FateInvalid;
                    return true;
                }
                if (fateById2.MotivationNpc?.IsTargetable ?? false)
                {
                    stopReason = MoveStopReason.NpcSpawned;
                    return true;
                }
            }
            if (returnToFateId != targetId && Environment.TickCount64 - lastRetargetAtMs >= 1500)
            {
                lastRetargetAtMs = Environment.TickCount64;
                IPlayerCharacter? localPlayer4 = Svc.Objects.LocalPlayer;
                if (localPlayer4 != null)
                {
                    float targetDistance = Vector3.Distance(localPlayer4.Position, fateById2.Position);
                    if (targetDistance > 20f)
                    {
                        PublicEvent? nextFate = FateScanner.PickNext(Plugin.Cfg, localPlayer4.Position, sessionStuckFateIds);
                        if (nextFate != null && nextFate.Id != targetId && !WasPassedOver(nextFate.Id) && Vector3.Distance(localPlayer4.Position, nextFate.Position) + 15f < targetDistance)
                        {
                            AutoCommon.Diag($"Mid-path retarget: {targetId} -> {nextFate.Id} ({nextFate.Name}) (closer by >{15f:F0}m)");
                            CommitToFate(nextFate.Id);
                            stopReason = MoveStopReason.HigherPriority;
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        MoveStopReason DeadlineExpired()
        {
            AutoCommon.Diag((deadline >= hardDeadline) ? $"Move to FATE {targetId} ({fateName}) still not there after the {240}s cap ({closestMeters:F0}m left at best); escalating to teleport" : $"Move to FATE {targetId} ({fateName}) got no closer than {closestMeters:F0}m in {60}s; escalating to teleport");
            return MoveStopReason.StuckTeleport;
        }
        void ExtendDeadlineOnProgress(long now)
        {
            if (now >= deadline || Svc.Objects.LocalPlayer is not { } localPlayer4)
            {
                return;
            }
            float progressMeters = Vector3.Distance(localPlayer4.Position, dest);
            if (progressMeters > closestMeters - 20f)
            {
                return;
            }
            closestMeters = progressMeters;
            deadline = Math.Min(now + 60000, hardDeadline);
        }
        bool AbortIfFrozen()
        {
            if (stopReason != MoveStopReason.None)
            {
                return false;
            }
            if (Environment.TickCount64 >= nextProgressLogMs)
            {
                nextProgressLogMs = Environment.TickCount64 + 15000;
                Vector3? playerPos = Svc.Objects.LocalPlayer?.Position;
                string posText = playerPos is { } pos ? $"({pos.X:F0},{pos.Y:F0},{pos.Z:F0})" : "?";
                AutoCommon.Diag($"Still moving to FATE {targetId}: pos={posText} navRun={NavmeshIPC.Instance.IsRunning()} busy={NavmeshIPC.Instance.IsBusy()} inCombat={Svc.Condition[ConditionFlag.InCombat]}");
            }
            long tickCount = Environment.TickCount64;
            if (tickCount >= deadline + 10000)
            {
                stopReason = DeadlineExpired();
                return true;
            }
            ExtendDeadlineOnProgress(tickCount);
            if (!extensionLogged && deadline > tickCount && tickCount - startedAtMs >= 60000)
            {
                extensionLogged = true;
                AutoCommon.Diag($"Move to FATE {targetId} ({fateName}) still closing in after {60}s ({closestMeters:F0}m left); keeping it going, cap {240}s");
            }
            StallKind stallKind = stuck.Check();
            if (stallKind == StallKind.None)
            {
                return false;
            }
            stopReason = Svc.Condition[ConditionFlag.InCombat] ? MoveStopReason.StuckInCombat : MoveStopReason.StuckRetry;
            AutoCommon.Diag(Svc.Condition[ConditionFlag.InCombat] ? $"Move to FATE {targetId} ({fate.Name}) stalled in combat ({stallKind}); cancelling to clear aggro (teleport is blocked in combat)" : ((stallKind == StallKind.NavWedge) ? $"Move to FATE {targetId} ({fate.Name}) wedged: no progress toward the next waypoint in {3}s, even after a jump where one was possible; cancelling to retry" : $"Move to FATE {targetId} ({fate.Name}) idle: no nav/cast/mount progress in {8}s (clib teleport likely never started); cancelling to retry"));
            return true;
        }
        MoveOp op = new MoveOp((MoveOp o) => o.MoveInZoneWithFlightRecovery(dest, config, StopCondition, flightReplanPolicy, (string message) =>
        {
            AutoCommon.Diag(message);
        }));
        bool arrived = await RunCancellable(op, 250000, label, AbortIfFrozen);
        if (CancelToken.IsCancellationRequested)
        {
            return MoveStopReason.None;
        }
        if (Svc.ClientState.TerritoryType != zone.TerritoryId)
        {
            AutoCommon.Diag($"Move to FATE {targetId} ended in territory {Svc.ClientState.TerritoryType}, not {zone.TerritoryId} ({zone.Name}); its fastest teleport route leaves the zone");
            return MoveStopReason.LeftZone;
        }
        if (!arrived && stopReason == MoveStopReason.None)
        {
            stopReason = MoveStopReason.StuckTeleport;
        }
        if (stopReason == MoveStopReason.None && op.Fault is { } fault)
        {
            AutoCommon.Diag($"Move to FATE {targetId} ({fate.Name}) faulted: {fault.Message}; retrying");
            stopReason = MoveStopReason.StuckRetry;
        }
        if (stopReason != MoveStopReason.None)
        {
            return stopReason;
        }
        if (PublicEvent.GetFateById(targetId) is { } fateById && Svc.Objects.LocalPlayer is { } localPlayer3)
        {
            float centreDistance = FateGround.HorizontalDistance(localPlayer3.Position, fateById.Position);
            if (centreDistance > fateById.Radius)
            {
                AutoCommon.Diag($"Move to FATE {targetId} ({fateById.Name}) ended {centreDistance:F0}m from center (radius {fateById.Radius:F0}); outside the ring, treating as stuck");
                return MoveStopReason.StuckRetry;
            }
        }
        await SafeDismount($"dismount-{targetId}");
        return MoveStopReason.None;
    }

    private async Task TryTeleportShortcut(Vector3 fatePos, uint fateId, string fateName)
    {
        if (Plugin.Cfg.EconomyTravel)
        {
            return;
        }
        if (KeepingTwistOfFate())
        {
            return;
        }
        if (Svc.Condition[ConditionFlag.InCombat])
        {
            return;
        }
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return;
        }
        if (!ZoneAetherytes.TryFindNearest(zone.TerritoryId, fatePos, out var aetheryte))
        {
            return;
        }
        float playerDistance = Vector3.Distance(localPlayer.Position, fatePos);
        float aetheryteDistance = Vector3.Distance(aetheryte.Position, fatePos);
        float savingMeters = Pacing.TeleportShortcutSavingMeters(300f);
        if (playerDistance - aetheryteDistance < savingMeters)
        {
            return;
        }
        RefreshPendingCollectReward();
        if (CollectRewardPending)
        {
            AutoCommon.Diag($"Skipping the teleport shortcut to {aetheryte.Name} for FATE {fateId} ({fateName}): a same-zone teleport reloads the zone while Collect FATE {pendingRewardSpawn.FateId} ({pendingRewardName}) still owes its reward; flying instead");
            return;
        }
        Status = "Teleporting to " + aetheryte.Name;
        AutoCommon.Diag($"Teleport shortcut for FATE {fateId} ({fateName}): {aetheryte.Name} leaves {aetheryteDistance:F0}m to fly vs {playerDistance:F0}m from here (threshold {savingMeters:F0}m)");
        await PrepareForTeleport($"fate-approach-{fateId}");
        if (CancelToken.IsCancellationRequested)
        {
            return;
        }
        TeleportOutcome teleportOutcome = await RunTeleport(zone.TerritoryId, aetheryte.Position, allowSameZoneTeleport: true, 60000, $"fate-approach-{fateId}");
        Exception? fault = teleportOutcome.Fault;
        if (fault != null)
        {
            AutoCommon.Diag($"Teleport shortcut for FATE {fateId} faulted: {fault.Message}; flying from here instead");
        }
    }

    private async Task<bool> TryTeleportToFate(PublicEvent fate)
    {
        uint id = fate.Id;
        if (!ZoneAetherytes.TryFindNearest(zone.TerritoryId, fate.Position, out var nearest))
        {
            AutoCommon.Diag($"Teleport recovery for FATE {id}: {zone.Name} has no aetheryte of its own to teleport to");
            return false;
        }
        Status = "Teleporting to " + fate.Name;
        AutoCommon.Diag($"Teleport recovery to FATE {id} via {nearest.Name}");
        return await TeleportToZoneAetheryte(nearest, $"teleport-recovery-{id}");
    }

    private async Task<bool> TeleportToZoneAetheryte(ZoneAetheryte aetheryte, string scope)
    {
        await PrepareForTeleport(scope);
        if (CancelToken.IsCancellationRequested)
        {
            return false;
        }
        Vector3? before = Svc.Objects.LocalPlayer?.Position;
        TeleportOutcome teleportOutcome = await RunTeleport(zone.TerritoryId, aetheryte.Position, allowSameZoneTeleport: true, 60000, scope);
        if (!teleportOutcome.Succeeded)
        {
            AutoCommon.Diag($"{scope}: teleport to {aetheryte.Name} did not go through ({teleportOutcome.Fault?.Message ?? "timed out"})");
            return false;
        }
        Vector3? after = Svc.Objects.LocalPlayer?.Position;
        if (!before.HasValue || !after.HasValue)
        {
            return false;
        }
        float moved = Vector3.Distance(before.Value, after.Value);
        if (moved < 3f)
        {
            AutoCommon.Diag($"{scope}: teleport moved only {moved:F1}m; treating as failed");
            return false;
        }
        return true;
    }

    private async Task ClearBlockingCombat()
    {
        if (!Svc.Condition[ConditionFlag.InCombat])
        {
            return;
        }
        Status = "Clearing aggro";
        AutoCommon.Diag("In combat outside a FATE; enabling rotation to fight free before continuing");
        string preset = Plugin.Cfg.CombatPresetName;
        EnsureCombatPreset(preset);
        await SafeDismount("dismount-clearcombat");
        AssertPresetActive(preset);
        long deadline = Environment.TickCount64 + 30000;
        uint? runningFateId = null;
        try
        {
            while (Environment.TickCount64 < deadline)
            {
                if (CancelToken.IsCancellationRequested)
                {
                    return;
                }
                if (!(Svc.Condition[ConditionFlag.InCombat] && !IsPlayerKO()))
                {
                    break;
                }
                PublicEvent? currentFate = PublicEvent.CurrentFate;
                if (currentFate != null && currentFate.State == FateState.Running && currentFate.Id != abandonedFateId)
                {
                    runningFateId = currentFate.Id;
                    break;
                }
                if (Svc.Condition[ConditionFlag.Mounted])
                {
                    BossModIPC.Instance.ClearActive();
                    await SafeDismount("dismount-clearcombat");
                }
                AssertPresetActive(preset);
                await NextFrame(30);
            }
        }
        finally
        {
            BossModIPC.Instance.ClearActive();
        }
        if (!Svc.Condition[ConditionFlag.InCombat])
        {
            return;
        }
        if (runningFateId is { } runningId)
        {
            AutoCommon.Diag($"FATE {runningId} is running on top of us; leaving its combat to the state machine");
            return;
        }
        if (IsPlayerKO())
        {
            AutoCommon.Diag("KO'd while fighting free; leaving the recovery to the state machine");
            return;
        }
        AutoCommon.Diag($"Still in combat after {30}s of fighting");
    }

    private void EnsureCombatPreset(string preset)
    {
        if (presetEnsured)
        {
            return;
        }
        if (preset != "FATE Autopilot")
        {
            presetEnsured = true;
            return;
        }
        Configuration cfg = Plugin.Cfg;
        bool missing = BossModIPC.Instance.GetPreset(preset) == null;
        bool outdated = cfg.BundledCombatPresetRevision < 1;
        if (missing | outdated)
        {
            AutoCommon.Diag(missing ? ("Default preset '" + preset + "' missing from BossMod, creating it.") : $"Default preset '{preset}' is at revision {cfg.BundledCombatPresetRevision}, bundled is {1}; overwriting it.");
            if (BossModIPC.Instance.CreatePreset(DefaultCombatPreset.GetSerialized(), overwrite: true))
            {
                cfg.BundledCombatPresetRevision = 1;
                cfg.Save();
            }
            else
            {
                AutoCommon.Diag("BossMod.Presets.Create returned false for '" + preset + "'.");
            }
        }
        presetEnsured = true;
    }

    private void AssertPresetActive(string preset)
    {
        if (BossModIPC.Instance.GetActive() == preset)
        {
            BossModFateHelper.SyncChocobo(preset, reapply: false);
            return;
        }
        if (!BossModIPC.Instance.SetActive(preset))
        {
            AutoCommon.Diag("BossMod.Presets.SetActive('" + preset + "') returned false — preset may not exist.");
            return;
        }
        BossModIPC.Instance.AddTransientStrategy(preset, "BossMod.Autorotation.MiscAI.AutoTarget", "MaxTargets", PullSize().ToString());
        BossModFateHelper.SyncChocobo(preset, reapply: true);
    }

    private unsafe static void SyncToFate(uint fateId)
    {
        FateManager* fateManager = FateManager.Instance();
        if (fateManager == null)
        {
            return;
        }
        if (fateManager->CurrentFate == null)
        {
            return;
        }
        if (fateManager->CurrentFate->FateId != fateId)
        {
            return;
        }
        if (fateManager->SyncedFateId == fateId)
        {
            return;
        }
        fateManager->LevelSync();
    }

    private static int PullSize()
    {
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return 3;
        }
        byte role = localPlayer.ClassJob.Value.Role;
        return role switch
        {
            1 => 0,
            4 => 5,
            _ => 3,
        };
    }

    private static Vector3 RandomPointInsideRadius(Vector3 center, float radius)
    {
        double angle = rng.NextDouble() * Math.PI * 2.0;
        float radial = (float)(Math.Sqrt(rng.NextDouble()) * (double)radius);
        return new Vector3(center.X + (float)Math.Cos(angle) * radial, center.Y, center.Z + (float)Math.Sin(angle) * radial);
    }

    private bool StopConditionMet()
    {
        Configuration cfg = Plugin.Cfg;
        ModeContext ctx = new ModeContext
        {
            CompletedCount = session.CompletedCount,
            Zones = zones,
            Elapsed = session.Elapsed,
            LevelsGained = session.LevelsGained,
            LevelingJobAtMax = (cfg.StopAfterLevelsEnabled && ClassSwitcher.LevelingJobAtMax(cfg))
        };
        if (!cfg.ActiveMode.IsComplete(ctx))
        {
            return RunLimits.Reached(cfg, ctx);
        }
        return true;
    }

    private bool AdvanceClassQueueIfCapHit()
    {
        Configuration cfg = Plugin.Cfg;
        if (!cfg.ApplyClassOnStart)
        {
            return false;
        }
        if (cfg.ClassQueue.Count == 0)
        {
            return false;
        }
        int activeIndex = ClassSwitcher.FindActiveEntryIndex(cfg.ClassQueue);
        if (activeIndex < 0)
        {
            if (cfg.AfterClassQueueDone == AfterClassQueueDone.StopRun)
            {
                Status = "Class queue done";
                AutoCommon.Diag("All queued classes hit their level caps, stopping run");
                return true;
            }
            return false;
        }
        ClassQueueEntry classQueueEntry = cfg.ClassQueue[activeIndex];
        byte targetJobId = ClassSwitcher.JobIdForUserIndex(classQueueEntry.GearsetIndex);
        uint currentJobId = Svc.Objects.LocalPlayer?.ClassJob.RowId ?? 0;
        if (targetJobId == 0 || targetJobId == currentJobId)
        {
            return false;
        }
        AutoCommon.Diag($"Class cap reached; switching to gearset {classQueueEntry.GearsetIndex} ({ClassSwitcher.JobNameForUserIndex(classQueueEntry.GearsetIndex)})");
        ClassSwitcher.TryEquip(classQueueEntry);
        return false;
    }

    private static void EnableTextAdvanceForCollect()
    {
        if (textAdvanceArmed)
        {
            return;
        }
        if (!ExternalPlugins.IsInstalled(ExternalPlugin.TextAdvance))
        {
            return;
        }
        try
        {
            TextAdvanceIPC.EnableExternalControl("FateAutopilot", talkSkip: true, requestFill: true, requestHandin: true);
            textAdvanceArmed = true;
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "TextAdvance enable failed");
        }
    }

    private static void DisableTextAdvance()
    {
        if (!textAdvanceArmed)
        {
            return;
        }
        try
        {
            TextAdvanceIPC.DisableExternalControl("FateAutopilot");
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "TextAdvance disable failed");
        }
        textAdvanceArmed = false;
    }
}
