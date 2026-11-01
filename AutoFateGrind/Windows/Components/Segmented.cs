using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoFateGrind.Windows.Components;

internal static class Segmented
{
    public readonly record struct Item(FontAwesomeIcon? Icon, string Label);

    private readonly record struct Metrics(Vector2 Icon, Vector2 Label, float Content);

    public const int MaxItems = 8;

    private const float Inset = 3f;

    private const float SegmentPadX = 14f;

    private const float IconGap = 8f;

    private static readonly Metrics[] metrics = new Metrics[MaxItems];

    private static readonly float[] segmentWidths = new float[MaxItems];

    public static float PreferredWidth(ReadOnlySpan<Item> items)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var count = Math.Min(items.Length, MaxItems);
        var widest = 0f;
        for (int i = 0; i < count; i++)
        {
            widest = MathF.Max(widest, Measure(items[i]).Content);
        }
        return (widest + SegmentPadX * 2f * globalScale) * (float)count + Inset * 2f * globalScale;
    }

    public static bool Draw(string id, ReadOnlySpan<Item> items, ref int selected, bool enabled = true, float height = Layout.SegmentHeight, float width = 0f)
    {
        var count = Math.Min(items.Length, MaxItems);
        if (count == 0)
        {
            return false;
        }
        var globalScale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(width > 0f ? width : ImGui.GetContentRegionAvail().X, height * globalScale);
        var origin = ImGui.GetCursorScreenPos();
        var max = origin + size;
        var inset = Inset * globalScale;
        var radius = size.Y * 0.5f;
        var windowDrawList = ImGui.GetWindowDrawList();
        var selectedIndex = selected < 0 ? -1 : Math.Clamp(selected, 0, count - 1);
        ResolveWidths(items, count, size.X - inset * 2f);
        Paint.Fill(windowDrawList, origin, max, Styling.WithAlpha(Styling.Surface0, 0.9f), radius);
        Paint.Stroke(windowDrawList, origin, max, Styling.WithAlpha(Styling.BorderDim, 0.6f), radius);
        ImGui.PushID(id);
        if (selectedIndex >= 0)
        {
            var offsetX = Motion.Approach(Motion.Key("##seg"), SegmentOffset(selectedIndex), 18f);
            var thumbWidth = Motion.Approach(Motion.Key("##seg", 1), segmentWidths[selectedIndex], 18f);
            var thumbMin = new Vector2(origin.X + inset + offsetX, origin.Y + inset);
            var thumbMax = thumbMin + new Vector2(thumbWidth, size.Y - inset * 2f);
            var accent = enabled ? Styling.AccentViolet : Styling.Surface3;
            Paint.Gradient(windowDrawList, thumbMin, thumbMax, Styling.Tint(Styling.Surface3, accent, 0.3f), Styling.Tint(Styling.Surface2, accent, 0.22f), radius - inset);
            Paint.TopLight(windowDrawList, thumbMin, thumbMax, radius - inset, 0.1f);
            Paint.Stroke(windowDrawList, thumbMin, thumbMax, Styling.WithAlpha(enabled ? Styling.AccentVioletSoft : Styling.BorderDim, 0.55f), radius - inset);
        }
        var changed = false;
        var segmentX = origin.X + inset;
        for (int i = 0; i < count; i++)
        {
            var segmentWidth = segmentWidths[i];
            var segmentMin = new Vector2(segmentX, origin.Y);
            ImGui.SetCursorScreenPos(segmentMin);
            ImGui.PushID(i + 1);
            var hit = Hit.Area("##segment", new Vector2(segmentWidth, size.Y), enabled);
            var hover = Motion.Hover(Motion.Key("##segment"), hit.Hovered);
            ImGui.PopID();
            if (hit.Clicked && selected != i)
            {
                selected = i;
                changed = true;
            }
            var isSelected = i == selectedIndex;
            if (!isSelected && hover > 0.01f)
            {
                Paint.Fill(windowDrawList, segmentMin + new Vector2(0f, inset), segmentMin + new Vector2(segmentWidth, size.Y - inset), Styling.WithAlpha(Styling.Surface2, 0.7f * hover), radius - inset);
            }
            DrawContent(items[i], metrics[i], segmentMin, segmentWidth, size.Y, isSelected, hover, enabled);
            if (hit.Hovered && IsTruncated(metrics[i], segmentWidth))
            {
                Tooltip.Show(items[i].Label);
            }
            segmentX += segmentWidth;
        }
        ImGui.PopID();
        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
        return changed;
    }

    private static void ResolveWidths(ReadOnlySpan<Item> items, int count, float inner)
    {
        var padding = SegmentPadX * 2f * ImGuiHelpers.GlobalScale;
        var equalWidth = inner / (float)count;
        var widestContent = 0f;
        var totalContent = 0f;
        for (int i = 0; i < count; i++)
        {
            metrics[i] = Measure(items[i]);
            widestContent = MathF.Max(widestContent, metrics[i].Content);
            totalContent += metrics[i].Content + padding;
        }
        if (widestContent + padding <= equalWidth)
        {
            for (int j = 0; j < count; j++)
            {
                segmentWidths[j] = equalWidth;
            }
            return;
        }
        var shrink = totalContent > inner ? inner / totalContent : 1f;
        var extra = MathF.Max(0f, inner - totalContent) / (float)count;
        for (int k = 0; k < count; k++)
        {
            segmentWidths[k] = (metrics[k].Content + padding) * shrink + extra;
        }
    }

    private static float SegmentOffset(int index)
    {
        var offset = 0f;
        for (int i = 0; i < index; i++)
        {
            offset += segmentWidths[i];
        }
        return offset;
    }

    private static Metrics Measure(Item item)
    {
        var iconSize = item.Icon is { } iconValue ? TextDraw.IconSize(iconValue) : Vector2.Zero;
        var label = TextDraw.Measure(item.Label);
        var content = label.X + (item.Icon.HasValue ? iconSize.X + IconGap * ImGuiHelpers.GlobalScale : 0f);
        return new Metrics(iconSize, label, content);
    }

    private static bool IsTruncated(Metrics metric, float segmentWidth)
    {
        return metric.Content > segmentWidth - SegmentPadX * 2f * ImGuiHelpers.GlobalScale;
    }

    private static void DrawContent(Item item, Metrics metric, Vector2 segmentMin, float segmentWidth, float height, bool selected, float hover, bool enabled)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var iconAdvance = item.Icon.HasValue ? metric.Icon.X + IconGap * globalScale : 0f;
        var label = item.Label;
        var labelWidth = metric.Label.X;
        if (IsTruncated(metric, segmentWidth))
        {
            label = TextDraw.Truncate(label, MathF.Max(0f, segmentWidth - SegmentPadX * 2f * globalScale - iconAdvance));
            labelWidth = TextDraw.Measure(label).X;
        }
        var textX = segmentMin.X + (segmentWidth - iconAdvance - labelWidth) * 0.5f;
        var centerY = segmentMin.Y + height * 0.5f;
        var textColor = !enabled ? Styling.TextMuted : (selected ? Styling.TextStrong : Vector4.Lerp(Styling.TextDim, Styling.TextSecondary, hover));
        if (item.Icon is { } iconValue)
        {
            var iconColor = !enabled ? Styling.TextMuted : (selected ? Styling.AccentVioletSoft : textColor);
            TextDraw.Icon(iconValue, new Vector2(textX, centerY - metric.Icon.Y * 0.5f), iconColor);
            textX += iconAdvance;
        }
        TextDraw.At(label, new Vector2(textX, centerY - metric.Label.Y * 0.5f), textColor);
    }
}
