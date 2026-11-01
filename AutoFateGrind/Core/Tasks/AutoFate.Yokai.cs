using System;
using System.Threading.Tasks;
using AutoFateGrind.Core.Game.Yokai;
using AutoFateGrind.Core.Zones;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;

namespace AutoFateGrind.Core.Tasks;

public sealed partial class AutoFate
{
    private const int YokaiActionWaitMs = 3000;

    private const int YokaiSummonWindowMs = 12000;

    private const int YokaiSummonIdlePollFrames = 10;

    private const int YokaiSummonRetryFrames = 30;

    private const int YokaiParkRetryMs = 3000;

    private const int YokaiParkWarnRepeatMs = 60000;

    private long yokaiParkWarnedAtMs;

    private void ReportYokaiGoalMet()
    {
        if (!ZoneSelection.IsYokaiGoal(Plugin.Cfg))
        {
            return;
        }
        AutoCommon.Diag("Yo-kai roster at goal completion: " + YokaiProgress.DescribeRoster(Plugin.Cfg));
        Svc.Chat.Print("[FTA] Yo-kai goal met: " + YokaiProgress.CompletionSummary(Plugin.Cfg) + ". The roster is in /xllog.");
    }

    private async Task HandOffToNextYokai()
    {
        Status = "Switching to the next yo-kai";
        int targetIndex = YokaiProgress.ResolveTargetIndex(Plugin.Cfg, session.YokaiTargetMinionId);
        string targetName = ((targetIndex < 0) ? "none" : YokaiProgress.MinionName(targetIndex));
        AutoCommon.Diag($"Yo-kai minion {session.YokaiTargetMinionId} no longer needs medals; handing off to plan the next one ({targetName})");
        await HoldForCollectReward();
        await WaitOutSettle();
        await ClearBlockingCombat();
        session.PendingYokaiAdvance = true;
    }

    private bool YokaiMinionMissing()
    {
        if (!ZoneSelection.IsYokaiGoal(Plugin.Cfg))
        {
            return false;
        }
        uint yokaiTargetMinionId = session.YokaiTargetMinionId;
        if (yokaiTargetMinionId == 0 || Svc.Condition[ConditionFlag.InCombat] || IsPlayerKO())
        {
            return false;
        }
        return YokaiOps.SummonedMinionId() != yokaiTargetMinionId;
    }

    private async Task TickYokaiMinionWait()
    {
        string minionName = YokaiTargetName();
        Status = "Waiting to summon " + minionName;
        await EnsureYokaiCompanion();
        if (CancelToken.IsCancellationRequested || !YokaiMinionMissing())
        {
            yokaiParkWarnedAtMs = 0L;
            return;
        }
        WarnYokaiParked(minionName);
        await DelayMs(3000);
    }

    private void WarnYokaiParked(string minionName)
    {
        long tickCount = Environment.TickCount64;
        if (!(yokaiParkWarnedAtMs == 0L || tickCount - yokaiParkWarnedAtMs >= 60000))
        {
            return;
        }
        yokaiParkWarnedAtMs = tickCount;
        AutoCommon.Warn($"Parked in {zone.Name}: {minionName} is not summoned and its Legendary Medals need it out ({AutoCommon.ConditionTag()})");
        Svc.Chat.PrintError($"[FTA] Waiting in {zone.Name}: cannot summon {minionName}, and its Legendary Medals only drop while it is out. If it is raining, put your umbrella away or turn off auto-umbrella under Character Configuration > Fashion Accessories.");
    }

    private string YokaiTargetName()
    {
        return YokaiMinionName(session.YokaiTargetMinionId);
    }

    private void LogYokaiDropState()
    {
        if (!ZoneSelection.IsYokaiGoal(Plugin.Cfg))
        {
            return;
        }
        int minionIndex = YokaiCatalog.IndexOfMinion(session.YokaiTargetMinionId);
        int legendaryMedals = ((minionIndex < 0) ? (-1) : YokaiProgress.Statuses[minionIndex].Medals);
        AutoCommon.Diag($"Yo-kai after FATE: target {YokaiTargetName()} legendary medals {legendaryMedals}, plain medals {YokaiOps.PlainMedalCount()}, " + $"minion out {YokaiOps.SummonedMinionId()} (game active id {YokaiOps.PendingCompanionId()}), watch equipped {YokaiOps.IsWatchEquipped()}");
    }

    private static string YokaiMinionName(uint minionId)
    {
        int index = YokaiCatalog.IndexOfMinion(minionId);
        if (index >= 0)
        {
            return YokaiProgress.MinionName(index);
        }
        return minionId.ToString();
    }

    private async Task EnsureYokaiCompanion()
    {
        if (!ZoneSelection.IsYokaiGoal(Plugin.Cfg))
        {
            return;
        }
        uint minionId = session.YokaiTargetMinionId;
        if (minionId == 0 || Svc.Condition[ConditionFlag.InCombat] || IsPlayerKO())
        {
            return;
        }
        if (!YokaiOps.IsWatchEquipped())
        {
            await EquipYokaiWatch();
        }
        if (YokaiOps.SummonedMinionId() != minionId)
        {
            await SummonYokaiMinion(minionId);
        }
    }

    private async Task EquipYokaiWatch()
    {
        Status = "Equipping the Yo-kai Watch";
        if (!YokaiOps.TryEquipWatch())
        {
            AutoCommon.Warn("Yo-kai Watch is not equipped and was not found in the armoury chest or inventory; plain Yo-kai Medals will not drop");
            return;
        }
        if (!(await WaitUntilTimed(YokaiOps.IsWatchEquipped, 3000, "yokai-equip-watch")))
        {
            AutoCommon.Warn("Yo-kai Watch did not equip; retrying before the next FATE");
        }
    }

    private async Task SummonYokaiMinion(uint minionId)
    {
        string minionName = YokaiMinionName(minionId);
        Status = "Summoning " + minionName;
        await SafeDismount("dismount-yokai-summon");
        if (Svc.Condition[ConditionFlag.Mounted] || Svc.Condition[ConditionFlag.InCombat])
        {
            return;
        }
        await StowFashionAccessory();
        long deadline = Environment.TickCount64 + 12000;
        int attempts = 0;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested || YokaiOps.SummonedMinionId() == minionId)
            {
                return;
            }
            if (!YokaiOps.CanIssueSummon())
            {
                await NextFrame(10);
                continue;
            }
            if (YokaiOps.PendingCompanionId() == minionId && await WaitUntilTimed(() => YokaiOps.SummonedMinionId() == minionId, 3000, "yokai-spawn"))
            {
                AutoCommon.Diag(minionName + " spawned on its own");
                return;
            }
            attempts++;
            if (YokaiOps.TrySummon(minionId) && await WaitUntilTimed(() => YokaiOps.SummonedMinionId() == minionId, 3000, "yokai-summon"))
            {
                AutoCommon.Diag($"Summoned {minionName} (attempt {attempts})");
                return;
            }
            await NextFrame(30);
        }
        if (YokaiOps.SummonedMinionId() == minionId)
        {
            return;
        }
        AutoCommon.Warn($"Could not summon {minionName} within {12}s ({attempts} attempts, {AutoCommon.ConditionTag()})");
    }

    private async Task StowFashionAccessory()
    {
        if (!YokaiOps.IsFashionAccessoryDeployed())
        {
            return;
        }
        Status = "Putting the fashion accessory away";
        AutoCommon.Diag("A fashion accessory is deployed, which blocks minion summoning; withdrawing it");
        if (!YokaiOps.TryWithdrawFashionAccessory())
        {
            return;
        }
        if (!(await WaitUntilTimed(() => !YokaiOps.IsFashionAccessoryDeployed(), 3000, "yokai-stow-accessory")))
        {
            AutoCommon.Warn("The fashion accessory did not go away; the summon is attempted anyway");
        }
    }
}
