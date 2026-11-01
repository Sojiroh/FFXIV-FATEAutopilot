using System;
using AutoFateGrind.Core.Game.Watchers;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoFateGrind.Windows.Sections.Config;

internal static class GmAlertSettings
{
    private static string gmCommandDraft = string.Empty;

    public static void Draw(Configuration cfg)
    {
        DrawAlertsGroup(cfg);
        DrawActionsGroup(cfg);
    }

    private static void DrawAlertsGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.GmAlerts)))
        {
            SettingsRow.Draw(Loc.T(L.Settings.GmStopRun), Loc.T(L.Settings.GmStopRunHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.GmAlertStopRun, v =>
                {
                    cfg.GmAlertStopRun = v;
                }, "##gm_stop");
            }, 22f);
            SettingsRow.Draw(Loc.T(L.Settings.GmToast), Loc.T(L.Settings.GmToastHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.GmAlertToast, v =>
                {
                    cfg.GmAlertToast = v;
                }, "##gm_toast");
            }, 22f);
            SettingsRow.Draw(Loc.T(L.Settings.GmChat), Loc.T(L.Settings.GmChatHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.GmAlertChat, v =>
                {
                    cfg.GmAlertChat = v;
                }, "##gm_chat");
            }, 22f);
            SettingsRow.Draw(Loc.T(L.Settings.GmSound), Loc.T(L.Settings.GmSoundHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.GmAlertSound, v =>
                {
                    cfg.GmAlertSound = v;
                }, "##gm_sound");
            }, 22f);
            using var sectionStyle = Motion.PushSection("##gm_beep", cfg.GmAlertSound);
            if (sectionStyle == null)
            {
                return;
            }
            DrawBeepRows(cfg);
        }
    }

    private static void DrawBeepRows(Configuration cfg)
    {
        SettingsRow.Draw(Loc.T(L.Settings.BeepCount), Loc.T(L.Settings.BeepCountHelp), SettingsControls.RowSliderWidth, () =>
        {
            SettingsControls.DrawIntSlider(cfg, "##gm_beep_count", () => cfg.GmAlertBeepCount, v =>
            {
                cfg.GmAlertBeepCount = Math.Clamp(v, 1, 20);
            }, 1, 20, Loc.T(L.Settings.BeepCountFormat));
        });
        SettingsRow.Draw(Loc.T(L.Settings.BeepLength), Loc.T(L.Settings.BeepLengthHelp), SettingsControls.RowSliderWidth, () =>
        {
            SettingsControls.DrawIntSlider(cfg, "##gm_beep_dur", () => cfg.GmAlertBeepDurationMs, v =>
            {
                cfg.GmAlertBeepDurationMs = Math.Clamp(v, 50, 1000);
            }, 50, 1000, Loc.T(L.Settings.BeepLengthFormat));
        });
        SettingsRow.Draw(Loc.T(L.Settings.BeepPitch), Loc.T(L.Settings.BeepPitchHelp), SettingsControls.RowSliderWidth, () =>
        {
            SettingsControls.DrawIntSlider(cfg, "##gm_beep_freq", () => cfg.GmAlertBeepFrequencyHz, v =>
            {
                cfg.GmAlertBeepFrequencyHz = Math.Clamp(v, 100, 5000);
            }, 100, 5000, Loc.T(L.Settings.BeepPitchFormat));
        });
        SettingsRow.DrawBlock(Loc.T(L.Common.Test), null, () =>
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentAmber))
            {
                if (ImGui.SmallButton($"{Loc.T(L.Common.Preview)}##gm_beep_preview"))
                {
                    GmAlertWatcher.PlayBeeps(cfg.GmAlertBeepCount, cfg.GmAlertBeepFrequencyHz, cfg.GmAlertBeepDurationMs);
                }
            }
        });
    }

    private static void DrawActionsGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.GmActions)))
        {
            SettingsRow.DrawBlock(Loc.T(L.Settings.GmCommands), Loc.T(L.Settings.GmCommandsHelp), () =>
            {
                DrawGmCommandList(cfg);
            });
            SettingsRow.Draw(Loc.T(L.Settings.GmKill), Loc.T(L.Settings.GmKillHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.GmAlertKillGame, v =>
                {
                    cfg.GmAlertKillGame = v;
                }, "##gm_kill");
            }, 22f);
        }
    }

    private static void DrawGmCommandList(Configuration cfg)
    {
        var buf = gmCommandDraft;
        bool submitted;
        using (SettingsControls.PushFrameColors())
        {
            ImGui.SetNextItemWidth(360f * ImGuiHelpers.GlobalScale);
            submitted = ImGui.InputTextWithHint("##gm_cmd_in", "/logout", ref buf, 200, ImGuiInputTextFlags.EnterReturnsTrue);
        }
        if (submitted)
        {
            AddCommand(cfg, buf);
            gmCommandDraft = string.Empty;
        }
        else
        {
            gmCommandDraft = buf;
        }
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentMint))
        {
            if (ImGui.SmallButton($"{Loc.T(L.Common.Add)}##gm_cmd_add"))
            {
                AddCommand(cfg, gmCommandDraft);
                gmCommandDraft = string.Empty;
            }
        }
        if (cfg.GmAlertCommands.Count == 0)
        {
            SettingsRow.Note(Loc.T(L.Settings.NoCommands));
            return;
        }
        int? pendingRemove = null;
        var frameHeight = ImGui.GetFrameHeight();
        for (int i = 0; i < cfg.GmAlertCommands.Count; i++)
        {
            ImGui.AlignTextToFramePadding();
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            {
                ImGui.TextUnformatted($"{i + 1}.");
            }
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            {
                ImGui.TextUnformatted(cfg.GmAlertCommands[i]);
            }
            ImGui.SameLine(SettingsGroup.InnerRightLocalX() - frameHeight);
            if (IconButton.Draw(FontAwesomeIcon.Times, $"##gm_cmd_rm_{i}", frameHeight, Styling.AccentRose, Loc.T(L.Common.Remove)))
            {
                pendingRemove = i;
            }
        }
        if (pendingRemove is { } removeIndex)
        {
            cfg.GmAlertCommands.RemoveAt(removeIndex);
            cfg.SaveDebounced();
        }
    }

    private static void AddCommand(Configuration cfg, string raw)
    {
        var command = raw.Trim();
        if (command.Length == 0)
        {
            return;
        }
        var normalized = (command.StartsWith('/') ? command : ("/" + command));
        if (!cfg.GmAlertCommands.Contains(normalized))
        {
            cfg.GmAlertCommands.Add(normalized);
            cfg.SaveDebounced();
        }
    }
}
