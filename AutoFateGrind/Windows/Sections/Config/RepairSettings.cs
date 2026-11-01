using System;
using AutoFateGrind.Core.Game.Ops;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;

namespace AutoFateGrind.Windows.Sections.Config;

internal static class RepairSettings
{
    private static readonly RepairMode[] repairModes =
    [
        RepairMode.SelfThenNpc,
        RepairMode.SelfOnly,
        RepairMode.NpcOnly
    ];

    private static readonly SettingsControls.Choices.Choice[] repairModeChoices =
    [
        new SettingsControls.Choices.Choice(L.Settings.RepairSelfThenNpcName, L.Settings.RepairSelfThenNpcDetail),
        new SettingsControls.Choices.Choice(L.Settings.RepairSelfOnlyName, L.Settings.RepairSelfOnlyDetail),
        new SettingsControls.Choices.Choice(L.Settings.RepairNpcOnlyName, L.Settings.RepairNpcOnlyDetail)
    ];

    public static void Draw(Configuration cfg)
    {
        DrawTriggerGroup(cfg);
        using var sectionStyle = Motion.PushSection("##rp_source", cfg.AutoRepair);
        if (sectionStyle == null)
        {
            return;
        }
        DrawSourceGroup(cfg);
    }

    private static void DrawTriggerGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.RepairTrigger)))
        {
            SettingsRow.Draw(Loc.T(L.Settings.AutoRepair), Loc.T(L.Settings.AutoRepairHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.AutoRepair, v =>
                {
                    cfg.AutoRepair = v;
                }, "##rp_on");
            }, 22f);
            using var bodyStyle = Motion.PushSwitch("##rp_body", cfg.AutoRepair);
            if (!cfg.AutoRepair)
            {
                SettingsRow.Note(Loc.T(L.Settings.AutoRepairOff));
                return;
            }
            SettingsRow.Draw(Loc.T(L.Settings.RepairThreshold), Loc.T(L.Settings.RepairThresholdHelp), SettingsControls.RowSliderWidth, () =>
            {
                SettingsControls.DrawIntSlider(cfg, "##rp_threshold", () => cfg.AutoRepairThresholdPct, v =>
                {
                    cfg.AutoRepairThresholdPct = Math.Clamp(v, 5, 80);
                }, 5, 80, "%d%%");
            });
        }
    }

    private static void DrawSourceGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.RepairSource));
        var selected = Math.Max(0, Array.IndexOf(repairModes, cfg.RepairMode));
        SettingsRow.Draw(Loc.T(L.Settings.RepairSource), Loc.T(L.Settings.RepairSourceHelp), SettingsControls.RowComboWidth, () =>
        {
            SettingsControls.Choices.DrawCombo("##rp_mode", repairModeChoices, selected, choice =>
            {
                cfg.RepairMode = repairModes[choice];
                cfg.SaveDebounced();
            });
        });
        SettingsRow.Caption(Loc.T(repairModeChoices[selected].Detail));
        using var sectionStyle = Motion.PushSection("##rp_npc", cfg.RepairMode != RepairMode.SelfOnly);
        if (sectionStyle == null)
        {
            return;
        }
        SettingsRow.DrawBlock(Loc.T(L.Settings.CustomNpc), Loc.T(L.Settings.CustomNpcHelp), () =>
        {
            DrawCustomRepairNpc(cfg);
        });
        SettingsRow.Note(Loc.T(L.Settings.NpcNote));
    }

    private static void DrawCustomRepairNpc(Configuration cfg)
    {
        var preferredRepairNpc = cfg.PreferredRepairNpc;
        if (preferredRepairNpc != null)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentMint))
            {
                ImGui.TextWrapped(Loc.T(L.Settings.NpcSet, preferredRepairNpc.Name, preferredRepairNpc.TerritoryId));
            }
        }
        else
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            {
                ImGui.TextWrapped(Loc.T(L.Settings.NpcNone));
            }
        }
        if (ImGui.Button($"{Loc.T(L.Settings.SetFromTarget)}##rp_set"))
        {
            var repairNpc = RepairOps.CaptureCurrentTargetAsRepairNpc();
            if (repairNpc == null)
            {
                Svc.Chat.PrintError(Loc.T(L.Settings.NoTargetChat));
            }
            else
            {
                cfg.PreferredRepairNpc = repairNpc;
                GameTextGlyphs.Add(repairNpc.Name);
                cfg.SaveDebounced();
                Svc.Chat.Print(Loc.T(L.Settings.NpcSetChat, repairNpc.Name, repairNpc.TerritoryId));
            }
        }
        if (preferredRepairNpc != null)
        {
            ImGui.SameLine();
            if (ImGui.Button($"{Loc.T(L.Common.Clear)}##rp_clear"))
            {
                cfg.PreferredRepairNpc = null;
                cfg.SaveDebounced();
            }
        }
    }
}
