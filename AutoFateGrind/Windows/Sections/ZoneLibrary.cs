using System;
using System.Collections.Generic;
using System.Numerics;
using AutoFateGrind.Core.Game.SharedFates;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Tasks;
using AutoFateGrind.Core.Zones;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoFateGrind.Windows.Sections;

internal static class ZoneLibrary
{
    private const float Gap = 8f;

    private const float SummaryRowHeight = 32f;

    private const float ListSlide = 8f;

    private const int CachedLabelCount = 64;

    private const int SearchMaxLength = 64;

    private static readonly ExpansionKind[] expansions = new ExpansionKind[]
    {
        ExpansionKind.DT,
        ExpansionKind.EW,
        ExpansionKind.ShB,
        ExpansionKind.SB,
        ExpansionKind.HW,
        ExpansionKind.ARR
    };

    private static readonly Segmented.Item[] segments = new Segmented.Item[expansions.Length];

    private static readonly string[] countLabels = BuildLabels(string.Empty);

    private static readonly string[] queueLabels = BuildLabels("#");

    private static readonly (SharedFateRank Rank, LanguageInfo? Language, string? Text)[] rankLabels = new (SharedFateRank, LanguageInfo?, string?)[SharedFateCatalog.Entries.Length];

    private static ZoneInfo[]? snapshot;

    private static ZoneInfo[][] groups = Array.Empty<ZoneInfo[]>();

    private static HashSet<uint> knownIds = new HashSet<uint>();

    private static int currentExpansion;

    private static string searchQuery = string.Empty;

    private static string matchedQuery = string.Empty;

    private static ZoneInfo[] matches = Array.Empty<ZoneInfo>();

    public static void Draw(Configuration cfg, AutoFateController ctrl, bool scrollIntoView)
    {
        RefreshGroups();
        DrawHeader(scrollIntoView);
        Styling.VSpace(10f);
        DrawOrder(cfg, ctrl);
        Styling.VSpace(12f);
        var searching = searchQuery.Length > 0;
        if (!searching)
        {
            DrawExpansionPicker();
        }
        using (Motion.PushSwitch("##fta_zone_list", searching ? -1 : currentExpansion, 240f, ListSlide))
        {
            if (searching)
            {
                EnsureMatches();
                DrawSearchSummary();
                DrawGrid(cfg, ctrl, matches, showExpansion: true);
            }
            else
            {
                ZoneInfo[] zones = groups[currentExpansion];
                DrawSummaryRow(cfg, ctrl, zones);
                DrawGrid(cfg, ctrl, zones, showExpansion: false);
            }
        }
    }

    private static void DrawOrder(Configuration cfg, AutoFateController ctrl)
    {
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(6f, 6f) * ImGuiHelpers.GlobalScale))
        {
            ImGui.PushID("##fta_plan_chips");
            ImGui.BeginGroup();
            QueueStrip.Draw(cfg, ctrl);
            ImGui.EndGroup();
            ImGui.PopID();
        }
    }

    private static void DrawExpansionPicker()
    {
        for (int i = 0; i < expansions.Length; i++)
        {
            segments[i] = new Segmented.Item(null, ExpansionLabels.Name(expansions[i]));
        }
        Segmented.Draw("##fta_expansions", segments, ref currentExpansion);
        Styling.VSpace(8f);
    }

    private static void RefreshGroups()
    {
        ZoneInfo[] zones = ZoneRegistry.Zones;
        if (zones == snapshot)
        {
            return;
        }
        snapshot = zones;
        matchedQuery = string.Empty;
        matches = Array.Empty<ZoneInfo>();
        knownIds = new HashSet<uint>(zones.Length);
        List<ZoneInfo>[] byExpansion = new List<ZoneInfo>[expansions.Length];
        for (int i = 0; i < byExpansion.Length; i++)
        {
            byExpansion[i] = new List<ZoneInfo>();
        }
        for (int i = 0; i < zones.Length; i++)
        {
            var zone = zones[i];
            knownIds.Add(zone.TerritoryId);
            byExpansion[(int)zone.Expansion].Add(zone);
        }
        groups = new ZoneInfo[expansions.Length][];
        for (int i = 0; i < byExpansion.Length; i++)
        {
            groups[i] = byExpansion[(int)expansions[i]].ToArray();
        }
    }

    private static void EnsureMatches()
    {
        if (matchedQuery == searchQuery)
        {
            return;
        }
        matchedQuery = searchQuery;
        ZoneInfo[] zones = ZoneRegistry.Zones;
        var found = new List<ZoneInfo>();
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i].Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                found.Add(zones[i]);
            }
        }
        matches = found.ToArray();
    }

    private static void DrawHeader(bool scrollIntoView)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var rowHeight = 36f * scale;
        var midY = origin.Y + rowHeight * 0.5f;
        var label = Loc.T(L.Grind.Zones);
        var titleSize = TextDraw.SectionTitleSize(label);
        TextDraw.SectionTitle(label, new Vector2(origin.X, midY - titleSize.Y * 0.5f), Styling.TextStrong);
        var searchWidth = MathF.Min(220f * scale, width * 0.5f);
        ImGui.SetCursorScreenPos(new Vector2(origin.X + width - searchWidth, origin.Y));
        DrawSearch(searchWidth, rowHeight);
        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, rowHeight));
        if (scrollIntoView)
        {
            ImGui.SetScrollHereY(0f);
        }
    }

    private static void DrawSearch(float width, float height)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var max = origin + new Vector2(width, height);
        var drawList = ImGui.GetWindowDrawList();
        var searching = searchQuery.Length > 0;
        Paint.Pill(drawList, origin, max, Styling.WithAlpha(Styling.Surface0, 0.9f), Styling.WithAlpha(searching ? Styling.AccentViolet : Styling.BorderDim, 0.6f));
        var iconSize = TextDraw.IconSize(FontAwesomeIcon.Search);
        var iconX = origin.X + 12f * scale;
        TextDraw.Icon(FontAwesomeIcon.Search, new Vector2(iconX, origin.Y + (height - iconSize.Y) * 0.5f), searching ? Styling.AccentVioletSoft : Styling.TextDim);
        var rightPad = searching ? height : 8f * scale;
        var inputX = iconX + iconSize.X + 6f * scale;
        ImGui.SetCursorScreenPos(new Vector2(inputX, origin.Y + (height - ImGui.GetFrameHeight()) * 0.5f));
        ImGui.SetNextItemWidth(max.X - rightPad - inputX);
        using (ImRaii.PushColor(ImGuiCol.FrameBg, Vector4.Zero).Push(ImGuiCol.FrameBgHovered, Vector4.Zero).Push(ImGuiCol.FrameBgActive, Vector4.Zero))
        {
            using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(4f * scale, ImGui.GetStyle().FramePadding.Y)))
            {
                ImGui.InputTextWithHint("##fta_zone_search", Loc.T(L.Grind.SearchZones), ref searchQuery, SearchMaxLength);
            }
        }
        if (searching)
        {
            ImGui.SetCursorScreenPos(new Vector2(max.X - height, origin.Y));
            if (IconButton.Draw(FontAwesomeIcon.Times, "##fta_zone_search_clear", height, tooltip: Loc.T(L.Grind.ClearSearch)))
            {
                searchQuery = string.Empty;
            }
        }
        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, height));
    }

    private static void DrawSearchSummary()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var rowHeight = SummaryRowHeight * scale;
        using (Fonts.PushCaption())
        {
            var summary = matches.Length == 0 ? Loc.T(L.Grind.NoMatches, searchQuery) : Loc.Plural(L.Grind.Matches, matches.Length, searchQuery);
            var summarySize = TextDraw.Measure(summary);
            TextDraw.At(summary, new Vector2(origin.X + 2f * scale, origin.Y + (rowHeight - summarySize.Y) * 0.5f), Styling.TextDim);
        }
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, rowHeight));
    }

    private static void DrawSummaryRow(Configuration cfg, AutoFateController ctrl, ZoneInfo[] zones)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var rowHeight = SummaryRowHeight * scale;
        var selectedCount = CountSelected(cfg, zones);
        using (Fonts.PushCaption())
        {
            var summary = Loc.T(L.Grind.SelectedSummary, selectedCount, zones.Length);
            var summarySize = TextDraw.Measure(summary);
            TextDraw.At(summary, new Vector2(origin.X + 2f * scale, origin.Y + (rowHeight - summarySize.Y) * 0.5f), Styling.TextDim);
        }
        var allSelected = AllUnlockedSelected(cfg, zones);
        var label = allSelected ? Loc.T(L.Common.Clear) : Loc.T(L.Common.SelectAll);
        var buttonWidth = PillButton.Width(label);
        ImGui.SetCursorScreenPos(new Vector2(origin.X + width - buttonWidth, origin.Y));
        if (PillButton.Draw("##fta_zone_bulk", label, allSelected ? Styling.AccentRose : Styling.AccentViolet, PillButton.Emphasis.Ghost, enabled: !ctrl.Running && zones.Length != 0, height: SummaryRowHeight))
        {
            SetAll(cfg, zones, !allSelected);
        }
        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, rowHeight));
    }

    private static void DrawGrid(Configuration cfg, AutoFateController ctrl, ZoneInfo[] zones, bool showExpansion)
    {
        if (zones.Length == 0)
        {
            return;
        }
        var scale = ImGuiHelpers.GlobalScale;
        var gap = Gap * scale;
        var availWidth = ImGui.GetContentRegionAvail().X;
        var columns = Math.Max(1, (int)MathF.Floor((availWidth + gap) / (230f * scale + gap)));
        var width = (availWidth - gap * (float)(columns - 1)) / (float)columns;
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(gap, gap)))
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (i % columns != 0)
                {
                    ImGui.SameLine(0f, gap);
                }
                var zone = zones[i];
                ZoneStateReader.Refresh(zone);
                DrawCard(zone, cfg, ctrl, width, QueuePosition(cfg, zone.TerritoryId), showExpansion);
            }
        }
    }

    private static void DrawCard(ZoneInfo zone, Configuration cfg, AutoFateController ctrl, float width, int queuePosition, bool showExpansion)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(width, 48f * scale);
        var origin = ImGui.GetCursorScreenPos();
        var max = origin + size;
        var selected = queuePosition > 0;
        var locked = !zone.Unlocked;
        var running = ctrl.Running;
        var rankIndex = ZoneSelection.GoalNeedsRankedZones(cfg) ? SharedFateCatalog.IndexOfTerritory(zone.TerritoryId) : -1;
        var skipped = ZoneSelection.IsSkippedByGoal(cfg, zone.TerritoryId);
        var enabled = !locked && !running && !skipped;
        ImGui.PushID((nint)zone.TerritoryId);
        Hit.Result hit = Hit.Area("##zone", size, enabled);
        var hover = Motion.Hover(Motion.Key("##zone"), hit.Hovered);
        var selectedAmount = Motion.Approach(Motion.Key("##zone", 1), selected ? 1f : 0f);
        ImGui.PopID();
        if (hit.Clicked)
        {
            SetSelected(cfg, zone.TerritoryId, !selected);
        }
        var drawList = ImGui.GetWindowDrawList();
        Paint.Glass(drawList, origin, max, 10f * scale, Styling.AccentViolet, 0.02f + 0.16f * selectedAmount, hover);
        var midY = origin.Y + size.Y * 0.5f;
        var radius = 9f * scale;
        var center = new Vector2(origin.X + 14f * scale + radius, midY);
        DrawSelector(drawList, center, radius, locked, selectedAmount);
        var rightX = max.X - 12f * scale;
        if (queuePosition > 0)
        {
            rightX -= DrawQueueBadge(drawList, Label(queueLabels, queuePosition), rightX, midY) + 8f * scale;
        }
        if (rankIndex >= 0 && !locked)
        {
            SharedFateProgress.Request(zone.TerritoryId);
            rightX -= DrawRankBadge(drawList, zone.TerritoryId, rankIndex, rightX, midY) + 8f * scale;
        }
        if (zone.ActiveFateCount > 0)
        {
            rightX -= DrawActiveFates(drawList, zone.ActiveFateCount, rightX, midY) + 8f * scale;
        }
        if (showExpansion)
        {
            rightX -= DrawExpansionTag(drawList, ExpansionLabels.Name(zone.Expansion), rightX, midY) + 8f * scale;
        }
        var textX = center.X + radius + 12f * scale;
        var color = locked | skipped ? Styling.TextMuted : Vector4.Lerp(Styling.TextSecondary, Styling.TextStrong, MathF.Max(selectedAmount, hover));
        var name = TextDraw.Truncate(zone.Name, rightX - textX);
        var nameSize = TextDraw.Measure(name);
        TextDraw.At(name, new Vector2(textX, midY - nameSize.Y * 0.5f), color);
        SharedFateRank rank;
        if (hit.Hovered && zone.ActiveFateCount > 0)
        {
            Tooltip.Show(Loc.Plural(L.Grind.ActiveFates, zone.ActiveFateCount));
        }
        else if (!enabled && Hit.HoveringRect(origin, max))
        {
            Tooltip.Show(running ? Loc.T(L.Grind.ZonesLockedRunning) : (skipped ? Loc.T(L.Grind.NoRanksHere) : LockedTooltip(zone)));
        }
        else if (rankIndex >= 0 && Hit.HoveringRect(origin, max) && SharedFateProgress.TryGet(zone.TerritoryId, out rank))
        {
            Tooltip.Show(rank.IsMaxed ? Loc.T(L.Grind.RankMaxedTooltip, rank.Total) : Loc.T(L.Grind.RankTooltip, rank.Rank, rank.MaxRank, rank.RankProgress, rank.RankSize, rank.Completed, rank.Total));
        }
    }

    private static float DrawRankBadge(ImDrawListPtr dl, uint territoryId, int rankIndex, float rightX, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var known = SharedFateProgress.TryGet(territoryId, out var rank);
        var label = RankLabel(rankIndex, known, rank);
        using (Fonts.PushCaption())
        {
            if (!known)
            {
                var labelSize = TextDraw.Measure(label);
                TextDraw.At(label, new Vector2(rightX - labelSize.X, midY - labelSize.Y * 0.5f), Styling.TextMuted);
                return labelSize.X;
            }
            var badgeSize = TextDraw.Measure(label) + new Vector2(12f * scale, 4f * scale);
            var badgeMin = new Vector2(rightX - badgeSize.X, midY - badgeSize.Y * 0.5f);
            var max = badgeMin + badgeSize;
            var tint = rank.IsMaxed ? Styling.AccentMint : Styling.AccentBlue;
            Paint.Pill(dl, badgeMin, max, Styling.WithAlpha(tint, 0.22f), Styling.WithAlpha(tint, 0.5f));
            TextDraw.Middle(label, badgeMin, max, rank.IsMaxed ? Styling.AccentMintSoft : Styling.AccentBlueSoft);
            return badgeSize.X;
        }
    }

    private static string RankLabel(int rankIndex, bool known, SharedFateRank rank)
    {
        if (!known)
        {
            return Loc.T(L.Grind.RankSyncing);
        }
        var cached = rankLabels[rankIndex];
        if (cached.Text != null && cached.Rank == rank && cached.Language == Loc.Current)
        {
            return cached.Text;
        }
        var label = rank.IsMaxed ? Loc.T(L.Grind.RankMaxed) : Loc.T(L.Grind.RankBadge, rank.Rank, rank.RankProgress, rank.RankSize);
        rankLabels[rankIndex] = (Rank: rank, Language: Loc.Current, Text: label);
        return label;
    }

    private static void DrawSelector(ImDrawListPtr dl, Vector2 center, float radius, bool locked, float active)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        if (locked)
        {
            TextDraw.IconCentered(FontAwesomeIcon.Lock, center, Styling.TextMuted);
            return;
        }
        var color = Vector4.Lerp(Styling.WithAlpha(Styling.BorderDim, 0.9f), Styling.AccentVioletSoft, active);
        dl.AddCircle(center, radius, Paint.Col(color), 0, 1.4f * globalScale);
        if (active <= 0.01f)
        {
            return;
        }
        dl.AddCircleFilled(center, radius * active, Paint.Col(Styling.AccentViolet));
        if (active > 0.5f)
        {
            Paint.Check(dl, center, radius * 1.1f, Styling.WithAlpha(Styling.TextStrong, (active - 0.5f) * 2f), 1.8f * globalScale);
        }
    }

    private static float DrawQueueBadge(ImDrawListPtr dl, string label, float rightX, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        using (Fonts.PushCaption())
        {
            var size = TextDraw.Measure(label) + new Vector2(12f * scale, 4f * scale);
            var min = new Vector2(rightX - size.X, midY - size.Y * 0.5f);
            var max = min + size;
            Paint.Pill(dl, min, max, Styling.WithAlpha(Styling.AccentViolet, 0.35f), Styling.WithAlpha(Styling.AccentVioletSoft, 0.5f));
            TextDraw.Middle(label, min, max, Styling.TextStrong);
            return size.X;
        }
    }

    private static float DrawExpansionTag(ImDrawListPtr dl, string label, float rightX, float midY)
    {
        using (Fonts.PushCaption())
        {
            var labelSize = TextDraw.Measure(label);
            TextDraw.At(label, new Vector2(rightX - labelSize.X, midY - labelSize.Y * 0.5f), Styling.TextMuted);
            return labelSize.X;
        }
    }

    private static float DrawActiveFates(ImDrawListPtr dl, int count, float rightX, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var label = Label(countLabels, count);
        var labelSize = TextDraw.Measure(label);
        TextDraw.At(label, new Vector2(rightX - labelSize.X, midY - labelSize.Y * 0.5f), Styling.AccentAmber);
        var boltSize = TextDraw.IconSize(FontAwesomeIcon.Bolt);
        var boltX = rightX - labelSize.X - 4f * scale - boltSize.X;
        TextDraw.Icon(FontAwesomeIcon.Bolt, new Vector2(boltX, midY - boltSize.Y * 0.5f), Styling.AccentAmber);
        return rightX - boltX;
    }

    private static string LockedTooltip(ZoneInfo zone)
    {
        if (!ZoneAetherytes.TryFindGateway(zone.TerritoryId, out var gateway))
        {
            return Loc.T(L.Grind.LockedAetheryte);
        }
        return Loc.T(L.Grind.LockedGateway, gateway.Name, zone.Name);
    }

    private static int CountSelected(Configuration cfg, ZoneInfo[] zones)
    {
        int count = 0;
        for (int i = 0; i < zones.Length; i++)
        {
            if (cfg.SelectedZones.Contains(zones[i].TerritoryId))
            {
                count++;
            }
        }
        return count;
    }

    private static bool AllUnlockedSelected(Configuration cfg, ZoneInfo[] zones)
    {
        int unlocked = 0;
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i].Unlocked && !ZoneSelection.IsSkippedByGoal(cfg, zones[i].TerritoryId))
            {
                unlocked++;
                if (!cfg.SelectedZones.Contains(zones[i].TerritoryId))
                {
                    return false;
                }
            }
        }
        return unlocked > 0;
    }

    private static int QueuePosition(Configuration cfg, uint territoryId)
    {
        int position = 0;
        var selectedZones = cfg.SelectedZones;
        for (int i = 0; i < selectedZones.Count; i++)
        {
            if (knownIds.Contains(selectedZones[i]))
            {
                position++;
                if (selectedZones[i] == territoryId)
                {
                    return position;
                }
            }
        }
        return 0;
    }

    private static void SetSelected(Configuration cfg, uint territoryId, bool selected)
    {
        if (selected && !cfg.SelectedZones.Contains(territoryId))
        {
            cfg.SelectedZones.Add(territoryId);
        }
        else if (!selected)
        {
            cfg.SelectedZones.Remove(territoryId);
        }
        cfg.SaveDebounced();
    }

    private static void SetAll(Configuration cfg, ZoneInfo[] zones, bool selected)
    {
        for (int i = 0; i < zones.Length; i++)
        {
            var territoryId = zones[i].TerritoryId;
            if (selected && zones[i].Unlocked && !ZoneSelection.IsSkippedByGoal(cfg, territoryId) && !cfg.SelectedZones.Contains(territoryId))
            {
                cfg.SelectedZones.Add(territoryId);
            }
            else if (!selected)
            {
                cfg.SelectedZones.Remove(territoryId);
            }
        }
        cfg.SaveDebounced();
    }

    private static string Label(string[] cache, int value)
    {
        if (value < 0 || value >= cache.Length)
        {
            return $"{((cache == queueLabels) ? "#" : "")}{value}";
        }
        return cache[value];
    }

    private static string[] BuildLabels(string prefix)
    {
        string[] labels = new string[CachedLabelCount];
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i] = prefix + i;
        }
        return labels;
    }
}
