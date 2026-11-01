using System;
using System.Numerics;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Stats;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoFateGrind.Windows.Pages;

internal sealed class HistoryPage
{
    private const int ChartRuns = 24;

    private const float PadX = 14f;

    private const float MetricWidth = 64f;

    private const float ConfirmSlide = 12f;

    private bool confirmClear;

    public void Draw(Plugin plugin)
    {
        var history = plugin.History;
        var lifetime = history.Lifetime;
        var subtitle = ((history.Records.Count == 0) ? Loc.T(L.History.Empty) : Loc.Plural(L.History.Summary, history.Records.Count, Formatting.Elapsed(lifetime.Duration), lifetime.FatesPerHour.ToString("F1", Loc.Culture)));
        PageHeader.Draw(Loc.T(L.History.Title), subtitle);
        DrawLifetime(lifetime);
        Styling.VSpace(14f);
        if (history.Records.Count == 0)
        {
            DrawEmptyState();
            return;
        }
        Label(Loc.T(L.History.FatesPerRun));
        DrawChart(history);
        Styling.VSpace(12f);
        Label(Loc.T(L.History.RecentRuns));
        var records = history.Records;
        for (int i = 0; i < records.Count; i++)
        {
            DrawRow(records[i], i);
            Styling.VSpace(2f);
        }
        Styling.VSpace(8f);
        DrawClearControl(history);
    }

    private static void Label(string text)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursor = ImGui.GetCursorScreenPos();
        var titleSize = TextDraw.SectionTitleSize(text);
        TextDraw.SectionTitle(text, cursor, Styling.TextStrong);
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, titleSize.Y + 8f * globalScale));
    }

    private static void DrawLifetime(RunHistory.LifetimeTotals totals)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var gap = 8f * globalScale;
        var width = (ImGui.GetContentRegionAvail().X - gap * 4f) / 5f;
        StatTile.Draw(Loc.T(L.History.TileRuns), totals.Runs.ToString("N0", Loc.Culture), null, Styling.AccentViolet, width);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.History.TileFates), totals.Fates.ToString("N0", Loc.Culture), null, Styling.AccentBlue, width);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.History.TileGems), totals.Gemstones.ToString("N0", Loc.Culture), null, Styling.AccentAmber, width);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.History.TileExp), Formatting.Exp(totals.Exp), null, Styling.AccentMint, width);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.History.TileLevels), (totals.Levels > 0) ? $"+{totals.Levels}" : "0", null, Styling.AccentPink, width);
    }

    private static void DrawEmptyState()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, 110f * globalScale);
        var min = ImGui.GetCursorScreenPos();
        var max = min + size;
        Paint.Surface(ImGui.GetWindowDrawList(), min, max, 10f * globalScale, Styling.WithAlpha(Styling.Surface0, 0.6f), Styling.WithAlpha(Styling.BorderDim, 0.5f), topLight: false);
        var center = new Vector2((min.X + max.X) * 0.5f, min.Y + size.Y * 0.42f);
        ProgressRing.CenterIcon(center, FontAwesomeIcon.History, Styling.TextMuted, 26f * globalScale);
        TextDraw.Center(Loc.T(L.History.NoRuns), center.X, center.Y + 22f * globalScale, Styling.TextMuted);
        ImGui.Dummy(size);
    }

    private static void DrawChart(RunHistory history)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var records = history.Records;
        var shownRuns = Math.Min(ChartRuns, records.Count);
        var size = new Vector2(ImGui.GetContentRegionAvail().X, 96f * globalScale);
        var cardMin = ImGui.GetCursorScreenPos();
        var cardMax = cardMin + size;
        var drawList = ImGui.GetWindowDrawList();
        Paint.Surface(drawList, cardMin, cardMax, 10f * globalScale, Styling.WithAlpha(Styling.Surface0, 0.6f), Styling.WithAlpha(Styling.BorderDim, 0.5f), topLight: false);
        var padX = PadX * globalScale;
        var min = new Vector2(cardMin.X + padX, cardMin.Y + 28f * globalScale);
        var plotMax = new Vector2(cardMax.X - padX, cardMax.Y - 12f * globalScale);
        var plotWidth = plotMax.X - min.X;
        var plotHeight = plotMax.Y - min.Y;
        var peak = 1;
        for (int i = 0; i < shownRuns; i++)
        {
            peak = Math.Max(peak, records[i].FatesCompleted);
        }
        using (Fonts.PushCaption())
        {
            TextDraw.At(Loc.Plural(L.History.ChartRange, shownRuns), new Vector2(min.X, cardMin.Y + 9f * globalScale), Styling.TextMuted);
            TextDraw.Right(Loc.T(L.History.ChartPeak, peak), plotMax.X, cardMin.Y + 9f * globalScale, Styling.TextMuted);
        }
        drawList.AddLine(new Vector2(min.X, plotMax.Y), new Vector2(plotMax.X, plotMax.Y), Paint.Col(Styling.WithAlpha(Styling.BorderDim, 0.7f)), 1f);
        var barGap = 2f * globalScale;
        var barWidth = MathF.Max(2f * globalScale, (plotWidth - barGap * (float)(shownRuns - 1)) / (float)shownRuns);
        var barStep = barWidth + barGap;
        var hoveredBar = -1;
        if (Hit.HoveringRect(min, plotMax))
        {
            hoveredBar = (int)((ImGui.GetMousePos().X - min.X) / barStep);
            if (hoveredBar >= shownRuns)
            {
                hoveredBar = -1;
            }
        }
        var rounding = MathF.Min(4f * globalScale, barWidth * 0.5f);
        for (int j = 0; j < shownRuns; j++)
        {
            var record = records[shownRuns - 1 - j];
            var barHeight = MathF.Max(2f * globalScale, plotHeight * (float)record.FatesCompleted / (float)peak);
            var barMin = new Vector2(min.X + barStep * (float)j, plotMax.Y - barHeight);
            var barMax = new Vector2(barMin.X + barWidth, plotMax.Y);
            var color = ((j == hoveredBar) ? Styling.AccentBlueSoft : Styling.AccentBlue);
            drawList.AddRectFilled(barMin, barMax, Paint.Col(color), rounding, ImDrawFlags.RoundCornersTop);
        }
        if (hoveredBar >= 0)
        {
            var hoveredRecord = records[shownRuns - 1 - hoveredBar];
            Tooltip.Show(Loc.T(L.History.ChartTooltip, RelativeTime(hoveredRecord.EndedAtUtc), hoveredRecord.FatesCompleted, hoveredRecord.GemstonesEarned, Formatting.Elapsed(hoveredRecord.Duration)));
        }
        ImGui.Dummy(size);
    }

    private static void DrawRow(RunRecord record, int index)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, 60f * globalScale);
        if (!ImGui.IsRectVisible(size))
        {
            ImGui.Dummy(size);
            return;
        }
        var min = ImGui.GetCursorScreenPos();
        var max = min + size;
        var drawList = ImGui.GetWindowDrawList();
        ImGui.PushID(index + 1);
        var hit = Hit.Area("##run", size, enabled: true, handCursor: false);
        var hover = Motion.Hover(Motion.Key("##run"), hit.Hovered);
        ImGui.PopID();
        Paint.Glass(drawList, min, max, 10f * globalScale, Styling.AccentViolet, 0.02f, hover);
        var padX = PadX * globalScale;
        var midY = min.Y + size.Y * 0.5f;
        var relativeTime = RelativeTime(record.EndedAtUtc);
        var job = (string.IsNullOrEmpty(record.JobAbbr) ? "—" : record.JobAbbr);
        var elapsed = Formatting.Elapsed(record.Duration);
        var detail = (string.IsNullOrEmpty(record.ModeName) ? Loc.T(L.History.RowDetailNoMode, job, elapsed) : Loc.T(L.History.RowDetail, job, record.ModeName, elapsed));
        var whenSize = TextDraw.Measure(relativeTime);
        Vector2 detailSize;
        using (Fonts.PushCaption())
        {
            detailSize = TextDraw.Measure(detail);
        }
        var textY = midY - (whenSize.Y + 3f * globalScale + detailSize.Y) * 0.5f;
        TextDraw.At(relativeTime, new Vector2(min.X + padX, textY), Styling.TextStrong);
        using (Fonts.PushCaption())
        {
            TextDraw.At(detail, new Vector2(min.X + padX, textY + whenSize.Y + 3f * globalScale), Styling.TextDim);
        }
        var metricWidth = MetricWidth * globalScale;
        var levelsX = max.X - padX - metricWidth;
        DrawMetric(levelsX, midY, metricWidth, (record.LevelsGained > 0) ? $"+{record.LevelsGained}" : "—", Loc.T(L.History.TileLevels), (record.LevelsGained > 0) ? Styling.AccentPink : Styling.TextMuted);
        var expX = levelsX - metricWidth;
        DrawMetric(expX, midY, metricWidth, (record.ExpEarned > 0) ? Formatting.Exp(record.ExpEarned) : "—", Loc.T(L.History.TileExp), (record.ExpEarned > 0) ? Styling.AccentMint : Styling.TextMuted);
        var gemsX = expX - metricWidth;
        DrawMetric(gemsX, midY, metricWidth, (record.GemstonesEarned > 0) ? record.GemstonesEarned.ToString(Loc.Culture) : "—", Loc.T(L.History.TileGems), (record.GemstonesEarned > 0) ? Styling.AccentAmber : Styling.TextMuted);
        DrawMetric(gemsX - metricWidth, midY, metricWidth, record.FatesCompleted.ToString(Loc.Culture), Loc.T(L.History.TileFates), Styling.AccentBlue);
        if (hit.Hovered)
        {
            Tooltip.Show(RunTooltip(record));
        }
    }

    private static void DrawMetric(float x, float midY, float width, string value, string label, Vector4 color)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var valueSize = TextDraw.Measure(value);
        var labelSize = TextDraw.SmallCapsSize(label);
        var textY = midY - (valueSize.Y + 2f * globalScale + labelSize.Y) * 0.5f;
        var centerX = x + width * 0.5f;
        TextDraw.At(value, new Vector2(centerX - valueSize.X * 0.5f, textY), color);
        TextDraw.SmallCaps(label, new Vector2(centerX - labelSize.X * 0.5f, textY + valueSize.Y + 2f * globalScale), Styling.TextMuted);
    }

    private static string RunTooltip(RunRecord record)
    {
        var tooltip = record.EndedAtUtc.ToLocalTime().ToString("g", Loc.Culture);
        if (!string.IsNullOrEmpty(record.ModeName))
        {
            tooltip = tooltip + "\n" + Loc.T(L.History.TooltipMode, record.ModeName);
        }
        if (record.EndLevel > 0)
        {
            tooltip = tooltip + "\n" + Loc.T(L.History.TooltipLevel, record.StartLevel, record.EndLevel);
        }
        if (record.FatesPerHour > 0.0)
        {
            tooltip = tooltip + "\n" + Loc.T(L.History.TooltipRate, record.FatesPerHour.ToString("F1", Loc.Culture), Formatting.Exp((long)record.ExpPerHour));
        }
        if (record.ZoneNames.Count > 0)
        {
            tooltip = tooltip + "\n" + Loc.T(L.History.TooltipZones, string.Join(", ", record.ZoneNames));
        }
        return tooltip;
    }

    private static string RelativeTime(DateTime utc)
    {
        var age = DateTime.UtcNow - utc;
        if (age.TotalSeconds < 60.0)
        {
            return Loc.T(L.History.JustNow);
        }
        if (age.TotalMinutes < 60.0)
        {
            return Loc.T(L.History.MinutesAgo, (int)age.TotalMinutes);
        }
        if (age.TotalHours < 24.0)
        {
            return Loc.T(L.History.HoursAgo, (int)age.TotalHours);
        }
        if (age.TotalDays < 7.0)
        {
            return Loc.T(L.History.DaysAgo, (int)age.TotalDays);
        }
        return utc.ToLocalTime().ToString("M", Loc.Culture);
    }

    private void DrawClearControl(RunHistory history)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var width = ImGui.GetContentRegionAvail().X;
        var cursor = ImGui.GetCursorScreenPos();
        var reveal = Motion.Transition(Motion.Key("##fta_hist_clear_state"), confirmClear);
        var slide = (1f - reveal) * ConfirmSlide * globalScale;
        using (Motion.PushAlpha(reveal))
        {
            if (!confirmClear)
            {
                var label = Loc.T(L.History.ClearHistory);
                ImGui.SetCursorScreenPos(new Vector2(cursor.X + width - PillButton.Width(label, FontAwesomeIcon.Trash) - slide, cursor.Y));
                if (PillButton.Draw("##fta_hist_clear", label, Styling.AccentRose, PillButton.Emphasis.Ghost, FontAwesomeIcon.Trash))
                {
                    confirmClear = true;
                }
                return;
            }
            var question = Loc.T(L.History.ClearQuestion);
            var yesLabel = Loc.T(L.History.ClearYes);
            var cancelLabel = Loc.T(L.Common.Cancel);
            var questionSize = TextDraw.Measure(question);
            var yesWidth = PillButton.Width(yesLabel);
            var cancelWidth = PillButton.Width(cancelLabel);
            var gap = 8f * globalScale;
            var rowHeight = 28f * globalScale;
            var buttonX = cursor.X + width - cancelWidth - slide;
            ImGui.SetCursorScreenPos(new Vector2(buttonX, cursor.Y));
            if (PillButton.Draw("##fta_hist_clear_no", cancelLabel, Styling.AccentViolet, PillButton.Emphasis.Ghost))
            {
                confirmClear = false;
            }
            buttonX -= gap + yesWidth;
            ImGui.SetCursorScreenPos(new Vector2(buttonX, cursor.Y));
            if (PillButton.Draw("##fta_hist_clear_yes", yesLabel, Styling.AccentRose, PillButton.Emphasis.Filled))
            {
                history.Clear();
                confirmClear = false;
            }
            TextDraw.At(question, new Vector2(buttonX - gap * 1.5f - questionSize.X, cursor.Y + (rowHeight - questionSize.Y) * 0.5f), Styling.AccentRoseSoft);
        }
    }
}
