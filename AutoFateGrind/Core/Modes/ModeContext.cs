using System;
using System.Collections.Generic;
using AutoFateGrind.Core.Zones;

namespace AutoFateGrind.Core.Modes;

public readonly struct ModeContext
{
    public int CompletedCount { get; init; }

    public IReadOnlyList<ZoneInfo> Zones { get; init; }

    public TimeSpan Elapsed { get; init; }

    public int LevelsGained { get; init; }

    public bool LevelingJobAtMax { get; init; }
}

public interface IFateGrindMode
{
    string Id { get; }

    string DisplayName { get; }

    string Description { get; }

    bool IsComplete(ModeContext ctx);

    string? GetRemainingDisplay(ModeContext ctx)
    {
        return null;
    }

    bool PlansZones => false;

    IReadOnlyList<ZoneInfo> PlanZones(Configuration cfg)
    {
        return Array.Empty<ZoneInfo>();
    }

    bool AcceptsZone(uint territoryId)
    {
        return true;
    }

    bool IsZoneDone(uint territoryId)
    {
        return false;
    }

    string ZoneDoneReason(uint territoryId)
    {
        return "nothing left to earn here";
    }
}

public static class RunLimits
{
    public const int MaxFates = 9999;

    public const int MaxMinutes = 1440;

    public const int MaxLevels = 99;

    public const int MaxFatesPerZone = 999;

    public static bool Reached(Configuration cfg, ModeContext ctx)
    {
        if ((!cfg.StopAfterFatesEnabled || ctx.CompletedCount < FateCap(cfg)) && (!cfg.StopAfterMinutesEnabled || !(ctx.Elapsed >= TimeSpan.FromMinutes(MinuteCap(cfg)))))
        {
            if (cfg.StopAfterLevelsEnabled)
            {
                if (ctx.LevelsGained < LevelCap(cfg))
                {
                    return ctx.LevelingJobAtMax;
                }
                return true;
            }
            return false;
        }
        return true;
    }

    public static bool Any(Configuration cfg)
    {
        if (!cfg.StopAfterFatesEnabled && !cfg.StopAfterMinutesEnabled)
        {
            return cfg.StopAfterLevelsEnabled;
        }
        return true;
    }

    public static bool ZoneShareDone(Configuration cfg, ZoneInfo zone)
    {
        if (cfg.FatesPerZoneEnabled)
        {
            return zone.CompletedThisRun >= FatesPerZoneCap(cfg);
        }
        return false;
    }

    public static int FateCap(Configuration cfg) => Math.Clamp(cfg.TargetFateCount, 1, MaxFates);

    public static int MinuteCap(Configuration cfg) => Math.Clamp(cfg.TargetMinutes, 1, MaxMinutes);

    public static int LevelCap(Configuration cfg) => Math.Clamp(cfg.TargetLevels, 1, MaxLevels);

    public static int FatesPerZoneCap(Configuration cfg) => Math.Clamp(cfg.TargetFatesPerZone, 1, MaxFatesPerZone);
}
