using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Game.Ops;

internal static class FoodOps
{
    private static readonly HashSet<uint> ConsumableUiCategories = new HashSet<uint> { 44u, 45u, 46u };

    public const uint WellFedStatusId = 48u;

    public const uint MedicatedStatusId = 49u;

    private const uint HqItemIdOffset = 1000000u;

    private const uint UseActionAnySlotParam = 65535u;

    private static List<ConsumableEntry>? catalog;

    public static IReadOnlyList<ConsumableEntry> Catalog => catalog ?? (catalog = BuildCatalog());

    private static List<ConsumableEntry> BuildCatalog()
    {
        var itemSheet = Svc.Data.GetExcelSheet<Item>();
        if (itemSheet == null)
        {
            return new List<ConsumableEntry>();
        }
        var entries = new List<ConsumableEntry>();
        foreach (var item in itemSheet)
        {
            var itemName = item.Name.ExtractText();
            if (string.IsNullOrWhiteSpace(itemName))
            {
                continue;
            }
            if (item.ItemUICategory.ValueNullable?.RowId is not { } categoryId)
            {
                continue;
            }
            if (!ConsumableUiCategories.Contains(categoryId))
            {
                continue;
            }
            if (item.ItemAction.ValueNullable is not { } itemAction)
            {
                continue;
            }
            uint statusId = itemAction.Data[0];
            if (statusId == WellFedStatusId || statusId == MedicatedStatusId)
            {
                entries.Add(new ConsumableEntry
                {
                    ItemId = item.RowId,
                    Name = itemName,
                    StatusId = statusId,
                    CanBeHq = item.CanBeHq
                });
            }
        }
        return entries.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public unsafe static int ItemCount(uint itemId)
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager != null)
        {
            return inventoryManager->GetInventoryItemCount(itemId, isHq: false, checkEquipped: true, checkArmory: true, 0);
        }
        return 0;
    }

    public static bool IsAvailable(ConsumableEntry e) => (e.CanBeHq && ItemCount(e.ItemId + HqItemIdOffset) >= 1) || ItemCount(e.ItemId) >= 1;

    public static bool HasStatus(uint statusId, float minSeconds)
    {
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return false;
        }
        foreach (var status in localPlayer.StatusList)
        {
            if (status.StatusId == statusId && (minSeconds <= 0f || status.RemainingTime > minSeconds))
            {
                return true;
            }
        }
        return false;
    }

    public static bool AnyNeeded(Configuration cfg)
    {
        var minSeconds = (float)Math.Max(0, cfg.AutoConsumeMinMinutes) * 60f;
        foreach (var autoConsumeItem in cfg.AutoConsumeItems)
        {
            if (!HasStatus(autoConsumeItem.StatusId, minSeconds) && IsAvailable(autoConsumeItem))
            {
                return true;
            }
        }
        return false;
    }

    public unsafe static bool UseConsumable(ConsumableEntry e)
    {
        if (!EzThrottler.Throttle(AfgConstants.ThrottleKeys.FoodUse))
        {
            return false;
        }
        var actionManager = ActionManager.Instance();
        if (actionManager == null)
        {
            return false;
        }
        var hqItemId = e.ItemId + HqItemIdOffset;
        var useItemId = (e.CanBeHq && ItemCount(hqItemId) >= 1) ? hqItemId : ((ItemCount(e.ItemId) >= 1) ? e.ItemId : 0u);
        if (useItemId == 0)
        {
            return false;
        }
        actionManager->UseAction(ActionType.Item, useItemId, 3758096384uL, UseActionAnySlotParam, ActionManager.UseActionMode.None, 0u, null);
        return true;
    }
}
