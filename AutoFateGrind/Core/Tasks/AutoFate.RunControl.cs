using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFateGrind.Core.Game.Fates;
using AutoFateGrind.Core.Zones;
using ECommons.DalamudServices;

namespace AutoFateGrind.Core.Tasks;

public sealed partial class AutoFate
{
    private readonly HashSet<uint> retryAfterCompletionIds = new HashSet<uint>();

    private async Task FinishSoftStop()
    {
        Status = "Stopping after this FATE";
        await HoldForCollectReward();
        await ClearBlockingCombat();
        AutoCommon.Diag("Soft stop: the current FATE is over and the character is out of combat; ending the run");
        Svc.Chat.Print("[FTA] Stopped after the FATE, as requested.");
        Status = "Stopped";
    }

    private static bool KeepingTwistOfFate()
    {
        if (Plugin.Cfg.KeepTwistOfFate)
        {
            return FateScanner.PlayerHasTwistOfFate();
        }
        return false;
    }

    private void DeferSkipUntilNextCompletion(uint fateId)
    {
        retryAfterCompletionIds.Add(fateId);
    }

    private void ReleaseDeferredSkips()
    {
        if (retryAfterCompletionIds.Count == 0)
        {
            return;
        }
        sessionStuckFateIds.ExceptWith(retryAfterCompletionIds);
        AutoCommon.Diag($"Released {retryAfterCompletionIds.Count} FATE(s) skipped for routing trouble; they are eligible again after this completion");
        retryAfterCompletionIds.Clear();
    }

    private static bool SharedFateGoal => ZoneSelection.GoalNeedsRankedZones(Plugin.Cfg);
}
