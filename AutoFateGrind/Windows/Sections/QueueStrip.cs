using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Tasks;
using AutoFateGrind.Core.Zones;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoFateGrind.Windows.Sections;

internal static class QueueStrip
{
    private readonly record struct ChipMetrics(float Total, float BodyWidth, float PadX, float NumberWidth, float NameWidth, bool HasActive, float BoltWidth, float Gap, float Height, string Number, string Name, string Count);

    private const float ChipHeight = 32f;

    private const float Gap = 8f;

    public static void Draw(Configuration cfg, AutoFateController controller)
    {
        var zonesById = ZoneRegistry.Zones.ToDictionary(z => z.TerritoryId);
        var selectedIds = cfg.SelectedZones.Where(zonesById.ContainsKey).ToList();
        if (selectedIds.Count == 0)
        {
            dragIndex = null;
            using (Fonts.PushCaption())
            {
                var hintPos = ImGui.GetCursorScreenPos();
                var hint = Loc.T(L.Grind.OrderHint);
                TextDraw.At(hint, new Vector2(hintPos.X + 2f * ImGuiHelpers.GlobalScale, hintPos.Y), Styling.TextMuted);
                ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, TextDraw.Measure(hint).Y));
            }
            return;
        }
        var running = controller.Running;
        var scale = ImGuiHelpers.GlobalScale;
        var chipHeight = ChipHeight * scale;
        var gap = Gap * scale;
        var boltWidth = TextDraw.IconSize(FontAwesomeIcon.Bolt).X;
        var timesWidth = TextDraw.IconSize(FontAwesomeIcon.Times).X;
        var mousePos = ImGui.GetMousePos();
        var dragging = !running && dragIndex is { } && ImGui.IsMouseDown(ImGuiMouseButton.Left);
        var origin = ImGui.GetCursorScreenPos();
        var availWidth = ImGui.GetContentRegionAvail().X;
        var cursorX = origin.X;
        var cursorY = origin.Y;
        var bottomY = cursorY + chipHeight;
        int? remove = null;
        (Vector2, Vector2)[] chipRects = new (Vector2, Vector2)[selectedIds.Count];
        for (int i = 0; i < selectedIds.Count; i++)
        {
            var zone = zonesById[selectedIds[i]];
            ZoneStateReader.Refresh(zone);
            var metrics = Measure(zone, i + 1, chipHeight, boltWidth, timesWidth, scale);
            if (cursorX > origin.X && cursorX + metrics.Total > origin.X + availWidth)
            {
                cursorX = origin.X;
                cursorY += chipHeight + gap;
            }
            var chipMin = new Vector2(cursorX, cursorY);
            var chipMax = chipMin + new Vector2(metrics.Total, chipHeight);
            chipRects[i] = (chipMin, chipMax);
            var isDropTarget = dragging && dragIndex != i && Contains(chipMin, chipMax, mousePos);
            var skipped = ZoneSelection.IsSkippedByGoal(cfg, zone.TerritoryId);
            DrawChip(chipMin, chipMax, metrics, zone, i, running, skipped, isDropTarget, ref remove);
            cursorX += metrics.Total + gap;
            bottomY = Math.Max(bottomY, cursorY + chipHeight);
        }
        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(availWidth, bottomY - origin.Y));
        (int, int)? move = null;
        if (!running && dragIndex is { } draggedIndex)
        {
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                int dropIndex = -1;
                for (int j = 0; j < chipRects.Length; j++)
                {
                    if (Contains(chipRects[j].Item1, chipRects[j].Item2, mousePos))
                    {
                        dropIndex = j;
                    }
                }
                if (dropIndex >= 0 && dropIndex != draggedIndex)
                {
                    move = (draggedIndex, dropIndex);
                }
                dragIndex = null;
            }
            else if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                dragIndex = null;
            }
            else if (draggedIndex < selectedIds.Count)
            {
                DrawDragPreview(zonesById[selectedIds[draggedIndex]].Name, mousePos);
            }
        }
        var changed = false;
        if (remove is { } removeIndex)
        {
            changed = RemoveAtFiltered(cfg.SelectedZones, zonesById, removeIndex);
        }
        else if (move is { } moveIndices)
        {
            changed = MoveFiltered(cfg.SelectedZones, zonesById, moveIndices.Item1, moveIndices.Item2);
        }
        if (changed)
        {
            cfg.SaveDebounced();
        }
    }

    private static int? dragIndex;

    private static bool Contains(Vector2 min, Vector2 max, Vector2 p)
    {
        if (p.X >= min.X && p.X <= max.X && p.Y >= min.Y)
        {
            return p.Y <= max.Y;
        }
        return false;
    }

    private static ChipMetrics Measure(ZoneInfo zone, int position, float height, float boltWidth, float timesWidth, float scale)
    {
        var padX = 11f * scale;
        var gap = 6f * scale;
        var number = position.ToString();
        var name = zone.Name;
        var numberWidth = TextDraw.Measure(number).X;
        var nameWidth = TextDraw.Measure(name).X;
        var hasActive = zone.ActiveFateCount > 0;
        var count = hasActive ? zone.ActiveFateCount.ToString() : string.Empty;
        var countWidth = hasActive ? TextDraw.Measure(count).X : 0f;
        var bodyWidth = padX + numberWidth + gap + nameWidth + (hasActive ? (gap + boltWidth + 3f * scale + countWidth) : 0f) + gap;
        var closeWidth = timesWidth + gap * 2f;
        return new ChipMetrics(bodyWidth + closeWidth, bodyWidth, padX, numberWidth, nameWidth, hasActive, boltWidth, gap, height, number, name, count);
    }

    private static void DrawChip(Vector2 origin, Vector2 end, ChipMetrics metrics, ZoneInfo zone, int index, bool running, bool skipped, bool isDropTarget, ref int? remove)
    {
        var drawList = ImGui.GetWindowDrawList();
        var scale = ImGuiHelpers.GlobalScale;
        var disabled = running | skipped;
        ImGui.SetCursorScreenPos(origin);
        ImGui.InvisibleButton($"##qchip{zone.TerritoryId}", new Vector2(metrics.BodyWidth, metrics.Height));
        var bodyHovered = ImGui.IsItemHovered();
        if (!running && ImGui.IsItemActivated())
        {
            dragIndex = index;
        }
        var dragged = dragIndex == index;
        if (!running && (bodyHovered | dragged))
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
        }
        if ((!running & bodyHovered) && !dragged && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            Tooltip.Show(skipped ? Loc.T(L.Grind.OrderSkippedNoRanks) : Loc.T(L.Grind.DragToReorder));
        }
        ImGui.SetCursorScreenPos(new Vector2(origin.X + metrics.BodyWidth, origin.Y));
        var removeClicked = ImGui.InvisibleButton($"##qx{zone.TerritoryId}", new Vector2(end.X - origin.X - metrics.BodyWidth, metrics.Height));
        var removeHovered = ImGui.IsItemHovered();
        if (!running)
        {
            if (removeHovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            if (removeClicked)
            {
                remove = index;
            }
        }
        var hover = Motion.Hover(Motion.Key("##qchip", zone.TerritoryId), !running && (bodyHovered | dragged));
        var accentViolet = Styling.AccentViolet;
        var rounding = metrics.Height * 0.5f;
        var violetAmount = disabled ? 0.06f : dragged ? 0.55f : isDropTarget ? 0.5f : 0.28f + 0.14f * hover;
        var topColor = Styling.Tint(Styling.Surface2, accentViolet, violetAmount);
        var bottomColor = Styling.Tint(Styling.Surface1, accentViolet, violetAmount * 0.8f);
        Paint.Gradient(drawList, origin, end, topColor, bottomColor, rounding);
        Paint.TopLight(drawList, origin, end, rounding, 0.09f);
        var strokeColor = disabled ? Styling.WithAlpha(Styling.BorderDim, 0.6f) : (isDropTarget ? Styling.AccentVioletSoft : Styling.WithAlpha(accentViolet, 0.5f + 0.35f * hover));
        Paint.Stroke(drawList, origin, end, strokeColor, rounding, isDropTarget ? 1.8f : 1f);
        var numberColor = disabled ? Styling.TextMuted : Styling.TextDim;
        var nameColor = disabled ? Styling.TextDim : Styling.TextStrong;
        var midY = origin.Y + metrics.Height * 0.5f;
        var cursorX = origin.X + metrics.PadX;
        PutText(metrics.Number, cursorX, midY, numberColor);
        cursorX += metrics.NumberWidth + metrics.Gap;
        PutText(metrics.Name, cursorX, midY, nameColor);
        cursorX += metrics.NameWidth;
        if (metrics.HasActive)
        {
            cursorX += metrics.Gap;
            var boltColor = disabled ? Styling.TextMuted : Styling.AccentAmber;
            var boltSize = TextDraw.IconSize(FontAwesomeIcon.Bolt);
            TextDraw.Icon(FontAwesomeIcon.Bolt, new Vector2(cursorX, midY - boltSize.Y * 0.5f), boltColor);
            cursorX += metrics.BoltWidth + 3f * scale;
            PutText(metrics.Count, cursorX, midY, boltColor);
        }
        var removeColor = running ? Styling.TextMuted : (removeHovered ? Styling.AccentRose : Styling.TextDim);
        var timesSize = TextDraw.IconSize(FontAwesomeIcon.Times);
        TextDraw.Icon(FontAwesomeIcon.Times, new Vector2(origin.X + metrics.BodyWidth + metrics.Gap, midY - timesSize.Y * 0.5f), removeColor);
        if (!running & removeHovered)
        {
            Tooltip.Show(Loc.T(L.Grind.RemoveFromOrder));
        }
    }

    private static void DrawDragPreview(string name, Vector2 mouse)
    {
        var drawList = ImGui.GetForegroundDrawList();
        var scale = ImGuiHelpers.GlobalScale;
        var padding = new Vector2(10f, 5f) * scale;
        var textPos = mouse + new Vector2(14f, 8f) * scale;
        var textSize = ImGui.CalcTextSize(name);
        var min = textPos - padding;
        var max = textPos + textSize + padding;
        Paint.Shadow(drawList, min, max, (max.Y - min.Y) * 0.5f, 6f * scale, 0.5f);
        Paint.Pill(drawList, min, max, Styling.Tint(Styling.Surface2, Styling.AccentViolet, 0.45f), Styling.WithAlpha(Styling.AccentVioletSoft, 0.7f));
        drawList.AddText(textPos, Paint.Col(Styling.TextStrong), name);
    }

    private static void PutText(string text, float x, float midY, Vector4 color)
    {
        var textSize = TextDraw.Measure(text);
        TextDraw.At(text, new Vector2(x, midY - textSize.Y * 0.5f), color);
    }

    private static int FindId(List<uint> selected, Dictionary<uint, ZoneInfo> byId, int filteredIndex)
    {
        int matchCount = -1;
        for (int i = 0; i < selected.Count; i++)
        {
            if (byId.ContainsKey(selected[i]) && ++matchCount == filteredIndex)
            {
                return i;
            }
        }
        return -1;
    }

    private static bool RemoveAtFiltered(List<uint> selected, Dictionary<uint, ZoneInfo> byId, int filteredIndex)
    {
        var index = FindId(selected, byId, filteredIndex);
        if (index < 0)
        {
            return false;
        }
        selected.RemoveAt(index);
        return true;
    }

    private static bool MoveFiltered(List<uint> selected, Dictionary<uint, ZoneInfo> byId, int fromFiltered, int toFiltered)
    {
        var from = FindId(selected, byId, fromFiltered);
        var to = FindId(selected, byId, toFiltered);
        return ListReorder.Move(selected, from, to);
    }
}
