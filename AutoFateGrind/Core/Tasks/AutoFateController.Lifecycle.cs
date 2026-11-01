using System;
using System.Linq;
using AutoFateGrind.Core.Stats;

namespace AutoFateGrind.Core.Tasks;

internal sealed partial class AutoFateController
{
    private int pausedZoneIndex;

    public void Pause(PauseReason reason)
    {
        if (reason == PauseReason.None)
        {
            return;
        }
        if (Paused)
        {
            if (reason == PauseReason.Manual && PauseReason == PauseReason.InContent)
            {
                PauseReason = PauseReason.Manual;
                Diag("Auto-pause promoted to a manual pause; leaving content will no longer resume.");
            }
            return;
        }
        if (!CanPause)
        {
            if (reason == PauseReason.Manual)
            {
                ECommons.DalamudServices.Svc.Chat.Print("[FTA] Nothing to pause.");
            }
            return;
        }
        pausedZoneIndex = CurrentZoneIndex();
        PauseReason = reason;
        Phase = AutoPhase.Paused;
        session!.BeginPause();
        currentTask = null;
        clib.Services.Svc.Automation.Stop();
        string resumeZoneName = ((activeZones.Count > 0) ? activeZones[pausedZoneIndex].Name : "?");
        Diag($"Run paused ({reason}); session kept, resume zone {resumeZoneName}.");
        ECommons.DalamudServices.Svc.Chat.Print((reason == PauseReason.InContent) ? "[FTA] Paused: you are in instanced content. The grind resumes once you are back outside." : "[FTA] Paused. Your zones, goal, and session stats are kept until you resume or stop.");
    }

    public void Resume()
    {
        if (!Paused)
        {
            return;
        }
        AutoFateSession? resumedSession = session;
        if (resumedSession == null || activeZones.Count == 0)
        {
            Diag("Resume requested with no session or no zones; stopping instead.");
            Stop();
            return;
        }
        PauseReason = PauseReason.None;
        resumedSession.EndPause();
        int resumeIndex = Math.Clamp(pausedZoneIndex, 0, activeZones.Count - 1);
        Diag("Resuming FATE grind at " + activeZones[resumeIndex].Name + ".");
        ECommons.DalamudServices.Svc.Chat.Print("[FTA] Resuming in " + activeZones[resumeIndex].Name + ".");
        StartFateGrind(resumeIndex, resumedSession);
    }

    public void TogglePause()
    {
        if (Paused)
        {
            Resume();
        }
        else
        {
            Pause(PauseReason.Manual);
        }
    }

    private int CurrentZoneIndex()
    {
        if (activeZones.Count == 0)
        {
            return 0;
        }
        if (grindTask == null)
        {
            return 0;
        }
        return Math.Clamp(grindTask.ZoneIndex, 0, activeZones.Count - 1);
    }

    private void EndRun(AutoFateSession? s)
    {
        FinalizeRun(s);
        Phase = AutoPhase.Idle;
        MaybeRunAfterAction(s);
    }

    private void MaybeRunAfterAction(AutoFateSession? s)
    {
        if (s == null || s != session || !s.CompletedByStopCondition || s.AfterActionDispatched)
        {
            return;
        }
        s.AfterActionDispatched = true;
        AfterRunAction action = Plugin.Cfg.AfterRun;
        string postRunCommands = Plugin.Cfg.PostRunCommands;
        bool hasPostRunCommands = !string.IsNullOrWhiteSpace(postRunCommands);
        if (action == AfterRunAction.StayLoggedIn && !hasPostRunCommands)
        {
            Diag("Run completed by stop condition; after-run action = StayLoggedIn and no chat commands (no-op).");
            return;
        }
        if (s.DidNothing)
        {
            Diag($"Run ended by stop condition without doing any work; skipping chat commands and after-run action {action}.");
            return;
        }
        Diag($"Run completed by stop condition; starting after-run action {action}.");
        Phase = AutoPhase.Finishing;
        if (!hasPostRunCommands && action == AfterRunAction.ReturnToInn)
        {
            RunTask(new AutoReturnToInn(), () =>
            {
                FinishAfterAction(action);
            });
            return;
        }
        RunTask(new AutoAfterRun(action, postRunCommands), () =>
        {
            if (action != AfterRunAction.ReturnToInn)
            {
                FinishAfterAction(action);
            }
            else
            {
                RunTask(new AutoReturnToInn(), () =>
                {
                    FinishAfterAction(action);
                });
            }
        });
    }

    private void FinishAfterAction(AfterRunAction action)
    {
        Diag($"After-run action {action} finished.");
        Phase = AutoPhase.Idle;
    }

    private void FinalizeRun(AutoFateSession? s)
    {
        if (s == null || s.Recorded)
        {
            return;
        }
        if (s.DidNothing)
        {
            s.Recorded = true;
            return;
        }
        s.Recorded = true;
        try
        {
            s.UpdateExp();
            RunRecord runRecord = new RunRecord
            {
                StartedAtUtc = s.StartedAt,
                EndedAtUtc = DateTime.UtcNow,
                DurationSeconds = s.Elapsed.TotalSeconds,
                FatesCompleted = s.CompletedCount,
                GemstonesEarned = s.GemstonesEarned,
                ExpEarned = s.ExpEarned,
                LevelsGained = s.LevelsGained,
                StartLevel = s.StartLevel,
                EndLevel = s.CurrentLevel,
                JobId = s.JobId,
                JobAbbr = s.JobAbbr,
                ModeName = Plugin.Cfg.ActiveMode.DisplayName,
                ZoneNames = activeZones.Select(z => z.Name).ToList()
            };
            Plugin.Instance.History.Append(runRecord);
            Diag($"Run recorded to history: {runRecord.FatesCompleted} FATEs, {runRecord.GemstonesEarned}g, {runRecord.ExpEarned} exp over {runRecord.Duration}.");
        }
        catch (Exception ex)
        {
            Diag("FinalizeRun failed to record history: " + ex.Message);
        }
    }

    private const int MaxFaultResumes = 3;

    private static long FaultResumeWindowMs => 5L * (long)AutoFate.NoProgressFaultMsFor(Plugin.Cfg);

    private bool TryAutoResumeAfterFault(AutoFateSession s)
    {
        s.EndedWithFault = false;
        if (activeZones.Count == 0)
        {
            return false;
        }
        long tickCount = Environment.TickCount64;
        if (tickCount - s.FaultWindowStartedAtMs > FaultResumeWindowMs)
        {
            s.FaultResumeCount = 0;
            s.FaultWindowStartedAtMs = tickCount;
        }
        if (s.FaultResumeCount >= 3)
        {
            Diag($"Grind task faulted {s.FaultResumeCount}× within {FaultResumeWindowMs / 60000}m; not resuming. Run ends.");
            return false;
        }
        s.FaultResumeCount++;
        int resumeIndex = Math.Clamp(s.FaultResumeZoneIndex, 0, activeZones.Count - 1);
        Diag($"Grind task ended with an unexpected fault; auto-resuming at {activeZones[resumeIndex].Name} (resume {s.FaultResumeCount}/{3} this {FaultResumeWindowMs / 60000}m window).");
        StartFateGrind(resumeIndex, s);
        return true;
    }
}
