using System;
using AutoFateGrind.Core.Game.Player;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Windows.Components;

namespace AutoFateGrind.Windows.Sections.Config;

internal static class TravelSettings
{
    private const float MountComboWidth = 220f;

    private const int MountListRefreshMs = 2000;

    private const int SwapWaitMinSec = 0;

    private const int SwapWaitMaxSec = 300;

    private const int CollectHandInBatchMin = 1;

    private const int CollectHandInBatchMax = 9;

    private const int PullReachMin = 10;

    private const int PullReachMax = 40;

    private const int PullMaxMobsMin = 3;

    private const int PullMaxMobsMax = 32;

    private const int PullHpMin = 10;

    private const int PullHpMax = 100;

    private static MountOption[] mountOptions = Array.Empty<MountOption>();

    private static string[] mountLabels = Array.Empty<string>();

    private static long mountListBuiltAtMs;

    public static void Draw(Configuration cfg)
    {
        DrawMountGroup(cfg);
        DrawZoneSwapGroup(cfg);
        DrawFatePlayGroup(cfg);
    }

    private static void DrawMountGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.TravelMount)))
        {
            DrawMountRow(cfg);
            SettingsRow.Draw(Loc.T(L.Settings.MountWhileWaiting), Loc.T(L.Settings.MountWhileWaitingHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.MountWhileWaitingForFates, v =>
                {
                    cfg.MountWhileWaitingForFates = v;
                }, "##travel_mount_waiting");
            }, 22f);
            SettingsRow.Draw(Loc.T(L.Settings.EconomyTravel), Loc.T(L.Settings.EconomyTravelHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.EconomyTravel, v =>
                {
                    cfg.EconomyTravel = v;
                }, "##travel_economy");
            }, 22f);
        }
    }

    private static void DrawMountRow(Configuration cfg)
    {
        SettingsRow.Draw(Loc.T(L.Settings.Mount), Loc.T(L.Settings.MountHelp), MountComboWidth, () =>
        {
            RefreshMountList();
            var selectedIndex = 0;
            for (int i = 0; i < mountOptions.Length; i++)
            {
                if (mountOptions[i].Id == cfg.PreferredMountId)
                {
                    selectedIndex = i + 1;
                    break;
                }
            }
            if (SettingsControls.DrawSearchableCombo("##travel_mount", mountLabels, ref selectedIndex, MountComboWidth))
            {
                cfg.PreferredMountId = selectedIndex == 0 ? 0u : mountOptions[selectedIndex - 1].Id;
                cfg.SaveDebounced();
            }
        });
        if (cfg.PreferredMountId != 0 && !MountOps.IsUsable(cfg.PreferredMountId))
        {
            SettingsRow.Note(Loc.T(L.Settings.MountNotOwned, MountOps.NameOf(cfg.PreferredMountId)), Styling.AccentRose);
        }
    }

    private static void RefreshMountList()
    {
        var tickCount = Environment.TickCount64;
        if (mountLabels.Length != 0 && tickCount - mountListBuiltAtMs < MountListRefreshMs)
        {
            return;
        }
        mountListBuiltAtMs = tickCount;
        mountOptions = MountOps.OwnedMounts();
        mountLabels = new string[mountOptions.Length + 1];
        mountLabels[0] = Loc.T(L.Settings.MountRandom);
        for (int i = 0; i < mountOptions.Length; i++)
        {
            mountLabels[i + 1] = mountOptions[i].Name;
        }
    }

    private static void DrawZoneSwapGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.TravelZoneSwap)))
        {
            SettingsRow.Draw(Loc.T(L.Settings.SwapZones), Loc.T(L.Settings.SwapZonesHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.SwapZonesWhenEmpty, v =>
                {
                    cfg.SwapZonesWhenEmpty = v;
                }, "##travel_swap");
            }, 22f);
            using (Motion.PushSwitch("##travel_swap_body", cfg.SwapZonesWhenEmpty))
            {
                if (cfg.SwapZonesWhenEmpty)
                {
                    SettingsRow.Draw(Loc.T(L.Settings.SwapWait), Loc.T(L.Settings.SwapWaitHelp), SettingsControls.RowSliderWidth, () =>
                    {
                        SettingsControls.DrawIntSlider(cfg, "##travel_swap_wait", () => cfg.SwapZoneWaitSec, v =>
                        {
                            cfg.SwapZoneWaitSec = v;
                        }, SwapWaitMinSec, SwapWaitMaxSec, Loc.T(L.Settings.MinTimeFormat));
                    });
                }
            }
        }
    }

    private static void DrawPullTuning(Configuration cfg)
    {
        SettingsRow.Draw(Loc.T(L.Settings.PullReach), Loc.T(L.Settings.PullReachHelp), SettingsControls.RowSliderWidth, () =>
        {
            SettingsControls.DrawIntSlider(cfg, "##travel_pull_reach", () => cfg.PullReachMeters, v =>
            {
                cfg.PullReachMeters = v;
            }, PullReachMin, PullReachMax, Loc.T(L.Settings.MetersFormat));
        });
        SettingsRow.Draw(Loc.T(L.Settings.PullMaxMobs), Loc.T(L.Settings.PullMaxMobsHelp), SettingsControls.RowSliderWidth, () =>
        {
            SettingsControls.DrawIntSlider(cfg, "##travel_pull_max", () => cfg.PullMaxMobs, v =>
            {
                cfg.PullMaxMobs = v;
            }, PullMaxMobsMin, PullMaxMobsMax, Loc.T(L.Settings.PullMaxMobsFormat));
        });
        SettingsRow.Draw(Loc.T(L.Settings.PullStopHp), Loc.T(L.Settings.PullStopHpHelp), SettingsControls.RowSliderWidth, () =>
        {
            SettingsControls.DrawIntSlider(cfg, "##travel_pull_stop_hp", () => cfg.PullStopHpPercent, v =>
            {
                cfg.PullStopHpPercent = v;
                cfg.PullResumeHpPercent = Math.Max(cfg.PullResumeHpPercent, v);
            }, PullHpMin, PullHpMax, "%d%%");
        });
        SettingsRow.Draw(Loc.T(L.Settings.PullResumeHp), Loc.T(L.Settings.PullResumeHpHelp), SettingsControls.RowSliderWidth, () =>
        {
            SettingsControls.DrawIntSlider(cfg, "##travel_pull_resume_hp", () => cfg.PullResumeHpPercent, v =>
            {
                cfg.PullResumeHpPercent = v;
                cfg.PullStopHpPercent = Math.Min(cfg.PullStopHpPercent, v);
            }, PullHpMin, PullHpMax, "%d%%");
        });
    }

    private static void DrawFatePlayGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.TravelFatePlay)))
        {
            SettingsRow.Draw(Loc.T(L.Settings.KeepTwist), Loc.T(L.Settings.KeepTwistHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.KeepTwistOfFate, v =>
                {
                    cfg.KeepTwistOfFate = v;
                }, "##travel_keeptwist");
            }, 22f);
            SettingsRow.Draw(Loc.T(L.Settings.PullMany), Loc.T(L.Settings.PullManyHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.PullAsManyAsPossible, v =>
                {
                    cfg.PullAsManyAsPossible = v;
                }, "##travel_pull_many");
            }, 22f);
            using (Motion.PushSwitch("##travel_pull_many_body", cfg.PullAsManyAsPossible))
            {
                if (cfg.PullAsManyAsPossible)
                {
                    DrawPullTuning(cfg);
                }
            }
            SettingsRow.Draw(Loc.T(L.Settings.CollectHandIn), Loc.T(L.Settings.CollectHandInHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.CollectHandInEnabled, v =>
                {
                    cfg.CollectHandInEnabled = v;
                }, "##travel_collect_handin");
            }, 22f);
            if (!cfg.CollectHandInEnabled)
            {
                return;
            }
            SettingsRow.Draw(Loc.T(L.Settings.CollectHandInBatch), Loc.T(L.Settings.CollectHandInBatchHelp), SettingsControls.RowSliderWidth, () =>
            {
                SettingsControls.DrawIntSlider(cfg, "##travel_collect_batch", () => cfg.CollectHandInBatch, v =>
                {
                    cfg.CollectHandInBatch = v;
                }, CollectHandInBatchMin, CollectHandInBatchMax, Loc.T(L.Settings.CollectHandInBatchFormat));
            });
        }
    }
}
