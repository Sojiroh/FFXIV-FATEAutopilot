using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using AutoFateGrind.Core.Game.Fates;
using AutoFateGrind.Core.Ipc;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using clib.Utils;

namespace AutoFateGrind.Core.Tasks;

public sealed partial class AutoFate
{
    private const float ShortHopWalkMeters = 30f;

    private const byte RoleTank = 1;

    private const byte RoleMelee = 2;

    private const byte RoleHealer = 4;

    private const int TankMaxTargets = 0;

    private const int HealerMaxTargets = 5;

    private const int DefaultMaxTargets = 3;

    private static bool textAdvanceArmed;

    private const string TextAdvanceScope = "FateAutopilot";

    private const int SettleTickMs = 250;

    private long settleUntilMs;

    private readonly List<PublicEvent> varietyPool = new List<PublicEvent>();

    private readonly HashSet<uint> varietyConsideredIds = new HashSet<uint>();

    private void BeginSettle(string reason)
    {
        int delayMs = Pacing.ReactionDelayMs();
        if (delayMs <= 0)
        {
            return;
        }
        settleUntilMs = Environment.TickCount64 + delayMs;
        AutoCommon.Diag($"Pacing: {reason}; waiting {(double)delayMs / 1000.0:F1}s before moving on");
    }

    private bool IsSettling()
    {
        if (settleUntilMs == 0L)
        {
            return false;
        }
        if (Environment.TickCount64 >= settleUntilMs || Svc.Condition[ConditionFlag.InCombat])
        {
            settleUntilMs = 0L;
            return false;
        }
        return true;
    }

    private async Task TickSettle()
    {
        long remainingMs = Math.Max(0L, settleUntilMs - Environment.TickCount64);
        Status = $"Pausing before the next FATE ({remainingMs / 1000 + 1}s)";
        await DelayMs((int)Math.Min(remainingMs, 250L));
    }

    private async Task WaitOutSettle()
    {
        while (IsSettling() && !CancelToken.IsCancellationRequested)
        {
            await TickSettle();
        }
    }

    private bool WasPassedOver(uint fateId)
    {
        return varietyConsideredIds.Contains(fateId);
    }

    private void CommitToFate(uint fateId)
    {
        varietyCommittedId = fateId;
    }

    private PublicEvent? PickFate(Vector3 from)
    {
        varietyConsideredIds.Clear();
        if (!(Pacing.PickVarietyActive && returnToFateId is null))
        {
            return FateScanner.PickNext(Plugin.Cfg, from, sessionStuckFateIds, returnToFateId);
        }
        FateScanner.CollectEquivalentPicks(Plugin.Cfg, from, sessionStuckFateIds, varietyPool);
        if (varietyPool.Count == 0)
        {
            return null;
        }
        for (int i = 0; i < varietyPool.Count; i++)
        {
            PublicEvent candidate = varietyPool[i];
            varietyConsideredIds.Add(candidate.Id);
            if (candidate.Id == varietyCommittedId)
            {
                return candidate;
            }
        }
        PublicEvent picked = varietyPool[Pacing.RollIndex(varietyPool.Count)];
        varietyCommittedId = picked.Id;
        if (picked.Id != varietyPool[0].Id)
        {
            AutoCommon.Diag($"Pacing: picked FATE {picked.Id} ({picked.Name}) over the top-ranked {varietyPool[0].Id} ({varietyPool[0].Name}) from {varietyPool.Count} near-equal choices");
        }
        return picked;
    }

    private static bool PullAllowed()
    {
        if (Plugin.Cfg.PullAsManyAsPossible)
        {
            IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
            if (localPlayer != null && localPlayer.ClassJob.Value.Role == 1 && !Svc.Condition[ConditionFlag.Mounted])
            {
                return BossModIPC.Instance.CanClearTransientStrategy;
            }
        }
        return false;
    }

    private bool TickPull(uint fateId, PublicEvent fate, string preset)
    {
        if (!(PullAllowed() && Svc.Objects.LocalPlayer is { } localPlayer))
        {
            EndPull(preset, fateId, null);
            return false;
        }
        Configuration cfg = Plugin.Cfg;
        long tickCount = Environment.TickCount64;
        int hpPercent = (localPlayer.MaxHp == 0) ? 100 : (int)(100L * (long)localPlayer.CurrentHp / localPlayer.MaxHp);
        if (pullHpLow && hpPercent >= Math.Max(cfg.PullResumeHpPercent, cfg.PullStopHpPercent))
        {
            pullHpLow = false;
        }
        else if (!pullHpLow && hpPercent < cfg.PullStopHpPercent)
        {
            pullHpLow = true;
            if (pullActive)
            {
                EndPull(preset, fateId, $"HP down to {hpPercent}%; fighting the pack before pulling more");
            }
        }
        if (pullHpLow)
        {
            return false;
        }
        if (!pullActive && tickCount < pullNextScanMs)
        {
            return false;
        }
        if (pullActive && pullTargetId != 0L && TickPullTarget(fateId, localPlayer, tickCount))
        {
            return true;
        }
        PruneSkippedPullTargets(tickCount);
        FatePullSurvey survey = FateMobScanner.SurveyPull(fateId, localPlayer.Position, fate.Position, fate.Radius, cfg.PullReachMeters, pullSkippedUntilMs);
        if (survey.OnPlayer >= cfg.PullMaxMobs || !survey.HasCandidate)
        {
            if (pullActive)
            {
                EndPull(preset, fateId, survey.HasCandidate ? $"{survey.OnPlayer} mobs on the character" : $"no idle mob left within {cfg.PullReachMeters}m");
            }
            pullNextScanMs = tickCount + 1000;
            return false;
        }
        if (!pullActive && !BeginPull(fateId, preset))
        {
            pullNextScanMs = tickCount + 60000;
            return false;
        }
        pullTargetId = survey.Next.GameObjectId;
        pullTargetSinceMs = tickCount;
        if (FateMobScanner.TryGetPullTarget(fateId, pullTargetId, out IBattleNpc npc))
        {
            Svc.Targets.Target = npc;
            FirePullAction(localPlayer, npc, tickCount);
        }
        UpdatePullStatus(fate, in survey);
        return true;
    }

    private void UpdatePullStatus(PublicEvent fate, in FatePullSurvey survey)
    {
        if (survey.Next.GameObjectId == pullStatusTargetId && survey.OnPlayer == pullStatusOnPlayer && survey.Candidates == pullStatusCandidates)
        {
            return;
        }
        pullStatusTargetId = survey.Next.GameObjectId;
        pullStatusOnPlayer = survey.OnPlayer;
        pullStatusCandidates = survey.Candidates;
        Status = $"Pulling {fate.Name} ({survey.OnPlayer} on you, {survey.Candidates} more in reach)";
    }
}
