using System;
using System.Numerics;
using AutoFateGrind.Core.Game.Yokai;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Tasks;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoFateGrind.Windows.Sections;

internal static class YokaiRoster
{
    private const float Gap = 8f;

    private static readonly (int Medals, int Target, string? Text)[] progressLabels = new (int, int, string?)[YokaiCatalog.Entries.Length];

    public static void Draw(Configuration cfg, AutoFateController ctrl, bool scrollIntoView)
    {
        DrawHeader(cfg, scrollIntoView);
        Styling.VSpace(6f);
        DrawPlanNote(cfg);
        Styling.VSpace(10f);
        DrawGrid(cfg, ctrl);
    }

    public static void DrawPlanNote(Configuration cfg)
    {
        var targetIndex = YokaiProgress.ResolveTargetIndex(cfg, 0u);
        var note = targetIndex < 0 ? Loc.T(L.Grind.DetailNoYokai) : Loc.T(L.Grind.YokaiPlanNext, YokaiProgress.MinionName(targetIndex), YokaiProgress.ZoneNames(targetIndex));
        using (Fonts.PushCaption())
        {
            var origin = ImGui.GetCursorScreenPos();
            var width = ImGui.GetContentRegionAvail().X;
            TextDraw.Wrapped(note, new Vector2(origin.X + 2f * ImGuiHelpers.GlobalScale, origin.Y), width, Styling.TextMuted);
            ImGui.Dummy(new Vector2(width, TextDraw.MeasureWrapped(note, width).Y));
        }
    }

    private static void DrawHeader(Configuration cfg, bool scrollIntoView)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var rowHeight = 36f * scale;
        var midY = origin.Y + rowHeight * 0.5f;
        var label = Loc.T(L.Grind.Yokai);
        var titleSize = TextDraw.SectionTitleSize(label);
        TextDraw.SectionTitle(label, new Vector2(origin.X, midY - titleSize.Y * 0.5f), Styling.TextStrong);
        var (medals, target) = YokaiProgress.Totals(cfg);
        using (Fonts.PushCaption())
        {
            var summary = Loc.T(L.Grind.YokaiSummary, medals, target);
            var summarySize = TextDraw.Measure(summary);
            TextDraw.At(summary, new Vector2(origin.X + width - summarySize.X, midY - summarySize.Y * 0.5f), Styling.TextDim);
        }
        ImGui.Dummy(new Vector2(width, rowHeight));
        if (scrollIntoView)
        {
            ImGui.SetScrollHereY(0f);
        }
    }

    private static void DrawGrid(Configuration cfg, AutoFateController ctrl)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var gap = Gap * scale;
        var availWidth = ImGui.GetContentRegionAvail().X;
        var columns = Math.Max(1, (int)MathF.Floor((availWidth + gap) / (230f * scale + gap)));
        var width = (availWidth - gap * (float)(columns - 1)) / (float)columns;
        var targetIndex = YokaiProgress.ResolveTargetIndex(cfg, 0u);
        var entryCount = YokaiCatalog.Entries.Length;
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(gap, gap)))
        {
            for (int i = 0; i < entryCount; i++)
            {
                if (i % columns != 0)
                {
                    ImGui.SameLine(0f, gap);
                }
                DrawCard(cfg, ctrl, i, width, i == targetIndex);
            }
        }
    }

    private static void DrawCard(Configuration cfg, AutoFateController ctrl, int entryIndex, float width, bool isTarget)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(width, 48f * scale);
        var origin = ImGui.GetCursorScreenPos();
        var max = origin + size;
        var status = YokaiProgress.Statuses[entryIndex];
        var target = YokaiProgress.MedalTarget(cfg);
        var selectable = status.MinionUnlocked && !status.WeaponOwned;
        var selected = selectable && YokaiProgress.IsEnabled(cfg, entryIndex);
        var enabled = selectable && !ctrl.Running;
        ImGui.PushID(entryIndex + 1);
        Hit.Result hit = Hit.Area("##yokai", size, enabled);
        var hover = Motion.Hover(Motion.Key("##yokai"), hit.Hovered);
        var selectedAmount = Motion.Approach(Motion.Key("##yokai", 1), selected ? 1f : 0f);
        ImGui.PopID();
        if (hit.Clicked)
        {
            Toggle(cfg, YokaiCatalog.Entries[entryIndex].MinionId);
        }
        var drawList = ImGui.GetWindowDrawList();
        Paint.Glass(drawList, origin, max, 10f * scale, Styling.AccentViolet, 0.02f + 0.16f * selectedAmount, hover);
        var midY = origin.Y + size.Y * 0.5f;
        var radius = 9f * scale;
        var center = new Vector2(origin.X + 14f * scale + radius, midY);
        DrawSelector(drawList, center, radius, status, selectedAmount);
        var rightX = max.X - 12f * scale;
        rightX -= DrawProgress(entryIndex, status, target, rightX, midY) + 8f * scale;
        if (isTarget)
        {
            rightX -= DrawNextBadge(drawList, rightX, midY) + 8f * scale;
        }
        var textX = center.X + radius + 12f * scale;
        var color = selectable ? Vector4.Lerp(Styling.TextSecondary, Styling.TextStrong, MathF.Max(selectedAmount, hover)) : Styling.TextMuted;
        var name = TextDraw.Truncate(YokaiProgress.MinionName(entryIndex), rightX - textX);
        var nameSize = TextDraw.Measure(name);
        TextDraw.At(name, new Vector2(textX, midY - nameSize.Y * 0.5f), color);
        if (Hit.HoveringRect(origin, max))
        {
            Tooltip.Show(CardTooltip(ctrl, entryIndex, status));
        }
    }

    private static void DrawSelector(ImDrawListPtr dl, Vector2 center, float radius, YokaiStatus status, float active)
    {
        var scale = ImGuiHelpers.GlobalScale;
        if (!status.MinionUnlocked)
        {
            TextDraw.IconCentered(FontAwesomeIcon.Lock, center, Styling.TextMuted);
            return;
        }
        if (status.WeaponOwned)
        {
            TextDraw.IconCentered(FontAwesomeIcon.Check, center, Styling.AccentMint);
            return;
        }
        var color = Vector4.Lerp(Styling.WithAlpha(Styling.BorderDim, 0.9f), Styling.AccentVioletSoft, active);
        dl.AddCircle(center, radius, Paint.Col(color), 0, 1.4f * scale);
        if (active <= 0.01f)
        {
            return;
        }
        dl.AddCircleFilled(center, radius * active, Paint.Col(Styling.AccentViolet));
        if (active > 0.5f)
        {
            Paint.Check(dl, center, radius * 1.1f, Styling.WithAlpha(Styling.TextStrong, (active - 0.5f) * 2f), 1.8f * scale);
        }
    }

    private static string ProgressLabel(int entryIndex, int medals, int target)
    {
        var cached = progressLabels[entryIndex];
        if (cached.Text != null && cached.Medals == medals && cached.Target == target)
        {
            return cached.Text;
        }
        var label = $"{medals} / {target}";
        progressLabels[entryIndex] = (Medals: medals, Target: target, Text: label);
        return label;
    }

    private static float DrawProgress(int entryIndex, YokaiStatus status, int target, float rightX, float midY)
    {
        if (!status.MinionUnlocked)
        {
            return 0f;
        }
        using (Fonts.PushCaption())
        {
            var label = status.WeaponOwned ? Loc.T(L.Grind.YokaiWeaponTag) : ProgressLabel(entryIndex, status.Medals, target);
            var color = status.WeaponOwned ? Styling.TextMuted : (status.Medals >= target ? Styling.AccentMint : (!status.Reachable ? Styling.AccentAmber : Styling.TextDim));
            var labelSize = TextDraw.Measure(label);
            TextDraw.At(label, new Vector2(rightX - labelSize.X, midY - labelSize.Y * 0.5f), color);
            return labelSize.X;
        }
    }

    private static float DrawNextBadge(ImDrawListPtr dl, float rightX, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        using (Fonts.PushCaption())
        {
            var label = Loc.T(L.Grind.YokaiNext);
            var size = TextDraw.Measure(label) + new Vector2(12f * scale, 4f * scale);
            var min = new Vector2(rightX - size.X, midY - size.Y * 0.5f);
            var max = min + size;
            Paint.Pill(dl, min, max, Styling.WithAlpha(Styling.AccentViolet, 0.35f), Styling.WithAlpha(Styling.AccentVioletSoft, 0.5f));
            TextDraw.Middle(label, min, max, Styling.TextStrong);
            return size.X;
        }
    }

    private static string CardTooltip(AutoFateController ctrl, int entryIndex, YokaiStatus status)
    {
        if (!status.MinionUnlocked)
        {
            return Loc.T(L.Grind.YokaiMinionMissing);
        }
        if (status.WeaponOwned)
        {
            return Loc.T(L.Grind.YokaiWeaponOwned);
        }
        if (ctrl.Running)
        {
            return Loc.T(L.Grind.YokaiLockedRunning);
        }
        if (!status.Reachable)
        {
            return Loc.T(L.Grind.YokaiUnreachable, YokaiProgress.ZoneNames(entryIndex));
        }
        return Loc.T(L.Grind.YokaiDropsIn, YokaiProgress.ZoneNames(entryIndex));
    }

    private static void Toggle(Configuration cfg, uint minionId)
    {
        if (!cfg.YokaiSkippedMinionIds.Remove(minionId))
        {
            cfg.YokaiSkippedMinionIds.Add(minionId);
        }
        cfg.SaveDebounced();
    }
}
