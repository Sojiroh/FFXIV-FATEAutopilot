using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AutoFateGrind.Windows.Components;

internal static class ToggleSwitch
{
    public const float Width = 40f;

    public const float Height = 22f;

    private const float KnobInset = 3f;

    public static bool Draw(string id, ref bool value, bool enabled = true)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(Width * globalScale, Height * globalScale);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var hit = Hit.Area(id, size, enabled);
        var changed = false;
        if (hit.Clicked)
        {
            value = !value;
            changed = true;
        }
        var onAmount = Motion.Approach(Motion.Key(id), value ? 1f : 0f, 16f);
        var hover = Motion.Hover(Motion.Key(id, 1), hit.Hovered);
        var windowDrawList = ImGui.GetWindowDrawList();
        var max = cursorScreenPos + size;
        var radius = size.Y * 0.5f;
        var offColor = Vector4.Lerp(Styling.Surface2, Styling.Surface3, hover);
        var onColor = Vector4.Lerp(Styling.AccentViolet, Styling.AccentVioletSoft, hover * 0.4f);
        var trackColor = Vector4.Lerp(offColor, onColor, onAmount);
        if (onAmount > 0.01f)
        {
            var glowExpand = new Vector2(2f * globalScale, 2f * globalScale);
            windowDrawList.AddRectFilled(cursorScreenPos - glowExpand, max + glowExpand, Paint.Col(Styling.WithAlpha(Styling.AccentViolet, 0.18f * onAmount)), radius + glowExpand.X);
        }
        Paint.Fill(windowDrawList, cursorScreenPos, max, trackColor, radius);
        var borderColor = Vector4.Lerp(Styling.WithAlpha(Styling.BorderDim, 0.8f), Styling.WithAlpha(Styling.AccentVioletSoft, 0.6f), onAmount);
        Paint.Stroke(windowDrawList, cursorScreenPos, max, borderColor, radius);
        var knobPad = KnobInset * globalScale;
        var knobRadius = (size.Y - knobPad * 2f) * 0.5f;
        var travel = size.X - (knobPad + knobRadius) * 2f;
        var knobCenter = new Vector2(cursorScreenPos.X + knobPad + knobRadius + travel * onAmount, cursorScreenPos.Y + size.Y * 0.5f);
        windowDrawList.AddCircleFilled(knobCenter + new Vector2(0f, 1f * globalScale), knobRadius, Paint.Col(new Vector4(0f, 0f, 0f, 0.3f)));
        windowDrawList.AddCircleFilled(knobCenter, knobRadius, Paint.Col(Vector4.Lerp(Styling.TextSecondary, Styling.ForegroundOn(onColor), onAmount)));
        return changed;
    }
}
