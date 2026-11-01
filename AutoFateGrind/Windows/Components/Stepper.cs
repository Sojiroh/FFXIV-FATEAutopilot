using System;
using System.Numerics;
using AutoFateGrind.Core.Localization;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoFateGrind.Windows.Components;

internal static class Stepper
{
    public const float DefaultWidth = 168f;

    public static bool Draw(string id, ref int value, int step, int min, int max, string format, float width = DefaultWidth)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var frameHeight = ImGui.GetFrameHeight();
        var size = new Vector2(width * globalScale, frameHeight);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var rectMax = cursorScreenPos + size;
        var windowDrawList = ImGui.GetWindowDrawList();
        var rounding = frameHeight * 0.5f;
        Paint.Fill(windowDrawList, cursorScreenPos, rectMax, Styling.WithAlpha(Styling.Surface0, 0.9f), rounding);
        Paint.Stroke(windowDrawList, cursorScreenPos, rectMax, Styling.WithAlpha(Styling.BorderDim, 0.6f), rounding);
        var changed = false;
        ImGui.PushID(id);
        ImGui.SetCursorScreenPos(cursorScreenPos);
        if (IconButton.Draw(FontAwesomeIcon.Minus, "##dec", frameHeight, enabled: value > min))
        {
            value = Math.Max(min, value - step);
            changed = true;
        }
        ImGui.SetCursorScreenPos(cursorScreenPos + new Vector2(frameHeight, 0f));
        ImGui.SetNextItemWidth(size.X - frameHeight * 2f);
        using (ImRaii.PushColor(ImGuiCol.FrameBg, Vector4.Zero).Push(ImGuiCol.FrameBgHovered, Styling.WithAlpha(Styling.Surface2, 0.6f)).Push(ImGuiCol.FrameBgActive, Styling.WithAlpha(Styling.Surface3, 0.6f))
            .Push(ImGuiCol.Text, Styling.TextStrong))
        {
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0f))
            {
                var dragValue = value;
                if (ImGui.DragInt("##value", ref dragValue, MathF.Max(0.25f, (float)step * 0.1f), min, max, format))
                {
                    value = Math.Clamp(dragValue, min, max);
                    changed = true;
                }
            }
        }
        if (ImGui.IsItemHovered())
        {
            Tooltip.Show(Loc.T(L.Common.DragAdjustHint));
        }
        ImGui.SetCursorScreenPos(new Vector2(rectMax.X - frameHeight, cursorScreenPos.Y));
        if (IconButton.Draw(FontAwesomeIcon.Plus, "##inc", frameHeight, enabled: value < max))
        {
            value = Math.Min(max, value + step);
            changed = true;
        }
        ImGui.PopID();
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(size);
        return changed;
    }
}
