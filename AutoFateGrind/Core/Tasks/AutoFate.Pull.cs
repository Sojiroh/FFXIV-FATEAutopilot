using System;
using System.Collections.Generic;
using AutoFateGrind.Core.Game.Fates;
using AutoFateGrind.Core.Ipc;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using clib.Utils;

namespace AutoFateGrind.Core.Tasks;

public sealed partial class AutoFate
{
    private uint? varietyCommittedId;

    private const string AutoTargetRetargetTrack = "Retarget";

    private const string AutoTargetRetargetNeverOption = "Never";

    private const string RotationTargetingTrack = "Targeting";

    private const string RotationTargetingManualOption = "Manual";

    private const int PullTagTimeoutMs = 8000;

    private const int PullSkipMs = 30000;

    private const int PullMaxTimeoutsPerTarget = 2;

    private const int PullScanIntervalMs = 1000;

    private const int PullActionRetryMs = 500;

    private const int PullSetupRetryMs = 60000;

    private static readonly Dictionary<uint, uint> TankPullActions = new Dictionary<uint, uint>
    {
        [1u] = 24u,
        [19u] = 24u,
        [3u] = 46u,
        [21u] = 46u,
        [32u] = 3624u,
        [37u] = 16143u
    };

    private static readonly Dictionary<uint, string> TankRotationModules = new Dictionary<uint, string>
    {
        [1u] = "BossMod.Autorotation.xan.PLD",
        [19u] = "BossMod.Autorotation.xan.PLD",
        [32u] = "BossMod.Autorotation.xan.DRK",
        [37u] = "BossMod.Autorotation.xan.GNB"
    };

    private static readonly string[] TankRotationModuleNames = new string[3] { "BossMod.Autorotation.xan.PLD", "BossMod.Autorotation.xan.DRK", "BossMod.Autorotation.xan.GNB" };

    private readonly Dictionary<ulong, long> pullSkippedUntilMs = new Dictionary<ulong, long>();

    private readonly Dictionary<ulong, int> pullTimeouts = new Dictionary<ulong, int>();

    private readonly List<ulong> pullExpiredSkips = new List<ulong>();

    private bool pullOverridesApplied;

    private bool pullActive;

    private bool pullHpLow;

    private string? pullRotationModule;

    private ulong pullTargetId;

    private long pullTargetSinceMs;

    private long pullNextScanMs;

    private long pullNextActionMs;

    private int pullTagged;

    private bool pullSetupFailureLogged;

    private ulong pullStatusTargetId;

    private int pullStatusOnPlayer = -1;

    private bool TickPullTarget(uint fateId, IPlayerCharacter player, long now)
    {
        switch (FateMobScanner.PullTagState(fateId, pullTargetId))
        {
            case FatePullTagState.OnPlayer:
                pullTagged++;
                pullTargetId = 0uL;
                return false;
            case FatePullTagState.Gone:
                pullTargetId = 0uL;
                return false;
            default:
                if (now - pullTargetSinceMs > 8000)
                {
                    int timeouts = pullTimeouts.GetValueOrDefault(pullTargetId) + 1;
                    pullTimeouts[pullTargetId] = timeouts;
                    if (timeouts >= 2)
                    {
                        AutoCommon.Diag($"Pull target {pullTargetId:X} of FATE {fateId} still untagged after {8}s again; skipping it for the rest of the FATE");
                        pullSkippedUntilMs[pullTargetId] = long.MaxValue;
                    }
                    else
                    {
                        AutoCommon.Diag($"Pull target {pullTargetId:X} of FATE {fateId} still untagged after {8}s; skipping it for {30}s");
                        pullSkippedUntilMs[pullTargetId] = now + 30000;
                    }
                    pullTargetId = 0uL;
                    return false;
                }
                if (FateMobScanner.TryGetPullTarget(fateId, pullTargetId, out IBattleNpc npc))
                {
                    if (Svc.Targets.Target?.GameObjectId != pullTargetId)
                    {
                        Svc.Targets.Target = npc;
                    }
                    FirePullAction(player, npc, now);
                }
                return true;
        }
    }

    private unsafe void FirePullAction(IPlayerCharacter player, IGameObject target, long now)
    {
        if (now < pullNextActionMs || !TankPullActions.TryGetValue(player.ClassJob.RowId, out var actionId))
        {
            return;
        }
        ActionManager* actionManager = ActionManager.Instance();
        if (actionManager == null)
        {
            return;
        }
        if (ActionManager.GetActionInRangeOrLoS(actionId, (GameObject*)player.Address, (GameObject*)target.Address) != 0)
        {
            return;
        }
        if (actionManager->GetActionStatus(ActionType.Action, actionId, target.GameObjectId, checkRecastActive: true, checkCastingActive: true, null) != 0)
        {
            return;
        }
        pullNextActionMs = now + 500;
        actionManager->UseAction(ActionType.Action, actionId, target.GameObjectId, 0u, ActionManager.UseActionMode.None, 0u, null);
    }

    private bool BeginPull(uint fateId, string preset)
    {
        BossModIPC instance = BossModIPC.Instance;
        if (!instance.AddTransientStrategy(preset, "BossMod.Autorotation.MiscAI.AutoTarget", "Retarget", "Never"))
        {
            if (!pullSetupFailureLogged)
            {
                pullSetupFailureLogged = true;
                AutoCommon.Diag("Pulling more mobs is on, but BossMod would not take a Retarget override on '" + preset + "' (no AutoTarget module in the preset?); leaving targeting to BossMod");
            }
            return false;
        }
        pullRotationModule = null;
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer != null && TankRotationModules.TryGetValue(localPlayer.ClassJob.RowId, out string? rotationModule) && instance.AddTransientStrategy(preset, rotationModule, "Targeting", "Manual"))
        {
            pullRotationModule = rotationModule;
        }
        pullOverridesApplied = true;
        pullActive = true;
        pullTagged = 0;
        pullTargetId = 0uL;
        AutoCommon.Diag($"Pulling more mobs in FATE {fateId}: FTA picks targets, BossMod keeps the rotation (rotation targeting override: {pullRotationModule ?? "none"})");
        return true;
    }

    private void EndPull(string preset, uint fateId, string? reason)
    {
        if (!pullActive)
        {
            return;
        }
        pullActive = false;
        BossModIPC instance = BossModIPC.Instance;
        instance.ClearTransientStrategy(preset, "BossMod.Autorotation.MiscAI.AutoTarget", "Retarget");
        if (pullRotationModule != null)
        {
            instance.ClearTransientStrategy(preset, pullRotationModule, "Targeting");
            pullRotationModule = null;
        }
        if (pullTargetId != 0L)
        {
            FateMobScanner.DropUnpulledTarget(fateId);
            pullTargetId = 0uL;
        }
        pullNextScanMs = Environment.TickCount64 + 1000;
        pullStatusTargetId = 0uL;
        pullStatusOnPlayer = -1;
        pullStatusCandidates = -1;
        if (reason != null)
        {
            AutoCommon.Diag($"Pull in FATE {fateId} done after tagging {pullTagged}: {reason}; BossMod retargets and fights the pack");
            PublicEvent? fateById = PublicEvent.GetFateById(fateId);
            if (fateById != null)
            {
                Status = "Engaging " + fateById.Name;
            }
        }
    }

    private void PruneSkippedPullTargets(long now)
    {
        if (pullSkippedUntilMs.Count == 0)
        {
            return;
        }
        pullExpiredSkips.Clear();
        foreach (var (expiredId, skipUntilMs) in pullSkippedUntilMs)
        {
            if (skipUntilMs <= now)
            {
                pullExpiredSkips.Add(expiredId);
            }
        }
        for (int i = 0; i < pullExpiredSkips.Count; i++)
        {
            pullSkippedUntilMs.Remove(pullExpiredSkips[i]);
        }
    }

    private void ResetPullOverrides(string preset)
    {
        pullActive = false;
        pullHpLow = false;
        pullRotationModule = null;
        pullTargetId = 0uL;
        pullNextScanMs = 0L;
        pullNextActionMs = 0L;
        pullTagged = 0;
        pullStatusTargetId = 0uL;
        pullStatusOnPlayer = -1;
        pullStatusCandidates = -1;
        pullSkippedUntilMs.Clear();
        pullTimeouts.Clear();
        BossModIPC instance = BossModIPC.Instance;
        if (!((Plugin.Cfg.PullAsManyAsPossible || pullOverridesApplied) && instance.CanClearTransientStrategy))
        {
            return;
        }
        pullOverridesApplied = false;
        instance.ClearTransientStrategy(preset, "BossMod.Autorotation.MiscAI.AutoTarget", "Retarget");
        string[] tankRotationModuleNames = TankRotationModuleNames;
        foreach (string module in tankRotationModuleNames)
        {
            instance.ClearTransientStrategy(preset, module, "Targeting");
        }
    }
}
