using System;
using System.Linq;
using System.Numerics;
using AutoFateGrind.Core.Game.Ops;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoFateGrind.Windows.Sections.Config;

internal static class ConsumableSettings
{
    private static int consumablePickerSelection;

    public static void Draw(Configuration cfg)
    {
        DrawConsumableGroup(cfg);
        using var sectionStyle = Motion.PushSection("##con_items", cfg.AutoConsume);
        if (sectionStyle == null)
        {
            return;
        }
        DrawItemsGroup(cfg);
    }

    private static void DrawConsumableGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.ConsumablesGroup)))
        {
            SettingsRow.Draw(Loc.T(L.Settings.AutoSummonChocobo), Loc.T(L.Settings.AutoSummonChocoboHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.AutoSummonChocobo, v =>
                {
                    cfg.AutoSummonChocobo = v;
                }, "##con_chocobo");
            }, 22f);
            SettingsRow.Draw(Loc.T(L.Settings.AutoConsume), Loc.T(L.Settings.AutoConsumeHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.AutoConsume, v =>
                {
                    cfg.AutoConsume = v;
                }, "##con_on");
            }, 22f);
            using var switchStyle = Motion.PushSwitch("##con_body", cfg.AutoConsume);
            if (!cfg.AutoConsume)
            {
                SettingsRow.Note(Loc.T(L.Settings.AutoConsumeOff));
                return;
            }
            SettingsRow.Draw(Loc.T(L.Settings.RefreshUnder), Loc.T(L.Settings.RefreshUnderHelp), SettingsControls.RowSliderWidth, () =>
            {
                SettingsControls.DrawIntSlider(cfg, "##con_min", () => cfg.AutoConsumeMinMinutes, v =>
                {
                    cfg.AutoConsumeMinMinutes = Math.Clamp(v, 0, 29);
                }, 0, 29, (cfg.AutoConsumeMinMinutes == 0) ? Loc.T(L.Settings.RefreshWornOff) : Loc.T(L.Settings.RefreshFormat));
            });
        }
    }

    private static void DrawItemsGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.ConsumablesItems)))
        {
            SettingsRow.DrawBlock(Loc.T(L.Settings.AddItem), Loc.T(L.Settings.AddItemHelp), () =>
            {
                DrawAddConsumableRow(cfg);
            });
            SettingsRow.DrawBlock(Loc.T(L.Settings.ActiveItems), Loc.T(L.Settings.ActiveItemsHelp), () =>
            {
                DrawConsumableList(cfg);
            });
        }
    }

    private static void DrawAddConsumableRow(Configuration cfg)
    {
        ConsumableEntry[] available = FoodOps.Catalog.Where(FoodOps.IsAvailable).ToArray();
        if (available.Length == 0)
        {
            SettingsRow.Note(Loc.T(L.Settings.NoneInBag));
            return;
        }
        var queued = cfg.AutoConsumeItems.Select(e => e.ItemId).ToHashSet();
        var addedSuffix = "  " + Loc.T(L.Settings.Added);
        string[] labels = available.Select(e =>
        {
            var kind = ((e.StatusId == 48) ? Loc.T(L.Settings.KindFood) : Loc.T(L.Settings.KindMedicine));
            var addedMark = (queued.Contains(e.ItemId) ? addedSuffix : "");
            return Loc.T(L.Settings.ItemLabel, e.Name, kind, addedMark);
        }).ToArray();
        consumablePickerSelection = Math.Clamp(consumablePickerSelection, 0, available.Length - 1);
        SettingsControls.DrawSearchableCombo("##con_picker", labels, ref consumablePickerSelection, 340f);
        var consumableEntry = available[consumablePickerSelection];
        var alreadyAdded = queued.Contains(consumableEntry.ItemId);
        ImGui.SameLine();
        var size = new Vector2(96f * ImGuiHelpers.GlobalScale, ImGui.GetFrameHeight());
        using (ImRaii.Disabled(alreadyAdded))
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentMint))
            {
                if (ImGui.Button($"{Loc.T(L.Common.Add)}##con_add", size))
                {
                    cfg.AutoConsumeItems.Add(new ConsumableEntry
                    {
                        ItemId = consumableEntry.ItemId,
                        Name = consumableEntry.Name,
                        StatusId = consumableEntry.StatusId,
                        CanBeHq = consumableEntry.CanBeHq
                    });
                    cfg.SaveDebounced();
                }
            }
        }
        if (alreadyAdded)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            {
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(Loc.T(L.Settings.AlreadyAdded));
            }
        }
    }

    private static void DrawConsumableList(Configuration cfg)
    {
        if (cfg.AutoConsumeItems.Count == 0)
        {
            SettingsRow.Note(Loc.T(L.Settings.NoItemsAdded));
            return;
        }
        int? pendingRemove = null;
        var frameHeight = ImGui.GetFrameHeight();
        for (int i = 0; i < cfg.AutoConsumeItems.Count; i++)
        {
            var consumableEntry = cfg.AutoConsumeItems[i];
            ImGui.AlignTextToFramePadding();
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            {
                ImGui.TextUnformatted($"{i + 1}.");
            }
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            {
                ImGui.TextUnformatted(consumableEntry.Name);
            }
            ImGui.SameLine();
            var kindLabel = ((consumableEntry.StatusId == 48) ? Loc.T(L.Settings.WellFed) : Loc.T(L.Settings.Medicated));
            var inBag = FoodOps.IsAvailable(consumableEntry);
            using (ImRaii.PushColor(ImGuiCol.Text, inBag ? Styling.TextMuted : Styling.AccentRose))
            {
                ImGui.TextUnformatted("  " + (inBag ? kindLabel : Loc.T(L.Settings.NoneInBagShort, kindLabel)));
            }
            ImGui.SameLine(SettingsGroup.InnerRightLocalX() - frameHeight);
            if (IconButton.Draw(FontAwesomeIcon.Times, $"##con_rm_{i}", frameHeight, Styling.AccentRose, Loc.T(L.Common.Remove)))
            {
                pendingRemove = i;
            }
        }
        if (pendingRemove is { } removeIndex)
        {
            cfg.AutoConsumeItems.RemoveAt(removeIndex);
            cfg.SaveDebounced();
        }
    }
}
