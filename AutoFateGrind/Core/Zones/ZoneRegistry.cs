using System;
using System.Collections.Generic;
using System.Linq;
using ECommons.DalamudServices;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Zones;

public static class ZoneRegistry
{
    private static ZoneInfo[]? cached;

    public static ZoneInfo[] Zones => cached ?? (cached = LoadFromLumina());

    public static IEnumerable<ZoneInfo> ByExpansion(ExpansionKind exp) => Zones.Where(z => z.Expansion == exp);

    public static void Invalidate()
    {
        cached = null;
    }

    private static ZoneInfo[] LoadFromLumina()
    {
        ExcelSheet<TerritoryType>? territorySheet = Svc.Data.GetExcelSheet<TerritoryType>();
        if (territorySheet == null)
        {
            return Array.Empty<ZoneInfo>();
        }
        var zones = new List<ZoneInfo>(80);
        foreach (var territory in territorySheet)
        {
            if (IsFateZone(territory))
            {
                var name = territory.PlaceName.ValueNullable?.Name.ExtractText() ?? "";
                if (!string.IsNullOrWhiteSpace(name))
                {
                    zones.Add(new ZoneInfo
                    {
                        TerritoryId = territory.RowId,
                        Name = name,
                        Expansion = ExpansionKindExtensions.FromExVersion(territory.ExVersion.RowId),
                        MinLevel = 1
                    });
                }
            }
        }
        return zones.OrderBy(z => z.Expansion).ThenBy(z => z.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    internal const byte StandardFieldUse = 1;

    private static bool IsFateZone(TerritoryType t)
    {
        if (t.RowId == 0)
        {
            return false;
        }
        if (t.IsPvpZone)
        {
            return false;
        }
        if (t.ExVersion.RowId > 5)
        {
            return false;
        }
        if (t.TerritoryIntendedUse.ValueNullable?.RowId != StandardFieldUse)
        {
            return false;
        }
        return true;
    }
}
