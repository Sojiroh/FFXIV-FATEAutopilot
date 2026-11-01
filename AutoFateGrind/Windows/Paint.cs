using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AutoFateGrind.Windows;

internal static class Paint
{
    private const int ShadowLayers = 5;

    public static uint Col(Vector4 color) => ImGui.GetColorU32(color);

    private static uint Opaque(Vector4 color)
    {
        var opaque = color;
        opaque.W = 1f;
        return ImGui.ColorConvertFloat4ToU32(opaque);
    }

    public static void Fill(ImDrawListPtr dl, Vector2 min, Vector2 max, Vector4 color, float rounding, ImDrawFlags flags = ImDrawFlags.RoundCornersAll)
    {
        dl.AddRectFilled(min, max, Col(color), rounding, flags);
    }

    public static void Stroke(ImDrawListPtr dl, Vector2 min, Vector2 max, Vector4 color, float rounding, float thickness = 1f, ImDrawFlags flags = ImDrawFlags.RoundCornersAll)
    {
        dl.AddRect(min, max, Col(color), rounding, flags, thickness);
    }

    public static void Gradient(ImDrawListPtr dl, Vector2 min, Vector2 max, Vector4 top, Vector4 bottom, float rounding, ImDrawFlags flags = ImDrawFlags.RoundCornersAll)
    {
        var firstVertex = dl.VtxBuffer.Size;
        dl.AddRectFilled(min, max, Col(new Vector4(1f, 1f, 1f, top.W)), rounding, flags);
        var lastVertex = dl.VtxBuffer.Size;
        ImGuiP.ShadeVertsLinearColorGradientKeepAlpha(dl, firstVertex, lastVertex, min, new Vector2(min.X, max.Y), Opaque(top), Opaque(bottom));
    }

    public static void GradientH(ImDrawListPtr dl, Vector2 min, Vector2 max, Vector4 left, Vector4 right, float rounding, ImDrawFlags flags = ImDrawFlags.RoundCornersAll)
    {
        var firstVertex = dl.VtxBuffer.Size;
        dl.AddRectFilled(min, max, Col(new Vector4(1f, 1f, 1f, left.W)), rounding, flags);
        var lastVertex = dl.VtxBuffer.Size;
        ImGuiP.ShadeVertsLinearColorGradientKeepAlpha(dl, firstVertex, lastVertex, min, new Vector2(max.X, min.Y), Opaque(left), Opaque(right));
    }

    public static void Shadow(ImDrawListPtr dl, Vector2 min, Vector2 max, float rounding, float spread, float alpha)
    {
        var offset = new Vector2(0f, spread * 0.4f);
        for (var layer = ShadowLayers; layer >= 1; layer--)
        {
            var layerFraction = (float)layer / ShadowLayers;
            var grow = new Vector2(spread * layerFraction, spread * layerFraction);
            var layerAlpha = alpha / ShadowLayers * (1.25f - layerFraction);
            dl.AddRectFilled(min - grow + offset, max + grow + offset, Col(new Vector4(0f, 0f, 0f, layerAlpha)), rounding + grow.X);
        }
    }

    public static void Glow(ImDrawListPtr dl, Vector2 min, Vector2 max, float rounding, Vector4 color, float intensity)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        for (var layer = 3; layer >= 1; layer--)
        {
            var grow = new Vector2((float)layer * 3f * globalScale, (float)layer * 3f * globalScale);
            var a = 0.04f * (float)(4 - layer) * intensity;
            dl.AddRectFilled(min - grow, max + grow, Col(Styling.WithAlpha(color, a)), rounding + grow.X);
        }
    }

    public static void TopLight(ImDrawListPtr dl, Vector2 min, Vector2 max, float rounding, float alpha = 0.075f)
    {
        dl.AddLine(new Vector2(min.X + rounding, min.Y + 1f), new Vector2(max.X - rounding, min.Y + 1f), Col(new Vector4(1f, 1f, 1f, alpha)), 1f);
    }

    public static void Surface(ImDrawListPtr dl, Vector2 min, Vector2 max, float rounding, Vector4 fill, Vector4 border, bool topLight = true, float borderThickness = 1f)
    {
        Fill(dl, min, max, fill, rounding);
        if (topLight)
        {
            TopLight(dl, min, max, rounding);
        }
        Stroke(dl, min, max, border, rounding, borderThickness);
    }

    public static void Glass(ImDrawListPtr dl, Vector2 min, Vector2 max, float rounding, Vector4 accent, float tint, float hover = 0f, bool elevated = false)
    {
        if (elevated)
        {
            Shadow(dl, min, max, rounding, 10f * ImGuiHelpers.GlobalScale, 0.5f);
        }
        var topFill = Vector4.Lerp(Styling.Surface1, accent, tint * 1.3f);
        var bottomFill = Vector4.Lerp(Styling.Surface0, accent, tint * 0.8f);
        if (hover > 0f)
        {
            topFill = Vector4.Lerp(topFill, Styling.Surface2, hover * 0.6f);
            bottomFill = Vector4.Lerp(bottomFill, Styling.Surface1, hover * 0.6f);
        }
        Gradient(dl, min, max, topFill with { W = 0.97f }, bottomFill with { W = 0.97f }, rounding);
        TopLight(dl, min, max, rounding);
        var amount = Math.Clamp(tint * 2.2f + hover * 0.4f, 0f, 1f);
        var color = Vector4.Lerp(Styling.WithAlpha(Styling.BorderDim, 0.75f), Styling.WithAlpha(accent, 0.9f), amount);
        Stroke(dl, min, max, color, rounding);
    }

    public static void Pill(ImDrawListPtr dl, Vector2 min, Vector2 max, Vector4 fill, Vector4 border)
    {
        var rounding = (max.Y - min.Y) * 0.5f;
        Fill(dl, min, max, fill, rounding);
        Stroke(dl, min, max, border, rounding);
    }

    public static void Bar(ImDrawListPtr dl, Vector2 origin, float width, float height, float fraction, Vector4 color)
    {
        var radius = height * 0.5f;
        var max = origin + new Vector2(width, height);
        Fill(dl, origin, max, Styling.WithAlpha(Styling.Surface0, 0.9f), radius);
        Stroke(dl, origin, max, Styling.WithAlpha(Styling.BorderDim, 0.55f), radius);
        fraction = Math.Clamp(fraction, 0f, 1f);
        if (fraction <= 0f)
        {
            return;
        }
        var fillWidth = MathF.Max(height, width * fraction);
        var fillMax = new Vector2(origin.X + fillWidth, max.Y);
        Gradient(dl, origin, fillMax, Styling.Lighten(color, 0.22f), color, radius);
        dl.AddCircleFilled(new Vector2(fillMax.X - radius, origin.Y + radius), radius * 1.7f, Col(Styling.WithAlpha(color, 0.22f)));
    }

    public static void IndeterminateBar(ImDrawListPtr dl, Vector2 origin, float width, float height, Vector4 color, double periodMs = 1500.0)
    {
        var rounding = height * 0.5f;
        var trackMax = origin + new Vector2(width, height);
        Fill(dl, origin, trackMax, Styling.WithAlpha(Styling.Surface0, 0.9f), rounding);
        Stroke(dl, origin, trackMax, Styling.WithAlpha(Styling.BorderDim, 0.55f), rounding);
        var segmentWidth = width * 0.3f;
        var eased = Motion.EaseInOutCubic(Styling.Phase(periodMs));
        var segmentStart = origin.X - segmentWidth + (width + segmentWidth) * eased;
        var min = new Vector2(MathF.Max(origin.X, segmentStart), origin.Y);
        var max = new Vector2(MathF.Min(trackMax.X, segmentStart + segmentWidth), trackMax.Y);
        if (max.X - min.X < 1f)
        {
            return;
        }
        dl.PushClipRect(origin, trackMax, intersectWithCurrentClipRect: true);
        GradientH(dl, min, max, Styling.WithAlpha(Styling.Lighten(color, 0.3f), 0.85f), Styling.WithAlpha(color, 0.85f), rounding);
        dl.PopClipRect();
    }

    public static void Dot(ImDrawListPtr dl, Vector2 center, float radius, Vector4 color, float haloAlpha = 0.22f)
    {
        dl.AddCircleFilled(center, radius * 2.3f, Col(Styling.WithAlpha(color, haloAlpha)));
        dl.AddCircleFilled(center, radius, Col(color));
    }

    public static void Hairline(ImDrawListPtr dl, Vector2 from, Vector2 to)
    {
        dl.AddLine(from, to, Col(Styling.Hairline), 1f);
    }

    public static void Divider(float verticalPadding = 6f)
    {
        Styling.VSpace(verticalPadding);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var x = ImGui.GetContentRegionAvail().X;
        Hairline(ImGui.GetWindowDrawList(), cursorScreenPos, cursorScreenPos + new Vector2(x, 0f));
        ImGui.Dummy(new Vector2(x, 1f));
        Styling.VSpace(verticalPadding);
    }

    public static void Check(ImDrawListPtr dl, Vector2 center, float size, Vector4 color, float thickness)
    {
        var start = center + new Vector2(-size * 0.42f, 0f);
        var corner = center + new Vector2(-size * 0.1f, size * 0.32f);
        var end = center + new Vector2(size * 0.46f, -size * 0.34f);
        var col = Col(color);
        dl.AddLine(start, corner, col, thickness);
        dl.AddLine(corner, end, col, thickness);
    }
}
