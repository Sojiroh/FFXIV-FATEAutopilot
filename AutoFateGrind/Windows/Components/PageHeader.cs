using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AutoFateGrind.Windows.Components;

internal static class PageHeader
{
    public static void Draw(string title, string subtitle, Vector4? subtitleColor = null)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        Vector2 titleSize;
        using (Fonts.PushTitle())
        {
            titleSize = TextDraw.Measure(title);
            TextDraw.At(title, cursorScreenPos, Styling.TextStrong);
        }
        var subtitleTop = cursorScreenPos.Y + titleSize.Y + 5f * globalScale;
        var subtitleSize = TextDraw.MeasureWrapped(subtitle, width);
        TextDraw.Wrapped(subtitle, new Vector2(cursorScreenPos.X, subtitleTop), width, subtitleColor ?? Styling.TextDim);
        var totalHeight = titleSize.Y + 5f * globalScale + subtitleSize.Y;
        ImGui.Dummy(new Vector2(width, totalHeight));
        Paint.Divider(12f);
    }
}
