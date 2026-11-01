using System;
using System.Collections.Generic;
using System.Linq;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Game.Fates;

internal readonly record struct FateCatalogEntry(string Name, uint[] FateIds);

internal static class FateCatalog
{
    private static FateCatalogEntry[] entries = Array.Empty<FateCatalogEntry>();

    private static string[] labels = Array.Empty<string>();

    public static FateCatalogEntry[] All
    {
        get
        {
            EnsureLoaded();
            return entries;
        }
    }

    public static string[] Labels
    {
        get
        {
            EnsureLoaded();
            return labels;
        }
    }

    private static void EnsureLoaded()
    {
        if (entries.Length != 0)
        {
            return;
        }
        var fateSheet = Svc.Data.GetExcelSheet<Fate>();
        if (fateSheet == null)
        {
            return;
        }
        var byName = new Dictionary<string, (byte Level, List<uint> FateIds)>(StringComparer.OrdinalIgnoreCase);
        foreach (var fate in fateSheet)
        {
            if (fate.EurekaFate != 0)
            {
                continue;
            }
            var fateName = fate.Name.ExtractText();
            if (string.IsNullOrWhiteSpace(fateName))
            {
                continue;
            }
            if (!byName.TryGetValue(fateName, out var group))
            {
                byName[fateName] = group = (fate.ClassJobLevel, new List<uint>());
            }
            group.FateIds.Add(fate.RowId);
        }
        var sorted = byName.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).ToArray();
        var catalogEntries = new FateCatalogEntry[sorted.Length];
        var catalogLabels = new string[sorted.Length];
        for (var i = 0; i < sorted.Length; i++)
        {
            var (name, group) = sorted[i];
            catalogEntries[i] = new FateCatalogEntry(name, group.FateIds.ToArray());
            catalogLabels[i] = group.Level > 0 ? $"{name}  (Lv {group.Level})" : name;
        }
        entries = catalogEntries;
        labels = catalogLabels;
    }
}
