using System;
using System.Collections.Generic;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;
using clib.Utils;

namespace AutoFateGrind.Core.Game.Fates;

internal static class FateBlacklist
{
    private static readonly Dictionary<BlacklistedFate, string> nameCache = new Dictionary<BlacklistedFate, string>();

    public static bool Contains(Configuration cfg, PublicEvent f)
    {
        if (cfg.BlacklistedFateIds.Contains(f.Id))
        {
            return true;
        }
        if (cfg.BlacklistedTypeIds.TryGetValue((int)f.FateType, out HashSet<uint>? typeIds) && typeIds.Contains(f.Id))
        {
            return true;
        }
        return false;
    }

    public static void ToggleId(Configuration cfg, PublicEvent f)
    {
        HashSet<uint> ids = SetFor(cfg, f.FateType);
        if (!ids.Add(f.Id))
        {
            ids.Remove(f.Id);
        }
        cfg.SaveDebounced();
    }

    public static void Add(Configuration cfg, FateType type, uint fateId)
    {
        if (SetFor(cfg, type).Add(fateId))
        {
            cfg.SaveDebounced();
        }
    }

    public static void Add(Configuration cfg, FateType type, uint[] fateIds)
    {
        HashSet<uint> ids = SetFor(cfg, type);
        bool changed = false;
        for (var i = 0; i < fateIds.Length; i++)
        {
            changed |= ids.Add(fateIds[i]);
        }
        if (changed)
        {
            cfg.SaveDebounced();
        }
    }

    private static HashSet<uint> SetFor(Configuration cfg, FateType type)
    {
        if (!cfg.BlacklistedTypeIds.TryGetValue((int)type, out HashSet<uint>? ids))
        {
            cfg.BlacklistedTypeIds[(int)type] = ids = new HashSet<uint>();
        }
        return ids;
    }

    public static IReadOnlyList<BlacklistedFateGroup> All(Configuration cfg)
    {
        var byName = new Dictionary<string, List<BlacklistedFate>>(StringComparer.OrdinalIgnoreCase);
        foreach (var blacklistedFateId in cfg.BlacklistedFateIds)
        {
            Collect(byName, new BlacklistedFate(FateType.Normal, blacklistedFateId));
        }
        foreach (var (type, ids) in cfg.BlacklistedTypeIds)
        {
            foreach (var fateId in ids)
            {
                Collect(byName, new BlacklistedFate((FateType)type, fateId));
            }
        }
        var groups = new List<BlacklistedFateGroup>(byName.Count);
        foreach (var (name, entries) in byName)
        {
            groups.Add(new BlacklistedFateGroup(name, entries.ToArray()));
        }
        groups.Sort((BlacklistedFateGroup left, BlacklistedFateGroup right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
        return groups;
    }

    private static void Collect(Dictionary<string, List<BlacklistedFate>> byName, BlacklistedFate entry)
    {
        var key = DisplayName(entry);
        if (!byName.TryGetValue(key, out List<BlacklistedFate>? entries))
        {
            byName[key] = entries = new List<BlacklistedFate>();
        }
        if (!entries.Contains(entry))
        {
            entries.Add(entry);
        }
    }

    public static void Remove(Configuration cfg, BlacklistedFateGroup group)
    {
        var changed = false;
        for (var i = 0; i < group.Entries.Length; i++)
        {
            changed |= RemoveEntry(cfg, group.Entries[i]);
        }
        if (changed)
        {
            cfg.SaveDebounced();
        }
    }

    private static bool RemoveEntry(Configuration cfg, BlacklistedFate entry)
    {
        var removed = entry.Type == FateType.Normal && cfg.BlacklistedFateIds.Remove(entry.Id);
        var type = (int)entry.Type;
        if (cfg.BlacklistedTypeIds.TryGetValue(type, out HashSet<uint>? ids) && ids.Remove(entry.Id))
        {
            removed = true;
            if (ids.Count == 0)
            {
                cfg.BlacklistedTypeIds.Remove(type);
            }
        }
        return removed;
    }

    private static string DisplayName(BlacklistedFate entry)
    {
        if (nameCache.TryGetValue(entry, out string? cachedName))
        {
            return cachedName;
        }
        var resolvedName = ResolveName(entry);
        var displayName = (string.IsNullOrWhiteSpace(resolvedName) ? FallbackName(entry) : resolvedName);
        nameCache[entry] = displayName;
        return displayName;
    }

    private static string? ResolveName(BlacklistedFate entry)
    {
        return entry.Type switch
        {
            FateType.Normal => Svc.Data.GetExcelSheet<Fate>()?.GetRowOrDefault(entry.Id)?.Name.ExtractText(),
            FateType.DynamicEvent => Svc.Data.GetExcelSheet<DynamicEvent>()?.GetRowOrDefault(entry.Id)?.Name.ExtractText(),
            FateType.MechaEvent => Svc.Data.GetExcelSheet<WKSMechaEventData>()?.GetRowOrDefault(entry.Id)?.Name.ExtractText(),
            _ => null,
        };
    }

    private static string FallbackName(BlacklistedFate entry)
    {
        if (entry.Type != FateType.Normal)
        {
            return $"{entry.Type} #{entry.Id}";
        }
        return $"FATE #{entry.Id}";
    }
}
