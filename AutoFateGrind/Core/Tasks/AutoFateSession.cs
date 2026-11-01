using System;
using System.Collections.Generic;
using AutoFateGrind.Core.Game.Player;
using AutoFateGrind.Core.Trading;
using AutoFateGrind.Core.Zones;

namespace AutoFateGrind.Core.Tasks;

public sealed class AutoFateSession
{
    public int CompletedCount;

    public DateTime StartedAt = DateTime.UtcNow;

    public int GemstoneCurrent;

    public int GemstonesEarned;

    private int gemWalletLastSeen;

    private bool gemBaselineCaptured;

    public uint JobId;

    public string JobAbbr = "";

    public int StartLevel;

    public int CurrentLevel;

    public long ExpEarned;

    public int LevelsGained;

    private uint trackedJobId;

    private long segmentBaselineExp;

    private int segmentBaselineLevel;

    private bool startCaptured;

    public int StuckRescuesWithoutFate;

    public int StuckRescueCompletedMark = -1;

    public bool Recorded;

    public bool CompletedByStopCondition;

    public bool AfterActionDispatched;

    public ZoneInfo? PendingTradeFromZone;

    public readonly HashSet<uint> TradeSkippedItemIds = new HashSet<uint>();

    public int TradeTransportBlocks;

    public bool PendingRepair;

    public ZoneInfo? PendingRepairFromZone;

    public int FatesSinceLastBreak;

    private int fatesBeforeNextBreak;

    public bool PendingHumanize;

    public ZoneInfo? PendingHumanizeFromZone;

    public uint YokaiTargetMinionId;

    public bool PendingYokaiAdvance;

    public bool StopWhenSafe;

    public IdleSpot? LastIdleSpot;

    public readonly HashSet<uint> UnreachableZoneIds = new HashSet<uint>();

    private readonly Dictionary<uint, int> deathsByFateId = new Dictionary<uint, int>();

    public bool EndedWithFault;

    public int FaultResumeZoneIndex;

    public int FaultResumeCount;

    public long FaultWindowStartedAtMs;

    private long pausedMs;

    private long pauseStartedAtMs;

    public bool DidNothing
    {
        get
        {
            if (CompletedCount == 0 && ExpEarned == 0L)
            {
                return GemstonesEarned == 0;
            }
            return false;
        }
    }

    public void CaptureStartExp()
    {
        if (ExpReader.Read() is not { } current)
        {
            return;
        }
        JobId = current.JobId;
        JobAbbr = current.JobAbbr;
        StartLevel = current.Level;
        CurrentLevel = current.Level;
        RebaselineTo(current);
        startCaptured = true;
    }

    public void UpdateExp()
    {
        if (ExpReader.Read() is not { } current)
        {
            return;
        }
        if (!startCaptured)
        {
            StartLevel = current.Level;
            CurrentLevel = current.Level;
            RebaselineTo(current);
            startCaptured = true;
            return;
        }
        if (current.JobId != trackedJobId)
        {
            RebaselineTo(current);
            CurrentLevel = current.Level;
            return;
        }
        long cumulative = ExpReader.Cumulative(current);
        if (cumulative > segmentBaselineExp)
        {
            ExpEarned += cumulative - segmentBaselineExp;
        }
        segmentBaselineExp = cumulative;
        if (current.Level > segmentBaselineLevel)
        {
            LevelsGained += current.Level - segmentBaselineLevel;
        }
        segmentBaselineLevel = current.Level;
        CurrentLevel = current.Level;
        JobId = current.JobId;
        JobAbbr = current.JobAbbr;
    }

    private void RebaselineTo(ExpReader.Snapshot s)
    {
        trackedJobId = s.JobId;
        segmentBaselineExp = ExpReader.Cumulative(s);
        segmentBaselineLevel = s.Level;
        JobId = s.JobId;
        JobAbbr = s.JobAbbr;
    }

    public double ExpPerHour
    {
        get
        {
            if (!(Elapsed.TotalHours > 0.0))
            {
                return 0.0;
            }
            return (double)ExpEarned / Elapsed.TotalHours;
        }
    }

    public int FatesBeforeNextBreak(int configured)
    {
        if (fatesBeforeNextBreak == 0)
        {
            fatesBeforeNextBreak = Pacing.FatesBeforeBreak(configured);
        }
        return fatesBeforeNextBreak;
    }

    public void ResetBreakCounter()
    {
        FatesSinceLastBreak = 0;
        fatesBeforeNextBreak = 0;
    }

    public void ClearQueuedHandoffs()
    {
        PendingRepair = false;
        PendingRepairFromZone = null;
        PendingTradeFromZone = null;
        PendingHumanize = false;
        PendingHumanizeFromZone = null;
    }

    public int CountDeath(uint fateId)
    {
        deathsByFateId.TryGetValue(fateId, out var deaths);
        deaths++;
        deathsByFateId[fateId] = deaths;
        return deaths;
    }

    public void UpdateGemstones()
    {
        if (!GemstoneCatalog.TryCurrentWalletCount(out var count))
        {
            return;
        }
        GemstoneCurrent = count;
        if (!gemBaselineCaptured)
        {
            gemWalletLastSeen = count;
            gemBaselineCaptured = true;
            return;
        }
        if (count > gemWalletLastSeen)
        {
            GemstonesEarned += count - gemWalletLastSeen;
        }
        gemWalletLastSeen = count;
    }

    public void BeginPause()
    {
        if (pauseStartedAtMs != 0L)
        {
            return;
        }
        pauseStartedAtMs = Environment.TickCount64;
    }

    public void EndPause()
    {
        if (pauseStartedAtMs == 0L)
        {
            return;
        }
        pausedMs += Environment.TickCount64 - pauseStartedAtMs;
        pauseStartedAtMs = 0L;
    }

    private long PausedTotalMs => pausedMs + ((pauseStartedAtMs == 0L) ? 0 : (Environment.TickCount64 - pauseStartedAtMs));

    public TimeSpan Elapsed => DateTime.UtcNow - StartedAt - TimeSpan.FromMilliseconds(PausedTotalMs);

    public double FatesPerHour
    {
        get
        {
            if (!(Elapsed.TotalHours > 0.0))
            {
                return 0.0;
            }
            return (double)CompletedCount / Elapsed.TotalHours;
        }
    }
}
