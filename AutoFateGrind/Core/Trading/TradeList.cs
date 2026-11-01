using System;
using System.Collections.Generic;
using AutoFateGrind.Core.Zones;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Trading;

public static class TradeList
{
    public unsafe static int HeldCount(uint itemId)
    {
        var inventory = InventoryManager.Instance();
        if (inventory != null)
        {
            return inventory->GetInventoryItemCount(itemId, isHq: false, checkEquipped: true, checkArmory: true, 0);
        }
        return 0;
    }

    public static bool IsLearned(uint itemId)
    {
        if (Svc.Data.GetExcelSheet<Item>().GetRowOrDefault(itemId) is { } item)
        {
            return Svc.UnlockState.IsItemUnlocked(item);
        }
        return false;
    }

    public static int RemainingNeed(GemstoneTradeItem item, int stopAtCount)
    {
        var held = HeldCount(item.ItemId);
        if (item.IsCollectible)
        {
            if (held <= 0 && !IsLearned(item.ItemId))
            {
                return 1;
            }
            return 0;
        }
        if (stopAtCount > 0)
        {
            return Math.Max(0, stopAtCount - held);
        }
        return int.MaxValue;
    }

    public static TradePlan? Plan(int wallet, uint? preferTerritoryId, ExpansionKind? preferExpansion, IReadOnlySet<uint> skippedItemIds, int spentThisTrade = 0)
    {
        var tradeList = Plugin.Cfg.TradeList;
        for (var i = 0; i < tradeList.Count; i++)
        {
            if (!TryNeeded(tradeList[i], skippedItemIds, out var order, out var need) || GemstoneCatalog.ComputeBuyQuantity(wallet, order.Item.CostPerOne, need, spentThisTrade) <= 0)
            {
                continue;
            }
            var trader = GemstoneTrader.PickForItem(order.Item.ItemId, preferTerritoryId, preferExpansion, out var _);
            if (trader is null)
            {
                continue;
            }
            var orders = new List<TradeOrder> { order };
            for (var j = i + 1; j < tradeList.Count; j++)
            {
                if (TryNeeded(tradeList[j], skippedItemIds, out var laterOrder, out var _) && GemstoneTrader.Sells(trader, laterOrder.Item))
                {
                    orders.Add(laterOrder);
                }
            }
            return new TradePlan(trader, orders.ToArray());
        }
        return null;
    }

    public static TradeEntryState StateOf(TradeListEntry entry, out GemstoneTradeItem? item, out int held)
    {
        item = GemstoneCatalog.FindById(entry.ItemId);
        held = 0;
        if (item is null)
        {
            return TradeEntryState.Unknown;
        }
        held = HeldCount(item.ItemId);
        if (item.IsCollectible && IsLearned(item.ItemId))
        {
            return TradeEntryState.Learned;
        }
        if (item.IsCollectible && held > 0)
        {
            return TradeEntryState.Held;
        }
        if (!item.IsCollectible && entry.StopAtCount > 0 && held >= entry.StopAtCount)
        {
            return TradeEntryState.Stocked;
        }
        if (GemstoneTrader.PickForItem(item.ItemId, null, null, out var _) is not null)
        {
            return TradeEntryState.Needed;
        }
        return TradeEntryState.NoTrader;
    }

    public static string DescribeBlocked(int wallet, IReadOnlySet<uint> skippedItemIds)
    {
        var tradeList = Plugin.Cfg.TradeList;
        if (tradeList.Count == 0)
        {
            return "the shopping list is empty";
        }
        var descriptions = new string[tradeList.Count];
        for (var i = 0; i < tradeList.Count; i++)
        {
            descriptions[i] = DescribeEntry(tradeList[i], wallet, skippedItemIds);
        }
        return string.Join("; ", descriptions);
    }

    private static string DescribeEntry(TradeListEntry entry, int wallet, IReadOnlySet<uint> skippedItemIds)
    {
        GemstoneTradeItem? tradeItem = GemstoneCatalog.FindById(entry.ItemId);
        if (tradeItem is null)
        {
            return $"item {entry.ItemId} is not in the gem catalog";
        }
        if (skippedItemIds.Contains(tradeItem.ItemId))
        {
            return tradeItem.ItemName + " could not be bought earlier this run";
        }
        var need = RemainingNeed(tradeItem, entry.StopAtCount);
        if (need <= 0)
        {
            if (!tradeItem.IsCollectible)
            {
                return $"{tradeItem.ItemName} is stocked ({HeldCount(tradeItem.ItemId)}/{entry.StopAtCount})";
            }
            return tradeItem.ItemName + " is already owned or learned";
        }
        if (GemstoneCatalog.ComputeBuyQuantity(wallet, tradeItem.CostPerOne, need, 0) <= 0)
        {
            return $"{tradeItem.ItemName} ({tradeItem.CostPerOne}g each) is out of budget at {wallet}g with the current spend settings";
        }
        return "no trader you can reach sells " + tradeItem.ItemName;
    }

    private static bool TryNeeded(TradeListEntry entry, IReadOnlySet<uint> skippedItemIds, out TradeOrder order, out int need)
    {
        order = default;
        need = 0;
        if (skippedItemIds.Contains(entry.ItemId))
        {
            return false;
        }
        GemstoneTradeItem? tradeItem = GemstoneCatalog.FindById(entry.ItemId);
        if (tradeItem is null)
        {
            return false;
        }
        order = new TradeOrder(tradeItem, entry.StopAtCount);
        need = RemainingNeed(tradeItem, entry.StopAtCount);
        return need > 0;
    }
}
