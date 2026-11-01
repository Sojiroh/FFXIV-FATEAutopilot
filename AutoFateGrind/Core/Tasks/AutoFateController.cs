using System;
using System.Collections.Generic;
using System.Linq;
using AutoFateGrind.Core.External;
using AutoFateGrind.Core.Game.Player;
using AutoFateGrind.Core.Game.Yokai;
using AutoFateGrind.Core.Trading;
using AutoFateGrind.Core.Zones;

namespace AutoFateGrind.Core.Tasks;

internal sealed partial class AutoFateController
{
    private AutoFateSession? session;

    private IReadOnlyList<ZoneInfo> activeZones = Array.Empty<ZoneInfo>();

    private static readonly Random rng = new Random();

    public bool Running
    {
        get
        {
            if (!clib.Services.Svc.Automation.Running)
            {
                return Paused;
            }
            return true;
        }
    }

    public string Status => PauseReason switch
    {
        PauseReason.InContent => "Paused while you are in content",
        PauseReason.Manual => "Paused",
        _ => clib.Services.Svc.Automation.CurrentTask?.Status ?? "Idle",
    };

    public AutoPhase Phase { get; private set; }

    public AutoFateSession? SessionSnapshot => session;

    public int ActiveZoneCount => activeZones.Count;

    private static void Diag(string message)
    {
        RunLog.Info(message);
    }

    private int ResumeIndexFor(ZoneInfo? origin, int fallback = 0)
    {
        if (origin == null)
        {
            return fallback;
        }
        for (int i = 0; i < activeZones.Count; i++)
        {
            if (activeZones[i].TerritoryId == origin.TerritoryId)
            {
                return i;
            }
        }
        return fallback;
    }

    public void RunAll(IEnumerable<ZoneInfo> zones)
    {
        activeZones = zones.ToList();
        ResetZoneShares();
        if (activeZones.Count == 0)
        {
            Diag("Start aborted: no zones selected.");
            return;
        }
        if (!ExternalPlugins.AllRequiredInstalled())
        {
            string missingNames = string.Join(", ", from p in ExternalPlugins.All
                                                    where ExternalPlugins.Catalog[p].Required && !ExternalPlugins.IsInstalled(p)
                                                    select ExternalPlugins.Catalog[p].DisplayName);
            Diag("Start aborted: required plugins missing (" + missingNames + ").");
            ECommons.DalamudServices.Svc.Chat.PrintError("[FTA] Cannot start — install all required plugins first: " + missingNames + ".");
            return;
        }
        PauseReason = PauseReason.None;
        int wallet = GemstoneCatalog.CurrentWalletCount();
        AutoFateSession newSession = new AutoFateSession
        {
            GemstoneCurrent = wallet
        };
        newSession.CaptureStartExp();
        int startIndex = 0;
        if (ZoneSelection.IsYokaiGoal(Plugin.Cfg) && !PlanYokaiZones(newSession))
        {
            Diag("Start aborted: no yo-kai is left to farm.");
            activeZones = Array.Empty<ZoneInfo>();
            return;
        }
        if (ZoneSelection.GoalPlansZones(Plugin.Cfg))
        {
            startIndex = CurrentTerritoryIndex();
        }
        session = newSession;
        Diag($"Run starting: {activeZones.Count} zone(s), mode {Plugin.Cfg.ActiveMode.DisplayName}, wallet {wallet}g, threshold {Plugin.Cfg.TradeThreshold}g, trade-on-cap {(Plugin.Cfg.TradeOnCap ? "on" : "off")}.");
        ApplyStartingClass();
        if (HandoffTriggers.QueueIfDue(newSession, activeZones[startIndex]))
        {
            Diag("Hand-off due at run start; running it before the first FATE.");
            HandlePostFateHandoffs(newSession);
        }
        else
        {
            StartFateGrind(startIndex, newSession);
        }
    }

    private void ResetZoneShares()
    {
        for (int i = 0; i < activeZones.Count; i++)
        {
            activeZones[i].CompletedThisRun = 0;
        }
    }

    private bool PlanYokaiZones(AutoFateSession owningSession)
    {
        int targetIndex = YokaiProgress.ResolveTargetIndex(Plugin.Cfg, owningSession.YokaiTargetMinionId);
        IReadOnlyList<ZoneInfo> targetZones = YokaiProgress.ZonesFor(targetIndex);
        Diag("Yo-kai roster: " + YokaiProgress.DescribeRoster(Plugin.Cfg));
        if (targetZones.Count == 0)
        {
            return false;
        }
        owningSession.YokaiTargetMinionId = YokaiCatalog.Entries[targetIndex].MinionId;
        activeZones = targetZones;
        ResetZoneShares();
        Diag($"Yo-kai target: {YokaiProgress.MinionName(targetIndex)} ({YokaiProgress.Statuses[targetIndex].Medals}/{YokaiProgress.MedalTarget(Plugin.Cfg)} medals), {targetZones.Count} zone(s).");
        return true;
    }

    private int CurrentTerritoryIndex()
    {
        uint territoryType = ECommons.DalamudServices.Svc.ClientState.TerritoryType;
        for (int i = 0; i < activeZones.Count; i++)
        {
            if (activeZones[i].TerritoryId == territoryType)
            {
                return i;
            }
        }
        return 0;
    }

    private static void ApplyStartingClass()
    {
        Configuration cfg = Plugin.Cfg;
        if (!cfg.ApplyClassOnStart)
        {
            return;
        }
        if (cfg.ClassQueue.Count == 0)
        {
            return;
        }
        int activeIndex = ClassSwitcher.FindActiveEntryIndex(cfg.ClassQueue);
        if (activeIndex < 0)
        {
            ECommons.DalamudServices.Svc.Chat.Print("[FTA] Class queue: every entry is at its level cap, staying on current class.");
            return;
        }
        ClassQueueEntry entry = cfg.ClassQueue[activeIndex];
        string gearsetLabel = $"gearset {entry.GearsetIndex} ({ClassSwitcher.JobNameForUserIndex(entry.GearsetIndex)})";
        if (ClassSwitcher.TryEquip(entry))
        {
            ECommons.DalamudServices.Svc.Chat.Print("[FTA] Switching to " + gearsetLabel + ".");
        }
        else
        {
            ECommons.DalamudServices.Svc.Chat.PrintError("[FTA] Could not equip " + gearsetLabel + " (game refused — combat, mount, or transient lock?). See /xllog for details.");
        }
    }

    public bool StopPending => session?.StopWhenSafe ?? false;

    public void StopWhenSafe()
    {
        if (session == null || !Running || Paused)
        {
            Stop();
            return;
        }
        if (session.StopWhenSafe)
        {
            return;
        }
        session.StopWhenSafe = true;
        Diag("Soft stop requested; the run ends once the current FATE is over.");
        ECommons.DalamudServices.Svc.Chat.Print("[FTA] Stopping after the current FATE.");
    }

    public void Stop()
    {
        AutoFateSession? endedSession = session;
        currentTask = null;
        grindTask = null;
        PauseReason = PauseReason.None;
        clib.Services.Svc.Automation.Stop();
        FinalizeRun(endedSession);
        session = null;
        activeZones = Array.Empty<ZoneInfo>();
        Phase = AutoPhase.Idle;
        if (endedSession != null)
        {
            Diag("Stop requested; session cleared.");
        }
    }

    private AutoCommon? currentTask;

    private AutoFate? grindTask;

    private void RunTask(AutoCommon task, Action onCompleted)
    {
        currentTask = task;
        clib.Services.Svc.Automation.Start(task, () =>
        {
            ECommons.DalamudServices.Svc.Framework.RunOnFrameworkThread(() =>
            {
                CompleteTask(task, onCompleted);
            });
        });
    }
}

internal enum AutoPhase
{
    Idle,
    Grinding,
    Trading,
    Repairing,
    Humanizing,
    Finishing,
    Paused
}

internal enum PauseReason
{
    None,
    Manual,
    InContent
}
