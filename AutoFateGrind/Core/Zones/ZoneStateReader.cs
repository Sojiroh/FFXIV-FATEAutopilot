using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Aetherytes;
using Dalamud.Game.ClientState.Fates;
using ECommons.DalamudServices;

namespace AutoFateGrind.Core.Zones;

internal static class ZoneStateReader
{
    private static HashSet<uint>? attunedAetheryteCache;

    private static long attunedCacheTickMs;

    private const int AttunedCacheLifetimeMs = 1000;

    public static void Refresh(ZoneInfo zone)
    {
        zone.Unlocked = IsTerritoryUnlocked(zone.TerritoryId);
        zone.ActiveFateCount = ((Svc.ClientState.TerritoryType == zone.TerritoryId) ? CountActiveFatesInCurrentZone() : 0);
    }

    public static bool IsTerritoryUnlocked(uint territoryId)
    {
        var attuned = AttunedSet();
        var attunableIds = ZoneAetherytes.AttunableIdsIn(territoryId);
        for (var i = 0; i < attunableIds.Length; i++)
        {
            if (attuned.Contains(attunableIds[i]))
            {
                return true;
            }
        }
        if (ZoneAetherytes.TryFindGateway(territoryId, out var gateway))
        {
            return attuned.Contains(gateway.AetheryteId);
        }
        return false;
    }

    public static bool AnyAetheryteAttuned() => AttunedSet().Count > 0;

    private static HashSet<uint> AttunedSet()
    {
        var tickCount = Environment.TickCount64;
        if (attunedAetheryteCache == null || tickCount - attunedCacheTickMs > AttunedCacheLifetimeMs)
        {
            attunedAetheryteCache = BuildAttunedSet();
            attunedCacheTickMs = tickCount;
        }
        return attunedAetheryteCache;
    }

    private static HashSet<uint> BuildAttunedSet()
    {
        var attuned = new HashSet<uint>(64);
        foreach (IAetheryteEntry aetheryte in Svc.AetheryteList)
        {
            if (aetheryte.AetheryteId != 0)
            {
                attuned.Add(aetheryte.AetheryteId);
            }
        }
        return attuned;
    }

    private static int CountActiveFatesInCurrentZone()
    {
        var activeCount = 0;
        foreach (var fate in Svc.Fates)
        {
            if (fate.State == FateState.Running)
            {
                activeCount++;
            }
        }
        return activeCount;
    }
}
