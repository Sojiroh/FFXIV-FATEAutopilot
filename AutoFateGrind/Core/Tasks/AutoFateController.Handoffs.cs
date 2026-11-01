using System;
using System.Collections.Generic;
using AutoFateGrind.Core.Game.Yokai;
using AutoFateGrind.Core.Trading;
using AutoFateGrind.Core.Zones;

namespace AutoFateGrind.Core.Tasks;

internal sealed partial class AutoFateController
{
    private void CompleteTask(AutoCommon task, Action onCompleted)
    {
        if (currentTask != task)
        {
            Diag(task.GetType().Name + " finished but is no longer the current task (stopped, paused, or superseded); skipping hand-off.");
            return;
        }
        try
        {
            onCompleted();
        }
        catch (Exception ex)
        {
            RunLog.Error(ex, "Hand-off after " + task.GetType().Name + " failed; the run ends.");
            ECommons.DalamudServices.Svc.Chat.PrintError($"[FTA] The run stopped after {task.GetType().Name}: {ex.Message}. /xllog has the details.");
            EndRun(session);
        }
    }

    private void StartFateGrind(int startZoneIndex, AutoFateSession owningSession)
    {
        Phase = AutoPhase.Grinding;
        string zoneName = ((startZoneIndex < activeZones.Count) ? activeZones[startZoneIndex].Name : "?");
        Diag($"FATE grind phase entering at zone[{startZoneIndex}] {zoneName}.");
        grindTask = new AutoFate(activeZones, owningSession, startZoneIndex);
        RunTask(grindTask, () =>
        {
            HandlePostFateHandoffs(owningSession);
        });
    }

    private void HandlePostFateHandoffs(AutoFateSession owningSession)
    {
        if (owningSession != session)
        {
            Diag("FATE grind ended: owning session is stale (Stop was called or a new run replaced it). Ignoring hand-offs.");
            EndRun(owningSession);
            return;
        }
        if (owningSession.StopWhenSafe)
        {
            Diag("Soft stop: the grind task ended and no hand-off runs. Run ends.");
            EndRun(owningSession);
            return;
        }
        if (owningSession.PendingYokaiAdvance)
        {
            owningSession.PendingYokaiAdvance = false;
            if (PlanYokaiZones(owningSession))
            {
                StartFateGrind(CurrentTerritoryIndex(), owningSession);
                return;
            }
            Diag("Yo-kai hand-off found nothing left to farm. Run ends.");
            ECommons.DalamudServices.Svc.Chat.Print("[FTA] Yo-kai goal met: " + YokaiProgress.CompletionSummary(Plugin.Cfg) + ". The roster is in /xllog.");
            owningSession.CompletedByStopCondition = true;
            EndRun(owningSession);
            return;
        }
        if (owningSession.PendingRepair)
        {
            owningSession.PendingRepair = false;
            Phase = AutoPhase.Repairing;
            Diag("Repair phase entering.");
            RunTask(new AutoRepair(), () =>
            {
                HandlePostFateHandoffs(owningSession);
            });
            return;
        }
        ZoneInfo? origin = owningSession.PendingTradeFromZone;
        if (origin == null)
        {
            if (Phase == AutoPhase.Repairing && activeZones.Count > 0)
            {
                int resumeIndex = ResumeIndexFor(owningSession.PendingRepairFromZone);
                owningSession.PendingRepairFromZone = null;
                Diag("Repair finished with no pending trade; resuming FATE grind at " + activeZones[resumeIndex].Name + ".");
                ResumeGrindOrHumanize(owningSession, resumeIndex);
            }
            else if (owningSession.PendingHumanize && activeZones.Count > 0)
            {
                int humanizeIndex = ResumeIndexFor(owningSession.PendingHumanizeFromZone);
                Diag("Humanize triggered with no other hand-offs; entering break from " + activeZones[humanizeIndex].Name + ".");
                ResumeGrindOrHumanize(owningSession, humanizeIndex);
            }
            else
            {
                if (owningSession.EndedWithFault && TryAutoResumeAfterFault(owningSession))
                {
                    return;
                }
                Diag("FATE grind ended without queueing further hand-offs. Run ends.");
                EndRun(owningSession);
            }
            return;
        }
        owningSession.PendingTradeFromZone = null;
        TradePlan? tradePlan = TradeList.Plan(owningSession.GemstoneCurrent, origin.TerritoryId, origin.Expansion, owningSession.TradeSkippedItemIds);
        if (tradePlan is null)
        {
            Diag("Trade hand-off dropped: nothing on the shopping list can be bought any more; resuming the grind.");
            ResumeGrindOrHumanize(owningSession, ResumeIndexFor(origin));
            return;
        }
        Phase = AutoPhase.Trading;
        Diag($"Trade phase entering: {tradePlan.Trader.Name} for {tradePlan.DescribeItems()}, origin zone {origin.Name} ({origin.TerritoryId}).");
        AutoTrade trade = new AutoTrade(tradePlan, owningSession.TradeSkippedItemIds);
        RunTask(trade, () =>
        {
            if (owningSession != session)
            {
                Diag("AutoTrade finished: owning session is stale; not resuming.");
                EndRun(owningSession);
                return;
            }
            SkipFailedTradeItems(owningSession, trade);
            if (Plugin.Cfg.AfterTrade != AfterTradeAction.Resume)
            {
                Diag($"AutoTrade finished: AfterTrade = {Plugin.Cfg.AfterTrade}; not resuming.");
                EndRun(owningSession);
                return;
            }
            if (activeZones.Count == 0)
            {
                Diag("AutoTrade finished: no active zones recorded; cannot resume.");
                EndRun(owningSession);
                return;
            }
            int resumeAt = ResumeIndexFor(origin);
            Diag("AutoTrade finished: resuming FATE grind at " + activeZones[resumeAt].Name + ".");
            ResumeGrindOrHumanize(owningSession, resumeAt);
        });
    }

    private static void SkipFailedTradeItems(AutoFateSession owningSession, AutoTrade trade)
    {
        if (trade.TransportBlocked)
        {
            owningSession.TradeTransportBlocks++;
            if (owningSession.TradeTransportBlocks == 1)
            {
                Diag("AutoTrade never reached " + trade.Trader.Name + ": the character's own state stalled every teleport cast. The shopping list is kept and the next FATE retries the trade (issue #99).");
                ECommons.DalamudServices.Svc.Chat.Print("[FTA] Auto-trade could not teleport to " + trade.Trader.Name + "; it retries after the next FATE.");
                return;
            }
        }
        List<string> failedNames = new List<string>(trade.Orders.Count);
        for (int i = 0; i < trade.Orders.Count; i++)
        {
            GemstoneTradeItem tradeItem = trade.Orders[i].Item;
            if (trade.Failed(tradeItem.ItemId) && owningSession.TradeSkippedItemIds.Add(tradeItem.ItemId))
            {
                failedNames.Add(tradeItem.ItemName);
            }
        }
        if (failedNames.Count == 0)
        {
            return;
        }
        string names = string.Join(", ", failedNames);
        Diag("AutoTrade could not buy " + names + "; skipping them for the rest of the run.");
        ECommons.DalamudServices.Svc.Chat.PrintError("[FTA] Auto-trade could not buy " + names + ", so the rest of this run skips them. /xllog has the details.");
    }

    private void ResumeGrindOrHumanize(AutoFateSession owningSession, int resumeIndex)
    {
        if (!owningSession.PendingHumanize)
        {
            StartFateGrind(resumeIndex, owningSession);
            return;
        }
        owningSession.PendingHumanize = false;
        ZoneInfo? pendingHumanizeFromZone = owningSession.PendingHumanizeFromZone;
        owningSession.PendingHumanizeFromZone = null;
        Configuration cfg = Plugin.Cfg;
        BreakPlan? maybeBreak = (cfg.HumanizerEnabled ? HumanizeBreaks.Pick(cfg, owningSession.LastIdleSpot, rng) : ((BreakPlan?)null));
        if (maybeBreak is not { } plan)
        {
            Diag("Humanize hand-off skipped: feature disabled, or no saved idle spot and no selected city in the current catalog.");
            owningSession.ResetBreakCounter();
            StartFateGrind(resumeIndex, owningSession);
            return;
        }
        owningSession.LastIdleSpot = plan.Spot;
        int minMinutes = Math.Max(1, cfg.HumanizerBreakMinMinutes);
        int maxMinutes = Math.Max(minMinutes, cfg.HumanizerBreakMaxMinutes);
        int breakMinutes = rng.Next(minMinutes, maxMinutes + 1);
        int durationMs = breakMinutes * 60000;
        int resumeIdx = ResumeIndexFor(pendingHumanizeFromZone, resumeIndex);
        Phase = AutoPhase.Humanizing;
        Diag($"Humanize phase entering: {plan.Describe()}, duration {breakMinutes}m, resume zone {activeZones[resumeIdx].Name}.");
        AutoHumanize humanize = new AutoHumanize(plan, durationMs);
        RunTask(humanize, () =>
        {
            if (owningSession != session)
            {
                Diag("Humanize finished: owning session is stale; not resuming.");
                EndRun(owningSession);
            }
            else
            {
                if (humanize.BreakTaken)
                {
                    owningSession.ResetBreakCounter();
                    Diag("Humanize finished: resuming FATE grind at " + activeZones[resumeIdx].Name + ".");
                }
                else
                {
                    Diag($"Humanize did not take a break (could not reach {plan.Place}); leaving counter at {owningSession.FatesSinceLastBreak} to retry next FATE. Resuming at {activeZones[resumeIdx].Name}.");
                }
                StartFateGrind(resumeIdx, owningSession);
            }
        });
    }

    public PauseReason PauseReason { get; private set; }

    public bool Paused => PauseReason != PauseReason.None;

    public bool CanPause => session is not null && Phase is AutoPhase.Grinding or AutoPhase.Trading or AutoPhase.Repairing or AutoPhase.Humanizing;
}
