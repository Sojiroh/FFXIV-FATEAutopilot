using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace AutoFateGrind.Windows.Components;

internal static class IconButton
{
    public static bool Draw(FontAwesomeIcon icon, string id, float size, Vector4? color = null, string? tooltip = null, bool enabled = true)
    {
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var buttonSize = new Vector2(size, size);
        var hit = Hit.Area(id, buttonSize, enabled);
        var hover = Motion.Hover(Motion.Key(id), hit.Hovered);
        var windowDrawList = ImGui.GetWindowDrawList();
        var center = cursorScreenPos + buttonSize * 0.5f;
        if (hover > 0.01f)
        {
            var alpha = hover * (hit.Held ? 1f : 0.85f);
            windowDrawList.AddCircleFilled(center, size * 0.5f, Paint.Col(Styling.WithAlpha(Styling.Surface3, alpha)));
        }
        var baseColor = color ?? Styling.TextSecondary;
        var iconColor = enabled ? Vector4.Lerp(baseColor, Styling.TextStrong, hover * 0.55f) : Styling.TextMuted;
        TextDraw.IconCentered(icon, center, iconColor);
        if (hit.Hovered && tooltip != null)
        {
            Tooltip.Show(tooltip);
        }
        return hit.Clicked;
    }
}
