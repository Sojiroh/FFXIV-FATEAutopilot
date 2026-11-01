using System;
using System.Collections.Generic;
using System.Text;
using AutoFateGrind.Core.Zones;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Game.Yokai;

internal readonly record struct YokaiStatus(int Medals, bool MinionUnlocked, bool WeaponOwned, byte UnlockedZoneMask)
{
    public bool Reachable => UnlockedZoneMask != 0;
}

internal static class YokaiProgress
{
    private const int RefreshIntervalMs = 500;

    private static readonly YokaiStatus[] statuses = new YokaiStatus[YokaiCatalog.Entries.Length];

    private static readonly ZoneInfo[]?[] zoneCache = new ZoneInfo[YokaiCatalog.Entries.Length][];

    private static readonly string?[] minionNames = new string[YokaiCatalog.Entries.Length];

    private static readonly string?[] zoneNames = new string[YokaiCatalog.Entries.Length];

    private static uint[]? weaponCabinetIds;

    private static ZoneInfo[]? registrySnapshot;

    private static long refreshedAtMs;

    public static bool Ready { get; private set; }

    public static ReadOnlySpan<YokaiStatus> Statuses
    {
        get
        {
            Refresh();
            return statuses;
        }
    }

    public static void Invalidate()
    {
        refreshedAtMs = 0L;
    }

    public static string MinionName(int entryIndex)
    {
        ref string? nameSlot = ref minionNames[entryIndex];
        return nameSlot ?? (nameSlot = ResolveMinionName(YokaiCatalog.Entries[entryIndex].MinionId));
    }

    public static string ZoneNames(int entryIndex)
    {
        DropZoneCacheIfRegistryChanged();
        ref string? namesSlot = ref zoneNames[entryIndex];
        return namesSlot ?? (namesSlot = BuildZoneNames(entryIndex));
    }

    public static bool IsEnabled(Configuration configuration, int entryIndex) => !configuration.YokaiSkippedMinionIds.Contains(YokaiCatalog.Entries[entryIndex].MinionId);

    public static bool IsFarmable(Configuration configuration, int entryIndex)
    {
        var status = Statuses[entryIndex];
        return IsEnabled(configuration, entryIndex) && status.MinionUnlocked && !status.WeaponOwned && status.Reachable && status.Medals < MedalTarget(configuration);
    }

    public static int MedalTarget(Configuration configuration) => Math.Max(1, configuration.TargetYokaiMedals);

    public static int ResolveTargetIndex(Configuration configuration, uint preferredMinionId)
    {
        var summonedIndex = YokaiCatalog.IndexOfMinion(YokaiOps.SummonedMinionId());
        if (summonedIndex >= 0 && IsFarmable(configuration, summonedIndex))
        {
            return summonedIndex;
        }
        var preferredIndex = YokaiCatalog.IndexOfMinion(preferredMinionId);
        if (preferredIndex >= 0 && IsFarmable(configuration, preferredIndex))
        {
            return preferredIndex;
        }
        for (var i = 0; i < statuses.Length; i++)
        {
            if (IsFarmable(configuration, i))
            {
                return i;
            }
        }
        return -1;
    }

    public static uint ResolveTargetMinionId(Configuration configuration, uint preferredMinionId)
    {
        var targetIndex = ResolveTargetIndex(configuration, preferredMinionId);
        if (targetIndex >= 0)
        {
            return YokaiCatalog.Entries[targetIndex].MinionId;
        }
        return 0u;
    }

    public static bool IsComplete(Configuration configuration) => Ready && ResolveTargetIndex(configuration, 0u) < 0;

    public static IReadOnlyList<ZoneInfo> ZonesFor(int entryIndex)
    {
        if (entryIndex < 0)
        {
            return Array.Empty<ZoneInfo>();
        }
        Refresh();
        ZoneInfo[]?[] cache = zoneCache;
        return cache[entryIndex] ??= BuildZones(entryIndex);
    }

    public static (int Collected, int Needed) Totals(Configuration configuration)
    {
        var target = MedalTarget(configuration);
        var statusSpan = Statuses;
        var collected = 0;
        var needed = 0;
        for (var i = 0; i < statusSpan.Length; i++)
        {
            var status = statusSpan[i];
            if (IsEnabled(configuration, i) && status.MinionUnlocked && !status.WeaponOwned)
            {
                needed += target;
                collected += Math.Min(status.Medals, target);
            }
        }
        return (Collected: collected, Needed: needed);
    }

    public static (int Owned, int WeaponsLeft) Ownership()
    {
        var statusSpan = Statuses;
        var owned = 0;
        var weaponsLeft = 0;
        for (var i = 0; i < statusSpan.Length; i++)
        {
            if (statusSpan[i].MinionUnlocked)
            {
                owned++;
                if (!statusSpan[i].WeaponOwned)
                {
                    weaponsLeft++;
                }
            }
        }
        return (Owned: owned, WeaponsLeft: weaponsLeft);
    }

    public static string DescribeRoster(Configuration configuration)
    {
        var statusSpan = Statuses;
        var target = MedalTarget(configuration);
        var builder = new StringBuilder(statusSpan.Length * 32);
        for (var i = 0; i < statusSpan.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(" | ");
            }
            builder.Append(MinionName(i)).Append(' ').Append(statusSpan[i].Medals)
                .Append('/')
                .Append(target);
            string? reason = ExclusionReason(configuration, i);
            if (reason != null)
            {
                builder.Append(" (").Append(reason).Append(')');
            }
        }
        return builder.ToString();
    }

    public static string CompletionSummary(Configuration configuration)
    {
        var statusSpan = Statuses;
        var target = MedalTarget(configuration);
        var completed = 0;
        var locked = 0;
        var weaponOwned = 0;
        var unticked = 0;
        var unreachable = 0;
        for (var i = 0; i < statusSpan.Length; i++)
        {
            var status = statusSpan[i];
            if (!status.MinionUnlocked)
            {
                locked++;
            }
            else if (status.WeaponOwned)
            {
                weaponOwned++;
            }
            else if (!IsEnabled(configuration, i))
            {
                unticked++;
            }
            else if (!status.Reachable)
            {
                unreachable++;
            }
            else if (status.Medals >= target)
            {
                completed++;
            }
        }
        return $"{completed} at {target} medals, {weaponOwned} weapon owned, {locked} minion locked, {unreachable} without an attuned aetheryte, {unticked} unticked";
    }

    private static string? ExclusionReason(Configuration configuration, int entryIndex)
    {
        var status = statuses[entryIndex];
        if (!status.MinionUnlocked)
        {
            return "minion locked";
        }
        if (status.WeaponOwned)
        {
            return "weapon owned";
        }
        if (!IsEnabled(configuration, entryIndex))
        {
            return "unticked";
        }
        if (!status.Reachable)
        {
            return "no attuned aetheryte";
        }
        if (status.Medals < MedalTarget(configuration))
        {
            return null;
        }
        return "done";
    }

    private unsafe static void Refresh()
    {
        var tickCount = Environment.TickCount64;
        if (tickCount - refreshedAtMs < RefreshIntervalMs)
        {
            return;
        }
        var inventoryManager = InventoryManager.Instance();
        var uiState = UIState.Instance();
        if (inventoryManager == null || uiState == null || Svc.Objects.LocalPlayer == null)
        {
            return;
        }
        refreshedAtMs = tickCount;
        DropZoneCacheIfRegistryChanged();
        var cabinetIds = weaponCabinetIds ?? (weaponCabinetIds = ResolveWeaponCabinetIds());
        var cabinetLoaded = uiState->Cabinet.IsCabinetLoaded();
        bool filterByAttunement = ZoneStateReader.AnyAetheryteAttuned();
        var entries = YokaiCatalog.Entries;
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            bool weaponOwned = inventoryManager->GetInventoryItemCount(entry.WeaponItemId, isHq: false, checkEquipped: true, checkArmory: true, 0) > 0 || (cabinetLoaded && cabinetIds[i] != 0 && uiState->Cabinet.IsItemInCabinet(cabinetIds[i]));
            var zoneMask = UnlockedZoneMask(entry.ZoneIds, filterByAttunement);
            if (zoneMask != statuses[i].UnlockedZoneMask)
            {
                zoneCache[i] = null;
            }
            statuses[i] = new YokaiStatus(inventoryManager->GetInventoryItemCount(entry.MedalItemId, isHq: false, checkEquipped: true, checkArmory: true, 0), uiState->IsCompanionUnlocked(entry.MinionId), weaponOwned, zoneMask);
        }
        Ready = true;
    }

    private static byte UnlockedZoneMask(uint[] zoneIds, bool filterByAttunement)
    {
        byte mask = 0;
        for (var i = 0; i < zoneIds.Length; i++)
        {
            if (!filterByAttunement || ZoneStateReader.IsTerritoryUnlocked(zoneIds[i]))
            {
                mask |= (byte)(1 << i);
            }
        }
        return mask;
    }

    private static void DropZoneCacheIfRegistryChanged()
    {
        var zones = ZoneRegistry.Zones;
        if (zones == registrySnapshot)
        {
            return;
        }
        registrySnapshot = zones;
        Array.Clear(zoneCache);
        Array.Clear(zoneNames);
    }

    private static string BuildZoneNames(int entryIndex)
    {
        var zoneIds = YokaiCatalog.Entries[entryIndex].ZoneIds;
        var names = new List<string>(zoneIds.Length);
        for (var i = 0; i < zoneIds.Length; i++)
        {
            var zone = FindZone(zoneIds[i]);
            if (zone != null)
            {
                names.Add(zone.Name);
            }
        }
        return string.Join(", ", names);
    }

    private static ZoneInfo[] BuildZones(int entryIndex)
    {
        var zoneIds = YokaiCatalog.Entries[entryIndex].ZoneIds;
        var unlockedZoneMask = statuses[entryIndex].UnlockedZoneMask;
        var zones = new List<ZoneInfo>(zoneIds.Length);
        for (var i = 0; i < zoneIds.Length; i++)
        {
            if ((unlockedZoneMask & (1 << i)) != 0)
            {
                var zone = FindZone(zoneIds[i]);
                if (zone != null)
                {
                    zones.Add(zone);
                }
            }
        }
        return zones.ToArray();
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

    private static uint[] ResolveWeaponCabinetIds()
    {
        var entries = YokaiCatalog.Entries;
        var cabinetIds = new uint[entries.Length];
        var cabinetSheet = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Cabinet>();
        for (var i = 0; i < cabinetSheet.Count; i++)
        {
            var cabinetRow = cabinetSheet.GetRowAt(i);
            for (var j = 0; j < entries.Length; j++)
            {
                if (cabinetRow.Item.RowId == entries[j].WeaponItemId)
                {
                    cabinetIds[j] = cabinetRow.RowId;
                }
            }
        }
        return cabinetIds;
    }

    private static string ResolveMinionName(uint minionId)
    {
        string? minionName = Svc.Data.GetExcelSheet<Companion>().GetRowOrDefault(minionId)?.Singular.ExtractText();
        if (!string.IsNullOrWhiteSpace(minionName))
        {
            return minionName;
        }
        return minionId.ToString();
    }
}
