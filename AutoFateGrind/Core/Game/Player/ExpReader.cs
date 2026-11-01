using System;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Game.Player;

internal static class ExpReader
{
    public readonly record struct Snapshot(uint JobId, string JobAbbr, int Level, long ExpIntoLevel, bool IsMax);

    private static long[]? cumulativeByLevel;

    public static Snapshot? Read()
    {
        try
        {
            if (Svc.Objects.LocalPlayer == null)
            {
                return null;
            }
            var playerState = Svc.PlayerState;
            var classJob = playerState.ClassJob;
            if (classJob.RowId == 0)
            {
                return null;
            }
            if (classJob.ValueNullable is not { } job)
            {
                return null;
            }
            var level = playerState.Level;
            if (level <= 0)
            {
                return null;
            }
            var gameMaxLevel = ClassSwitcher.GameMaxLevel;
            var atMaxLevel = level >= gameMaxLevel;
            long expIntoLevel = (atMaxLevel ? 0 : playerState.GetClassJobExperience(job));
            var jobAbbr = job.Abbreviation.ExtractText() ?? string.Empty;
            return new Snapshot(classJob.RowId, jobAbbr, level, expIntoLevel, atMaxLevel);
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "ExpReader.Read failed");
            return null;
        }
    }

    public static long Cumulative(Snapshot s) => CumulativeAtLevelStart(s.Level) + s.ExpIntoLevel;

    public static long? CumulativeExp()
    {
        if (Read() is not { } current)
        {
            return null;
        }
        return Cumulative(current);
    }

    public static long? ExpToCap()
    {
        if (Read() is not { } current || current.IsMax)
        {
            return null;
        }
        var capStart = CumulativeAtLevelStart(ClassSwitcher.GameMaxLevel);
        return Math.Max(0L, capStart - Cumulative(current));
    }

    private static long CumulativeAtLevelStart(int level)
    {
        long[]? table = EnsureTable();
        if (table == null || level < 1)
        {
            return 0L;
        }
        if (level >= table.Length)
        {
            level = table.Length - 1;
        }
        return table[level];
    }

    private static long[]? EnsureTable()
    {
        if (cumulativeByLevel != null)
        {
            return cumulativeByLevel;
        }
        var paramGrowSheet = Svc.Data.GetExcelSheet<ParamGrow>();
        if (paramGrowSheet == null)
        {
            return null;
        }
        var maxLevel = Math.Max(1, ClassSwitcher.GameMaxLevel);
        var table = new long[maxLevel + 1];
        var cumulative = 0L;
        for (var i = 1; i < maxLevel; i++)
        {
            var row = paramGrowSheet.GetRowOrDefault((uint)i);
            var expToNext = row is { } paramGrow ? paramGrow.ExpToNext : 0;
            if (expToNext > 0)
            {
                cumulative += expToNext;
            }
            table[i + 1] = cumulative;
        }
        cumulativeByLevel = table;
        return table;
    }
}
