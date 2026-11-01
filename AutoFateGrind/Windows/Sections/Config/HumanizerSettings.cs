using System;
using System.Linq;
using AutoFateGrind.Core.Game.Ops;
using AutoFateGrind.Core.Ipc;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Zones;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;

namespace AutoFateGrind.Windows.Sections.Config;

internal static class HumanizerSettings
{
    private const long ModuleCheckIntervalMs = 5000L;

    private static readonly HumanizerBreakActivity[] breakActivities =
    [
        HumanizerBreakActivity.Wander,
        HumanizerBreakActivity.IdleAtSpot
    ];

    private static readonly SettingsControls.Choices.Choice[] breakActivityChoices =
    [
        new SettingsControls.Choices.Choice(L.Settings.BreakActivityWanderName, L.Settings.BreakActivityWanderDetail),
        new SettingsControls.Choices.Choice(L.Settings.BreakActivityIdleName, L.Settings.BreakActivityIdleDetail)
    ];

    private static string? moduleCheckPreset;

    private static long moduleCheckTick;

    private static bool moduleMissing;

    public static void Draw(Configuration cfg)
    {
        DrawPacingGroup(cfg);
        DrawCombatMovementGroup(cfg);
        DrawBreaksGroup(cfg);
        using var sectionStyle = Motion.PushSection("##hum_more", cfg.HumanizerEnabled);
        if (sectionStyle == null)
        {
            return;
        }
        var idleAtSpot = cfg.HumanizerBreakActivity == HumanizerBreakActivity.IdleAtSpot;
        if (idleAtSpot)
        {
            DrawIdleSpotsGroup(cfg);
        }
        else
        {
            DrawWanderingGroup(cfg);
        }
        if (!idleAtSpot || cfg.HumanizerIdleSpots.Count == 0)
        {
            DrawCitiesGroup(cfg);
        }
    }

    private static void DrawPacingGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.Pacing)))
        {
            SettingsRow.Draw(Loc.T(L.Settings.PacingEnable), Loc.T(L.Settings.PacingEnableHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.PacingEnabled, v =>
                {
                    cfg.PacingEnabled = v;
                }, "##pace_on");
            }, 22f);
            using var bodyStyle = Motion.PushSwitch("##pace_body", cfg.PacingEnabled);
            if (!cfg.PacingEnabled)
            {
                SettingsRow.Note(Loc.T(L.Settings.PacingOff));
                return;
            }
            SettingsRow.Draw(Loc.T(L.Settings.ReactionDelay), Loc.T(L.Settings.ReactionDelayHelp), SettingsControls.RangeInlineWidth(), () =>
            {
                SettingsControls.DrawRangeInline(cfg, "##pace_reaction_min", "##pace_reaction_max", () => cfg.PacingReactionMinSec, v =>
                {
                    cfg.PacingReactionMinSec = v;
                }, () => cfg.PacingReactionMaxSec, v =>
                {
                    cfg.PacingReactionMaxSec = v;
                }, 30, 0, Loc.T(L.Settings.SecondsFormat));
            });
            SettingsRow.Draw(Loc.T(L.Settings.PickVariety), Loc.T(L.Settings.PickVarietyHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.PacingPickVariety, v =>
                {
                    cfg.PacingPickVariety = v;
                }, "##pace_variety");
            }, 22f);
        }
    }

    private static void DrawCombatMovementGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.CombatMovement));
        SettingsRow.Draw(Loc.T(L.Settings.CombatMovementEnable), Loc.T(L.Settings.CombatMovementEnableHelp), SettingsControls.ToggleWidth, () =>
        {
            SettingsControls.DrawToggle(cfg, () => cfg.CombatMovementEnabled, v =>
            {
                cfg.CombatMovementEnabled = v;
            }, "##combat_move_on");
        }, 22f);
        using var bodyStyle = Motion.PushSwitch("##combat_move_body", cfg.CombatMovementEnabled);
        if (!cfg.CombatMovementEnabled)
        {
            SettingsRow.Note(Loc.T(L.Settings.CombatMovementOff));
            return;
        }
        var noneLabel = Loc.T(L.Settings.MovementLevelNone);
        string[] delayLabels =
        [
            noneLabel,
            Loc.T(L.Settings.DelayShort),
            Loc.T(L.Settings.DelayLong)
        ];
        string[] roomLabels =
        [
            noneLabel,
            Loc.T(L.Settings.RoomSmall),
            Loc.T(L.Settings.RoomMedium),
            Loc.T(L.Settings.RoomLarge)
        ];
        SettingsRow.Draw(Loc.T(L.Settings.DodgeDelay), Loc.T(L.Settings.DodgeDelayHelp), SettingsControls.ComboRangeWidth(), () =>
        {
            SettingsControls.DrawComboRange(cfg, "##combat_dodge_min", "##combat_dodge_max", delayLabels, () => cfg.CombatDodgeDelayMin, v =>
            {
                cfg.CombatDodgeDelayMin = v;
            }, () => cfg.CombatDodgeDelayMax, v =>
            {
                cfg.CombatDodgeDelayMax = v;
            });
        });
        SettingsRow.Draw(Loc.T(L.Settings.MoveDelay), Loc.T(L.Settings.MoveDelayHelp), SettingsControls.ComboRangeWidth(), () =>
        {
            SettingsControls.DrawComboRange(cfg, "##combat_move_min", "##combat_move_max", delayLabels, () => cfg.CombatMoveDelayMin, v =>
            {
                cfg.CombatMoveDelayMin = v;
            }, () => cfg.CombatMoveDelayMax, v =>
            {
                cfg.CombatMoveDelayMax = v;
            });
        });
        SettingsRow.Draw(Loc.T(L.Settings.DangerRoom), Loc.T(L.Settings.DangerRoomHelp), SettingsControls.ComboRangeWidth(), () =>
        {
            SettingsControls.DrawComboRange(cfg, "##combat_room_min", "##combat_room_max", roomLabels, () => cfg.CombatCushionMin, v =>
            {
                cfg.CombatCushionMin = v;
            }, () => cfg.CombatCushionMax, v =>
            {
                cfg.CombatCushionMax = v;
            });
        });
        if (PresetLacksMovementModule(cfg.CombatPresetName))
        {
            SettingsRow.Note(Loc.T(L.Settings.CombatMovementNoModule, cfg.CombatPresetName), Styling.AccentAmber);
        }
    }

    private static bool PresetLacksMovementModule(string preset)
    {
        var tickCount = Environment.TickCount64;
        if (preset != moduleCheckPreset || tickCount - moduleCheckTick >= ModuleCheckIntervalMs)
        {
            moduleCheckPreset = preset;
            moduleCheckTick = tickCount;
            moduleMissing = BossModMovementTuning.PresetHasModule(preset) == false;
        }
        return moduleMissing;
    }

    private static void DrawBreaksGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.HumanizerBreaks));
        SettingsRow.Draw(Loc.T(L.Settings.HumanizerEnable), Loc.T(L.Settings.HumanizerEnableHelp), SettingsControls.ToggleWidth, () =>
        {
            SettingsControls.DrawToggle(cfg, () => cfg.HumanizerEnabled, v =>
            {
                cfg.HumanizerEnabled = v;
            }, "##hum_on");
        }, 22f);
        using var bodyStyle = Motion.PushSwitch("##hum_body", cfg.HumanizerEnabled);
        if (!cfg.HumanizerEnabled)
        {
            SettingsRow.Note(Loc.T(L.Settings.HumanizerOff));
            return;
        }
        SettingsRow.Draw(Loc.T(L.Settings.FatesBetween), Loc.T(L.Settings.FatesBetweenHelp), SettingsControls.RowSliderWidth, () =>
        {
            SettingsControls.DrawIntSlider(cfg, "##hum_fates", () => cfg.HumanizerFatesBeforeBreak, v =>
            {
                cfg.HumanizerFatesBeforeBreak = Math.Clamp(v, 1, 100);
            }, 1, 100, Loc.T(L.Settings.FatesFormat));
        });
        SettingsRow.Draw(Loc.T(L.Settings.BreakLength), Loc.T(L.Settings.BreakLengthHelp), SettingsControls.RangeInlineWidth(), () =>
        {
            SettingsControls.DrawRangeInline(cfg, "##hum_min", "##hum_max", () => cfg.HumanizerBreakMinMinutes, v =>
            {
                cfg.HumanizerBreakMinMinutes = v;
            }, () => cfg.HumanizerBreakMaxMinutes, v =>
            {
                cfg.HumanizerBreakMaxMinutes = v;
            }, 60, 1, Loc.T(L.Settings.MinutesFormat));
        });
        var activity = Math.Max(0, Array.IndexOf(breakActivities, cfg.HumanizerBreakActivity));
        SettingsRow.Draw(Loc.T(L.Settings.BreakActivity), Loc.T(L.Settings.BreakActivityHelp), SettingsControls.RowComboWidth, () =>
        {
            SettingsControls.Choices.DrawCombo("##hum_activity", breakActivityChoices, activity, choice =>
            {
                cfg.HumanizerBreakActivity = breakActivities[choice];
                cfg.SaveDebounced();
            });
        });
        SettingsRow.Caption(Loc.T(breakActivityChoices[activity].Detail));
    }

    private static void DrawIdleSpotsGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.IdleSpots)))
        {
            SettingsRow.DrawBlock(Loc.T(L.Settings.SavedSpots), Loc.T(L.Settings.SavedSpotsHelp), () =>
            {
                DrawIdleSpotList(cfg);
            });
        }
    }

    private static void DrawIdleSpotList(Configuration cfg)
    {
        var humanizerIdleSpots = cfg.HumanizerIdleSpots;
        if (humanizerIdleSpots.Count == 0)
        {
            SettingsRow.Note(Loc.T(L.Settings.NoSpots));
        }
        var removeIndex = -1;
        var frameHeight = ImGui.GetFrameHeight();
        for (int i = 0; i < humanizerIdleSpots.Count; i++)
        {
            var idleSpot = humanizerIdleSpots[i];
            using (ImRaii.PushId(i))
            {
                ImGui.AlignTextToFramePadding();
                using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
                {
                    ImGui.TextUnformatted((idleSpot.Name.Length > 0) ? idleSpot.Name : TerritoryNames.Of(idleSpot.TerritoryId));
                }
                if (idleSpot.Name.Length > 0)
                {
                    ImGui.SameLine();
                    using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
                    {
                        ImGui.TextUnformatted(TerritoryNames.Of(idleSpot.TerritoryId));
                    }
                }
                ImGui.SameLine(SettingsGroup.InnerRightLocalX() - frameHeight);
                if (IconButton.Draw(FontAwesomeIcon.Times, "##hum_spot_rm", frameHeight, Styling.AccentRose, Loc.T(L.Common.Remove)))
                {
                    removeIndex = i;
                }
            }
        }
        if (removeIndex >= 0)
        {
            humanizerIdleSpots.RemoveAt(removeIndex);
            cfg.SaveDebounced();
        }
        using (ImRaii.Disabled(Svc.Objects.LocalPlayer == null))
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentMint))
            {
                if (ImGui.Button($"{Loc.T(L.Settings.SaveSpot)}##hum_spot_save"))
                {
                    SaveCurrentSpot(cfg);
                }
            }
        }
    }

    private static void SaveCurrentSpot(Configuration cfg)
    {
        IdleSpot? spot;
        switch (IdleSpotOps.TryCapture(out spot))
        {
            case IdleSpotCapture.Saved:
                cfg.HumanizerIdleSpots.Add(spot!);
                GameTextGlyphs.Add(spot!.Name);
                cfg.SaveDebounced();
                Svc.Chat.Print(Loc.T(L.Settings.SpotSavedChat, TerritoryNames.Of(spot.TerritoryId)));
                break;
            case IdleSpotCapture.Airborne:
                Svc.Chat.PrintError(Loc.T(L.Settings.SpotAirborneChat));
                break;
            case IdleSpotCapture.NoAetheryte:
                Svc.Chat.PrintError(Loc.T(L.Settings.SpotNoAetheryteChat));
                break;
            case IdleSpotCapture.NotLoaded:
                break;
        }
    }

    private static void DrawWanderingGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.HumanizerWandering)))
        {
            SettingsRow.Draw(Loc.T(L.Settings.PauseBetween), Loc.T(L.Settings.PauseBetweenHelp), SettingsControls.RangeInlineWidth(), () =>
            {
                SettingsControls.DrawRangeInline(cfg, "##hum_pause_min", "##hum_pause_max", () => cfg.HumanizerPauseMinSec, v =>
                {
                    cfg.HumanizerPauseMinSec = v;
                }, () => cfg.HumanizerPauseMaxSec, v =>
                {
                    cfg.HumanizerPauseMaxSec = v;
                }, 60, 0, Loc.T(L.Settings.SecondsFormat));
            });
            SettingsRow.Draw(Loc.T(L.Settings.WalkDistance), Loc.T(L.Settings.WalkDistanceHelp), SettingsControls.RangeInlineWidth(), () =>
            {
                SettingsControls.DrawRangeInline(cfg, "##hum_wander_min", "##hum_wander_max", () => cfg.HumanizerWanderMinMeters, v =>
                {
                    cfg.HumanizerWanderMinMeters = v;
                }, () => cfg.HumanizerWanderMaxMeters, v =>
                {
                    cfg.HumanizerWanderMaxMeters = v;
                }, 200, 5, Loc.T(L.Settings.MetersFormat));
            });
        }
    }

    private static void DrawCitiesGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.HumanizerCities)))
        {
            SettingsRow.DrawBlock(Loc.T(L.Settings.AllowedCities), Loc.T(L.Settings.AllowedCitiesHelp), () =>
            {
                DrawHumanizerCityList(cfg);
            });
        }
    }

    private static void DrawHumanizerCityList(Configuration cfg)
    {
        foreach (IGrouping<ExpansionKind, CityInfo> expansionGroup in from c in CityCatalog.All
                                                                      group c by c.Expansion into g
                                                                      orderby g.Key descending
                                                                      select g)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            {
                ImGui.TextUnformatted(ExpansionLabels.Name(expansionGroup.Key));
            }
            foreach (CityInfo city in expansionGroup)
            {
                var enabled = cfg.HumanizerCities.Contains(city.TerritoryId);
                if (ToggleSwitch.Draw($"##hum_city_{city.TerritoryId}", ref enabled))
                {
                    if (enabled)
                    {
                        cfg.HumanizerCities.Add(city.TerritoryId);
                    }
                    else
                    {
                        cfg.HumanizerCities.Remove(city.TerritoryId);
                    }
                    cfg.SaveDebounced();
                }
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
                {
                    ImGui.TextUnformatted(city.Name);
                }
            }
            ImGui.Spacing();
        }
        if (cfg.HumanizerCities.Count == 0)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentRose))
            {
                ImGui.TextWrapped(Loc.T(L.Settings.NoCities));
            }
        }
    }
}
