using System;
using System.Linq;
using AutoFateGrind.Core.Game.Player;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoFateGrind.Windows.Sections.Config;

internal static class ClassSettings
{
    private static int classPickerSelection;

    private static readonly SettingsControls.Choices.Choice[] afterDoneChoices =
    [
        new SettingsControls.Choices.Choice(L.Settings.DoneKeepName, L.Settings.DoneKeepDetail),
        new SettingsControls.Choices.Choice(L.Settings.DoneStopName, L.Settings.DoneStopDetail)
    ];

    public static void Draw(Configuration cfg)
    {
        DrawSwitchingGroup(cfg);
        using var sectionStyle = Motion.PushSection("##cls_more", cfg.ApplyClassOnStart);
        if (sectionStyle == null)
        {
            return;
        }
        DrawDoneGroup(cfg);
        DrawQueueGroup(cfg);
    }

    private static void DrawSwitchingGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.ClassesSwitching)))
        {
            SettingsRow.Draw(Loc.T(L.Settings.SwitchOnStart), Loc.T(L.Settings.SwitchOnStartHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(cfg, () => cfg.ApplyClassOnStart, v =>
                {
                    cfg.ApplyClassOnStart = v;
                }, "##cls_apply");
            }, 22f);
            using var sectionStyle = Motion.PushSection("##cls_off_note", !cfg.ApplyClassOnStart);
            if (sectionStyle == null)
            {
                return;
            }
            SettingsRow.Note(Loc.T(L.Settings.SwitchingOff));
        }
    }

    private static void DrawDoneGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.ClassesDone));
        var selected = ((cfg.AfterClassQueueDone == AfterClassQueueDone.StopRun) ? 1 : 0);
        SettingsRow.Draw(Loc.T(L.Settings.AllCapped), Loc.T(L.Settings.AllCappedHelp), SettingsControls.RowComboWidth, () =>
        {
            SettingsControls.Choices.DrawCombo("##cls_done", afterDoneChoices, selected, choice =>
            {
                cfg.AfterClassQueueDone = ((choice == 1) ? AfterClassQueueDone.StopRun : AfterClassQueueDone.KeepGrindingOnLast);
                cfg.SaveDebounced();
            });
        });
        SettingsRow.Caption(Loc.T(afterDoneChoices[selected].Detail));
    }

    private static void DrawQueueGroup(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.ClassesQueue)))
        {
            SettingsRow.DrawBlock(Loc.T(L.Settings.AddGearset), Loc.T(L.Settings.AddGearsetHelp), () =>
            {
                DrawAddClassRow(cfg);
            });
            SettingsRow.DrawBlock(Loc.T(L.Settings.QueueOrder), Loc.T(L.Settings.QueueOrderHelp), () =>
            {
                DrawClassQueueList(cfg);
            });
        }
    }

    private static void DrawAddClassRow(Configuration cfg)
    {
        var gearsets = ClassSwitcher.EnumerateGearsets();
        for (int i = 0; i < gearsets.Count; i++)
        {
            GameTextGlyphs.Add(gearsets[i].Name);
        }
        if (gearsets.Count == 0)
        {
            SettingsRow.Note(Loc.T(L.Settings.NoGearsets));
            return;
        }
        var alreadyQueued = cfg.ClassQueue.Select(e => e.GearsetIndex).ToHashSet();
        var queuedSuffix = "  " + Loc.T(L.Settings.Queued);
        string[] labels = gearsets.Select(g =>
        {
            var jobName = ClassSwitcher.JobNameForJobId(g.JobId);
            var gearsetName = (string.IsNullOrWhiteSpace(g.Name) ? "" : (" - " + g.Name));
            var queuedMark = (alreadyQueued.Contains(g.UserIndex) ? queuedSuffix : "");
            return $"{g.UserIndex,3}. {jobName}{gearsetName}{queuedMark}";
        }).ToArray();
        classPickerSelection = Math.Clamp(classPickerSelection, 0, gearsets.Count - 1);
        SettingsControls.DrawSearchableCombo("##cls_picker", labels, ref classPickerSelection, 360f);
        var picked = gearsets[classPickerSelection];
        var pickedQueued = alreadyQueued.Contains(picked.UserIndex);
        ImGui.SameLine();
        using (ImRaii.Disabled(pickedQueued))
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentMint))
            {
                if (ImGui.SmallButton($"{Loc.T(L.Common.Add)}##cls_add"))
                {
                    var gameMaxLevel = ClassSwitcher.GameMaxLevel;
                    var atMaxLevel = ClassSwitcher.UnsyncedLevelForJobId(picked.JobId) >= gameMaxLevel;
                    cfg.ClassQueue.Add(new ClassQueueEntry
                    {
                        GearsetIndex = picked.UserIndex,
                        JobId = picked.JobId,
                        StopAtLevel = (atMaxLevel ? 0 : gameMaxLevel)
                    });
                    cfg.SaveDebounced();
                    var nextIndex = gearsets.FindIndex(g => !alreadyQueued.Contains(g.UserIndex) && g.UserIndex != picked.UserIndex);
                    if (nextIndex >= 0)
                    {
                        classPickerSelection = nextIndex;
                    }
                }
            }
        }
        if (pickedQueued)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            {
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(Loc.T(L.Settings.AlreadyQueued));
            }
        }
    }

    private static void DrawClassQueueList(Configuration cfg)
    {
        if (cfg.ClassQueue.Count == 0)
        {
            SettingsRow.Note(Loc.T(L.Settings.NoClassesQueued));
            return;
        }
        int? moveUp = null;
        int? moveDown = null;
        int? remove = null;
        var frameHeight = ImGui.GetFrameHeight();
        var spacing = 4f * ImGuiHelpers.GlobalScale;
        var rowRightWidth = frameHeight * 3f + spacing * 2f;
        int i;
        for (i = 0; i < cfg.ClassQueue.Count; i++)
        {
            var entry = cfg.ClassQueue[i];
            DrawClassQueueRow(i, cfg.ClassQueue.Count, entry, cfg, frameHeight, spacing, rowRightWidth, () =>
            {
                moveUp = i;
            }, () =>
            {
                moveDown = i;
            }, () =>
            {
                remove = i;
            });
        }
        if (ListReorder.Apply(cfg.ClassQueue, cfg.ClassQueue.Count, moveUp, moveDown, remove))
        {
            cfg.SaveDebounced();
        }
    }

    private static void DrawClassQueueRow(int index, int total, ClassQueueEntry entry, Configuration cfg, float btnSize, float spacingX, float rowRightWidth, Action onUp, Action onDown, Action onRemove)
    {
        var running = Plugin.Instance.Controller.Running;
        using (ImRaii.Disabled(running))
        {
            ImGui.AlignTextToFramePadding();
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            {
                ImGui.TextUnformatted($"{index + 1}.");
            }
            ImGui.SameLine();
            var jobName = ClassSwitcher.JobNameForUserIndex(entry.GearsetIndex);
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            {
                ImGui.TextUnformatted(Loc.T(L.Settings.QueueEntry, jobName, entry.GearsetIndex));
            }
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            {
                var level = ClassSwitcher.UnsyncedLevelForJobId(ClassSwitcher.JobIdForUserIndex(entry.GearsetIndex));
                ImGui.TextUnformatted("  " + Loc.T(L.Settings.EntryLevel, level));
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(140f);
            var stopAtLevel = entry.StopAtLevel;
            using (SettingsControls.PushFrameColors())
            {
                if (ImGui.SliderInt($"##cls_cap_{index}", ref stopAtLevel, 0, ClassSwitcher.GameMaxLevel, (stopAtLevel == 0) ? Loc.T(L.Settings.NoCap) : Loc.T(L.Settings.StopAtLevel)))
                {
                    entry.StopAtLevel = stopAtLevel;
                    cfg.SaveDebounced();
                }
            }
            ImGui.SameLine(SettingsGroup.InnerRightLocalX() - rowRightWidth);
            if (IconButton.Draw(FontAwesomeIcon.ArrowUp, $"##cls_up_{index}", btnSize, tooltip: Loc.T(L.Common.MoveUp), enabled: index > 0 && !running))
            {
                onUp();
            }
            ImGui.SameLine(0f, spacingX);
            if (IconButton.Draw(FontAwesomeIcon.ArrowDown, $"##cls_dn_{index}", btnSize, tooltip: Loc.T(L.Common.MoveDown), enabled: index < total - 1 && !running))
            {
                onDown();
            }
            ImGui.SameLine(0f, spacingX);
            if (IconButton.Draw(FontAwesomeIcon.Times, $"##cls_rm_{index}", btnSize, Styling.AccentRose, Loc.T(L.Common.Remove), !running))
            {
                onRemove();
            }
        }
    }
}
