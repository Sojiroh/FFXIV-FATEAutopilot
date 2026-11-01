using System;
using System.Numerics;
using System.Threading.Tasks;
using AutoFateGrind.Core.Game.Fates;
using AutoFateGrind.Core.Game.Ops;
using AutoFateGrind.Core.Ipc;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using clib.Extensions;
using clib.TaskSystem;
using clib.Utils;

namespace AutoFateGrind.Core.Tasks;

public sealed partial class AutoFate
{
    private readonly record struct FateSpawnKey(uint FateId, int StartEpoch);

    private enum GrindState
    {
        Idle,
        WrongZone,
        SwapZone,
        AllDone,
        SoftStop,
        YokaiAdvance,
        GoalZoneDone,
        Unconscious,
        WaitingForFollowUp,
        WaitingForCollectReward,
        WaitingForYokaiMinion,
        Settling,
        BetweenFates,
        Engaging,
        WaitingForFates
    }

    private enum ExitReason
    {
        Continue,
        Quit
    }

    private sealed class EngageIdleTracker(float reachMeters)
    {
        private Vector3 anchor;

        private bool anchored;

        private long idleSinceMs;

        private long inReachSinceMs;

        public float Meters { get; } = reachMeters;

        public int Repositions { get; private set; }

        public bool Stalled(Vector3 position)
        {
            long tickCount = Environment.TickCount64;
            if (!anchored || Vector3.Distance(anchor, position) > 1.5f)
            {
                anchored = true;
                anchor = position;
                idleSinceMs = tickCount;
                return false;
            }
            return tickCount - idleSinceMs >= 8000;
        }

        public void CountReposition()
        {
            Repositions++;
        }

        public void MarkInReach()
        {
            long tickCount = Environment.TickCount64;
            if (inReachSinceMs == 0L)
            {
                inReachSinceMs = tickCount;
            }
            else if (tickCount - inReachSinceMs >= 5000)
            {
                Repositions = 0;
            }
            Restart();
        }

        public void MarkOutOfReach()
        {
            inReachSinceMs = 0L;
        }

        public void Restart()
        {
            anchored = false;
        }
    }

    private enum MoveStopReason
    {
        None,
        StuckRetry,
        StuckTeleport,
        StuckInCombat,
        HigherPriority,
        NpcSpawned,
        FateInvalid,
        LeftZone,
        CombatDropped
    }

    private const string AutoTargetModule = "BossMod.Autorotation.MiscAI.AutoTarget";

    private const string AutoTargetGeneralTrack = "General";

    private const string AutoTargetPassiveOption = "Passive";

    private const string AutoTargetCollectFateTrack = "CollectFATE";

    private const string AutoTargetEnabledOption = "Enabled";

    private const int HandInWalkWatchdogMs = 40000;

    private const int HandInCombatClearMs = 30000;

    private const int HandInDialogTimeoutMs = 20000;

    private const int HandInRetryBackoffMs = 8000;

    private const int HandInNpcMissingBackoffMs = 3000;

    private const int HandInRequestFillGraceMs = 2000;

    private const int MaxHandInFailuresPerFate = 3;

    private const int CollectRewardWatchMinMs = 90000;

    private const int CollectRewardWatchMaxMs = 240000;

    private const int CollectRewardWatchSlackMs = 15000;

    private const float PickupHoldRangeMeters = 30f;

    private const int PickupHoldTimeoutMs = 30000;

    private const int PickupHoldCooldownMs = 20000;

    private const int PickupWedgeMs = 5000;

    private const float PickupWedgeProgressMeters = 1f;

    private const int MaxPickupWalksPerNode = 2;

    private const int FateHelperHandInItems = 10;

    private FateSpawnKey lastCompletedSpawn;

    private FateSpawnKey handInSpawn;

    private FateSpawnKey pendingRewardSpawn;

    private string pendingRewardName = "";

    private long pendingRewardSinceMs;

    private long pendingRewardDeadlineMs;

    private bool afgHandInOwner;

    private int handInFailures;

    private long handInNextAttemptMs;

    private bool handInNpcMissingLogged;

    private bool collectPullsHeld;

    private bool presetHasFateHelper;

    private long pickupHoldSinceMs;

    private int pickupHoldHeldCount;

    private long pickupHoldSuppressedUntilMs;

    private ulong pickupWedgeNodeId;

    private void BeginCollectFate(FateSpawnKey spawn, string fateName)
    {
        EnableTextAdvanceForCollect();
        presetHasFateHelper = BossModIPC.Instance.GetPreset(Plugin.Cfg.CombatPresetName)?.Contains("BossMod.Autorotation.MiscAI.FateUtils", StringComparison.Ordinal) ?? false;
        if (handInSpawn == spawn)
        {
            return;
        }
        handInSpawn = spawn;
        handInFailures = 0;
        handInNextAttemptMs = 0L;
        handInNpcMissingLogged = false;
        pickupHoldSinceMs = 0L;
        pickupHoldSuppressedUntilMs = 0L;
        ResetPickupWedge();
        pickupGivenUpNodeId = 0uL;
        afgHandInOwner = Plugin.Cfg.CollectHandInEnabled;
        AutoCommon.Diag(afgHandInOwner ? $"Collect FATE {spawn.FateId} ({fateName}): FTA hands in every {HandInBatch} of item {FateItems.TurnInItemId(spawn.FateId)}; BossMod's own 10-item hand-in stays as the backstop" : $"Collect FATE {spawn.FateId} ({fateName}): hand-ins left to BossMod's FATE helper (FTA hand-in is off in settings)");
    }

    private void UpdateCollectPullHold(uint fateId, string preset)
    {
        string? holdReason = CollectPullHoldReason(fateId);
        if (holdReason == null)
        {
            ReleaseCollectPullHold(preset);
            return;
        }
        if (collectPullsHeld)
        {
            return;
        }
        if (HoldCollectPulls(fateId, preset))
        {
            AutoCommon.Diag($"Collect FATE {fateId}: {holdReason}; BossMod finishes the mobs already on the character and pulls no new ones");
        }
    }

    private bool HoldCollectPulls(uint fateId, string preset)
    {
        if (!collectPullsHeld)
        {
            if (!BossModIPC.Instance.CanClearTransientStrategy)
            {
                return false;
            }
            collectPullsHeld = BossModIPC.Instance.AddTransientStrategy(preset, "BossMod.Autorotation.MiscAI.AutoTarget", "CollectFATE", "Enabled");
        }
        if (collectPullsHeld)
        {
            FateMobScanner.DropUnpulledTarget(fateId);
        }
        return collectPullsHeld;
    }

    private void ReleaseCollectPullHold(string preset)
    {
        if (!collectPullsHeld)
        {
            return;
        }
        collectPullsHeld = false;
        BossModIPC.Instance.ClearTransientStrategy(preset, "BossMod.Autorotation.MiscAI.AutoTarget", "CollectFATE");
    }

    private string? CollectPullHoldReason(uint fateId)
    {
        uint turnInItemId = FateItems.TurnInItemId(fateId);
        if (turnInItemId == 0)
        {
            return null;
        }
        int heldCount = FateItems.HeldCount(turnInItemId);
        if (afgHandInOwner && heldCount >= HandInBatch && ResolveObjectiveNpc(fateId) != null)
        {
            return $"holding {heldCount} item(s) for the hand-in";
        }
        if (!PickupHoldAllowed(fateId, heldCount))
        {
            return null;
        }
        return "a FATE item lies on the ground nearby";
    }

    private bool PickupHoldAllowed(uint fateId, int held)
    {
        long tickCount = Environment.TickCount64;
        if (!(presetHasFateHelper && tickCount >= pickupHoldSuppressedUntilMs && Svc.Objects.LocalPlayer is { } localPlayer && FateMobScanner.HasPickupWithin(fateId, localPlayer.Position, 30f)))
        {
            pickupHoldSinceMs = 0L;
            return false;
        }
        if (pickupHoldSinceMs == 0L || held != pickupHoldHeldCount || Svc.Condition[ConditionFlag.InCombat])
        {
            pickupHoldSinceMs = tickCount;
            pickupHoldHeldCount = held;
            return true;
        }
        if (tickCount - pickupHoldSinceMs < 30000)
        {
            return true;
        }
        pickupHoldSinceMs = 0L;
        pickupHoldSuppressedUntilMs = tickCount + 20000;
        AutoCommon.Diag($"Collect FATE {fateId}: BossMod picked nothing up in {30}s of holding pulls for a nearby item; pulling again for {20}s");
        return false;
    }

    private async Task<bool> TickPickupWedge(uint fateId, string fateName, string preset)
    {
        if (!PickupWedged(fateId, out var pickupId, out var distance))
        {
            return false;
        }
        Status = "Walking to a " + fateName + " item";
        AutoCommon.Diag($"Collect FATE {fateId} ({fateName}): pickup {pickupId:X} {distance:F0}m away has not come closer in {5}s (BossMod's FATE helper pushes straight at it through walls); walking there with vnav");
        EndRingChase(preset);
        bool parkedPickup = BossModIPC.Instance.AddTransientStrategy(preset, "BossMod.Autorotation.MiscAI.FateUtils", "Collect", "Disabled");
        try
        {
            if (FateMobScanner.TryGetPickup(fateId, pickupId, out IGameObject pickup))
            {
                Vector3 dest = FateGround.Project(pickup.Position) ?? pickup.Position;
                await WalkWithBossModParked(dest, MovementConfig.InteractRange, () => PickupWalkSettled(fateId, pickupId), $"collect-pickup-walk-{fateId}");
            }
        }
        finally
        {
            if (parkedPickup)
            {
                BossModIPC.Instance.ClearTransientStrategy(preset, "BossMod.Autorotation.MiscAI.FateUtils", "Collect");
            }
        }
        bool pickupReachable = !FateMobScanner.TryGetPickup(fateId, pickupId, out IGameObject pickup2) || pickup2.IsInInteractRange();
        ResetPickupWedge();
        if (pickupReachable)
        {
            pickupWalks = 0;
            return true;
        }
        pickupWalks++;
        if (pickupWalks >= 2)
        {
            pickupGivenUpNodeId = pickupId;
            pickupWalks = 0;
            AutoCommon.Diag($"Collect FATE {fateId}: could not reach pickup {pickupId:X} after {2} walks; leaving it to BossMod");
        }
        return true;
    }

    private bool PickupWedged(uint fateId, out ulong pickupId, out float distance)
    {
        pickupId = 0uL;
        distance = 0f;
        if (!(presetHasFateHelper && !Svc.Condition[ConditionFlag.InCombat] && !Svc.Condition[ConditionFlag.Mounted] && !StuckDetector.IsPositionFrozenLegit() && Svc.Objects.LocalPlayer is { } localPlayer && FateItems.HeldCount(FateItems.TurnInItemId(fateId)) < 10 && FateMobScanner.TryFindNearestPickup(fateId, localPlayer.Position, out IGameObject pickup, out distance) && !pickup.IsInInteractRange()))
        {
            ResetPickupWedge();
            return false;
        }
        pickupId = pickup.GameObjectId;
        if (pickupId == pickupGivenUpNodeId)
        {
            return false;
        }
        long tickCount = Environment.TickCount64;
        if (pickupId != pickupWedgeNodeId || distance < pickupWedgeBestMeters - 1f)
        {
            pickupWedgeNodeId = pickupId;
            pickupWedgeBestMeters = distance;
            pickupWedgeSinceMs = tickCount;
            return false;
        }
        return tickCount - pickupWedgeSinceMs >= 5000;
    }

    private static bool PickupWalkSettled(uint fateId, ulong pickupId)
    {
        if (FateAlive(fateId) && !Svc.Condition[ConditionFlag.InCombat] && FateMobScanner.TryGetPickup(fateId, pickupId, out IGameObject pickup))
        {
            return pickup.IsInInteractRange();
        }
        return true;
    }

    private void ResetPickupWedge()
    {
        pickupWedgeNodeId = 0uL;
        pickupWedgeSinceMs = 0L;
        pickupWedgeBestMeters = float.MaxValue;
    }

    private static bool FateAlive(uint fateId)
    {
        PublicEvent? fateById = PublicEvent.GetFateById(fateId);
        return fateById != null && fateById.State is not (FateState.Ended or FateState.Failed);
    }

    private static IGameObject? ResolveObjectiveNpc(uint fateId)
    {
        PublicEvent? fateById = PublicEvent.GetFateById(fateId);
        if (fateById == null)
        {
            return null;
        }
        IGameObject? objectiveNpc = fateById.ObjectiveNpc;
        if (objectiveNpc == null || !objectiveNpc.IsTargetable)
        {
            return null;
        }
        return objectiveNpc;
    }

    private async Task<bool> MaybeHandInCollectItems(uint fateId, string fateName, string preset)
    {
        if (!afgHandInOwner || Environment.TickCount64 < handInNextAttemptMs || Svc.Condition[ConditionFlag.InCombat])
        {
            return false;
        }
        uint turnInItemId = FateItems.TurnInItemId(fateId);
        int heldCount = FateItems.HeldCount(turnInItemId);
        if (turnInItemId == 0 || heldCount < HandInBatch)
        {
            return false;
        }
        return await HandInHeldItems(fateId, fateName, preset, turnInItemId, heldCount);
    }

    private async Task HandInLeftovers(uint fateId, string fateName, string preset)
    {
        if (!afgHandInOwner)
        {
            return;
        }
        uint turnInItemId = FateItems.TurnInItemId(fateId);
        int heldCount = FateItems.HeldCount(turnInItemId);
        if (turnInItemId == 0 || heldCount <= 0)
        {
            return;
        }
        AutoCommon.Diag($"FATE {fateId} ({fateName}) is at 100% with {heldCount} item(s) still held; turning them in during the hand-in window");
        await HandInHeldItems(fateId, fateName, preset, turnInItemId, heldCount);
    }

    private async Task<bool> HandInHeldItems(uint fateId, string fateName, string preset, uint itemId, int held)
    {
        if (ResolveObjectiveNpc(fateId) == null)
        {
            if (!handInNpcMissingLogged)
            {
                handInNpcMissingLogged = true;
                AutoCommon.Diag($"FATE {fateId} ({fateName}): holding {held} item(s) but its hand-in NPC is not loaded or targetable yet; retrying");
            }
            handInNextAttemptMs = Environment.TickCount64 + 3000;
            return false;
        }
        handInNpcMissingLogged = false;
        if (await TryHandIn(fateId, fateName, preset, itemId, held))
        {
            handInFailures = 0;
            return true;
        }
        if (CancelToken.IsCancellationRequested || !FateAlive(fateId))
        {
            return true;
        }
        handInFailures++;
        handInNextAttemptMs = Environment.TickCount64 + 8000;
        if (handInFailures < 3)
        {
            AutoCommon.Diag($"Hand-in for FATE {fateId} did not go through (attempt {handInFailures}/{3}); retrying in {8}s");
            return true;
        }
        afgHandInOwner = false;
        AutoCommon.Diag($"Hand-in for FATE {fateId} failed {handInFailures} times; leaving the rest of this FATE's turn-ins to BossMod's 10-item hand-in");
        return true;
    }

    private async Task<bool> TryHandIn(uint fateId, string fateName, string preset, uint itemId, int held)
    {
        Status = $"Handing in {held} item(s) for {fateName}";
        AutoCommon.Diag($"FATE {fateId} ({fateName}): walking {held} item(s) to the hand-in NPC");
        EndRingChase(preset);
        bool parkedMovement = ParkBossModMovement(preset);
        BossModIPC.Instance.AddTransientStrategy(preset, "BossMod.Autorotation.MiscAI.AutoTarget", "General", "Passive");
        bool parkedPickup = BossModIPC.Instance.AddTransientStrategy(preset, "BossMod.Autorotation.MiscAI.FateUtils", "Collect", "Disabled");
        try
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                if (CancelToken.IsCancellationRequested || !FateAlive(fateId))
                {
                    return false;
                }
                IGameObject? npc = ResolveObjectiveNpc(fateId);
                if (npc == null)
                {
                    return false;
                }
                if (!(await ApproachHandInNpc(fateId, npc, attempt)))
                {
                    continue;
                }
                if (Svc.Condition[ConditionFlag.InCombat] && !(await FightFreeAtHandInNpc(fateId, preset, parkedMovement)))
                {
                    return false;
                }
                if (!(await ReadyToHandIn(fateId)))
                {
                    continue;
                }
                if (!(await OpenNpcDialog(fateId, npc, attempt, () => !FateAlive(fateId), "handin")))
                {
                    continue;
                }
                if (await DriveHandInDialog(fateId, fateName, itemId, held))
                {
                    return true;
                }
            }
            return false;
        }
        finally
        {
            if (parkedPickup)
            {
                BossModIPC.Instance.ClearTransientStrategy(preset, "BossMod.Autorotation.MiscAI.FateUtils", "Collect");
            }
            BossModIPC.Instance.ClearTransientStrategy(preset, "BossMod.Autorotation.MiscAI.AutoTarget", "General");
            if (parkedMovement)
            {
                ResumeBossModMovement(preset);
            }
        }
    }

    private async Task<bool> ApproachHandInNpc(uint fateId, IGameObject npc, int attempt)
    {
        if (npc.IsInInteractRange())
        {
            return true;
        }
        Vector3 npcPos = npc.Position;
        string scope = $"handin-walk-{fateId}#{attempt}";
        MoveOp walk = new MoveOp((MoveOp o) => o.MoveInZone(npcPos, MovementConfig.InteractRange, () => npc.IsInInteractRange() || !FateAlive(fateId)));
        await RunCancellable(walk, 40000, scope, StuckDetector.MoveStallAbort(scope));
        Exception? fault = walk.Fault;
        if (fault != null)
        {
            AutoCommon.Diag(scope + " faulted: " + fault.Message);
        }
        if (npc.IsInInteractRange())
        {
            return true;
        }
        AutoCommon.Diag($"Could not reach the hand-in NPC for FATE {fateId} (attempt {attempt}/{3})");
        return false;
    }

    private async Task<bool> ReadyToHandIn(uint fateId)
    {
        await SafeDismount($"dismount-handin-{fateId}");
        if (await WaitUntilTimed(NpcInteraction.PlayerReady, 10000, $"handin-ready-{fateId}", 2))
        {
            return true;
        }
        AutoCommon.Diag($"Cannot talk to the hand-in NPC for FATE {fateId} yet ({NpcInteraction.DescribeBlockers()}); retrying");
        return false;
    }

    private async Task<bool> FightFreeAtHandInNpc(uint fateId, string preset, bool movementParked)
    {
        Status = "Clearing aggro before handing in";
        HoldCollectPulls(fateId, preset);
        BossModIPC.Instance.ClearTransientStrategy(preset, "BossMod.Autorotation.MiscAI.AutoTarget", "General");
        if (movementParked)
        {
            ResumeBossModMovement(preset);
        }
        long deadline = Environment.TickCount64 + 30000;
        try
        {
            while (Environment.TickCount64 < deadline)
            {
                if (CancelToken.IsCancellationRequested || IsPlayerKO() || !FateAlive(fateId))
                {
                    return false;
                }
                if (!Svc.Condition[ConditionFlag.InCombat])
                {
                    return true;
                }
                AssertPresetActive(preset);
                await NextFrame(30);
            }
            AutoCommon.Diag($"Still in combat at the hand-in NPC for FATE {fateId} after {30}s; ending this trip and leaving the fight to BossMod");
            return false;
        }
        finally
        {
            BossModIPC.Instance.AddTransientStrategy(preset, "BossMod.Autorotation.MiscAI.AutoTarget", "General", "Passive");
            if (movementParked)
            {
                ParkBossModMovement(preset);
            }
        }
    }

    private async Task<bool> DriveHandInDialog(uint fateId, string fateName, uint itemId, int heldBefore)
    {
        Status = "Handing in items for " + fateName;
        long deadline = Environment.TickCount64 + 20000;
        long lastDialogSeenMs = Environment.TickCount64;
        long requestSeenAtMs = 0L;
        bool handedIn = false;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return handedIn;
            }
            long tickCount = Environment.TickCount64;
            int heldNow = FateItems.HeldCount(itemId);
            if (!handedIn && heldNow < heldBefore)
            {
                handedIn = true;
                AutoCommon.Diag($"Handed in {heldBefore - heldNow} item(s) for FATE {fateId} ({fateName})");
            }
            if (!NpcInteraction.RequestDialogOpen())
            {
                requestSeenAtMs = 0L;
            }
            else if (requestSeenAtMs == 0L)
            {
                requestSeenAtMs = tickCount;
            }
            bool dialogActive = NpcInteraction.DriveRequestDialog(!textAdvanceArmed || (requestSeenAtMs != 0L && tickCount - requestSeenAtMs >= 2000)) || NpcInteraction.DriveDialog() || NpcInteraction.DialogOpen();
            if (dialogActive)
            {
                lastDialogSeenMs = tickCount;
            }
            else
            {
                if (handedIn)
                {
                    return true;
                }
                if (tickCount - lastDialogSeenMs > 2000)
                {
                    AutoCommon.Diag($"Hand-in dialog for FATE {fateId} closed without taking the items; retrying");
                    return false;
                }
            }
            await NextFrame(2);
        }
        AutoCommon.Diag($"Hand-in dialog for FATE {fateId} did not finish within {20}s (handed in: {handedIn})");
        await DismissLingeringDialog();
        return handedIn;
    }

    private bool CollectRewardPending => pendingRewardSpawn != default(FateSpawnKey);

    private async Task WrapUpCollectFate(uint fateId, string fateName, string preset)
    {
        await HandInLeftovers(fateId, fateName, preset);
        PublicEvent? fateById = PublicEvent.GetFateById(fateId);
        if (fateById != null && fateById.State is not (FateState.Ended or FateState.Failed))
        {
            TrackCollectReward(fateById);
        }
    }

    private void TrackCollectReward(PublicEvent live)
    {
        FateSpawnKey fateSpawnKey = new FateSpawnKey(live.Id, live.StartTimeEpoch);
        if (pendingRewardSpawn == fateSpawnKey)
        {
            return;
        }
        string name = live.Name;
        long tickCount = Environment.TickCount64;
        long remainingMs = (long)(Math.Max(0f, FateClock.Remaining(live)) * 1000f);
        long holdMs = Math.Clamp(remainingMs + 15000, 90000L, 240000L);
        pendingRewardSpawn = fateSpawnKey;
        pendingRewardName = name;
        pendingRewardSinceMs = tickCount;
        pendingRewardDeadlineMs = tickCount + holdMs;
        AutoCommon.Diag($"Collect FATE {live.Id} ({name}) is at 100% with {FateItems.HandedInCount(live)} item(s) handed in; moving on inside {zone.Name} and collecting the reward when its row clears (FATE timer {remainingMs / 1000}s, zone held for up to {holdMs / 1000}s)");
    }

    private void RefreshPendingCollectReward()
    {
        if (!CollectRewardPending)
        {
            return;
        }
        uint fateId = pendingRewardSpawn.FateId;
        long elapsedSec = (Environment.TickCount64 - pendingRewardSinceMs) / 1000;
        PublicEvent? fateById = PublicEvent.GetFateById(fateId);
        string? clearReason = fateById switch
        {
            null => "row cleared",
            _ => fateById.StartTimeEpoch != pendingRewardSpawn.StartEpoch ? "respawned" : fateById.State is FateState.Ended or FateState.Failed ? fateById.State.ToString() : null,
        };
        if (clearReason != null)
        {
            session.UpdateGemstones();
            session.UpdateExp();
            AutoCommon.Diag($"Collect FATE {fateId} ({pendingRewardName}) {clearReason} {elapsedSec}s after leaving it; reward settled (wallet {session.GemstoneCurrent}g)");
            ClearPendingCollectReward();
        }
        else if (Svc.ClientState.TerritoryType != zone.TerritoryId)
        {
            AutoCommon.Diag($"Left {zone.Name} with Collect FATE {fateId} ({pendingRewardName}) still open; its reward is forfeit");
            ClearPendingCollectReward();
        }
        else if (Environment.TickCount64 >= pendingRewardDeadlineMs)
        {
            AutoCommon.Diag($"Collect FATE {fateId} ({pendingRewardName}) row still up {elapsedSec}s after leaving it; no longer holding {zone.Name} for it");
            ClearPendingCollectReward();
        }
    }

    private void ClearPendingCollectReward()
    {
        pendingRewardSpawn = default;
        pendingRewardName = "";
    }

    private async Task TickCollectRewardWait()
    {
        long secondsLeft = Math.Max(0L, pendingRewardDeadlineMs - Environment.TickCount64) / 1000 + 1;
        Status = $"Waiting for {pendingRewardName} rewards ({secondsLeft}s)";
        if (Svc.Condition[ConditionFlag.InCombat])
        {
            string combatPresetName = Plugin.Cfg.CombatPresetName;
            EnsureCombatPreset(combatPresetName);
            AssertPresetActive(combatPresetName);
        }
        await NextFrame(60);
    }

    private async Task HoldForCollectReward()
    {
        if (!CollectRewardPending)
        {
            return;
        }
        AutoCommon.Diag($"Hand-off queued while Collect FATE {pendingRewardSpawn.FateId} ({pendingRewardName}) still owes its reward; holding in {zone.Name} until it lands");
        try
        {
            while (!CancelToken.IsCancellationRequested && !IsPlayerKO())
            {
                RefreshPendingCollectReward();
                if (!CollectRewardPending)
                {
                    return;
                }
                await TickCollectRewardWait();
            }
        }
        finally
        {
            BossModIPC.Instance.ClearActive();
        }
    }
}
