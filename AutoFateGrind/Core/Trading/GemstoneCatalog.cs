using System;
using System.Collections.Generic;
using System.Linq;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Trading;

public sealed record GemstoneTradeItem(uint ItemId, string ItemName, uint CostPerOne, uint[] ShopRowIds, bool IsCollectible, uint StackSize);

public static class GemstoneCatalog
{
    public const uint BicolorGemstoneItemId = 26807u;

    private static GemstoneTradeItem[]? cached;

    public static GemstoneTradeItem[] All => cached ?? (cached = LoadFromLumina());

    public static GemstoneTradeItem? FindById(uint itemId) => Array.Find(All, i => i.ItemId == itemId);

    public unsafe static int CurrentWalletCount()
    {
        var inventory = InventoryManager.Instance();
        if (inventory != null)
        {
            return inventory->GetInventoryItemCount(BicolorGemstoneItemId, isHq: false, checkEquipped: true, checkArmory: true, 0);
        }
        return 0;
    }

    public unsafe static bool TryCurrentWalletCount(out int count)
    {
        var inventory = InventoryManager.Instance();
        if (inventory == null)
        {
            count = 0;
            return false;
        }
        count = inventory->GetInventoryItemCount(BicolorGemstoneItemId, isHq: false, checkEquipped: true, checkArmory: true, 0);
        return true;
    }

    public static int ComputeBuyQuantity(int wallet, uint costPerOne, int need, int spentThisTrade)
    {
        var cost = (int)costPerOne;
        if (cost <= 0 || need <= 0)
        {
            return 0;
        }
        var cfg = Plugin.Cfg;
        var budget = Math.Max(0, wallet - cfg.KeepGemstonesReserve);
        if (cfg.SpendMode == GemstoneSpendMode.SpendGems)
        {
            budget = Math.Min(budget, Math.Max(0, cfg.SpendGemsAmount - spentThisTrade));
        }
        var affordable = Math.Min(budget / cost, need);
        if (cfg.SpendMode != GemstoneSpendMode.BuyQuantity)
        {
            return affordable;
        }
        return Math.Min(affordable, cfg.BuyQuantityAmount);
    }

    private static GemstoneTradeItem[] LoadFromLumina()
    {
        ExcelSheet<SpecialShop>? shopSheet = Svc.Data.GetExcelSheet<SpecialShop>();
        ExcelSheet<Item>? itemSheet = Svc.Data.GetExcelSheet<Item>();
        if (shopSheet == null || itemSheet == null)
        {
            return Array.Empty<GemstoneTradeItem>();
        }
        var entriesByItemId = new Dictionary<uint, (uint cost, string name, List<uint> shopIds, bool collectible, uint stackSize)>(128);
        foreach (var shop in shopSheet)
        {
            foreach (var shopItem in shop.Item)
            {
                var itemCosts = shopItem.ItemCosts;
                var receiveItems = shopItem.ReceiveItems;
                if (itemCosts.Count == 0 || receiveItems.Count == 0)
                {
                    continue;
                }
                var gemCost = 0u;
                foreach (var itemCost in itemCosts)
                {
                    if (itemCost.ItemCost.RowId == BicolorGemstoneItemId)
                    {
                        gemCost = itemCost.CurrencyCost;
                        break;
                    }
                }
                if (gemCost == 0)
                {
                    continue;
                }
                foreach (var receiveItem in receiveItems)
                {
                    var receivedItemId = receiveItem.Item.RowId;
                    if (receivedItemId == 0)
                    {
                        continue;
                    }
                    if (!entriesByItemId.TryGetValue(receivedItemId, out var entry))
                    {
                        if (itemSheet.GetRowOrDefault(receivedItemId) is not { } item)
                        {
                            continue;
                        }
                        var itemName = item.Name.ExtractText();
                        if (string.IsNullOrWhiteSpace(itemName))
                        {
                            continue;
                        }
                        entry = (gemCost, itemName, new List<uint>(), item.IsUnique || Svc.UnlockState.IsItemUnlockable(item), Math.Max(1u, item.StackSize));
                        entriesByItemId[receivedItemId] = entry;
                    }
                    if (!entry.shopIds.Contains(shop.RowId))
                    {
                        entry.shopIds.Add(shop.RowId);
                    }
                }
            }
        }
        return (from i in entriesByItemId.Select(kv => new GemstoneTradeItem(kv.Key, kv.Value.name, kv.Value.cost, kv.Value.shopIds.ToArray(), kv.Value.collectible, kv.Value.stackSize))
                orderby i.CostPerOne
                select i).ThenBy(i => i.ItemName, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
