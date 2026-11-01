using System;
using System.Collections.Generic;
using AutoFateGrind.Core.Zones;

namespace AutoFateGrind.Core.Tasks;

internal static class HumanizeBreaks
{
    public static bool HasDestination(Configuration cfg)
    {
        if (UsesSpots(cfg))
        {
            return true;
        }
        var all = CityCatalog.All;
        for (var i = 0; i < all.Length; i++)
        {
            if (cfg.HumanizerCities.Contains(all[i].TerritoryId))
            {
                return true;
            }
        }
        return false;
    }

    public static BreakPlan? Pick(Configuration cfg, IdleSpot? previous, Random rng)
    {
        if (UsesSpots(cfg))
        {
            var idleSpot = PickSpot(cfg.HumanizerIdleSpots, previous, rng);
            return new BreakPlan(idleSpot.TerritoryId, TerritoryNames.Of(idleSpot.TerritoryId), idleSpot, Wander: false);
        }
        var list = new List<CityInfo>(CityCatalog.All.Length);
        for (var i = 0; i < CityCatalog.All.Length; i++)
        {
            var cityInfo = CityCatalog.All[i];
            if (cfg.HumanizerCities.Contains(cityInfo.TerritoryId))
            {
                list.Add(cityInfo);
            }
        }
        if (list.Count == 0)
        {
            return null;
        }
        var cityInfo2 = list[rng.Next(list.Count)];
        return new BreakPlan(cityInfo2.TerritoryId, cityInfo2.Name, null, cfg.HumanizerBreakActivity == HumanizerBreakActivity.Wander);
    }

    private static bool UsesSpots(Configuration cfg) => cfg.HumanizerBreakActivity == HumanizerBreakActivity.IdleAtSpot && cfg.HumanizerIdleSpots.Count > 0;

    private static IdleSpot PickSpot(List<IdleSpot> spots, IdleSpot? previous, Random rng)
    {
        var previousIndex = ((previous == null) ? (-1) : spots.IndexOf(previous));
        if (spots.Count == 1 || previousIndex < 0)
        {
            return spots[rng.Next(spots.Count)];
        }
        var pickedIndex = rng.Next(spots.Count - 1);
        return spots[(pickedIndex >= previousIndex) ? (pickedIndex + 1) : pickedIndex];
    }
}

internal readonly record struct FlightReplanPolicy(int GroundTravelGraceMs, float MinDistanceMeters, int MaxReplans);
