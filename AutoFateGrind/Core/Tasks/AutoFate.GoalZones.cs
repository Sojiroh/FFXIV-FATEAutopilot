using System.Threading.Tasks;
using AutoFateGrind.Core.Game.Items;
using AutoFateGrind.Core.Game.Player;
using AutoFateGrind.Core.Ipc;
using AutoFateGrind.Core.Modes;
using AutoFateGrind.Core.Zones;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;

namespace AutoFateGrind.Core.Tasks;

public sealed partial class AutoFate
{
    private async Task<ExitReason> LeaveDoneZone()
    {
        ZoneInfo done = zone;
        string reason = ZoneDoneReason(done);
        Status = done.Name + " is done; moving on";
        await HoldForCollectReward();
        await WaitOutSettle();
        await ClearBlockingCombat();
        if (CancelToken.IsCancellationRequested)
        {
            return ExitReason.Quit;
        }
        if (AdvanceZone())
        {
            AutoCommon.Diag($"{done.Name}: {reason}; moving on to {zone.Name}");
            Svc.Chat.Print($"[FTA] {done.Name}: {reason}. Moving on to {zone.Name}.");
            return ExitReason.Continue;
        }
        AutoCommon.Diag(done.Name + ": " + reason + ", and no other reachable zone is left; ending the run");
        Svc.Chat.Print($"[FTA] {done.Name}: {reason}, and no other reachable zone is left in the plan.");
        Status = "Every reachable zone is done";
        session.CompletedByStopCondition = true;
        return ExitReason.Quit;
    }

    private void ReportGoalMet()
    {
        ReportYokaiGoalMet();
        ReportItemGoalMet();
        ReportLevelLimitCutShort();
    }

    private void ReportLevelLimitCutShort()
    {
        Configuration cfg = Plugin.Cfg;
        int levelCap = RunLimits.LevelCap(cfg);
        if (!(cfg.StopAfterLevelsEnabled && session.LevelsGained < levelCap && ClassSwitcher.LevelingJobAtMax(cfg)))
        {
            return;
        }
        AutoCommon.Diag($"Level limit: the job is at max level after {session.LevelsGained} of {levelCap} levels; ending the run");
        Svc.Chat.Print($"[FTA] Your job reached the level cap after {session.LevelsGained} of {levelCap} levels, so the run ends here.");
    }

    private void ReportItemGoalMet()
    {
        ItemGoalDefinition? itemGoalDefinition = ItemGoalCatalog.Find(Plugin.Cfg.ActiveMode.Id);
        if (itemGoalDefinition == null)
        {
            return;
        }
        string progress = ItemGoalProgress.Describe(itemGoalDefinition, Plugin.Cfg);
        AutoCommon.Diag("Item goal " + itemGoalDefinition.DisplayName + " at completion: " + progress);
        Svc.Chat.Print(ItemGoalProgress.IsAvailable(itemGoalDefinition) ? $"[FTA] {itemGoalDefinition.DisplayName}: every item is collected ({progress})." : $"[FTA] {itemGoalDefinition.DisplayName}: the items no longer drop (quest turned in or relic unequipped); stopping with {progress}.");
    }

    private bool CanIdleMount()
    {
        if (!CancelToken.IsCancellationRequested)
        {
            IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
            if (localPlayer != null && !localPlayer.IsDead && !localPlayer.IsCasting && !Svc.Condition[ConditionFlag.InCombat] && !Svc.Condition[ConditionFlag.Mounting] && !Svc.Condition[ConditionFlag.Mounting71] && !Svc.Condition[ConditionFlag.BetweenAreas] && !Svc.Condition[ConditionFlag.BetweenAreas51])
            {
                return !NavmeshIPC.Instance.IsBusy();
            }
        }
        return false;
    }
}
