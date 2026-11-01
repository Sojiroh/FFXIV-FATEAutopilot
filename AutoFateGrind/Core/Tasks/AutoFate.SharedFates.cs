using System;
using System.Threading.Tasks;
using AutoFateGrind.Core.Game.SharedFates;
using ECommons.DalamudServices;

namespace AutoFateGrind.Core.Tasks;

public sealed partial class AutoFate
{
    private const int SharedFateSyncWaitMs = 8000;

    private const int SharedFateSyncPollFrames = 10;

    private async Task SyncSharedFateRanks()
    {
        if (!SharedFateGoal)
        {
            return;
        }
        Status = "Reading Shared FATE ranks";
        SharedFateProgress.Request(zones, force: true);
        bool allKnown = await WaitUntilTimed(() => SharedFateProgress.AllKnown(zones), 8000, "shared-fate-sync", 10);
        AutoCommon.Diag("Shared FATE ranks: " + SharedFateProgress.Describe(zones));
        if (allKnown)
        {
            return;
        }
        AutoCommon.Warn("The game did not report every Shared FATE rank; zones without one are grinded until it does");
        Svc.Chat.PrintError("[FTA] The game did not report every Shared FATE rank; zones without one are grinded until it does.");
    }

    private void TickSharedFateRefresh()
    {
        if (SharedFateGoal)
        {
            SharedFateProgress.Tick();
        }
    }

    private void NoteSharedFateCompletion()
    {
        if (!SharedFateGoal)
        {
            return;
        }
        SharedFateProgress.NoteCompletion(zone.TerritoryId);
        AutoCommon.Diag("Shared FATE rank after this FATE: " + SharedFateProgress.Describe(zone));
    }

    private string SharedFateHeartbeat()
    {
        if (!SharedFateGoal)
        {
            return string.Empty;
        }
        return " rank=" + SharedFateProgress.Describe(zone);
    }

    private static int StuckRescueDelayMs(Configuration cfg)
    {
        return Math.Clamp(cfg.StuckRescueMinutes, 2, 30) * 60000;
    }

    internal static int NoProgressFaultMsFor(Configuration cfg)
    {
        if (!cfg.StuckRescueEnabled)
        {
            return 300000;
        }
        return Math.Max(300000, StuckRescueDelayMs(cfg) + 60000);
    }

    private static bool StuckWatchApplies(GrindState state)
    {
        return state is GrindState.WaitingForYokaiMinion or GrindState.BetweenFates;
    }
}
