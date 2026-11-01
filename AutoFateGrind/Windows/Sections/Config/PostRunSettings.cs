using System;
using System.Numerics;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoFateGrind.Windows.Sections.Config;

internal static class PostRunSettings
{
    private const int MaxLength = 1000;

    private const int MinLines = 3;

    private const int MaxLines = 8;

    public static void Draw(Configuration cfg)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.PostRunGroup)))
        {
            SettingsRow.DrawBlock(Loc.T(L.Settings.PostRunLabel), Loc.T(L.Settings.PostRunHelp), () =>
            {
                DrawEditor(cfg);
            });
            SettingsRow.Caption(Loc.T(L.Settings.PostRunCaption));
        }
    }

    private static void DrawEditor(Configuration cfg)
    {
        var buf = cfg.PostRunCommands;
        var lineCount = 1;
        for (int i = 0; i < buf.Length; i++)
        {
            if (buf[i] == '\n')
            {
                lineCount++;
            }
        }
        var globalScale = ImGuiHelpers.GlobalScale;
        var height = ImGui.GetTextLineHeight() * Math.Clamp(lineCount + 1, MinLines, MaxLines) + ImGui.GetStyle().FramePadding.Y * 2f;
        var width = SettingsGroup.ContentRightEdge - ImGui.GetCursorScreenPos().X;
        using var frameColors = SettingsControls.PushFrameColors();
        using var frameStyle = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f).Push(ImGuiStyleVar.FrameRounding, 7f * globalScale);
        if (!ImGui.InputTextMultiline("##fta_post_run", ref buf, MaxLength, new Vector2(width, height)))
        {
            return;
        }
        cfg.PostRunCommands = buf;
        cfg.SaveDebounced();
    }
}
