using System.Collections.Generic;
using System.Linq;

namespace AutoFateGrind.Core.Zones;

internal static class ZoneSelection
{
    public static bool GoalPlansZones(Configuration cfg) => cfg.ActiveMode.PlansZones;

    public static bool GoalNeedsRankedZones(Configuration cfg) => cfg.ActiveMode.Id == "sharedfateranks";

    public static bool IsYokaiGoal(Configuration cfg) => cfg.ActiveMode.Id == "yokaimedals";

    public static bool IsSkippedByGoal(Configuration cfg, uint territoryId) => !cfg.ActiveMode.AcceptsZone(territoryId);

    public static IReadOnlyList<ZoneInfo> ResolveStartList(Configuration cfg)
    {
        if (GoalPlansZones(cfg))
        {
            return cfg.ActiveMode.PlanZones(cfg);
        }
        var byId = ZoneRegistry.Zones.ToDictionary(z => z.TerritoryId);
        return (from id in cfg.SelectedZones
                where byId.ContainsKey(id) && !IsSkippedByGoal(cfg, id)
                select byId[id]).ToList();
    }

    public static int AutoPick(Configuration cfg)
    {
        var zones = ZoneRegistry.Zones;
        for (ExpansionKind expansionKind = ExpansionKind.DT; expansionKind >= ExpansionKind.ARR; expansionKind--)
        {
            var pickedCount = 0;
            for (var i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                if (zone.Expansion == expansionKind && !IsSkippedByGoal(cfg, zone.TerritoryId) && !cfg.ActiveMode.IsZoneDone(zone.TerritoryId) && ZoneStateReader.IsTerritoryUnlocked(zone.TerritoryId))
                {
                    if (pickedCount == 0)
                    {
                        cfg.SelectedZones.Clear();
                    }
                    cfg.SelectedZones.Add(zone.TerritoryId);
                    pickedCount++;
                }
            }
            if (pickedCount > 0)
            {
                cfg.SaveDebounced();
                return pickedCount;
            }
        }
        return 0;
    }
}
