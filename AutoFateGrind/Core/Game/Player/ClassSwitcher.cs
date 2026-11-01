using System.Collections.Generic;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Game.Player;

internal static class ClassSwitcher
{
    public readonly record struct GearsetOption(byte UserIndex, byte JobId, string Name);

    private const byte InvalidGearsetIndex = byte.MaxValue;

    private const int MaxGearsetCount = 100;

    public unsafe static bool IsValidGearset(int index) => RaptureGearsetModule.Instance()->IsValidGearset(index);

    public static bool IsValidUserIndex(byte userIndex) => userIndex >= 1 && IsValidGearset(userIndex - 1);

    public unsafe static byte JobIdForUserIndex(byte userIndex)
    {
        if (!IsValidUserIndex(userIndex))
        {
            return 0;
        }
        var gearset = RaptureGearsetModule.Instance()->GetGearset(userIndex - 1);
        if (gearset != null)
        {
            return gearset->ClassJob;
        }
        return 0;
    }

    public static string JobNameForUserIndex(byte userIndex) => JobNameForJobId(JobIdForUserIndex(userIndex));

    public static string JobNameForJobId(byte jobId)
    {
        if (jobId == 0)
        {
            return "—";
        }
        var classJob = Svc.Data.GetExcelSheet<ClassJob>()?.GetRowOrDefault(jobId);
        var abbreviation = classJob?.Abbreviation.ExtractText();
        if (!string.IsNullOrEmpty(abbreviation))
        {
            return abbreviation;
        }
        return $"#{jobId}";
    }

    public static int UnsyncedLevelForJobId(byte jobId)
    {
        if (jobId == 0)
        {
            return 0;
        }
        var classJob = Svc.Data.GetExcelSheet<ClassJob>()?.GetRowOrDefault(jobId);
        if (!classJob.HasValue)
        {
            return 0;
        }
        return Svc.PlayerState.GetClassJobLevel(classJob.Value);
    }

    public unsafe static int GameMaxLevel
    {
        get
        {
            var playerState = PlayerState.Instance();
            if (playerState != null)
            {
                return playerState->MaxLevel;
            }
            return 100;
        }
    }

    public static bool IsCombatJob(byte jobId)
    {
        if (jobId == 0)
        {
            return false;
        }
        var classJob = Svc.Data.GetExcelSheet<ClassJob>()?.GetRowOrDefault(jobId);
        return classJob?.Role > 0;
    }

    public unsafe static List<GearsetOption> EnumerateGearsets()
    {
        var options = new List<GearsetOption>();
        var gearsetModule = RaptureGearsetModule.Instance();
        if (gearsetModule == null)
        {
            return options;
        }
        for (var i = 0; i < MaxGearsetCount; i++)
        {
            if (!gearsetModule->IsValidGearset(i))
            {
                continue;
            }
            var gearset = gearsetModule->GetGearset(i);
            if (gearset != null)
            {
                var classJob = gearset->ClassJob;
                if (IsCombatJob(classJob))
                {
                    var name = gearset->NameString ?? string.Empty;
                    options.Add(new GearsetOption((byte)(i + 1), classJob, name));
                }
            }
        }
        return options;
    }

    public static int FindActiveEntryIndex(IReadOnlyList<ClassQueueEntry> queue)
    {
        for (var i = 0; i < queue.Count; i++)
        {
            var queueEntry = queue[i];
            if (!IsValidUserIndex(queueEntry.GearsetIndex))
            {
                continue;
            }
            var jobId = JobIdForUserIndex(queueEntry.GearsetIndex);
            if (jobId != 0)
            {
                if (queueEntry.StopAtLevel <= 0)
                {
                    return i;
                }
                if (UnsyncedLevelForJobId(jobId) < queueEntry.StopAtLevel)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    public static bool LevelingJobAtMax(Configuration cfg)
    {
        var jobId = LevelingJobId(cfg);
        return jobId != 0 && UnsyncedLevelForJobId(jobId) >= GameMaxLevel;
    }

    private static byte LevelingJobId(Configuration cfg)
    {
        if (cfg.ApplyClassOnStart && cfg.ClassQueue.Count > 0)
        {
            var activeIndex = FindActiveEntryIndex(cfg.ClassQueue);
            if (activeIndex >= 0)
            {
                return JobIdForUserIndex(cfg.ClassQueue[activeIndex].GearsetIndex);
            }
        }
        return (byte)(Svc.Objects.LocalPlayer?.ClassJob.RowId ?? 0);
    }

    public unsafe static bool TryEquip(ClassQueueEntry entry)
    {
        if (!IsValidUserIndex(entry.GearsetIndex))
        {
            return false;
        }
        var gearsetIndex = entry.GearsetIndex - 1;
        var gearsetModule = RaptureGearsetModule.Instance();
        if (gearsetModule == null)
        {
            return false;
        }
        if (gearsetModule->CurrentGearsetIndex == gearsetIndex)
        {
            return true;
        }
        var equipResult = gearsetModule->EquipGearset(gearsetIndex, 0);
        if (equipResult != 0)
        {
            RunLog.Warning($"EquipGearset({gearsetIndex}) returned {equipResult}");
            return false;
        }
        return true;
    }
}
