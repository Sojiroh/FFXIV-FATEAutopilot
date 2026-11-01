using System;
using System.Collections.Generic;
using System.Numerics;
using AutoFateGrind.Core;
using AutoFateGrind.Core.Changelog;
using AutoFateGrind.Core.Modes;
using Dalamud.Configuration;
using ECommons.Throttlers;
using Newtonsoft.Json;

namespace AutoFateGrind;

public enum GrindMode
{
    Endless,
    MaxFates,
    MaxGemstones,
    RunCount
}

public enum AfterTradeAction
{
    Resume,
    Stop
}

public enum AfterRunAction
{
    StayLoggedIn,
    Logout,
    ReturnToInn,
    CloseGame
}

public enum GemstoneSpendMode
{
    SpendAll,
    SpendGems,
    BuyQuantity
}

public enum AfterClassQueueDone
{
    KeepGrindingOnLast,
    StopRun
}

public enum RepairMode
{
    SelfThenNpc,
    SelfOnly,
    NpcOnly
}

public enum HumanizerBreakActivity
{
    Wander,
    IdleAtSpot
}

public enum PartyInviteReplyChannel
{
    Tell,
    Say,
    Yell
}

public enum FateSortCriterion
{
    HasBonusWithTwist,
    Progress,
    HasBonus,
    TimeRemainingUrgent,
    Distance,
    TimeRemaining,
    Level,
    Name
}

public sealed class RepairNpc
{
    public uint TerritoryId { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public uint DataId { get; set; }

    public string Name { get; set; } = "";

    public int RepairIndex { get; set; }
}

[Serializable]
public sealed class IdleSpot
{
    public uint TerritoryId { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public string Name { get; set; } = "";

    [JsonIgnore]
    public Vector3 Position => new Vector3(X, Y, Z);
}

[Serializable]
public sealed class ConsumableEntry
{
    public uint ItemId { get; set; }

    public string Name { get; set; } = "";

    public uint StatusId { get; set; }

    public bool CanBeHq { get; set; }
}

[Serializable]
public sealed class FateSortEntry
{
    public FateSortCriterion Criterion { get; set; }

    public bool Descending { get; set; }
}

[Serializable]
public sealed class ClassQueueEntry
{
    public byte GearsetIndex { get; set; }

    public byte JobId { get; set; }

    public int StopAtLevel { get; set; }
}

[Serializable]
public sealed class TradeListEntry
{
    public uint ItemId { get; set; }

    public int StopAtCount { get; set; }
}

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public const int StuckRescueMinMinutes = 2;

    public const int StuckRescueMaxMinutes = 30;

    public int Version { get; set; }

    public bool AutoShowOnLogin { get; set; }

    public bool DragWindowFromAnywhere { get; set; }

    public string Language { get; set; } = "";

    public List<uint> SelectedZones { get; set; } = new List<uint>();

    public GrindMode Mode { get; set; } = GrindMode.MaxGemstones;

    public string ModeId { get; set; } = "";

    [JsonIgnore]
    public IFateGrindMode ActiveMode
    {
        get
        {
            if (string.IsNullOrEmpty(ModeId))
            {
                ModeId = FateGrindModes.IdForLegacy(Mode);
            }
            return FateGrindModes.GetById(ModeId) ?? FateGrindModes.Default;
        }
    }

    public int TargetFateCount { get; set; } = 30;

    public int TargetGemstoneCount { get; set; } = AfgConstants.BicolorCap;

    public int TargetMinutes { get; set; } = 60;

    public int TargetYokaiMedals { get; set; } = 10;

    public int TargetRelicCount { get; set; } = 1;

    public int TargetLevels { get; set; } = 10;

    public HashSet<uint> YokaiSkippedMinionIds { get; set; } = new HashSet<uint>();

    public bool StopAfterFatesEnabled { get; set; }

    public bool StopAfterMinutesEnabled { get; set; }

    public bool StopAfterLevelsEnabled { get; set; }

    public bool FatesPerZoneEnabled { get; set; }

    public int TargetFatesPerZone { get; set; } = 5;

    public string CombatPresetName { get; set; } = AfgConstants.BundledCombatPresetName;

    public int BundledCombatPresetRevision { get; set; }

    public int MinTimeRemainingSec { get; set; } = 120;

    public int MaxProgressPct { get; set; } = 90;

    public bool MaxDurationFilterEnabled { get; set; }

    public int MaxFateDurationSec { get; set; } = 900;

    public bool LevelWindowEnabled { get; set; }

    public int MinFateLevel { get; set; } = 1;

    public int MaxFateLevel { get; set; } = 100;

    public bool LevelRangeFilterEnabled { get; set; }

    public int MaxLevelBelow { get; set; } = 5;

    public int MaxLevelAbove { get; set; } = 5;

    public bool SwapZonesWhenEmpty { get; set; } = true;

    public int SwapZoneWaitSec { get; set; } = 30;

    public string FateNameFormat { get; set; } = "L{Level}   {Name}";

    public bool KeepTwistOfFate { get; set; } = true;

    public bool MountWhileWaitingForFates { get; set; }

    public uint PreferredMountId { get; set; }

    public bool EconomyTravel { get; set; }

    public bool ShowLivePopout { get; set; }

    public bool AutoResumeOnFault { get; set; } = true;

    public bool StuckRescueEnabled { get; set; } = true;

    public int StuckRescueMinutes { get; set; } = 5;

    public bool AutoPauseInContent { get; set; } = true;

    public bool CollectHandInEnabled { get; set; } = true;

    public int CollectHandInBatch { get; set; } = 5;

    public bool PullAsManyAsPossible { get; set; }

    public int PullReachMeters { get; set; } = 25;

    public int PullMaxMobs { get; set; } = 10;

    public int PullStopHpPercent { get; set; } = 50;

    public int PullResumeHpPercent { get; set; } = 70;

    public HashSet<uint> BlacklistedFateIds { get; set; } = new HashSet<uint> { 1831u, 1832u, 1914u, 1915u };

    public Dictionary<int, HashSet<uint>> BlacklistedTypeIds { get; set; } = new Dictionary<int, HashSet<uint>>();

    public bool AutoBlacklistOnDeaths { get; set; } = true;

    public int AutoBlacklistDeathCount { get; set; } = 3;

    public HashSet<int> SkippedFateRules { get; set; } = new HashSet<int>();

    public List<FateSortEntry> FateSortOrder { get; set; } = new List<FateSortEntry>();

    [JsonIgnore]
    public HashSet<uint> RuntimeBadObstacleMaps { get; set; } = new HashSet<uint>();

    public uint TargetTradeItemId { get; set; }

    public List<TradeListEntry> TradeList { get; set; } = new List<TradeListEntry>();

    public bool TradeOnCap { get; set; }

    public int TradeThreshold { get; set; } = AfgConstants.BicolorCap;

    public AfterTradeAction AfterTrade { get; set; }

    public AfterRunAction AfterRun { get; set; }

    public string PostRunCommands { get; set; } = string.Empty;

    public GemstoneSpendMode SpendMode { get; set; }

    public int SpendGemsAmount { get; set; } = 1000;

    public int BuyQuantityAmount { get; set; } = 10;

    public int KeepGemstonesReserve { get; set; }

    public bool ApplyClassOnStart { get; set; }

    public List<ClassQueueEntry> ClassQueue { get; set; } = new List<ClassQueueEntry>();

    public AfterClassQueueDone AfterClassQueueDone { get; set; }

    public bool AutoRepair { get; set; }

    public int AutoRepairThresholdPct { get; set; } = 20;

    public RepairMode RepairMode { get; set; }

    public RepairNpc? PreferredRepairNpc { get; set; }

    public bool GmAlertStopRun { get; set; } = true;

    public bool GmAlertToast { get; set; }

    public bool GmAlertChat { get; set; }

    public bool GmAlertSound { get; set; }

    public int GmAlertBeepCount { get; set; } = 3;

    public int GmAlertBeepDurationMs { get; set; } = 250;

    public int GmAlertBeepFrequencyHz { get; set; } = 900;

    public bool GmAlertKillGame { get; set; }

    public List<string> GmAlertCommands { get; set; } = new List<string>();

    public bool PacingEnabled { get; set; } = true;

    public int PacingReactionMinSec { get; set; } = 1;

    public int PacingReactionMaxSec { get; set; } = 6;

    public bool PacingPickVariety { get; set; }

    public bool CombatMovementEnabled { get; set; }

    public int CombatDodgeDelayMin { get; set; }

    public int CombatDodgeDelayMax { get; set; } = 1;

    public int CombatMoveDelayMin { get; set; }

    public int CombatMoveDelayMax { get; set; }

    public int CombatCushionMin { get; set; }

    public int CombatCushionMax { get; set; } = 2;

    public bool HumanizerEnabled { get; set; }

    public int HumanizerFatesBeforeBreak { get; set; } = 20;

    public int HumanizerBreakMinMinutes { get; set; } = 5;

    public int HumanizerBreakMaxMinutes { get; set; } = 10;

    public HumanizerBreakActivity HumanizerBreakActivity { get; set; }

    public List<IdleSpot> HumanizerIdleSpots { get; set; } = new List<IdleSpot>();

    public int HumanizerPauseMinSec { get; set; } = 3;

    public int HumanizerPauseMaxSec { get; set; } = 8;

    public int HumanizerWanderMinMeters { get; set; } = 25;

    public int HumanizerWanderMaxMeters { get; set; } = 80;

    public HashSet<uint> HumanizerCities { get; set; } = new HashSet<uint> { 129u, 132u, 1185u, 1186u };

    public bool AutoConsume { get; set; }

    public bool AutoSummonChocobo { get; set; } = true;

    public int AutoConsumeMinMinutes { get; set; } = 3;

    public List<ConsumableEntry> AutoConsumeItems { get; set; } = new List<ConsumableEntry>();

    public bool DeclinePartyInvites { get; set; }

    public int DeclineInviteDelayMinSec { get; set; } = 2;

    public int DeclineInviteDelayMaxSec { get; set; } = 6;

    public bool DeclineInviteReply { get; set; }

    public PartyInviteReplyChannel DeclineInviteReplyChannel { get; set; }

    public string DeclineInviteReplyMessage { get; set; } = "";

    public string LastSeenChangelogVersion { get; set; } = string.Empty;

    [JsonIgnore]
    public bool HasUnseenChangelog => !string.Equals(LastSeenChangelogVersion, ChangelogData.LatestVersion, StringComparison.Ordinal);

    public void MarkChangelogSeen()
    {
        if (!HasUnseenChangelog)
        {
            return;
        }
        LastSeenChangelogVersion = ChangelogData.LatestVersion;
        Save();
    }

    public bool MigrateGoal()
    {
        var changed = false;
        if (string.IsNullOrEmpty(ModeId))
        {
            ModeId = FateGrindModes.IdForLegacy(Mode);
            StopAfterFatesEnabled |= Mode == GrindMode.RunCount;
            changed = true;
        }
        switch (ModeId)
        {
            case "runcount":
                ModeId = "plainfates";
                StopAfterFatesEnabled = true;
                changed = true;
                break;
            case "timeboxed":
                ModeId = "plainfates";
                StopAfterMinutesEnabled = true;
                changed = true;
                break;
            case "endless":
                ModeId = "plainfates";
                changed = true;
                break;
        }
        return changed;
    }

    public bool MigrateTradeList()
    {
        if (TargetTradeItemId == 0)
        {
            return false;
        }
        var itemId = TargetTradeItemId;
        TargetTradeItemId = 0u;
        if (!TradeList.Exists((TradeListEntry entry) => entry.ItemId == itemId))
        {
            TradeList.Insert(0, new TradeListEntry
            {
                ItemId = itemId
            });
        }
        return true;
    }

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);

    public void SaveDebounced()
    {
        if (EzThrottler.Throttle(AfgConstants.ThrottleKeys.Save, AfgConstants.SaveThrottleMs))
        {
            Save();
        }
    }
}
