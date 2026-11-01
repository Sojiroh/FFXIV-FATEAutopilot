using System;
using System.Numerics;
using System.Threading.Tasks;
using AutoFateGrind.Core.Game.Fates;
using AutoFateGrind.Core.Game.Yokai;
using AutoFateGrind.Core.Ipc;
using AutoFateGrind.Core.Modes;
using AutoFateGrind.Core.Trading;
using AutoFateGrind.Core.Zones;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using clib.Extensions;
using clib.TaskSystem;
using clib.Utils;

namespace AutoFateGrind.Core.Tasks;

public sealed partial class AutoFate
{
    private async Task<ExitReason> MoveAndArrive()
    {
        IPlayerCharacter? player = Svc.Objects.LocalPlayer;
        if (player == null)
        {
            await NextFrame();
            return ExitReason.Continue;
        }
        await EnsureConsumables();
        await EnsureYokaiCompanion();
        if (CancelToken.IsCancellationRequested)
        {
            return ExitReason.Quit;
        }
        PublicEvent? fate = PickFate(player.Position);
        if (fate == null)
        {
            return ExitReason.Continue;
        }
        uint pickedId = fate.Id;
        string pickedName = fate.Name;
        Status = "Moving to " + fate.Name;
        AutoCommon.Diag($"Picked FATE {fate.Id} ({fate.Name}) at {fate.Position}");
        MoveStopReason moveStopReason = await MoveToFate(fate);
        if (CancelToken.IsCancellationRequested)
        {
            return ExitReason.Quit;
        }
        if (moveStopReason is MoveStopReason.HigherPriority or MoveStopReason.CombatDropped)
        {
            return ExitReason.Continue;
        }
        if (moveStopReason == MoveStopReason.StuckInCombat)
        {
            await ClearBlockingCombat();
            return ExitReason.Continue;
        }
        if (moveStopReason == MoveStopReason.LeftZone)
        {
            lastTeleportedFateId = null;
            lastStuckFateId = null;
            consecutiveStuckRetries = 0;
            sessionStuckFateIds.Add(pickedId);
            AutoCommon.Diag($"FATE {pickedId} ({pickedName}) left {zone.Name} despite an in-zone-only route; blacklisting for this session");
            return ExitReason.Continue;
        }
        if (lastTeleportedFateId == fate.Id && moveStopReason != MoveStopReason.None && moveStopReason != MoveStopReason.NpcSpawned)
        {
            AutoCommon.Diag($"Still stuck after teleport recovery for FATE {fate.Id} ({fate.Name}); skipping it until another FATE completes");
            sessionStuckFateIds.Add(fate.Id);
            DeferSkipUntilNextCompletion(fate.Id);
            lastTeleportedFateId = null;
            lastStuckFateId = null;
            consecutiveStuckRetries = 0;
            return ExitReason.Continue;
        }
        if (moveStopReason == MoveStopReason.StuckRetry)
        {
            if (lastStuckFateId == fate.Id)
            {
                consecutiveStuckRetries++;
            }
            else
            {
                lastStuckFateId = fate.Id;
                consecutiveStuckRetries = 1;
            }
            if (consecutiveStuckRetries >= 2)
            {
                AutoCommon.Diag($"Repeated stuck on FATE {fate.Id} ({fate.Name}); escalating to teleport");
                moveStopReason = MoveStopReason.StuckTeleport;
            }
            else
            {
                AutoCommon.Diag($"Stuck en route to FATE {fate.Id} ({fate.Name}); retrying from current position");
                return ExitReason.Continue;
            }
        }
        if (moveStopReason == MoveStopReason.StuckTeleport)
        {
            if (Svc.Condition[ConditionFlag.InCombat])
            {
                AutoCommon.Diag($"Stuck-teleport for FATE {fate.Id} but in combat; clearing aggro before teleporting (teleport is blocked in combat)");
                await ClearBlockingCombat();
                return ExitReason.Continue;
            }
            if (await TryTeleportToFate(fate))
            {
                lastTeleportedFateId = fate.Id;
                lastStuckFateId = null;
                consecutiveStuckRetries = 0;
                return ExitReason.Continue;
            }
            sessionStuckFateIds.Add(fate.Id);
            DeferSkipUntilNextCompletion(fate.Id);
            lastTeleportedFateId = null;
            lastStuckFateId = null;
            consecutiveStuckRetries = 0;
            AutoCommon.Diag($"Teleport recovery failed for FATE {fate.Id}; skipping it until another FATE completes");
            return ExitReason.Continue;
        }
        if (lastStuckFateId == fate.Id)
        {
            lastStuckFateId = null;
            consecutiveStuckRetries = 0;
        }
        if (lastTeleportedFateId == fate.Id)
        {
            lastTeleportedFateId = null;
        }
        PublicEvent? fateById = PublicEvent.GetFateById(fate.Id);
        if (fateById == null)
        {
            return ExitReason.Continue;
        }
        fate = fateById;
        if (FateScanner.AwaitsNpcStart(fate))
        {
            await ActivateFate(fate);
        }
        if (returnToFateId == fate.Id && fate.State == FateState.Running)
        {
            returnToFateId = null;
        }
        return ExitReason.Continue;
    }

    private async Task<ExitReason> EngageCurrentFate()
    {
        PublicEvent? fate = PublicEvent.CurrentFate;
        if (fate == null)
        {
            return ExitReason.Continue;
        }
        uint fateId = fate.Id;
        await EnsureYokaiCompanion();
        PublicEvent? fateById = PublicEvent.GetFateById(fateId);
        if (fateById == null || LeaveIfExcluded(fateById))
        {
            return ExitReason.Continue;
        }
        fate = fateById;
        string preset = Plugin.Cfg.CombatPresetName;
        EnsureCombatPreset(preset);
        SyncToFate(fateId);
        AssertPresetActive(preset);
        ResetEngageOverrides(preset);
        BossModMovementTuning.Apply(preset, fateId);
        await EnsureObstacleMapForEngage(fate);
        PublicEvent? fateById2 = PublicEvent.GetFateById(fateId);
        if (fateById2 == null)
        {
            return ExitReason.Continue;
        }
        fate = fateById2;
        string fateName = fate.Name;
        FateType fateType = fate.FateType;
        bool isCollect = fate.Rule == PublicEvent.FateRule.Collect;
        FateSpawnKey spawn = new FateSpawnKey(fateId, fate.StartTimeEpoch);
        Status = "Engaging " + fateName;
        int lastProgress = fate.Progress;
        long lastProgressAtMs = Environment.TickCount64;
        long lastInCombatAtMs = Environment.TickCount64;
        long lastBounceAtMs = Environment.TickCount64;
        int combatStallBounces = 0;
        bool sawRunning = false;
        EngageIdleTracker idle = new EngageIdleTracker(EngageReachMeters());
        if (isCollect)
        {
            BeginCollectFate(spawn, fateName);
        }
        try
        {
            while (!CancelToken.IsCancellationRequested)
            {
                PublicEvent? fateById3 = PublicEvent.GetFateById(fateId);
                if (fateById3 == null || fateById3.State != FateState.Running)
                {
                    break;
                }
                if (IsPlayerKO())
                {
                    RegisterDeath(fateId, fateName, fateType);
                    break;
                }
                fate = fateById3;
                sawRunning = true;
                if (LeaveIfExcluded(fate) || (isCollect && fate.Progress >= 100))
                {
                    break;
                }
                if (Svc.Condition[ConditionFlag.InCombat])
                {
                    lastInCombatAtMs = Environment.TickCount64;
                }
                if (fate.Progress != lastProgress)
                {
                    lastProgress = fate.Progress;
                    lastProgressAtMs = Environment.TickCount64;
                    combatStallBounces = 0;
                }
                else
                {
                    if (Environment.TickCount64 - lastProgressAtMs > 60000 && Environment.TickCount64 - lastInCombatAtMs > 30000)
                    {
                        AutoCommon.Diag($"EngageFate stalled: no progress in {60}s and out of combat {30}s on FATE {fateId}; bailing ({DescribeEngageSituation(fateId, idle.Meters)})");
                        RegisterEngageStall(fateId, fate.Name);
                        break;
                    }
                    if (Environment.TickCount64 - lastProgressAtMs > 30000 && Environment.TickCount64 - lastBounceAtMs > 30000 && !pullActive && (!Svc.Condition[ConditionFlag.InCombat] || !HasTargetInReach(fateId, idle.Meters)))
                    {
                        lastBounceAtMs = Environment.TickCount64;
                        combatStallBounces++;
                        if (combatStallBounces > 3)
                        {
                            AutoCommon.Diag($"FATE {fateId} still not progressing after {3} preset bounces ({DescribeEngageSituation(fateId, idle.Meters)})");
                            RegisterEngageStall(fateId, fate.Name);
                            break;
                        }
                        AutoCommon.Diag($"No progress in {30}s on FATE {fateId}; bouncing combat preset ({combatStallBounces}/{3}; {DescribeEngageSituation(fateId, idle.Meters)})");
                        await BounceCombatPreset(preset);
                    }
                }
                if (Svc.Condition[ConditionFlag.Mounted])
                {
                    BossModIPC.Instance.ClearActive();
                    await SafeDismount($"dismount-engage-{fateId}");
                    AssertPresetActive(preset);
                }
                else
                {
                    AssertPresetActive(preset);
                }
                SyncToFate(fateId);
                if (isCollect)
                {
                    UpdateCollectPullHold(fateId, preset);
                    if (await MaybeHandInCollectItems(fateId, fateName, preset) || await TickPickupWedge(fateId, fateName, preset))
                    {
                        lastProgressAtMs = Environment.TickCount64;
                        lastInCombatAtMs = Environment.TickCount64;
                    }
                }
                bool pulling = !isCollect && TickPull(fateId, fate, preset);
                if (pulling)
                {
                    EndRingChase(preset);
                    idle.Restart();
                }
                bool chasedRing = !pulling && await TickRingChase(fateId, idle, preset);
                if (!pulling && !chasedRing && !isCollect && await TickEngagementWatchdog(fateId, fate, idle))
                {
                    break;
                }
                await NextFrame(30);
            }
            if (isCollect & sawRunning)
            {
                PublicEvent? fateById4 = PublicEvent.GetFateById(fateId);
                if (fateById4 != null && fateById4.Progress >= 100)
                {
                    await WrapUpCollectFate(fateId, fateName, preset);
                }
            }
        }
        finally
        {
            EndPull(preset, fateId, null);
            EndRingChase(preset);
            ReleaseCollectPullHold(preset);
            BossModIPC.Instance.ClearActive();
            if (isCollect)
            {
                DisableTextAdvance();
            }
        }
        int finalProgress = PublicEvent.GetFateById(fateId)?.Progress ?? lastProgress;
        bool fateCompleted = sawRunning && (PublicEvent.GetFateById(fateId) == null || finalProgress >= 100);
        if (fateCompleted && lastCompletedSpawn != spawn)
        {
            lastCompletedSpawn = spawn;
            ClearEngageStall(fateId);
            session.CompletedCount++;
            session.FatesSinceLastBreak++;
            zone.CompletedThisRun++;
            if (isCollect)
            {
                session.UpdateGemstones();
            }
            else
            {
                await SettleGemstoneReward();
            }
            session.UpdateExp();
            YokaiProgress.Invalidate();
            NoteSharedFateCompletion();
            ReleaseDeferredSkips();
            AutoCommon.Diag($"FATE {fateId} done (session total: {session.CompletedCount}, wallet {session.GemstoneCurrent}g)");
            LogYokaiDropState();
            StartFollowUpWatch(fateId);
            BeginSettle($"FATE {fateId} done");
            if (AdvanceClassQueueIfCapHit())
            {
                return ExitReason.Quit;
            }
            if (HandoffTriggers.QueueIfDue(session, zone))
            {
                await HoldForCollectReward();
                await WaitOutSettle();
                await ClearBlockingCombat();
                if (CancelToken.IsCancellationRequested)
                {
                    return ExitReason.Quit;
                }
                if (Svc.Condition[ConditionFlag.InCombat])
                {
                    session.ClearQueuedHandoffs();
                    AutoCommon.Diag("Hand-off deferred: still in combat after fighting free; the grind plays on and queues it again after the next FATE");
                    return ExitReason.Continue;
                }
                return ExitReason.Quit;
            }
        }
        return ExitReason.Continue;
    }

    private static float EngageReachMeters()
    {
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return 25f;
        }
        byte role = localPlayer.ClassJob.Value.Role;
        if (role is not (1 or 2))
        {
            return 25f;
        }
        return 4f;
    }

    private async Task BounceCombatPreset(string preset)
    {
        BossModIPC.Instance.ClearActive();
        await NextFrame(2);
        AssertPresetActive(preset);
    }

    private async Task<bool> TickEngagementWatchdog(uint fateId, PublicEvent fate, EngageIdleTracker idle)
    {
        if (fate.Rule == PublicEvent.FateRule.Collect)
        {
            return false;
        }
        if (Svc.Condition[ConditionFlag.Mounted])
        {
            return false;
        }
        if (StuckDetector.IsPositionFrozenLegit())
        {
            return false;
        }
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return false;
        }
        if (Svc.Condition[ConditionFlag.InCombat] && HasTargetInReach(fateId, idle.Meters))
        {
            idle.MarkInReach();
            return false;
        }
        idle.MarkOutOfReach();
        if (!idle.Stalled(localPlayer.Position))
        {
            return false;
        }
        string fateName = fate.Name;
        FateMobSurvey survey = FateMobScanner.Survey(fateId, localPlayer.Position);
        if (!survey.Any)
        {
            if (await SeekFateCentre(fateId, fateName, fate.Position, localPlayer.Position))
            {
                Status = "Engaging " + fateName;
            }
            idle.Restart();
            return false;
        }
        if (idle.Repositions >= 3)
        {
            AutoCommon.Diag($"FATE {fateId} ({fateName}) unreachable: still {survey.NearestDistanceToHitbox:F0}m from the nearest mob's hitbox after {3} repositions; abandoning and blacklisting for this session ({DescribeEngageSituation(fateId, idle.Meters)})");
            AbandonFate(fateId);
            return true;
        }
        await RepositionToFateMob(fateId, fateName, survey, idle);
        idle.Restart();
        Status = "Engaging " + fateName;
        return false;
    }

    private static bool HasTargetInReach(uint fateId, float reachMeters)
    {
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer != null && FateMobScanner.TryGetTargetedMob(fateId, localPlayer.Position, out var distanceToHitbox))
        {
            return distanceToHitbox <= reachMeters;
        }
        return false;
    }

    private async Task RepositionToFateMob(uint fateId, string fateName, FateMobSurvey survey, EngageIdleTracker idle)
    {
        idle.CountReposition();
        Status = "Closing on " + fateName;
        AutoCommon.Diag($"Engagement idle on FATE {fateId} ({fateName}) for {8}s with nothing in reach; walking to the nearest mob with vnav (attempt {idle.Repositions}/{3}; {DescribeEngageSituation(fateId, idle.Meters)})");
        if (!FateMobScanner.IsTargetingMobOf(fateId) && Svc.Objects.LocalPlayer is { } localPlayer && FateMobScanner.TargetNearest(fateId, localPlayer.Position))
        {
            AutoCommon.Diag($"No mob of FATE {fateId} ({fateName}) was targeted; targeted the nearest one");
        }
        Vector3 dest = survey.NearestPosition.OnMesh();
        bool closeEnough = idle.Meters <= 4f || survey.NearestDistanceToHitbox <= 15f;
        float toleranceMeters = survey.NearestHitboxRadius + (closeEnough ? 2.5f : 15f);
        MovementConfig config = MovementConfig.Default.WithTolerance(toleranceMeters);
        float reachMeters = idle.Meters;
        await WalkWithBossModParked(dest, config, EngagedOrGone, $"engage-reposition-{fateId}");
        bool EngagedOrGone()
        {
            if (PublicEvent.GetFateById(fateId) is not { State: FateState.Running })
            {
                return true;
            }
            IPlayerCharacter? localPlayer2 = Svc.Objects.LocalPlayer;
            if (localPlayer2 == null || !FateMobScanner.Survey(fateId, localPlayer2.Position).Any)
            {
                return true;
            }
            if (Svc.Condition[ConditionFlag.InCombat])
            {
                return HasTargetInReach(fateId, reachMeters);
            }
            return false;
        }
    }

    private async Task<bool> SeekFateCentre(uint fateId, string fateName, Vector3 centre, Vector3 from)
    {
        float distance = Vector3.Distance(from, centre);
        if (distance <= 20f)
        {
            return false;
        }
        Status = "Searching " + fateName;
        AutoCommon.Diag($"No live mob of FATE {fateId} ({fateName}) is loaded; walking to the ring centre {distance:F0}m away to load the rest");
        Vector3 dest = FateGround.Project(centre) ?? centre;
        MovementConfig config = MovementConfig.Default.WithTolerance(10f);
        await WalkWithBossModParked(dest, config, MobSeenOrGone, $"engage-seek-centre-{fateId}");
        return true;
        bool MobSeenOrGone()
        {
            if (PublicEvent.GetFateById(fateId) is not { State: FateState.Running })
            {
                return true;
            }
            IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
            if (localPlayer == null)
            {
                return true;
            }
            return FateMobScanner.Survey(fateId, localPlayer.Position).Any;
        }
    }

    private async Task WalkWithBossModParked(Vector3 dest, MovementConfig config, Func<bool> stopCondition, string label)
    {
        string preset = Plugin.Cfg.CombatPresetName;
        bool parked = ParkBossModMovement(preset);
        try
        {
            await WalkInFight(dest, config, stopCondition, label);
        }
        finally
        {
            if (parked)
            {
                ResumeBossModMovement(preset);
            }
        }
    }

    private async Task WalkInFight(Vector3 dest, MovementConfig config, Func<bool> stopCondition, string label)
    {
        MoveOp op = new MoveOp((MoveOp o) => o.MoveInZone(dest, config, stopCondition));
        await RunCancellable(op, 40000, label, StuckDetector.MoveStallAbort(label));
        Exception? fault = op.Fault;
        if (fault != null)
        {
            AutoCommon.Diag(label + " faulted: " + fault.Message);
        }
    }

    private async Task<bool> TickRingChase(uint fateId, EngageIdleTracker idle, string preset)
    {
        if (!TryFindRingChaseTarget(fateId, idle.Meters, out var target))
        {
            EndRingChase(preset);
            return false;
        }
        if (target.GameObjectId != ringChaseTargetId)
        {
            ringChaseTargetId = target.GameObjectId;
            ringChaseFailures = 0;
            AutoCommon.Diag($"Target of FATE {fateId} stands outside BossMod's FATE-ring pathfind area ({target.DistanceToHitbox:F0}m off its hitbox); FTA walks the character while BossMod keeps attacking ({DescribeEngageSituation(fateId, idle.Meters)})");
        }
        if (!ringChaseParked)
        {
            ringChaseParked = BossModIPC.Instance.AddTransientStrategy(preset, "BossMod.Autorotation.MiscAI.NormalMovement", "Destination", "None");
        }
        float goalMeters = RingGoalMeters(idle.Meters);
        if (target.DistanceToHitbox <= goalMeters || StuckDetector.IsPositionFrozenLegit())
        {
            return true;
        }
        Status = "Closing on a mob outside the FATE ring";
        float distanceBefore = target.DistanceToHitbox;
        if (await ChaseRingTarget(fateId, target, goalMeters) || ClosedOnRingTarget(fateId, target.GameObjectId, distanceBefore))
        {
            ringChaseFailures = 0;
            return true;
        }
        ringChaseFailures++;
        if (ringChaseFailures < 3)
        {
            return true;
        }
        AutoCommon.Diag($"Could not reach the out-of-ring target of FATE {fateId} after {3} walks; handing movement back to BossMod until the target changes");
        ringChaseGivenUpTargetId = target.GameObjectId;
        EndRingChase(preset);
        return false;
    }

    private bool TryFindRingChaseTarget(uint fateId, float reachMeters, out FateMobTarget target)
    {
        target = default;
        if (!BossModIPC.Instance.CanClearTransientStrategy || Svc.Condition[ConditionFlag.Mounted])
        {
            return false;
        }
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null || PublicEvent.GetFateById(fateId) is not { State: FateState.Running } fateById)
        {
            return false;
        }
        if (!FateMobScanner.TryGetTarget(fateId, localPlayer.Position, out target))
        {
            return false;
        }
        if (target.GameObjectId == ringChaseGivenUpTargetId)
        {
            return false;
        }
        ringChaseGivenUpTargetId = 0uL;
        return IsBeyondBossModRing(fateById.Position, fateById.Radius, in target, RingGoalMeters(reachMeters), localPlayer.Position);
    }

    private static bool ClosedOnRingTarget(uint fateId, ulong targetId, float distanceBefore)
    {
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null || !FateMobScanner.TryGetTarget(fateId, localPlayer.Position, out var target))
        {
            return false;
        }
        if (target.GameObjectId == targetId)
        {
            return distanceBefore - target.DistanceToHitbox >= 1f;
        }
        return false;
    }

    private static float RingGoalMeters(float reachMeters)
    {
        if (!(reachMeters <= 4f))
        {
            return 25f;
        }
        return 3f;
    }

    private static bool IsBeyondBossModRing(Vector3 centre, float radius, in FateMobTarget target, float goalMeters, Vector3 playerPosition)
    {
        if (radius <= 0f)
        {
            return false;
        }
        Vector2 centreToTarget = new Vector2(target.Position.X - centre.X, target.Position.Z - centre.Z);
        float edgeGap = centreToTarget.Length() - target.HitboxRadius - (radius - 0.5f);
        if (edgeGap > goalMeters)
        {
            return true;
        }
        if (edgeGap <= 0f || target.DistanceToHitbox <= goalMeters)
        {
            return false;
        }
        Vector2 centreToPlayer = new Vector2(playerPosition.X - centre.X, playerPosition.Z - centre.Z);
        return centreToPlayer.Length() >= radius - goalMeters - 0.5f;
    }

    private async Task<bool> ChaseRingTarget(uint fateId, FateMobTarget target, float goalMeters)
    {
        float toleranceMeters = target.HitboxRadius + ((goalMeters <= 3f) ? 2.5f : 15f);
        MovementConfig config = MovementConfig.Default.WithTolerance(toleranceMeters);
        await WalkInFight(target.Position.OnMesh(), config, Settled, $"engage-ring-chase-{fateId}");
        return Settled();
        bool Settled()
        {
            if (PublicEvent.GetFateById(fateId) is not { State: FateState.Running })
            {
                return true;
            }
            IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
            if (localPlayer == null || !FateMobScanner.TryGetTarget(fateId, localPlayer.Position, out var target2))
            {
                return true;
            }
            if (target2.GameObjectId == target.GameObjectId && !(target2.DistanceToHitbox <= goalMeters))
            {
                return Vector3.Distance(target2.Position, target.Position) >= 3f;
            }
            return true;
        }
    }

    private void EndRingChase(string preset)
    {
        ringChaseTargetId = 0uL;
        ringChaseFailures = 0;
        if (!ringChaseParked)
        {
            return;
        }
        ringChaseParked = false;
        ResumeBossModMovement(preset);
    }

    private void ResetEngageOverrides(string preset)
    {
        ResetPullOverrides(preset);
        ringChaseParked = false;
        ringChaseTargetId = 0uL;
        ringChaseFailures = 0;
        ringChaseGivenUpTargetId = 0uL;
        collectPullsHeld = false;
        if (!BossModIPC.Instance.CanClearTransientStrategy)
        {
            return;
        }
        BossModIPC.Instance.ClearTransientStrategy(preset, "BossMod.Autorotation.MiscAI.NormalMovement", "Destination");
        BossModIPC.Instance.ClearTransientStrategy(preset, "BossMod.Autorotation.MiscAI.AutoTarget", "CollectFATE");
    }

    private void RegisterEngageStall(uint fateId, string fateName)
    {
        if (engageStallFateId != fateId)
        {
            engageStallFateId = fateId;
            engageStallStrikes = 0;
        }
        engageStallStrikes++;
        if (engageStallStrikes < 2)
        {
            AutoCommon.Diag($"FATE {fateId} ({fateName}) engagement bail {engageStallStrikes}/{2}; re-entering engagement from scratch");
        }
        else
        {
            AutoCommon.Diag($"FATE {fateId} ({fateName}) made no progress through {engageStallStrikes} engagement attempts; abandoning and blacklisting for this session");
            AbandonFate(fateId);
        }
    }

    private void ClearEngageStall(uint fateId)
    {
        if (!(engageStallFateId == fateId))
        {
            return;
        }
        engageStallFateId = null;
        engageStallStrikes = 0;
    }

    private void AbandonFate(uint fateId)
    {
        sessionStuckFateIds.Add(fateId);
        LeaveFate(fateId);
    }

    private void LeaveFate(uint fateId)
    {
        abandonedFateId = fateId;
        if (returnToFateId == fateId)
        {
            returnToFateId = null;
        }
        ClearEngageStall(fateId);
        if (FateMobScanner.IsTargetingMobOf(fateId))
        {
            Svc.Targets.Target = null;
        }
    }

    private void RegisterDeath(uint fateId, string fateName, FateType fateType)
    {
        int deaths = session.CountDeath(fateId);
        Configuration cfg = Plugin.Cfg;
        if (!cfg.AutoBlacklistOnDeaths)
        {
            AutoCommon.Diag($"KO'd in FATE {fateId} ({fateName}); death {deaths} there this run (auto-blacklist off)");
        }
        else if (deaths < cfg.AutoBlacklistDeathCount)
        {
            AutoCommon.Diag($"KO'd in FATE {fateId} ({fateName}); death {deaths}/{cfg.AutoBlacklistDeathCount} there this run");
        }
        else
        {
            AutoCommon.Diag($"KO'd in FATE {fateId} ({fateName}) {deaths} times this run; blacklisting it and moving on");
            FateBlacklist.Add(cfg, fateType, fateId);
            LeaveFate(fateId);
            Svc.Chat.Print($"[FTA] Died {deaths} times in {fateName}; it is now blacklisted. Remove it under Settings > FATE filters > Blacklist to grind it again.");
        }
    }

    private unsafe static string DescribeEngageSituation(uint fateId, float reachMeters)
    {
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return "player=none";
        }
        Vector3 position = localPlayer.Position;
        FateMobSurvey fateMobSurvey = FateMobScanner.Survey(fateId, position);
        IGameObject? target = Svc.Targets.Target;
        string targetText = target == null ? "none" : (FateMobScanner.TryGetTargetedMob(fateId, position, out var distanceToHitbox) ? $"{target.Name}@{distanceToHitbox:F0}m" : $"{target.Name}(not this FATE)");
        string nearestText = (fateMobSurvey.Any ? $"{fateMobSurvey.NearestDistanceToHitbox:F0}m dY={fateMobSurvey.NearestVerticalDelta:F0}" : "none");
        FateManager* fateManager = FateManager.Instance();
        bool synced = fateManager != null && fateManager->SyncedFateId == fateId;
        return $"pos=({position.X:F0},{position.Y:F0},{position.Z:F0}) combat={Svc.Condition[ConditionFlag.InCombat]} target={targetText} liveMobs={fateMobSurvey.LiveCount} nearest={nearestText} reach={reachMeters:F0}m synced={synced} preset={BossModIPC.Instance.GetActive() ?? "none"}";
    }

    private static bool ParkBossModMovement(string preset)
    {
        if (BossModIPC.Instance.CanClearTransientStrategy && BossModIPC.Instance.AddTransientStrategy(preset, "BossMod.Autorotation.MiscAI.NormalMovement", "Destination", "None"))
        {
            return true;
        }
        BossModIPC.Instance.ClearActive();
        return false;
    }

    private void ResumeBossModMovement(string preset)
    {
        if (BossModIPC.Instance.ClearTransientStrategy(preset, "BossMod.Autorotation.MiscAI.NormalMovement", "Destination"))
        {
            return;
        }
        AutoCommon.Diag("Could not clear the NormalMovement override on preset '" + preset + "'; re-applying the preset instead");
        BossModIPC.Instance.ClearActive();
    }

    private Vector3 noProgressPos;

    private const int ConsumeItemWaitMs = 6000;

    private const string NormalMovementModule = "BossMod.Autorotation.MiscAI.NormalMovement";

    private const string NormalMovementDestinationTrack = "Destination";

    private const string NormalMovementParkedOption = "None";

    private bool ringChaseParked;

    private ulong ringChaseTargetId;

    private int ringChaseFailures;

    private async Task SettleGemstoneReward()
    {
        if (!GemstoneCatalog.TryCurrentWalletCount(out var before))
        {
            session.UpdateGemstones();
            return;
        }
        long deadline = Environment.TickCount64 + 2500;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested)
            {
                break;
            }
            if (GemstoneCatalog.TryCurrentWalletCount(out var count) && count != before)
            {
                break;
            }
            await DelayMs(100);
        }
        session.UpdateGemstones();
    }

    private bool GoalZoneDone()
    {
        return ZoneDone(zone);
    }

    private bool GoalZoneDoneAt(int candidateIndex)
    {
        return ZoneDone(zones[candidateIndex]);
    }

    private static bool ZoneDone(ZoneInfo candidate)
    {
        if (!RunLimits.ZoneShareDone(Plugin.Cfg, candidate))
        {
            return Plugin.Cfg.ActiveMode.IsZoneDone(candidate.TerritoryId);
        }
        return true;
    }

    private static string ZoneDoneReason(ZoneInfo done)
    {
        if (!RunLimits.ZoneShareDone(Plugin.Cfg, done))
        {
            return Plugin.Cfg.ActiveMode.ZoneDoneReason(done.TerritoryId);
        }
        return $"{done.CompletedThisRun} of {RunLimits.FatesPerZoneCap(Plugin.Cfg)} FATEs done here";
    }
}
