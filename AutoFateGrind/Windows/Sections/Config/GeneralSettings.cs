using AutoFateGrind.Core.Localization;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AutoFateGrind.Windows.Sections.Config;

internal static class GeneralSettings
{
    public static void Draw(Configuration cfg)
    {
        DrawLanguageGroup(cfg);
        DrawWindowGroup(cfg);
        DrawRunGroup(cfg);
        PostRunSettings.Draw(cfg);
    }

    private static void DrawLanguageGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.Language)))
        {
            SettingsRow.Draw(Loc.T(L.Settings.Language), Loc.T(L.Settings.LanguageHelp), SettingsControls.RowComboWidth, () =>
            {
                SettingsControls.DrawLanguageCombo(cfg);
            });
        }
    }

    private static void DrawWindowGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.GeneralWindow)))
        {
            SettingsRow.Draw(Loc.T(L.Settings.OpenOnLogin), Loc.T(L.Settings.OpenOnLoginHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.AutoShowOnLogin, v =>
                {
                    cfg.AutoShowOnLogin = v;
                }, "##gen_autoshow");
            }, 22f);
            SettingsRow.Draw(Loc.T(L.Settings.DragAnywhere), Loc.T(L.Settings.DragAnywhereHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.DragWindowFromAnywhere, v =>
                {
                    cfg.DragWindowFromAnywhere = v;
                }, "##gen_draganywhere");
            }, 22f);
            SettingsRow.Draw(Loc.T(L.Settings.LivePopout), Loc.T(L.Settings.LivePopoutHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.ShowLivePopout, v =>
                {
                    cfg.ShowLivePopout = v;
                    Plugin.Instance.LiveFateWindow.IsOpen = v;
                }, "##gen_popout");
            }, 22f);
            SettingsRow.Draw(Loc.T(L.Settings.NameFormat), Loc.T(L.Settings.NameFormatHelp), 170f, () =>
            {
                DrawNameFormat(cfg);
            });
        }
    }

    private static void DrawNameFormat(Configuration cfg)
    {
        var buf = cfg.FateNameFormat;
        ImGui.SetNextItemWidth(170f * ImGuiHelpers.GlobalScale);
        using (SettingsControls.PushFrameColors())
        {
            if (ImGui.InputText("##gen_namefmt", ref buf, 64))
            {
                cfg.FateNameFormat = buf;
                cfg.SaveDebounced();
            }
        }
    }

    private static void DrawRunGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.GeneralRun)))
        {
            SettingsRow.Draw(Loc.T(L.Settings.AutoPause), Loc.T(L.Settings.AutoPauseHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.AutoPauseInContent, v =>
                {
                    cfg.AutoPauseInContent = v;
                }, "##gen_autopause");
            }, 22f);
            SettingsRow.Draw(Loc.T(L.Settings.AutoResume), Loc.T(L.Settings.AutoResumeHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.AutoResumeOnFault, v =>
                {
                    cfg.AutoResumeOnFault = v;
                }, "##gen_autoresume");
            }, 22f);
            SettingsRow.Draw(Loc.T(L.Settings.StuckRescue), Loc.T(L.Settings.StuckRescueHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.StuckRescueEnabled, v =>
                {
                    cfg.StuckRescueEnabled = v;
                }, "##gen_stuckrescue");
            }, 22f);
            using (Motion.PushSwitch("##gen_stuckrescue_body", cfg.StuckRescueEnabled))
            {
                if (cfg.StuckRescueEnabled)
                {
                    SettingsRow.Draw(Loc.T(L.Settings.StuckRescueAfter), Loc.T(L.Settings.StuckRescueAfterHelp), SettingsControls.RowSliderWidth, () =>
                    {
                        SettingsControls.DrawIntSlider(cfg, "##gen_stuckrescue_after", () => cfg.StuckRescueMinutes, v =>
                        {
                            cfg.StuckRescueMinutes = v;
                        }, 2, 30, Loc.T(L.Settings.MinutesFormat));
                    });
                }
            }
        }
    }
}
