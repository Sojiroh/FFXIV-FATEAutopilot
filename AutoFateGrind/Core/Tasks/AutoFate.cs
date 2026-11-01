using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using AutoFateGrind.Core.External;
using AutoFateGrind.Core.Game.Fates;
using AutoFateGrind.Core.Game.Ops;
using AutoFateGrind.Core.Ipc;
using AutoFateGrind.Core.Zones;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using clib.Utils;

namespace AutoFateGrind.Core.Tasks;

public sealed partial class AutoFate(IReadOnlyList<ZoneInfo> zones, AutoFateSession session, int startIndex = 0) : AutoCommon
{
    private float pickupWedgeBestMeters;

    private long pickupWedgeSinceMs;

    private int pickupWalks;

    private ulong pickupGivenUpNodeId;

    private readonly IReadOnlyList<ZoneInfo> zones = zones;

    private readonly AutoFateSession session = session;

    private int zoneIndex = zones.Count == 0 ? 0 : Math.Clamp(startIndex, 0, zones.Count - 1);

    private readonly HashSet<uint> sessionStuckFateIds = new HashSet<uint>();

    private static readonly HashSet<uint> obstacleMapBlacklist = new HashSet<uint> { 1831u, 1832u, 1914u, 1915u };

    private const float InteractRangeMeters = 3f;

    private const float TeleportRetryProgressMeters = 3f;

    private const float TeleportShortcutMinSavingMeters = 300f;

    private const int MoveToFateWatchdogMs = 60000;

    private const float MoveToFateProgressMeters = 20f;

    private const int MoveToFateHardCapMs = 240000;

    private const int FlightReplanGroundGraceMs = 3000;

    private const float FlightReplanMinDistanceMeters = 40f;

    private const int MaxFlightReplans = 2;

    private static readonly FlightReplanPolicy flightReplanPolicy = new FlightReplanPolicy(3000, 40f, 2);

    private const int MoveOpUnwindSlackMs = 10000;

    private const int MoveProgressLogMs = 15000;

    private const int FollowUpWatchMs = 15000;

    private const int NpcSpawnTimeoutMs = 30000;

    private const int EngageStallTimeoutMs = 60000;

    private const int EngageOutOfCombatGraceMs = 30000;

    private const float EngageMeleeReachMeters = 4f;

    private const float EngageRangedReachMeters = 25f;

    private const int EngageIdleStallMs = 8000;

    private const int EngageRepositionWatchdogMs = 40000;

    private const float EngageMeleeApproachToleranceMeters = 2.5f;

    private const float EngageRangedApproachToleranceMeters = 15f;

    private const int MaxEngageRepositions = 3;

    private const int EngageReachSettleMs = 5000;

    private const float EngageMeleeRingGoalMeters = 3f;

    private const float EngageRingEdgeMarginMeters = 0.5f;

    private const float EngageChaseRepathMeters = 3f;

    private const float EngageChaseProgressMeters = 1f;

    private const float EngageCentreSeekMinMeters = 20f;

    private const float EngageCentreSeekToleranceMeters = 10f;

    private const int EngageCombatStallMs = 30000;

    private const int MaxCombatStallBounces = 3;

    private const int MaxEngageStallStrikes = 2;

    private const int CombatClearTimeoutMs = 30000;

    private const int RaiseWaitMs = 30000;

    private const int ReleaseTransitionWaitMs = 60000;

    private const int DefaultSwapWaitMs = 30000;

    private const int MaxSwapWaitSec = 300;

    private const int IdleScanIntervalMs = 1000;

    private const int MidPathRetargetIntervalMs = 1500;

    private const float RetargetDistanceMarginMeters = 15f;

    private const float RetargetNearArrivalLockMeters = 20f;

    private const int TeleportWatchdogMs = 60000;

    private const int ActivateMoveWatchdogMs = 60000;

    private const int ActivateApproachWatchdogMs = 20000;

    private const float ActivateApproachToleranceMeters = 2f;

    private const float ActivateMountMinMeters = 30f;

    private const int NpcInteractAttempts = 3;

    private const int InteractReadyTimeoutMs = 10000;

    private const int TargetSettleTimeoutMs = 500;

    private const int DialogOpenTimeoutMs = 2500;

    private const int NpcDialogTimeoutMs = 30000;

    private const int DialogClosedGraceMs = 2000;

    private const int InteractRetryDelayMs = 1000;

    private const int DialogDismissTimeoutMs = 3000;

    private const int NavmeshReadyWaitMs = 60000;

    private const int HeartbeatMs = 30000;

    private const int MaxConsecutiveStateErrors = 20;

    private const int GemstoneSettleTimeoutMs = 2500;

    private const int ObstacleMapGenTimeoutMs = 5000;

    private const int WrongZoneSwapAfterFailures = 2;

    private const int WrongZoneFaultAfterFailures = 3;

    internal const int NoProgressFaultMs = 300000;

    private uint? lastStuckFateId;

    private int consecutiveStuckRetries;

    private uint? lastTeleportedFateId;

    private int consecutiveZoneTeleportFailures;

    private uint? returnToFateId;

    private uint? followUpFateId;

    private long followUpWatchUntilMs;

    private int followUpWatchMs = FollowUpWatchMs;

    private long zoneIdleSinceMs;

    private int zoneIdleWaitMs = 30000;

    private uint? abandonedFateId;

    private uint? engageStallFateId;

    private int engageStallStrikes;

    private static readonly Random rng = new Random();

    private bool presetEnsured;

    private const uint ReviveCommandId = 200u;

    private const int ReviveParamReturn = 8;

    private const int ReviveParamAccept = 5;

    private const int ReturnReissueMs = 1500;

    private const int RevivePollMs = 250;

    private const int WeaknessSettleMs = 1000;

    private const int GemstoneSettlePollMs = 100;

    private GrindState lastObservedState;

    private long lastStateChangedAtMs;

    private long lastHeartbeatAtMs;

    private long noProgressSinceMs;

    private ZoneInfo zone => zones[zoneIndex];

    public int ZoneIndex => zoneIndex;

    protected override async Task Execute()
    {
        ErrorIf(zones.Count == 0, "No zones to grind.");
        ErrorIf(!BossModIPC.Instance.IsAvailable || !ExternalPlugins.IsInstalled(ExternalPlugin.BossMod), "BossMod (or BossMod Reborn) not installed or not loaded.");
        Svc.Chat.Print("[FTA] Starting " + zone.Name + "...");
        AutoCommon.Diag($"PC clock offset from the game server: {FateClock.LocalClockOffsetSeconds:+0;-0;0}s");
        lastStateChangedAtMs = Environment.TickCount64;
        try
        {
            await EnsureConsumables();
            await EnsureYokaiCompanion();
            await SyncSharedFateRanks();
            await RunStateMachine();
            Svc.Chat.Print("[FTA] " + zone.Name + ": zone done.");
        }
        catch (Exception ex)
        {
            DisableTextAdvance();
            if (Plugin.Cfg.AutoResumeOnFault && !CancelToken.IsCancellationRequested && !(ex is OperationCanceledException) && !(ex is UnrecoverableRunException))
            {
                session.EndedWithFault = true;
                session.FaultResumeZoneIndex = zoneIndex;
            }
            string message = ex.Message;
            int bracketIndex = message.LastIndexOf("] ");
            if (bracketIndex >= 0)
            {
                message = message.Substring(bracketIndex + 2);
            }
            Svc.Chat.PrintError("[FTA] " + zone.Name + " stopped: " + message);
            throw;
        }
        finally
        {
            DisableTextAdvance();
            BossModFateHelper.ReleaseChocobo();
            BossModMovementTuning.Release();
        }
    }

    private async Task RunStateMachine()
    {
        int consecutiveErrors = 0;
        while (!CancelToken.IsCancellationRequested)
        {
            GuardForwardProgress(lastObservedState);
            try
            {
                session.UpdateGemstones();
                TickSharedFateRefresh();
                GrindState state = ComputeState();
                if (state != GrindState.WaitingForFates)
                {
                    zoneIdleSinceMs = 0L;
                }
                if (state != lastObservedState)
                {
                    AutoCommon.Diag($"State {lastObservedState} -> {state}");
                    if (state != GrindState.WrongZone)
                    {
                        consecutiveZoneTeleportFailures = 0;
                    }
                    if (lastObservedState == GrindState.WaitingForCollectReward)
                    {
                        BossModIPC.Instance.ClearActive();
                    }
                    if (lastObservedState == GrindState.WaitingForFates)
                    {
                        EndIdleMountPeriod();
                    }
                    lastObservedState = state;
                    lastStateChangedAtMs = Environment.TickCount64;
                }
                if (Environment.TickCount64 - lastHeartbeatAtMs >= 30000)
                {
                    lastHeartbeatAtMs = Environment.TickCount64;
                    LogHeartbeat(state);
                }
                if (await RescueIfStuck(state))
                {
                    await NextFrame();
                    continue;
                }
                switch (state)
                {
                    case GrindState.AllDone:
                        Status = "Stop condition met";
                        AutoCommon.Diag("Stop condition met; exiting");
                        ReportGoalMet();
                        session.CompletedByStopCondition = true;
                        return;
                    case GrindState.SoftStop:
                        await FinishSoftStop();
                        return;
                    case GrindState.YokaiAdvance:
                        await HandOffToNextYokai();
                        return;
                    case GrindState.GoalZoneDone:
                        if (await LeaveDoneZone() == ExitReason.Quit)
                        {
                            return;
                        }
                        break;
                    case GrindState.Unconscious:
                        await Revive();
                        break;
                    case GrindState.WrongZone:
                        await GoToZone();
                        break;
                    case GrindState.SwapZone:
                        if (!AdvanceZone())
                        {
                            Status = "Waiting for FATEs in " + zone.Name + " (no other reachable zone)";
                            AutoCommon.Diag("No other reachable zone to rotate to; continuing to wait here");
                        }
                        break;
                    case GrindState.WaitingForFollowUp:
                        await TickFollowUpWait();
                        break;
                    case GrindState.WaitingForCollectReward:
                        await TickCollectRewardWait();
                        break;
                    case GrindState.WaitingForYokaiMinion:
                        await TickYokaiMinionWait();
                        break;
                    case GrindState.Settling:
                        await TickSettle();
                        break;
                    case GrindState.BetweenFates:
                        if (await MoveAndArrive() == ExitReason.Quit)
                        {
                            return;
                        }
                        break;
                    case GrindState.Engaging:
                        if (await EngageCurrentFate() == ExitReason.Quit)
                        {
                            return;
                        }
                        break;
                    case GrindState.WaitingForFates:
                        await TickIdleScan();
                        break;
                    case GrindState.Idle:
                    default:
                        await NextFrame(30);
                        break;
                }
                await NextFrame();
                consecutiveErrors = 0;
            }
            catch (Exception ex) when (!CancelToken.IsCancellationRequested && !(ex is UnrecoverableRunException))
            {
                consecutiveErrors++;
                AutoCommon.Diag($"State machine caught {ex.GetType().Name} (#{consecutiveErrors}/{20}): {ex.Message}");
                if (consecutiveErrors >= 20)
                {
                    AutoCommon.Diag("Too many consecutive state-machine errors; surfacing and stopping.");
                    throw;
                }
                await NextFrame(30);
            }
        }
    }

    private void LogHeartbeat(GrindState state)
    {
        Vector3? position = Svc.Objects.LocalPlayer?.Position;
        string posText = position is { } playerPos ? $"({playerPos.X:F0},{playerPos.Y:F0},{playerPos.Z:F0})" : "?";
        PublicEvent? currentFate = PublicEvent.CurrentFate;
        string fateText = ((currentFate == null) ? "none" : $"{currentFate.Id}@{currentFate.Progress}%/{currentFate.State}");
        long stateSeconds = (Environment.TickCount64 - lastStateChangedAtMs) / 1000;
        NavmeshIPC instance = NavmeshIPC.Instance;
        string navText = $"run={instance.IsRunning()} busy={instance.IsBusy()}";
        AutoCommon.Diag($"HEARTBEAT state={state} ({stateSeconds}s) terr={Svc.ClientState.TerritoryType} zone={zone.Name} pos={posText} fate={fateText} {navText} cond={AutoCommon.ConditionTag()} " + $"done={session.CompletedCount} ret={returnToFateId?.ToString() ?? "-"} followUp={followUpFateId?.ToString() ?? "-"} collectReward={(CollectRewardPending ? pendingRewardSpawn.FateId.ToString() : "-")} stuckBL={sessionStuckFateIds.Count}{SharedFateHeartbeat()}{StuckWatchHeartbeat()}");
        if (state != GrindState.Engaging && state != GrindState.WaitingForFates && state != GrindState.WaitingForCollectReward && state != GrindState.WaitingForYokaiMinion && stateSeconds >= 180)
        {
            AutoCommon.Diag($"STALL WARNING: state {state} held {stateSeconds}s — see prior heartbeats for context.");
        }
    }

    private void GuardForwardProgress(GrindState state)
    {
        string? faultText = stuckRescueFault;
        if (faultText != null)
        {
            ErrorIf(condition: true, faultText);
        }
        long tickCount = Environment.TickCount64;
        uint territoryType = Svc.ClientState.TerritoryType;
        Vector3 currentPos = Svc.Objects.LocalPlayer?.Position ?? noProgressPos;
        if (noProgressCompleted < 0 || session.CompletedCount != noProgressCompleted || territoryType != noProgressTerritory || Vector3.Distance(currentPos, noProgressPos) > 1.5f || state is GrindState.WaitingForYokaiMinion or GrindState.Settling or GrindState.Engaging or GrindState.WaitingForFates)
        {
            noProgressCompleted = session.CompletedCount;
            noProgressTerritory = territoryType;
            noProgressPos = currentPos;
            noProgressSinceMs = tickCount;
            return;
        }
        int limitMs = ((state == GrindState.BetweenFates) ? NoProgressFaultMsFor(Plugin.Cfg) : 300000);
        ErrorIf(tickCount - noProgressSinceMs >= limitMs, $"No forward progress for {limitMs / 60000}m in state {state}; surfacing fault for recovery.");
    }

    private GrindState ComputeState()
    {
        RefreshPendingCollectReward();
        if (abandonedFateId is { } abandonedId && !IsStillExcluded(abandonedId))
        {
            abandonedFateId = null;
        }
        if (IsPlayerKO())
        {
            PublicEvent? currentFate = PublicEvent.CurrentFate;
            if (currentFate != null && currentFate.Progress < 100 && !IsExcluded(currentFate))
            {
                returnToFateId = currentFate.Id;
            }
            followUpFateId = null;
            return GrindState.Unconscious;
        }
        if (session.StopWhenSafe && SoftStopReady())
        {
            return GrindState.SoftStop;
        }
        if (StopConditionMet())
        {
            return GrindState.AllDone;
        }
        if (YokaiTargetChanged())
        {
            return GrindState.YokaiAdvance;
        }
        if (!KeepingTwistOfFate() && GoalZoneDone())
        {
            return GrindState.GoalZoneDone;
        }
        if (Svc.ClientState.TerritoryType != zone.TerritoryId)
        {
            return GrindState.WrongZone;
        }
        PublicEvent? currentFate2 = PublicEvent.CurrentFate;
        if (currentFate2 != null && currentFate2.State == FateState.Running && currentFate2.Progress < 100 && abandonedFateId != currentFate2.Id)
        {
            LeaveIfExcluded(currentFate2);
        }
        bool collectDone = currentFate2 != null && currentFate2.Rule == PublicEvent.FateRule.Collect && currentFate2.Progress >= 100 && abandonedFateId != currentFate2.Id && currentFate2.State is not (FateState.Ended or FateState.Failed);
        if (collectDone)
        {
            TrackCollectReward(currentFate2!);
        }
        else if (currentFate2 != null && currentFate2.State == FateState.Running && abandonedFateId != currentFate2.Id)
        {
            if (currentFate2.Progress >= 100)
            {
                StartFollowUpWatch(currentFate2.Id);
            }
            else if (followUpFateId == currentFate2.Id)
            {
                followUpFateId = null;
            }
            return GrindState.Engaging;
        }
        if (ShouldWaitForFollowUp())
        {
            return GrindState.WaitingForFollowUp;
        }
        if (YokaiMinionMissing())
        {
            return GrindState.WaitingForYokaiMinion;
        }
        if (returnToFateId is { } returnId)
        {
            PublicEvent? fateById = PublicEvent.GetFateById(returnId);
            if (fateById != null && fateById.Progress < 100 && !IsStillExcluded(returnId))
            {
                return GrindState.BetweenFates;
            }
            returnToFateId = null;
        }
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return GrindState.Idle;
        }
        if (FateScanner.PickNext(Plugin.Cfg, localPlayer.Position, sessionStuckFateIds, returnToFateId) != null)
        {
            if (lastObservedState == GrindState.WaitingForFates)
            {
                BeginSettle("a FATE appeared");
            }
            if (!IsSettling())
            {
                return GrindState.BetweenFates;
            }
            return GrindState.Settling;
        }
        if (CollectRewardPending)
        {
            return GrindState.WaitingForCollectReward;
        }
        if (zoneIdleSinceMs == 0L)
        {
            zoneIdleSinceMs = Environment.TickCount64;
            zoneIdleWaitMs = Pacing.IdleWaitBeforeSwapMs(Math.Clamp(Plugin.Cfg.SwapZoneWaitSec, 0, 300) * 1000);
        }
        if (Plugin.Cfg.SwapZonesWhenEmpty && zones.Count > 1 && !KeepingTwistOfFate() && Environment.TickCount64 - zoneIdleSinceMs >= zoneIdleWaitMs)
        {
            return GrindState.SwapZone;
        }
        return GrindState.WaitingForFates;
    }

    private bool IsStillExcluded(uint fateId)
    {
        PublicEvent? fateById = PublicEvent.GetFateById(fateId);
        if (fateById != null)
        {
            return IsExcluded(fateById);
        }
        return false;
    }

    private bool IsExcluded(PublicEvent fate)
    {
        return FateScanner.IsExcluded(fate, Plugin.Cfg, sessionStuckFateIds);
    }

    private bool LeaveIfExcluded(PublicEvent fate)
    {
        FateExclusion fateExclusion = FateScanner.ExclusionFor(fate, Plugin.Cfg, sessionStuckFateIds);
        if (fateExclusion == FateExclusion.None)
        {
            return false;
        }
        AutoCommon.Diag($"FATE {fate.Id} ({fate.Name}) is {FateScanner.DescribeExclusion(fate, fateExclusion, Plugin.Cfg)}; leaving it for the next pick");
        LeaveFate(fate.Id);
        return true;
    }

    private int noProgressCompleted = -1;

    private async Task GoToZone()
    {
        Status = "Teleporting to " + zone.Name;
        AutoCommon.Diag($"Off-zone (in {Svc.ClientState.TerritoryType}), teleporting to {zone.TerritoryId}");
        TerritoryTeleportResult territoryTeleportResult = await AttemptTerritoryTeleport(zone.TerritoryId, zone.CentralLanding, "teleport-to-zone", 60000);
        if (territoryTeleportResult == TerritoryTeleportResult.Reached)
        {
            consecutiveZoneTeleportFailures = 0;
            session.UnreachableZoneIds.Remove(zone.TerritoryId);
            return;
        }
        if (CancelToken.IsCancellationRequested)
        {
            return;
        }
        consecutiveZoneTeleportFailures++;
        if (territoryTeleportResult == TerritoryTeleportResult.Blocked)
        {
            FaultIfCharacterStaysBlocked();
            return;
        }
        AutoCommon.Warn($"Could not reach {zone.Name} (failure {consecutiveZoneTeleportFailures}); escalating to keep the run moving.");
        if (consecutiveZoneTeleportFailures >= 2 && zones.Count > 1)
        {
            string name = zone.Name;
            bool newlyUnreachable = session.UnreachableZoneIds.Add(zone.TerritoryId);
            bool advanced = AdvanceZone();
            if (newlyUnreachable)
            {
                Svc.Chat.PrintError(advanced ? ("[FTA] Could not teleport to " + name + " (aetherytes attuned?); skipping it for the rest of this run.") : ("[FTA] Could not teleport to " + name + " (aetherytes attuned?) and no other selected zone is reachable."));
            }
            if (advanced)
            {
                consecutiveZoneTeleportFailures = 0;
                return;
            }
        }
        ErrorIf(consecutiveZoneTeleportFailures >= 3, $"Unable to reach {zone.Name} after {consecutiveZoneTeleportFailures} teleport attempts.");
    }

    private void FaultIfCharacterStaysBlocked()
    {
        string blockedDetail = AutoCommon.ConditionTag() + "; " + AutoCommon.DescribeRefusal(LastTeleportRefusal);
        AutoCommon.Warn($"No teleport cast toward {zone.Name} could start (failure {consecutiveZoneTeleportFailures}, {blockedDetail}); the character is held, so the zone stays reachable.");
        if (consecutiveZoneTeleportFailures < 3)
        {
            return;
        }
        throw new UnrecoverableRunException($"No teleport could start in {consecutiveZoneTeleportFailures} rounds (character state: {blockedDetail}). Close any open window or dialog, then start again.");
    }

    private bool AdvanceZone()
    {
        for (int i = 1; i < zones.Count; i++)
        {
            int candidateIndex = (zoneIndex + i) % zones.Count;
            if (!session.UnreachableZoneIds.Contains(zones[candidateIndex].TerritoryId) && !GoalZoneDoneAt(candidateIndex))
            {
                zoneIndex = candidateIndex;
                sessionStuckFateIds.Clear();
                retryAfterCompletionIds.Clear();
                lastStuckFateId = null;
                consecutiveStuckRetries = 0;
                lastTeleportedFateId = null;
                return true;
            }
        }
        return false;
    }

    private async Task TickIdleScan()
    {
        await EnsureConsumables();
        bool keepingTwist = KeepingTwistOfFate();
        bool swapping = Plugin.Cfg.SwapZonesWhenEmpty && zones.Count > 1 && !keepingTwist;
        TryIdleMount();
        long secondsLeft = Math.Max(0L, zoneIdleWaitMs - (Environment.TickCount64 - zoneIdleSinceMs)) / 1000;
        Status = (swapping ? $"Waiting for FATEs in {zone.Name} (swapping in {secondsLeft}s)" : (keepingTwist ? ("Waiting for FATEs in " + zone.Name + " (keeping Twist of Fate)") : ("Waiting for FATEs in " + zone.Name)));
        await DelayMs(1000);
    }

    private uint noProgressTerritory;

    private async Task EnsureConsumables()
    {
        Configuration cfg = Plugin.Cfg;
        if (!cfg.AutoConsume || cfg.AutoConsumeItems.Count == 0)
        {
            return;
        }
        if (Svc.Condition[ConditionFlag.InCombat])
        {
            return;
        }
        if (IsPlayerKO())
        {
            return;
        }
        if (!FoodOps.AnyNeeded(cfg))
        {
            return;
        }
        await SafeDismount("dismount-consume");
        if (Svc.Condition[ConditionFlag.Mounted] || Svc.Condition[ConditionFlag.InCombat])
        {
            return;
        }
        float minSeconds = (float)Math.Max(0, cfg.AutoConsumeMinMinutes) * 60f;
        foreach (ConsumableEntry entry in cfg.AutoConsumeItems)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            if (FoodOps.HasStatus(entry.StatusId, minSeconds) || !FoodOps.IsAvailable(entry))
            {
                continue;
            }
            Status = "Consuming " + entry.Name;
            AutoCommon.Diag($"Auto-consume: {entry.Name} (status {entry.StatusId} missing or under {cfg.AutoConsumeMinMinutes}m)");
            await WaitUntilTimed(() =>
            {
                if (FoodOps.HasStatus(entry.StatusId, minSeconds))
                {
                    return true;
                }
                if (Svc.Condition[ConditionFlag.InCombat])
                {
                    return true;
                }
                FoodOps.UseConsumable(entry);
                return false;
            }, 6000, $"consume-{entry.ItemId}", 100);
        }
    }

    private async Task TickFollowUpWait()
    {
        long remainingMs = Math.Max(0L, followUpWatchUntilMs - Environment.TickCount64);
        Status = $"Watching for follow-up FATE ({remainingMs / 1000 + 1}s)";
        await NextFrame(100);
    }
}
