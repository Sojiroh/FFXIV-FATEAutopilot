using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoFateGrind.Windows.Components;

internal static class SearchField
{
    public const int DefaultMaxLength = 96;

    private const float PadX = 12f;

    private const float IconGap = 8f;

    private const float ClearButtonSize = 22f;

    public static bool Draw(string id, string hint, ref string text, ref bool focused, float width, float height, bool requestFocus = false, int maxLength = DefaultMaxLength)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var max = cursorScreenPos + new Vector2(width, height);
        var padX = PadX * globalScale;
        var windowDrawList = ImGui.GetWindowDrawList();
        var rounding = height * 0.5f;
        var amount = Motion.Approach(Motion.Key(id, 1), focused ? 1f : 0f, 16f);
        Paint.Fill(windowDrawList, cursorScreenPos, max, Styling.WithAlpha(Styling.Surface0, 0.9f), rounding);
        Paint.Stroke(windowDrawList, cursorScreenPos, max, Vector4.Lerp(Styling.WithAlpha(Styling.BorderDim, 0.75f), Styling.WithAlpha(Styling.AccentVioletSoft, 0.85f), amount), rounding);
        var iconSize = TextDraw.IconSize(FontAwesomeIcon.Search);
        TextDraw.Icon(FontAwesomeIcon.Search, new Vector2(cursorScreenPos.X + padX, cursorScreenPos.Y + (height - iconSize.Y) * 0.5f), Vector4.Lerp(Styling.TextMuted, Styling.AccentVioletSoft, amount));
        var clearSize = ClearButtonSize * globalScale;
        var hasText = text.Length > 0;
        var inputLeft = cursorScreenPos.X + padX + iconSize.X + IconGap * globalScale;
        var inputRight = max.X - padX - (hasText ? clearSize : 0f);
        ImGui.SetCursorScreenPos(new Vector2(inputLeft, cursorScreenPos.Y + (height - ImGui.GetFrameHeight()) * 0.5f));
        ImGui.SetNextItemWidth(MathF.Max(1f, inputRight - inputLeft));
        if (requestFocus)
        {
            ImGui.SetKeyboardFocusHere();
        }
        bool changed;
        using (ImRaii.PushColor(ImGuiCol.FrameBg, Vector4.Zero).Push(ImGuiCol.FrameBgHovered, Vector4.Zero).Push(ImGuiCol.FrameBgActive, Vector4.Zero))
        {
            changed = ImGui.InputTextWithHint(id, hint, ref text, maxLength);
        }
        focused = ImGui.IsItemActive();
        if (hasText)
        {
            ImGui.SetCursorScreenPos(new Vector2(max.X - padX * 0.5f - clearSize, cursorScreenPos.Y + (height - clearSize) * 0.5f));
            if (IconButton.Draw(FontAwesomeIcon.Times, id + "_clear", clearSize, Styling.TextDim))
            {
                text = string.Empty;
                changed = true;
            }
        }
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(new Vector2(width, height));
        return changed;
    }
}
