using System;
using System.Collections.Generic;
using System.Text;
using AutoFateGrind.Core.Zones;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Game.Items;

internal enum ItemGoalBlocker : byte
{
    None,
    NeedQuest,
    NeedZenith,
    AllCollected
}

internal static class ItemGoalProgress
{
    private const int RefreshIntervalMs = 500;

    private const int MainHandSlot = 0;

    private const int OffHandSlot = 1;

    private const uint ZenithRelicRow = 1u;

    private static readonly Dictionary<uint, string> itemNames = new Dictionary<uint, string>();

    private static readonly Dictionary<uint, int> counts = new Dictionary<uint, int>();

    private static readonly List<ZoneInfo> zoneScratch = new List<ZoneInfo>();

    private static readonly StringBuilder describeBuilder = new StringBuilder();

    private static HashSet<uint>? zenithItemIds;

    private static long refreshedAtMs;

    public static int WeaponCount(Configuration cfg) => Math.Clamp(cfg.TargetRelicCount, 1, 20);

    public static ItemGoalDropStatus Status(ItemGoalDefinition goal, int dropIndex, Configuration cfg)
    {
        Refresh(goal);
        var drop = goal.Drops[dropIndex];
        counts.TryGetValue(drop.ItemId, out var have);
        return new ItemGoalDropStatus(drop.ItemId, have, drop.PerWeapon * WeaponCount(cfg));
    }

    public static (int Have, int Need) Totals(ItemGoalDefinition goal, Configuration cfg)
    {
        var have = 0;
        var need = 0;
        for (var i = 0; i < goal.Drops.Length; i++)
        {
            var status = Status(goal, i, cfg);
            have += Math.Min(status.Have, status.Need);
            need += status.Need;
        }
        return (Have: have, Need: need);
    }

    public static int ItemsLeft(ItemGoalDefinition goal, Configuration cfg)
    {
        var (have, need) = Totals(goal, cfg);
        return Math.Max(0, need - have);
    }

    public static bool AllCollected(ItemGoalDefinition goal, Configuration cfg)
    {
        for (var i = 0; i < goal.Drops.Length; i++)
        {
            if (!Status(goal, i, cfg).Done)
            {
                return false;
            }
        }
        return true;
    }

    public unsafe static bool IsAvailable(ItemGoalDefinition goal)
    {
        return goal.Gate switch
        {
            ItemGoalGate.QuestAccepted => QuestManager.Instance()->IsQuestAccepted(goal.QuestId),
            ItemGoalGate.ZenithEquipped => IsZenithEquipped(),
            _ => true,
        };
    }

    public static ItemGoalBlocker Blocker(ItemGoalDefinition goal, Configuration cfg)
    {
        if (!IsAvailable(goal))
        {
            if (goal.Gate != ItemGoalGate.ZenithEquipped)
            {
                return ItemGoalBlocker.NeedQuest;
            }
            return ItemGoalBlocker.NeedZenith;
        }
        if (!AllCollected(goal, cfg))
        {
            return ItemGoalBlocker.None;
        }
        return ItemGoalBlocker.AllCollected;
    }

    public static IReadOnlyList<ZoneInfo> PlanZones(ItemGoalDefinition goal, Configuration cfg)
    {
        zoneScratch.Clear();
        var zones = ZoneRegistry.Zones;
        foreach (var zone in zones)
        {
            if (DropsHere(goal, zone.TerritoryId) && !IsZoneDone(goal, zone.TerritoryId, cfg))
            {
                zoneScratch.Add(zone);
            }
        }
        return [.. zoneScratch];
    }

    public static bool DropsHere(ItemGoalDefinition goal, uint territoryId)
    {
        for (var i = 0; i < goal.Drops.Length; i++)
        {
            if (Array.IndexOf(goal.Drops[i].ZoneIds, territoryId) >= 0)
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsZoneDone(ItemGoalDefinition goal, uint territoryId, Configuration cfg)
    {
        var anyDrop = false;
        for (var i = 0; i < goal.Drops.Length; i++)
        {
            if (Array.IndexOf(goal.Drops[i].ZoneIds, territoryId) >= 0)
            {
                anyDrop = true;
                if (!Status(goal, i, cfg).Done)
                {
                    return false;
                }
            }
        }
        return anyDrop;
    }

    public static string ItemName(uint itemId)
    {
        if (itemNames.TryGetValue(itemId, out string? cachedName))
        {
            return cachedName;
        }
        string? itemName = Svc.Data.GetExcelSheet<Item>().GetRowOrDefault(itemId)?.Name.ExtractText();
        itemName = (string.IsNullOrWhiteSpace(itemName) ? itemId.ToString() : itemName);
        itemNames[itemId] = itemName;
        return itemName;
    }

    public static string QuestName(uint questId)
    {
        string? questName = Svc.Data.GetExcelSheet<Quest>().GetRowOrDefault(questId)?.Name.ExtractText();
        if (!string.IsNullOrWhiteSpace(questName))
        {
            return questName.Trim();
        }
        return questId.ToString();
    }

    public static string ZoneNames(ItemGoalDefinition goal, int dropIndex)
    {
        var zoneIds = goal.Drops[dropIndex].ZoneIds;
        describeBuilder.Clear();
        for (var i = 0; i < zoneIds.Length; i++)
        {
            var zone = FindZone(zoneIds[i]);
            if (zone != null)
            {
                if (describeBuilder.Length > 0)
                {
                    describeBuilder.Append(", ");
                }
                describeBuilder.Append(zone.Name);
            }
        }
        return describeBuilder.ToString();
    }

    public static string Describe(ItemGoalDefinition goal, Configuration cfg)
    {
        describeBuilder.Clear();
        for (var i = 0; i < goal.Drops.Length; i++)
        {
            var status = Status(goal, i, cfg);
            if (i > 0)
            {
                describeBuilder.Append(" | ");
            }
            describeBuilder.Append(ItemName(status.ItemId)).Append(' ').Append(status.Have)
                .Append('/')
                .Append(status.Need);
        }
        return describeBuilder.ToString();
    }

    public static void Invalidate()
    {
        refreshedAtMs = 0L;
    }

    private unsafe static void Refresh(ItemGoalDefinition goal)
    {
        var tickCount = Environment.TickCount64;
        if (tickCount - refreshedAtMs < RefreshIntervalMs && counts.Count > 0)
        {
            return;
        }
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
        {
            return;
        }
        refreshedAtMs = tickCount;
        var definitions = ItemGoalCatalog.Definitions;
        for (var i = 0; i < definitions.Length; i++)
        {
            var drops = definitions[i].Drops;
            for (var j = 0; j < drops.Length; j++)
            {
                counts[drops[j].ItemId] = inventoryManager->GetInventoryItemCount(drops[j].ItemId, isHq: false, checkEquipped: true, checkArmory: true, 0);
            }
        }
    }

    private unsafe static bool IsZenithEquipped()
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
        {
            return false;
        }
        var equipped = inventoryManager->GetInventoryContainer(InventoryType.EquippedItems);
        if (equipped == null)
        {
            return false;
        }
        var zenithIds = zenithItemIds ?? (zenithItemIds = ResolveZenithItemIds());
        return zenithIds.Contains(equipped->GetInventorySlot(MainHandSlot)->ItemId) || zenithIds.Contains(equipped->GetInventorySlot(OffHandSlot)->ItemId);
    }

    private static HashSet<uint> ResolveZenithItemIds()
    {
        var itemIds = new HashSet<uint>(12);
        if (Svc.Data.GetExcelSheet<RelicItem>().GetRowOrDefault(ZenithRelicRow) is not { } relic)
        {
            return itemIds;
        }
        itemIds.Add(relic.GladiatorItem.RowId);
        itemIds.Add(relic.PugilistItem.RowId);
        itemIds.Add(relic.MarauderItem.RowId);
        itemIds.Add(relic.LancerItem.RowId);
        itemIds.Add(relic.ArcherItem.RowId);
        itemIds.Add(relic.ConjurerItem.RowId);
        itemIds.Add(relic.ThaumaturgeItem.RowId);
        itemIds.Add(relic.ArcanistSMNItem.RowId);
        itemIds.Add(relic.ArcanistSCHItem.RowId);
        itemIds.Add(relic.ShieldItem.RowId);
        itemIds.Add(relic.RogueItem.RowId);
        itemIds.Remove(0u);
        return itemIds;
    }

    private static ZoneInfo? FindZone(uint territoryId)
    {
        var zones = ZoneRegistry.Zones;
        for (var i = 0; i < zones.Length; i++)
        {
            if (zones[i].TerritoryId == territoryId)
            {
                return zones[i];
            }
        }
        return null;
    }
}
