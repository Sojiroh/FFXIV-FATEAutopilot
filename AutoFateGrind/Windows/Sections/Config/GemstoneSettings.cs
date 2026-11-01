using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Trading;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoFateGrind.Windows.Sections.Config;

internal static class GemstoneSettings
{
    private readonly record struct ListRow(uint ItemId, string Name, Vector4 NameColor, bool IsCollectible, string Status, Vector4 StatusColor, string? Tooltip);

    private static readonly GemstoneSpendMode[] spendModes =
    [
        GemstoneSpendMode.SpendAll,
        GemstoneSpendMode.SpendGems,
        GemstoneSpendMode.BuyQuantity
    ];

    private static readonly SettingsControls.Choices.Choice[] spendModeChoices =
    [
        new SettingsControls.Choices.Choice(L.Settings.SpendAllName, L.Settings.SpendAllDetail),
        new SettingsControls.Choices.Choice(L.Settings.SpendUpToName, L.Settings.SpendUpToDetail),
        new SettingsControls.Choices.Choice(L.Settings.BuyFixedName, L.Settings.BuyFixedDetail)
    ];

    public static void Draw(Configuration cfg)
    {
        DrawTriggerGroup(cfg);
        using var sectionStyle = Motion.PushSection("##tr_more", cfg.TradeOnCap);
        if (sectionStyle == null)
        {
            return;
        }
        DrawItemGroup(cfg);
        DrawSpendGroup(cfg);
        DrawAfterGroup(cfg);
    }

    private static void DrawTriggerGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.GemsTrigger)))
        {
            SettingsRow.Draw(Loc.T(L.Settings.AutoTrade), Loc.T(L.Settings.AutoTradeHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.TradeOnCap, v =>
                {
                    cfg.TradeOnCap = v;
                }, "##tr_oncap");
            }, 22f);
            using var bodyStyle = Motion.PushSwitch("##tr_body", cfg.TradeOnCap);
            if (!cfg.TradeOnCap)
            {
                SettingsRow.Note(Loc.T(L.Settings.AutoTradeOff));
                return;
            }
            SettingsRow.Draw(Loc.T(L.Settings.Threshold), Loc.T(L.Settings.ThresholdHelp), SettingsControls.RowSliderWidth, () =>
            {
                SettingsControls.DrawIntSlider(cfg, "##tr_threshold", () => cfg.TradeThreshold, v =>
                {
                    cfg.TradeThreshold = Math.Clamp(v, 100, 1500);
                }, 100, 1500, Loc.T(L.Settings.GemsFormat));
            });
        }
    }

    private static readonly SettingsControls.Choices.Choice[] afterTradeChoices =
    [
        new SettingsControls.Choices.Choice(L.Settings.AfterResumeName, L.Settings.AfterResumeDetail),
        new SettingsControls.Choices.Choice(L.Settings.AfterStopName, L.Settings.AfterStopDetail)
    ];

    private static GemstoneTradeItem[]? sortedItems;

    private static string[]? sortedLabels;

    private static void DrawItemGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.GemsItem)))
        {
            SettingsRow.DrawBlock(Loc.T(L.Settings.AddTradeItem), Loc.T(L.Settings.ItemToBuyHelp), () =>
            {
                DrawAddRow(cfg);
            });
            SettingsRow.DrawBlock(Loc.T(L.Settings.ShoppingList), Loc.T(L.Settings.ShoppingListHelp), () =>
            {
                DrawShoppingList(cfg);
            });
        }
    }

    private static void DrawAddRow(Configuration cfg)
    {
        EnsureSortedCatalog();
        if (sortedItems == null || sortedItems.Length == 0)
        {
            SettingsRow.Note(Loc.T(L.Settings.NoShopItems), Styling.AccentRose);
            return;
        }
        pickerSelection = Math.Clamp(pickerSelection, 0, sortedItems.Length - 1);
        SettingsControls.DrawSearchableCombo("##tr_item", sortedLabels!, ref pickerSelection, 380f);
        var gemstoneTradeItem = sortedItems[pickerSelection];
        var alreadyListed = IsListed(cfg.TradeList, gemstoneTradeItem.ItemId);
        ImGui.SameLine();
        var size = new Vector2(96f * ImGuiHelpers.GlobalScale, ImGui.GetFrameHeight());
        using (ImRaii.Disabled(alreadyListed))
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentMint))
            {
                if (ImGui.Button($"{Loc.T(L.Common.Add)}##tr_add", size))
                {
                    cfg.TradeList.Add(new TradeListEntry
                    {
                        ItemId = gemstoneTradeItem.ItemId
                    });
                    cfg.SaveDebounced();
                    listRowsTickMs = 0L;
                }
            }
        }
        if (alreadyListed)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            {
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(Loc.T(L.Settings.AlreadyListed));
            }
        }
        DrawTraderReach(gemstoneTradeItem);
    }

    private static bool IsListed(List<TradeListEntry> entries, uint itemId)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].ItemId == itemId)
            {
                return true;
            }
        }
        return false;
    }

    private static void DrawShoppingList(Configuration cfg)
    {
        if (cfg.TradeList.Count == 0)
        {
            SettingsRow.Note(Loc.T(L.Settings.ShoppingListEmpty), Styling.AccentRose);
            return;
        }
        RefreshListRows(cfg);
        int? moveUp = null;
        int? moveDown = null;
        int? remove = null;
        var globalScale = ImGuiHelpers.GlobalScale;
        var frameHeight = ImGui.GetFrameHeight();
        var spacing = 4f * globalScale;
        var stopWidth = RowControlWidth * globalScale;
        var rightBlock = stopWidth + spacing * 2f + frameHeight * 3f + spacing * 2f;
        var rightStart = SettingsGroup.InnerRightLocalX() - rightBlock;
        var count = cfg.TradeList.Count;
        for (int i = 0; i < count; i++)
        {
            var entry = cfg.TradeList[i];
            var row = listRows[i];
            ImGui.AlignTextToFramePadding();
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            {
                ImGui.TextUnformatted($"{i + 1}.");
            }
            ImGui.SameLine();
            var statusText = ((row.Status.Length == 0) ? "" : ("  " + row.Status));
            var nameWidth = rightStart - spacing * 2f - ImGui.GetCursorPosX() - ImGui.CalcTextSize(statusText).X;
            using (ImRaii.PushColor(ImGuiCol.Text, row.NameColor))
            {
                ImGui.TextUnformatted(TextDraw.Truncate(row.Name, Math.Max(0f, nameWidth)));
            }
            if (statusText.Length > 0)
            {
                ImGui.SameLine(0f, 0f);
                using (ImRaii.PushColor(ImGuiCol.Text, row.StatusColor))
                {
                    ImGui.TextUnformatted(statusText);
                }
                if (row.Tooltip != null && ImGui.IsItemHovered())
                {
                    Tooltip.Show(row.Tooltip);
                }
            }
            ImGui.SameLine(rightStart);
            DrawStopControl(cfg, entry, row, i, stopWidth);
            ImGui.SameLine(rightStart + stopWidth + spacing * 2f);
            if (IconButton.Draw(FontAwesomeIcon.ArrowUp, $"##tr_up_{i}", frameHeight, tooltip: Loc.T(L.Common.MoveUp), enabled: i > 0))
            {
                moveUp = i;
            }
            ImGui.SameLine(0f, spacing);
            if (IconButton.Draw(FontAwesomeIcon.ArrowDown, $"##tr_dn_{i}", frameHeight, tooltip: Loc.T(L.Common.MoveDown), enabled: i < count - 1))
            {
                moveDown = i;
            }
            ImGui.SameLine(0f, spacing);
            if (IconButton.Draw(FontAwesomeIcon.Times, $"##tr_rm_{i}", frameHeight, Styling.AccentRose, Loc.T(L.Common.Remove)))
            {
                remove = i;
            }
        }
        if (ListReorder.Apply(cfg.TradeList, count, moveUp, moveDown, remove))
        {
            cfg.SaveDebounced();
            listRowsTickMs = 0L;
        }
    }

    private static void DrawStopControl(Configuration cfg, TradeListEntry entry, ListRow row, int entryIndex, float width)
    {
        if (row.IsCollectible)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            {
                ImGui.TextUnformatted(Loc.T(L.Settings.BuyOnce));
            }
            return;
        }
        var stopAt = entry.StopAtCount;
        ImGui.SetNextItemWidth(width);
        using (SettingsControls.PushFrameColors())
        {
            if (ImGui.SliderInt($"##tr_stop_{entryIndex}", ref stopAt, 0, MaxStopAtCount, (stopAt == 0) ? Loc.T(L.Settings.KeepBuying) : Loc.T(L.Settings.StopAtCount)))
            {
                entry.StopAtCount = Math.Clamp(stopAt, 0, MaxStopAtCount);
                cfg.SaveDebounced();
                listRowsTickMs = 0L;
            }
        }
    }

    private static void RefreshListRows(Configuration cfg)
    {
        var tickCount = Environment.TickCount64;
        if (listRows.Length == cfg.TradeList.Count && tickCount - listRowsTickMs < ListRefreshMs && RowsMatch(cfg.TradeList))
        {
            return;
        }
        listRowsTickMs = tickCount;
        var skipped = Plugin.Instance.Controller.SessionSnapshot?.TradeSkippedItemIds ?? noSkippedItems;
        ListRow[] rows = new ListRow[cfg.TradeList.Count];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = BuildRow(cfg.TradeList[i], skipped);
        }
        listRows = rows;
        (previewText, previewColor) = BuildPreview(cfg);
    }

    private static bool RowsMatch(List<TradeListEntry> entries)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (listRows[i].ItemId != entries[i].ItemId)
            {
                return false;
            }
        }
        return true;
    }

    private static ListRow BuildRow(TradeListEntry entry, HashSet<uint> skipped)
    {
        var tradeEntryState = TradeList.StateOf(entry, out GemstoneTradeItem? item, out int held);
        if (item is null)
        {
            return new ListRow(entry.ItemId, Loc.T(L.Settings.UnknownTradeItem, entry.ItemId), Styling.AccentRose, IsCollectible: false, "", Styling.TextMuted, null);
        }
        var name = Loc.T(L.Settings.ItemCostLabel, item.ItemName, item.CostPerOne);
        if (skipped.Contains(item.ItemId))
        {
            return new ListRow(item.ItemId, name, Styling.TextStrong, item.IsCollectible, Loc.T(L.Settings.StatusSkipped), Styling.AccentAmber, null);
        }
        return tradeEntryState switch
        {
            TradeEntryState.Learned => new ListRow(item.ItemId, name, Styling.TextStrong, IsCollectible: true, Loc.T(L.Settings.StatusLearned), Styling.AccentMint, null),
            TradeEntryState.Held => new ListRow(item.ItemId, name, Styling.TextStrong, IsCollectible: true, Loc.T(L.Settings.StatusHeld), Styling.AccentMint, null),
            TradeEntryState.Stocked => new ListRow(item.ItemId, name, Styling.TextStrong, IsCollectible: false, Loc.T(L.Settings.StatusHaveOf, held, entry.StopAtCount), Styling.AccentMint, null),
            TradeEntryState.NoTrader => new ListRow(item.ItemId, name, Styling.TextStrong, item.IsCollectible, Loc.T(L.Settings.StatusNoTrader), Styling.AccentRose, BuildReachNote(item)),
            _ when item.IsCollectible => new ListRow(item.ItemId, name, Styling.TextStrong, IsCollectible: true, "", Styling.TextMuted, null),
            _ when entry.StopAtCount > 0 => new ListRow(item.ItemId, name, Styling.TextStrong, IsCollectible: false, Loc.T(L.Settings.StatusHaveOf, held, entry.StopAtCount), Styling.TextMuted, null),
            _ => new ListRow(item.ItemId, name, Styling.TextStrong, IsCollectible: false, Loc.T(L.Settings.StatusHave, held), Styling.TextMuted, null)
        };
    }

    private static void DrawTraderReach(GemstoneTradeItem item)
    {
        var tickCount = Environment.TickCount64;
        if (item.ItemId != reachNoteItemId || tickCount - reachNoteTickMs > ReachRecheckMs)
        {
            reachNoteItemId = item.ItemId;
            reachNoteTickMs = tickCount;
            reachNote = BuildReachNote(item);
        }
        if (reachNote == null)
        {
            return;
        }
        SettingsRow.Note(reachNote, Styling.AccentRose);
    }

    private static string? BuildReachNote(GemstoneTradeItem item)
    {
        if (GemstoneTrader.PickForItem(item.ItemId, null, null, out var availability) is not null)
        {
            return null;
        }
        if (availability != TraderAvailability.AllLocked)
        {
            return Loc.T(L.Settings.TraderMissing, item.ItemName);
        }
        return Loc.T(L.Settings.TraderLocked, item.ItemName, GemstoneTrader.DescribeSellerZones(item.ItemId));
    }

    private static int pickerSelection;

    private const int ListRefreshMs = 1000;

    private const int MaxStopAtCount = 999;

    private const float RowControlWidth = 150f;

    private static readonly HashSet<uint> noSkippedItems = new HashSet<uint>();

    private static ListRow[] listRows = Array.Empty<ListRow>();

    private static long listRowsTickMs;

    private static string previewText = "";

    private static Vector4 previewColor;

    private static uint reachNoteItemId;

    private static long reachNoteTickMs;

    private static string? reachNote;

    private const int ReachRecheckMs = 2000;

    private static void EnsureSortedCatalog()
    {
        GemstoneTradeItem[] all = GemstoneCatalog.All;
        if (sortedItems != null && sortedItems.Length == all.Length)
        {
            return;
        }
        sortedItems = all.OrderBy(i => i.ItemName, StringComparer.OrdinalIgnoreCase).ToArray();
        sortedLabels = new string[sortedItems.Length];
        for (int i = 0; i < sortedItems.Length; i++)
        {
            var gemstoneTradeItem = sortedItems[i];
            sortedLabels[i] = Loc.T(L.Settings.ItemCostLabel, gemstoneTradeItem.ItemName, gemstoneTradeItem.CostPerOne);
        }
    }

    private static void DrawSpendGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.GemsSpend));
        var selected = Math.Max(0, Array.IndexOf(spendModes, cfg.SpendMode));
        SettingsRow.Draw(Loc.T(L.Settings.SpendStrategy), Loc.T(L.Settings.SpendStrategyHelp), SettingsControls.RowComboWidth, () =>
        {
            SettingsControls.Choices.DrawCombo("##tr_spend_mode", spendModeChoices, selected, choice =>
            {
                cfg.SpendMode = spendModes[choice];
                cfg.SaveDebounced();
                listRowsTickMs = 0L;
            });
        });
        SettingsRow.Caption(Loc.T(spendModeChoices[selected].Detail));
        if (cfg.SpendMode == GemstoneSpendMode.SpendGems)
        {
            SettingsRow.Draw(Loc.T(L.Settings.SpendUpTo), Loc.T(L.Settings.SpendUpToHelp), SettingsControls.RowSliderWidth, () =>
            {
                SettingsControls.DrawIntSlider(cfg, "##tr_spend_gems", () => cfg.SpendGemsAmount, v =>
                {
                    cfg.SpendGemsAmount = Math.Clamp(v, 50, 1500);
                }, 50, 1500, Loc.T(L.Settings.GemsFormat));
            });
        }
        else if (cfg.SpendMode == GemstoneSpendMode.BuyQuantity)
        {
            SettingsRow.Draw(Loc.T(L.Settings.BuyQuantity), Loc.T(L.Settings.BuyQuantityHelp), SettingsControls.RowSliderWidth, () =>
            {
                SettingsControls.DrawIntSlider(cfg, "##tr_buy_qty", () => cfg.BuyQuantityAmount, v =>
                {
                    cfg.BuyQuantityAmount = Math.Clamp(v, 1, 99);
                }, 1, 99, Loc.T(L.Settings.BuyQuantityFormat));
            });
        }
        SettingsRow.Draw(Loc.T(L.Settings.Reserve), Loc.T(L.Settings.ReserveHelp), SettingsControls.RowSliderWidth, () =>
        {
            SettingsControls.DrawIntSlider(cfg, "##tr_reserve", () => cfg.KeepGemstonesReserve, v =>
            {
                cfg.KeepGemstonesReserve = Math.Clamp(v, 0, 1500);
            }, 0, 1500, Loc.T(L.Settings.GemsFormat));
        });
        DrawSpendPreview(cfg);
    }

    private static void DrawAfterGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.GemsAfter));
        var selected = ((cfg.AfterTrade == AfterTradeAction.Stop) ? 1 : 0);
        SettingsRow.Draw(Loc.T(L.Settings.WhenDone), Loc.T(L.Settings.WhenDoneHelp), SettingsControls.RowComboWidth, () =>
        {
            SettingsControls.Choices.DrawCombo("##tr_after", afterTradeChoices, selected, choice =>
            {
                cfg.AfterTrade = ((choice == 1) ? AfterTradeAction.Stop : AfterTradeAction.Resume);
                cfg.SaveDebounced();
            });
        });
        SettingsRow.Caption(Loc.T(afterTradeChoices[selected].Detail));
    }

    private static void DrawSpendPreview(Configuration cfg)
    {
        if (cfg.TradeList.Count == 0)
        {
            return;
        }
        RefreshListRows(cfg);
        SettingsRow.Note(previewText, previewColor);
    }

    private static (string Text, Vector4 Color) BuildPreview(Configuration cfg)
    {
        var skipped = new HashSet<uint>();
        var tradePlan = TradeList.Plan(cfg.TradeThreshold, null, null, skipped);
        if (tradePlan is null)
        {
            return (Text: Loc.T(L.Settings.PreviewNothing, cfg.TradeThreshold, cfg.KeepGemstonesReserve), Color: Styling.AccentRose);
        }
        var remaining = cfg.TradeThreshold;
        var spent = 0;
        var lines = new List<string>(cfg.TradeList.Count);
        while (tradePlan is not null)
        {
            for (int i = 0; i < tradePlan.Orders.Length; i++)
            {
                var item = tradePlan.Orders[i].Item;
                skipped.Add(item.ItemId);
                var need = TradeList.RemainingNeed(item, tradePlan.Orders[i].StopAtCount);
                var quantity = GemstoneCatalog.ComputeBuyQuantity(remaining, item.CostPerOne, need, spent);
                if (quantity > 0)
                {
                    var cost = quantity * (int)item.CostPerOne;
                    remaining -= cost;
                    spent += cost;
                    lines.Add(Loc.T(L.Settings.PreviewItem, quantity, item.ItemName));
                }
            }
            tradePlan = TradeList.Plan(remaining, tradePlan.Trader.TerritoryId, tradePlan.Trader.Expansion, skipped, spent);
        }
        return (Text: Loc.T(L.Settings.PreviewPlan, cfg.TradeThreshold, cfg.KeepGemstonesReserve, string.Join(", ", lines), spent), Color: Styling.TextMuted);
    }
}
