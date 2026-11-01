using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using AutoFateGrind.Core.Game.Items;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Tasks;
using AutoFateGrind.Core.Zones;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoFateGrind.Windows.Sections;

internal static class ItemGoalRoster
{
    private const float Gap = 8f;

    private const int MaxDrops = 12;

    private static readonly (uint ItemId, int Have, int Need, string? Text)[] progressLabels = new (uint, int, int, string?)[MaxDrops];

    private static readonly StringBuilder noteBuilder = new StringBuilder();

    public static void Draw(Configuration cfg, AutoFateController ctrl, bool scrollIntoView)
    {
        var itemGoal = ItemGoalCatalog.Find(cfg.ActiveMode.Id);
        if (itemGoal == null)
        {
            return;
        }
        DrawHeader(cfg, itemGoal, scrollIntoView);
        Styling.VSpace(6f);
        DrawPlanNote(cfg);
        Styling.VSpace(10f);
        DrawGrid(cfg, itemGoal);
    }

    public static void DrawPlanNote(Configuration cfg)
    {
        if (ItemGoalCatalog.Find(cfg.ActiveMode.Id) == null)
        {
            return;
        }
        var zones = ZoneSelection.ResolveStartList(cfg);
        var note = zones.Count == 0 ? Loc.T(L.Grind.DetailAllCollected) : Loc.T(L.Grind.ItemGoalPlanZones, JoinZoneNames(zones));
        using (Fonts.PushCaption())
        {
            var origin = ImGui.GetCursorScreenPos();
            var width = ImGui.GetContentRegionAvail().X;
            TextDraw.Wrapped(note, new Vector2(origin.X + 2f * ImGuiHelpers.GlobalScale, origin.Y), width, Styling.TextMuted);
            ImGui.Dummy(new Vector2(width, TextDraw.MeasureWrapped(note, width).Y));
        }
    }

    private static string JoinZoneNames(IReadOnlyList<ZoneInfo> zones)
    {
        noteBuilder.Clear();
        for (int i = 0; i < zones.Count; i++)
        {
            if (i > 0)
            {
                noteBuilder.Append(", ");
            }
            noteBuilder.Append(zones[i].Name);
        }
        return noteBuilder.ToString();
    }

    private static void DrawHeader(Configuration cfg, ItemGoalDefinition goal, bool scrollIntoView)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var rowHeight = 36f * scale;
        var midY = origin.Y + rowHeight * 0.5f;
        var label = Loc.T(goal.Name);
        var titleSize = TextDraw.SectionTitleSize(label);
        TextDraw.SectionTitle(label, new Vector2(origin.X, midY - titleSize.Y * 0.5f), Styling.TextStrong);
        var (have, need) = ItemGoalProgress.Totals(goal, cfg);
        using (Fonts.PushCaption())
        {
            var summary = Loc.T(L.Grind.ItemsSummary, have, need);
            var summarySize = TextDraw.Measure(summary);
            TextDraw.At(summary, new Vector2(origin.X + width - summarySize.X, midY - summarySize.Y * 0.5f), Styling.TextDim);
        }
        ImGui.Dummy(new Vector2(width, rowHeight));
        if (scrollIntoView)
        {
            ImGui.SetScrollHereY(0f);
        }
    }

    private static void DrawGrid(Configuration cfg, ItemGoalDefinition goal)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var gap = Gap * scale;
        var availWidth = ImGui.GetContentRegionAvail().X;
        var columns = Math.Max(1, (int)MathF.Floor((availWidth + gap) / (230f * scale + gap)));
        var width = (availWidth - gap * (float)(columns - 1)) / (float)columns;
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(gap, gap)))
        {
            for (int i = 0; i < goal.Drops.Length; i++)
            {
                if (i % columns != 0)
                {
                    ImGui.SameLine(0f, gap);
                }
                DrawCard(cfg, goal, i, width);
            }
        }
    }

    private static void DrawCard(Configuration cfg, ItemGoalDefinition goal, int dropIndex, float width)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(width, 48f * scale);
        var origin = ImGui.GetCursorScreenPos();
        var max = origin + size;
        var status = ItemGoalProgress.Status(goal, dropIndex, cfg);
        var drawList = ImGui.GetWindowDrawList();
        ImGui.PushID(dropIndex + 1);
        Hit.Area("##item", size, enabled: false);
        ImGui.PopID();
        var doneAmount = status.Done ? 1f : 0f;
        Paint.Glass(drawList, origin, max, 10f * scale, Styling.AccentMint, 0.02f + 0.12f * doneAmount);
        var midY = origin.Y + size.Y * 0.5f;
        var radius = 9f * scale;
        var center = new Vector2(origin.X + 14f * scale + radius, midY);
        if (status.Done)
        {
            TextDraw.IconCentered(FontAwesomeIcon.Check, center, Styling.AccentMint);
        }
        else
        {
            drawList.AddCircle(center, radius, Paint.Col(Styling.WithAlpha(Styling.BorderDim, 0.9f)), 0, 1.4f * scale);
        }
        var rightX = max.X - 12f * scale;
        rightX -= DrawProgress(dropIndex, status, rightX, midY) + 8f * scale;
        var textX = center.X + radius + 12f * scale;
        var name = TextDraw.Truncate(ItemGoalProgress.ItemName(status.ItemId), rightX - textX);
        var nameSize = TextDraw.Measure(name);
        TextDraw.At(name, new Vector2(textX, midY - nameSize.Y * 0.5f), status.Done ? Styling.TextDim : Styling.TextSecondary);
        if (Hit.HoveringRect(origin, max))
        {
            Tooltip.Show(Loc.T(L.Grind.ItemDropsIn, ItemGoalProgress.ZoneNames(goal, dropIndex)));
        }
    }

    private static float DrawProgress(int dropIndex, ItemGoalDropStatus status, float rightX, float midY)
    {
        using (Fonts.PushCaption())
        {
            var label = ProgressLabel(dropIndex, status);
            var color = status.Done ? Styling.AccentMint : Styling.TextDim;
            var labelSize = TextDraw.Measure(label);
            TextDraw.At(label, new Vector2(rightX - labelSize.X, midY - labelSize.Y * 0.5f), color);
            return labelSize.X;
        }
    }

    private static string ProgressLabel(int dropIndex, ItemGoalDropStatus status)
    {
        var cached = progressLabels[dropIndex];
        if (cached.Text != null && cached.ItemId == status.ItemId && cached.Have == status.Have && cached.Need == status.Need)
        {
            return cached.Text;
        }
        var text = $"{Math.Min(status.Have, status.Need)} / {status.Need}";
        progressLabels[dropIndex] = (ItemId: status.ItemId, Have: status.Have, Need: status.Need, Text: text);
        return text;
    }
}
