using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using AutoFateGrind.Core;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoFateGrind.Windows.Pages;

internal sealed class LogPage
{
    private readonly record struct Columns(float Time, float Tag, float Source, float MessageX);

    private struct RowText
    {
        public long Sequence;

        public int Repeat;

        public float MessageWidth;

        public float FontSize;

        public string Time;

        public string Message;

        public string? RepeatLabel;
    }

    private const float ToolbarGap = 8f;

    private const float SectionGap = 10f;

    private const float MinimumListHeight = 120f;

    private const int NoticeMs = 1800;

    private const int ClearConfirmMs = 3000;

    private static readonly RunLogLevel[] chipLevels = new RunLogLevel[5]
    {
        RunLogLevel.Verbose,
        RunLogLevel.Debug,
        RunLogLevel.Info,
        RunLogLevel.Warning,
        RunLogLevel.Error
    };

    private static readonly LocString[] chipLabels = new LocString[5]
    {
        L.Log.LevelVerbose,
        L.Log.LevelDebug,
        L.Log.LevelInfo,
        L.Log.LevelWarning,
        L.Log.LevelError
    };

    private static readonly string[] chipIds = new string[5] { "##fta_log_verbose", "##fta_log_debug", "##fta_log_info", "##fta_log_warning", "##fta_log_error" };

    private readonly RunLogLine[] view = new RunLogLine[3000];

    private readonly int[] levelTotals = new int[5];

    private int viewCount;

    private int bufferedCount;

    private int builtVersion = -1;

    private RunLogFilter builtFilter;

    private int levelMask = 31;

    private string search = string.Empty;

    private string? source;

    private bool searchFocused;

    private bool focusSearch;

    private long copiedAtMs;

    private long clearArmedAtMs;

    private string? notice;

    private long noticeAtMs;

    private const float InspectorShare = 0.34f;

    private const float InspectorMinHeight = 120f;

    private const float InspectorMaxHeight = 300f;

    private const float CardPadX = 14f;

    private const float CardPadY = 10f;

    private const float HeaderGap = 8f;

    private const float StatusGap = 8f;

    private const float InspectorIconButton = 26f;

    private const float InspectorWrapSlack = 6f;

    private const string StampFormat = "yyyy-MM-dd HH:mm:ss.fff";

    private const string InspectorDetailId = "##fta_log_detail";

    private const string DetailSeparator = "\n\n";

    private long inspectedSequence;

    private int inspectedRepeat;

    private float inspectedWidth;

    private float inspectedFontSize;

    private string inspectedStamp = string.Empty;

    private string inspectedText = string.Empty;

    private const int RowCacheSize = 256;

    private const float ListPadX = 6f;

    private const float ListPadY = 6f;

    private const float RowPadX = 10f;

    private const float RowPadY = 4f;

    private const float ColumnGap = 12f;

    private const float TagPadX = 6f;

    private const float MaxSourceColumn = 150f;

    private const float SelectionBarWidth = 2.5f;

    private const float TrailingGap = 8f;

    private const float JumpInset = 12f;

    private const string ListId = "##fta_log_list";

    private const string MenuId = "##fta_log_menu";

    private const string TimeSample = "00:00:00.000";

    private const string MultilineMark = " …";

    private static readonly string[] tagLabels = new string[5] { "VERB", "DBUG", "INFO", "WARN", "ERR" };

    private readonly RowText[] rowCache = new RowText[RowCacheSize];

    private readonly Dictionary<string, float> sourceWidths = new Dictionary<string, float>(StringComparer.Ordinal);

    private bool follow = true;

    private float lastScrollMax;

    private long detachedTail;

    private int newWhileDetached;

    private int pendingScrollIndex = -1;

    private int contextIndex = -1;

    private float sourceColumn;

    private float measuredFontSize;

    private long anchorSequence;

    private long focusSequence;

    private RunLogFilter Filter => new RunLogFilter(levelMask, search, source);

    public void Draw()
    {
        RunLog.MarkSeen();
        Refresh();
        PageHeader.Draw(Loc.T(L.Log.Title), Loc.Plural(L.Log.Entries, bufferedCount));
        DrawToolbar();
        Styling.VSpace(ToolbarGap);
        DrawFilters();
        Styling.VSpace(SectionGap);
        var globalScale = ImGuiHelpers.GlobalScale;
        var itemSpacing = ImGui.GetStyle().ItemSpacing.Y;
        var inspectorHeight = InspectorHeight();
        var inspectorBlock = ((inspectorHeight > 0f) ? (SectionGap * globalScale + itemSpacing + inspectorHeight + itemSpacing) : 0f);
        var reserved = itemSpacing + inspectorBlock + StatusGap * globalScale + itemSpacing + StatusLineHeight();
        var height = MathF.Max(MinimumListHeight * globalScale, ImGui.GetContentRegionAvail().Y - reserved);
        DrawList(height);
        DrawInspector(inspectorHeight);
        DrawStatus();
        HandleShortcuts();
    }

    private void Refresh()
    {
        var filter = Filter;
        var version = RunLog.Version;
        if (version == builtVersion && filter == builtFilter)
        {
            return;
        }
        var previousTail = ((viewCount > 0) ? view[viewCount - 1].Sequence : 0);
        viewCount = RunLog.Snapshot(in filter, view, levelTotals);
        bufferedCount = 0;
        for (var i = 0; i < 5; i++)
        {
            bufferedCount += levelTotals[i];
        }
        OnViewRebuilt(previousTail, filter != builtFilter);
        builtVersion = version;
        builtFilter = filter;
    }

    private void DrawToolbar()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var height = 30f * globalScale;
        var cursor = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var gap = ToolbarGap * globalScale;
        var now = Environment.TickCount64;
        var label = ((now - copiedAtMs < NoticeMs) ? Loc.T(L.Log.Copied) : (builtFilter.IsNarrowed ? Loc.Plural(L.Log.CopyFiltered, viewCount) : Loc.T(L.Log.CopyAll)));
        var clearArmed = now - clearArmedAtMs < ClearConfirmMs;
        var clearLabel = Loc.T(clearArmed ? L.Log.ConfirmClear : L.Log.Clear);
        var copyWidth = PillButton.Width(label, FontAwesomeIcon.Copy);
        var clearWidth = PillButton.Width(clearLabel, FontAwesomeIcon.Eraser);
        var searchWidth = MathF.Max(1f, width - copyWidth - clearWidth - gap * 2f);
        ImGui.SetCursorScreenPos(cursor);
        if (SearchField.Draw("##fta_log_search", Loc.T(L.Log.SearchHint), ref search, ref searchFocused, searchWidth, height, focusSearch))
        {
            ClearSelection();
        }
        focusSearch = false;
        ImGui.SetCursorScreenPos(cursor + new Vector2(searchWidth + gap, 0f));
        if (PillButton.Draw("##fta_log_copy", label, Styling.AccentViolet, PillButton.Emphasis.Filled, FontAwesomeIcon.Copy, viewCount > 0, 30f, Loc.T(L.Log.CopyTooltip)))
        {
            CopyView();
        }
        ImGui.SetCursorScreenPos(cursor + new Vector2(searchWidth + copyWidth + gap * 2f, 0f));
        if (PillButton.Draw("##fta_log_clear", clearLabel, clearArmed ? Styling.AccentRose : Styling.TextSecondary, clearArmed ? PillButton.Emphasis.Tinted : PillButton.Emphasis.Ghost, FontAwesomeIcon.Eraser, bufferedCount > 0, 30f))
        {
            if (clearArmed)
            {
                RunLog.Clear();
                ClearSelection();
                clearArmedAtMs = 0L;
            }
            else
            {
                clearArmedAtMs = now;
            }
        }
        ImGui.SetCursorScreenPos(cursor);
        ImGui.Dummy(new Vector2(width, height));
    }

    private void DrawFilters()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var height = 28f * globalScale;
        var gap = ToolbarGap * globalScale;
        var cursor = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var chipX = cursor.X;
        var chipY = cursor.Y;
        for (var i = 0; i < chipLevels.Length; i++)
        {
            var level = chipLevels[i];
            var label = Loc.T(chipLabels[i]);
            var count = levelTotals[(uint)level].ToString(Loc.Culture);
            var chipWidth = FilterChip.Width(label, count);
            WrapIfNeeded(ref chipX, ref chipY, chipWidth, cursor.X, width, height, gap);
            ImGui.SetCursorScreenPos(new Vector2(chipX, chipY));
            if (FilterChip.Draw(chipIds[i], label, count, LevelColor(level), builtFilter.Shows(level), height, tooltip: Loc.T(L.Log.LevelTooltip)))
            {
                ToggleLevel(level, ImGui.GetIO().KeyShift);
            }
            chipX += chipWidth + gap;
        }
        if (source != null)
        {
            var sourceLabel = Loc.T(L.Log.SourceChip, source);
            var itemWidth = FilterChip.Width(sourceLabel, null, FontAwesomeIcon.Times);
            WrapIfNeeded(ref chipX, ref chipY, itemWidth, cursor.X, width, height, gap);
            ImGui.SetCursorScreenPos(new Vector2(chipX, chipY));
            if (FilterChip.Draw("##fta_log_source", sourceLabel, null, Styling.AccentPink, active: true, height, FontAwesomeIcon.Times, Loc.T(L.Log.SourceChipTooltip)))
            {
                source = null;
                ClearSelection();
            }
        }
        ImGui.SetCursorScreenPos(cursor);
        ImGui.Dummy(new Vector2(width, chipY - cursor.Y + height));
    }

    private static void WrapIfNeeded(ref float x, ref float y, float itemWidth, float left, float width, float height, float gap)
    {
        if (x <= left || x + itemWidth <= left + width)
        {
            return;
        }
        x = left;
        y += height + gap;
    }

    private void ToggleLevel(RunLogLevel level, bool solo)
    {
        var bit = 1 << (int)level;
        if (solo)
        {
            levelMask = ((levelMask == bit) ? 31 : bit);
        }
        else
        {
            levelMask ^= bit;
        }
        ClearSelection();
    }

    private void ResetFilters()
    {
        levelMask = 31;
        search = string.Empty;
        source = null;
        ClearSelection();
    }

    private void ShowNotice(string text)
    {
        notice = text;
        noticeAtMs = Environment.TickCount64;
    }

    private static Vector4 LevelColor(RunLogLevel level)
    {
        return level switch
        {
            RunLogLevel.Verbose => Styling.TextDim,
            RunLogLevel.Debug => Styling.AccentBlue,
            RunLogLevel.Warning => Styling.AccentAmber,
            RunLogLevel.Error => Styling.AccentRose,
            _ => Styling.AccentViolet,
        };
    }

    private static Vector4 MessageColor(RunLogLevel level)
    {
        return level switch
        {
            RunLogLevel.Verbose => Styling.TextMuted,
            RunLogLevel.Debug => Styling.TextDim,
            RunLogLevel.Warning => Styling.AccentAmberSoft,
            RunLogLevel.Error => Styling.AccentRoseSoft,
            _ => Styling.TextSecondary,
        };
    }

    private float InspectorHeight()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        if (!HasSelection)
        {
            return 0f;
        }
        if (SingleSelectionIndex() < 0)
        {
            return 50f * globalScale;
        }
        return Math.Clamp(ImGui.GetContentRegionAvail().Y * InspectorShare, InspectorMinHeight * globalScale, InspectorMaxHeight * globalScale);
    }

    private void DrawInspector(float height)
    {
        if (!HasSelection)
        {
            return;
        }
        Styling.VSpace(SectionGap);
        var index = SingleSelectionIndex();
        if (index < 0)
        {
            DrawSelectionBar();
            return;
        }
        DrawLineCard(index, height);
    }

    private void DrawSelectionBar()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursor = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var height = 50f * globalScale;
        Paint.Surface(ImGui.GetWindowDrawList(), cursor, cursor + new Vector2(width, height), 10f * globalScale, Styling.WithAlpha(Styling.AccentViolet, 0.08f), Styling.WithAlpha(Styling.AccentViolet, 0.35f));
        var (low, high) = SelectionIndices();
        var selectedText = Loc.Plural(L.Log.Selected, high - low + 1);
        var selectedSize = TextDraw.Measure(selectedText);
        TextDraw.At(selectedText, new Vector2(cursor.X + CardPadX * globalScale, cursor.Y + (height - selectedSize.Y) * 0.5f), Styling.TextStrong);
        var clearLabel = Loc.T(L.Log.ClearSelection);
        var copyLabel = Loc.T(L.Log.CopySelection);
        var clearWidth = PillButton.Width(clearLabel, FontAwesomeIcon.Times);
        var copyWidth = PillButton.Width(copyLabel, FontAwesomeIcon.Copy);
        var rightX = cursor.X + width - CardPadX * globalScale;
        var buttonY = cursor.Y + CardPadY * globalScale;
        ImGui.SetCursorScreenPos(new Vector2(rightX - clearWidth, buttonY));
        if (PillButton.Draw("##fta_log_sel_clear", clearLabel, Styling.TextSecondary, PillButton.Emphasis.Ghost, FontAwesomeIcon.Times, enabled: true, 30f))
        {
            ClearSelection();
        }
        ImGui.SetCursorScreenPos(new Vector2(rightX - clearWidth - 8f * globalScale - copyWidth, buttonY));
        if (PillButton.Draw("##fta_log_sel_copy", copyLabel, Styling.AccentViolet, PillButton.Emphasis.Tinted, FontAwesomeIcon.Copy, enabled: true, 30f))
        {
            CopyRange(low, high);
        }
        ImGui.SetCursorScreenPos(cursor);
        ImGui.Dummy(new Vector2(width, height));
    }

    private void DrawLineCard(int index, float height)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var line = view[index];
        var cursor = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        if (inspectedSequence != line.Sequence || inspectedRepeat != line.Repeat)
        {
            inspectedStamp = line.AtUtc.ToLocalTime().ToString(StampFormat, CultureInfo.InvariantCulture);
        }
        var levelColor = LevelColor(line.Level);
        var drawList = ImGui.GetWindowDrawList();
        Paint.Surface(drawList, cursor, cursor + new Vector2(width, height), 10f * globalScale, Styling.WithAlpha(Styling.Surface1, 0.85f), Styling.WithAlpha(levelColor, 0.45f));
        Paint.Fill(drawList, cursor, new Vector2(cursor.X + 2.5f * globalScale, cursor.Y + height), levelColor, 10f * globalScale, ImDrawFlags.RoundCornersLeft);
        var padX = CardPadX * globalScale;
        var padY = CardPadY * globalScale;
        var buttonSize = InspectorIconButton * globalScale;
        var headerHeight = MathF.Max(buttonSize, TextDraw.LineHeight());
        var headerX = cursor.X + padX;
        var headerMidY = cursor.Y + padY + headerHeight * 0.5f;
        using (Fonts.PushCaption())
        {
            var tagSize = new Vector2(TextDraw.Measure(tagLabels[3]).X + 12f * globalScale, TextDraw.LineHeight() + 4f * globalScale);
            DrawTag(drawList, line.Level, new Vector2(headerX, headerMidY - tagSize.Y * 0.5f), tagSize);
            headerX += tagSize.X + HeaderGap * globalScale;
            var caption = ((line.Repeat > 1) ? string.Concat(inspectedStamp, " · ", line.Source, " · ", Loc.T(L.Log.Repeated, line.Repeat)) : (inspectedStamp + " · " + line.Source));
            var maxWidth = MathF.Max(1f, cursor.X + width - padX - buttonSize * 2f - HeaderGap * globalScale - headerX);
            TextDraw.At(TextDraw.Truncate(caption, maxWidth), new Vector2(headerX, headerMidY - TextDraw.LineHeight() * 0.5f), Styling.TextDim);
        }
        var buttonX = cursor.X + width - padX - buttonSize;
        ImGui.SetCursorScreenPos(new Vector2(buttonX, headerMidY - buttonSize * 0.5f));
        if (IconButton.Draw(FontAwesomeIcon.Times, "##fta_log_inspect_close", buttonSize, Styling.TextDim, Loc.T(L.Log.Close)))
        {
            ClearSelection();
        }
        ImGui.SetCursorScreenPos(new Vector2(buttonX - buttonSize - HeaderGap * globalScale * 0.5f, headerMidY - buttonSize * 0.5f));
        if (IconButton.Draw(FontAwesomeIcon.Copy, "##fta_log_inspect_copy", buttonSize, Styling.AccentVioletSoft, Loc.T(L.Log.CopyLine)))
        {
            CopyRange(index, index);
        }
        var bodyY = cursor.Y + padY + headerHeight + HeaderGap * globalScale;
        var bodySize = new Vector2(width - padX * 2f, cursor.Y + height - padY - bodyY);
        if (bodySize.Y > 1f)
        {
            ImGui.SetCursorScreenPos(new Vector2(cursor.X + padX, bodyY));
            DrawInspectedBody(in line, bodySize);
        }
        ImGui.SetCursorScreenPos(cursor);
        ImGui.Dummy(new Vector2(width, height));
    }

    private void DrawInspectedBody(in RunLogLine line, Vector2 size)
    {
        using (Fonts.PushCaption())
        {
            RefreshInspected(in line, size.X - ImGui.GetStyle().ScrollbarSize - InspectorWrapSlack * ImGuiHelpers.GlobalScale);
            using var textColors = ImRaii.PushColor(ImGuiCol.FrameBg, Vector4.Zero).Push(ImGuiCol.Text, (line.Detail == null) ? MessageColor(line.Level) : Styling.TextSecondary);
            using var framePadding = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
            ImGui.InputTextMultiline(InspectorDetailId, ref inspectedText, inspectedText.Length + 1, size, ImGuiInputTextFlags.ReadOnly);
        }
    }

    private void RefreshInspected(in RunLogLine line, float wrapWidth)
    {
        var fontSize = ImGui.GetFontSize();
        if (inspectedSequence == line.Sequence && inspectedRepeat == line.Repeat && inspectedWidth == wrapWidth && inspectedFontSize == fontSize)
        {
            return;
        }
        inspectedSequence = line.Sequence;
        inspectedRepeat = line.Repeat;
        inspectedWidth = wrapWidth;
        inspectedFontSize = fontSize;
        var combined = ((line.Detail == null) ? line.Message : (line.Message + DetailSeparator + line.Detail));
        inspectedText = TextWrap.Hard(combined, wrapWidth);
    }

    private static float StatusLineHeight()
    {
        using (Fonts.PushCaption())
        {
            return TextDraw.LineHeight();
        }
    }

    private void DrawStatus()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        Styling.VSpace(StatusGap);
        var cursor = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var now = Environment.TickCount64;
        var right = cursor.X + width;
        float lineHeight;
        using (Fonts.PushCaption())
        {
            lineHeight = TextDraw.LineHeight();
            float leftEnd;
            if (notice != null && now - noticeAtMs < NoticeMs)
            {
                var checkSize = TextDraw.IconSize(FontAwesomeIcon.Check);
                TextDraw.Icon(FontAwesomeIcon.Check, new Vector2(cursor.X, cursor.Y + (lineHeight - checkSize.Y) * 0.5f), Styling.AccentMint);
                var textX = cursor.X + checkSize.X + StatusGap * globalScale;
                TextDraw.At(notice, new Vector2(textX, cursor.Y), Styling.AccentMintSoft);
                leftEnd = textX + TextDraw.Measure(notice).X;
            }
            else
            {
                leftEnd = DrawShowing(cursor);
            }
            var shortcuts = Loc.T(L.Log.Shortcuts);
            var shortcutsWidth = TextDraw.Measure(shortcuts).X;
            if (right - shortcutsWidth > leftEnd + 12f * globalScale * 2f)
            {
                TextDraw.At(shortcuts, new Vector2(right - shortcutsWidth, cursor.Y), Styling.TextMuted);
            }
        }
        ImGui.SetCursorScreenPos(cursor);
        ImGui.Dummy(new Vector2(width, lineHeight));
    }

    private float DrawShowing(Vector2 origin)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var showing = Loc.T(L.Log.Showing, viewCount.ToString("N0", Loc.Culture), bufferedCount.ToString("N0", Loc.Culture));
        TextDraw.At(showing, origin, Styling.TextDim);
        var endX = origin.X + TextDraw.Measure(showing).X;
        if (!builtFilter.IsNarrowed)
        {
            return endX;
        }
        var resetLabel = Loc.T(L.Log.ResetFilters);
        var resetSize = TextDraw.Measure(resetLabel);
        var resetPos = new Vector2(endX + 12f * globalScale, origin.Y);
        ImGui.SetCursorScreenPos(resetPos);
        var hit = Hit.Area("##fta_log_reset", resetSize);
        TextDraw.At(color: hit.Hovered ? Styling.AccentVioletSoft : Styling.AccentViolet, text: resetLabel, pos: resetPos);
        if (hit.Hovered)
        {
            Paint.Hairline(ImGui.GetWindowDrawList(), resetPos + new Vector2(0f, resetSize.Y), resetPos + resetSize);
        }
        if (hit.Clicked)
        {
            ResetFilters();
        }
        return resetPos.X + resetSize.X;
    }

    private void DrawList(float height)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var width = ImGui.GetContentRegionAvail().X;
        var cursor = ImGui.GetCursorScreenPos();
        Paint.Surface(ImGui.GetWindowDrawList(), cursor, cursor + new Vector2(width, height), 10f * globalScale, Styling.WithAlpha(Styling.Surface0, 0.7f), Styling.WithAlpha(Styling.BorderDim, 0.5f));
        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(ListPadX, ListPadY) * globalScale))
        {
            using var listChild = ImRaii.Child(ListId, new Vector2(width, height), border: false, ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.AlwaysUseWindowPadding);
            if (!listChild)
            {
                return;
            }
            using (Fonts.PushCaption())
            {
                if (viewCount == 0)
                {
                    DrawEmpty();
                    return;
                }
                UpdateFollow();
                DrawRows();
            }
        }
    }

    private void UpdateFollow()
    {
        var scrollY = ImGui.GetScrollY();
        var scrollMaxY = ImGui.GetScrollMaxY();
        var mouseWheel = ImGui.GetIO().MouseWheel;
        if (follow)
        {
            var wheelUp = ImGui.IsWindowHovered() && mouseWheel > 0f;
            var scrolledAway = scrollMaxY <= lastScrollMax + 0.5f && scrollY < scrollMaxY - 2f;
            if (wheelUp | scrolledAway)
            {
                Detach();
            }
        }
        else if (scrollY >= scrollMaxY - 2f && mouseWheel <= 0f && pendingScrollIndex < 0)
        {
            follow = true;
        }
    }

    private void Detach()
    {
        follow = false;
        detachedTail = ((viewCount > 0) ? view[viewCount - 1].Sequence : 0);
        newWhileDetached = 0;
    }

    private void DrawRows()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var rowHeight = TextDraw.LineHeight() + RowPadY * 2f * globalScale;
        var cursor = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var scrollY = ImGui.GetScrollY();
        var windowHeight = ImGui.GetWindowHeight();
        var firstRow = Math.Clamp((int)(scrollY / rowHeight) - 1, 0, viewCount);
        var lastRow = Math.Min(viewCount, firstRow + (int)(windowHeight / rowHeight) + 3);
        if (measuredFontSize != ImGui.GetFontSize())
        {
            measuredFontSize = ImGui.GetFontSize();
            sourceWidths.Clear();
            sourceColumn = 0f;
        }
        for (var i = firstRow; i < lastRow; i++)
        {
            sourceColumn = MathF.Max(sourceColumn, SourceWidth(view[i].Source));
        }
        var columns = MeasureColumns(globalScale);
        var hoveredRow = HoveredRow(cursor, width, rowHeight);
        var drawList = ImGui.GetWindowDrawList();
        for (var row = firstRow; row < lastRow; row++)
        {
            var rowMin = new Vector2(cursor.X, cursor.Y + (float)row * rowHeight);
            DrawRow(drawList, row, rowMin, new Vector2(width, rowHeight), columns, row == hoveredRow);
        }
        DrawJumpPill(scrollY, windowHeight, width);
        ImGui.SetCursorScreenPos(cursor);
        ImGui.Dummy(new Vector2(width, (float)viewCount * rowHeight));
        HandleRowMouse(hoveredRow);
        ScrollAfterDraw(rowHeight, windowHeight);
        DrawContextMenu();
        lastScrollMax = ImGui.GetScrollMaxY();
    }

    private Columns MeasureColumns(float scale)
    {
        var timeWidth = TextDraw.Measure(TimeSample).X;
        var tagWidth = TextDraw.Measure(tagLabels[3]).X + TagPadX * 2f * scale;
        var sourceWidth = MathF.Min(sourceColumn, MaxSourceColumn * scale);
        var gap = ColumnGap * scale;
        return new Columns(timeWidth, tagWidth, sourceWidth, RowPadX * scale + timeWidth + gap + tagWidth + gap + sourceWidth + gap);
    }

    private float SourceWidth(string name)
    {
        if (sourceWidths.TryGetValue(name, out var width))
        {
            return width;
        }
        width = TextDraw.Measure(name).X;
        sourceWidths[name] = width;
        return width;
    }

    private int HoveredRow(Vector2 contentOrigin, float innerWidth, float rowHeight)
    {
        if (!ImGui.IsWindowHovered())
        {
            return -1;
        }
        var mousePos = ImGui.GetMousePos();
        if (mousePos.X < contentOrigin.X || mousePos.X > contentOrigin.X + innerWidth)
        {
            return -1;
        }
        var row = (int)MathF.Floor((mousePos.Y - contentOrigin.Y) / rowHeight);
        if (row < 0 || row >= viewCount)
        {
            return -1;
        }
        return row;
    }

    private void DrawRow(ImDrawListPtr dl, int index, Vector2 rowMin, Vector2 rowSize, Columns columns, bool hovered)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var line = view[index];
        var rowMax = rowMin + rowSize;
        var selected = IsSelected(line.Sequence);
        var rounding = 5f * globalScale;
        if (selected)
        {
            Paint.Fill(dl, rowMin, rowMax, Styling.WithAlpha(Styling.AccentViolet, hovered ? 0.22f : 0.16f), rounding);
            Paint.Fill(dl, rowMin, new Vector2(rowMin.X + SelectionBarWidth * globalScale, rowMax.Y), Styling.AccentViolet, rounding);
        }
        else if (hovered)
        {
            Paint.Fill(dl, rowMin, rowMax, Styling.WithAlpha(Styling.Surface2, 0.6f), rounding);
        }
        else if (line.Level == RunLogLevel.Error)
        {
            Paint.Fill(dl, rowMin, rowMax, Styling.WithAlpha(Styling.AccentRose, 0.07f), rounding);
        }
        else if (line.Level == RunLogLevel.Warning)
        {
            Paint.Fill(dl, rowMin, rowMax, Styling.WithAlpha(Styling.AccentAmber, 0.05f), rounding);
        }
        else if ((index & 1) == 1)
        {
            Paint.Fill(dl, rowMin, rowMax, Styling.WithAlpha(Styling.Surface1, 0.3f), rounding);
        }
        var lineHeight = TextDraw.LineHeight();
        var textY = rowMin.Y + (rowSize.Y - lineHeight) * 0.5f;
        var gap = ColumnGap * globalScale;
        var x = rowMin.X + RowPadX * globalScale;
        var trailingWidth = TrailingWidth(in line, globalScale);
        var messageWidth = MathF.Max(1f, rowMax.X - RowPadX * globalScale - trailingWidth - (rowMin.X + columns.MessageX));
        ref RowText cached = ref RowTextFor(in line, messageWidth);
        TextDraw.At(cached.Time, new Vector2(x, textY), Styling.TextMuted);
        x += columns.Time + gap;
        DrawTag(dl, line.Level, new Vector2(x, rowMin.Y + RowPadY * globalScale * 0.5f), new Vector2(columns.Tag, rowSize.Y - RowPadY * globalScale));
        x += columns.Tag + gap;
        dl.PushClipRect(new Vector2(x, rowMin.Y), new Vector2(x + columns.Source, rowMax.Y), intersectWithCurrentClipRect: true);
        TextDraw.At(line.Source, new Vector2(x, textY), Styling.TextDim);
        dl.PopClipRect();
        TextDraw.At(cached.Message, new Vector2(rowMin.X + columns.MessageX, textY), MessageColor(line.Level));
        DrawTrailing(dl, in line, cached.RepeatLabel, rowMax, rowSize.Y, hovered);
    }

    private static void DrawTag(ImDrawListPtr dl, RunLogLevel level, Vector2 min, Vector2 size)
    {
        var levelColor = LevelColor(level);
        Paint.Fill(dl, min, min + size, Styling.WithAlpha(levelColor, 0.14f), size.Y * 0.5f);
        TextDraw.Middle(tagLabels[(uint)level], min, min + size, Styling.Lighten(levelColor, 0.15f));
    }

    private float TrailingWidth(in RunLogLine line, float scale)
    {
        var width = 0f;
        if (line.Repeat > 1)
        {
            width += RepeatPillWidth(line.Repeat, scale) + TrailingGap * scale;
        }
        if (line.Detail != null)
        {
            width += TextDraw.IconSize(FontAwesomeIcon.AlignLeft).X + TrailingGap * scale;
        }
        return width;
    }

    private static float RepeatPillWidth(int repeat, float scale)
    {
        var digits = ((repeat < 10) ? 1 : ((repeat < 100) ? 2 : ((repeat < 1000) ? 3 : 4)));
        return TextDraw.Measure("×").X + TextDraw.Measure("0").X * (float)digits + 12f * scale;
    }

    private void DrawTrailing(ImDrawListPtr dl, in RunLogLine line, string? repeatLabel, Vector2 rowMax, float rowHeight, bool hovered)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var rightX = rowMax.X - RowPadX * globalScale;
        var midY = rowMax.Y - rowHeight * 0.5f;
        if (repeatLabel != null)
        {
            var pillWidth = RepeatPillWidth(line.Repeat, globalScale);
            var pillMin = new Vector2(rightX - pillWidth, rowMax.Y - rowHeight + RowPadY * globalScale * 0.5f);
            var pillMax = new Vector2(rightX, rowMax.Y - RowPadY * globalScale * 0.5f);
            Paint.Fill(dl, pillMin, pillMax, Styling.WithAlpha(Styling.Surface3, 0.9f), (pillMax.Y - pillMin.Y) * 0.5f);
            TextDraw.Middle(repeatLabel, pillMin, pillMax, Styling.TextSecondary);
            if (hovered && ImGui.IsMouseHoveringRect(pillMin, pillMax))
            {
                Tooltip.Show(Loc.T(L.Log.Repeated, line.Repeat));
            }
            rightX = pillMin.X - TrailingGap * globalScale;
        }
        if (line.Detail == null)
        {
            return;
        }
        var iconSize = TextDraw.IconSize(FontAwesomeIcon.AlignLeft);
        var iconPos = new Vector2(rightX - iconSize.X, midY - iconSize.Y * 0.5f);
        TextDraw.Icon(FontAwesomeIcon.AlignLeft, iconPos, Styling.WithAlpha(LevelColor(line.Level), 0.85f));
        if (hovered && ImGui.IsMouseHoveringRect(iconPos, iconPos + iconSize))
        {
            Tooltip.Show(Loc.T(L.Log.HasDetails));
        }
    }

    private ref RowText RowTextFor(in RunLogLine line, float messageWidth)
    {
        ref RowText cached = ref rowCache[(int)(line.Sequence & (RowCacheSize - 1))];
        var fontSize = ImGui.GetFontSize();
        if (cached.Sequence == line.Sequence && cached.Repeat == line.Repeat && cached.MessageWidth == messageWidth && cached.FontSize == fontSize)
        {
            return ref cached;
        }
        var message = line.Message;
        var newline = message.IndexOfAny(new char[2] { '\r', '\n' });
        if (newline >= 0)
        {
            message = string.Concat(message.AsSpan(0, newline), MultilineMark);
        }
        cached.Sequence = line.Sequence;
        cached.Repeat = line.Repeat;
        cached.MessageWidth = messageWidth;
        cached.FontSize = fontSize;
        cached.Time = RunLog.Time(line.AtUtc);
        cached.Message = TextDraw.Truncate(message, messageWidth);
        cached.RepeatLabel = ((line.Repeat > 1) ? ("×" + line.Repeat.ToString(CultureInfo.InvariantCulture)) : null);
        return ref cached;
    }

    private void DrawJumpPill(float scrollY, float visibleHeight, float innerWidth)
    {
        if (follow)
        {
            return;
        }
        var globalScale = ImGuiHelpers.GlobalScale;
        var label = ((newWhileDetached > 0) ? (Loc.T(L.Log.JumpLatest) + " · " + Loc.Plural(L.Log.NewLines, newWhileDetached)) : Loc.T(L.Log.JumpLatest));
        var width = PillButton.Width(label, FontAwesomeIcon.ArrowDown);
        var height = 30f * globalScale;
        var windowY = ImGui.GetWindowPos().Y;
        var x = ImGui.GetCursorScreenPos().X + (innerWidth - width) * 0.5f;
        var y = windowY + visibleHeight - height - JumpInset * globalScale;
        ImGui.SetCursorScreenPos(new Vector2(x, y));
        Paint.Shadow(ImGui.GetWindowDrawList(), new Vector2(x, y), new Vector2(x + width, y + height), height * 0.5f, 10f * globalScale, 0.45f);
        if (PillButton.Draw("##fta_log_jump", label, Styling.AccentViolet, PillButton.Emphasis.Filled, FontAwesomeIcon.ArrowDown, enabled: true, 30f))
        {
            follow = true;
            newWhileDetached = 0;
        }
    }

    private void HandleRowMouse(int hoveredIndex)
    {
        if (hoveredIndex < 0 || ImGui.IsAnyItemHovered())
        {
            return;
        }
        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            CopyRange(hoveredIndex, hoveredIndex);
            return;
        }
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            ClickRow(hoveredIndex, ImGui.GetIO().KeyShift);
        }
        if (!ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            return;
        }
        if (!IsSelected(view[hoveredIndex].Sequence))
        {
            SelectSingle(hoveredIndex);
        }
        contextIndex = hoveredIndex;
        ImGui.OpenPopup(MenuId);
    }

    private void ScrollAfterDraw(float rowHeight, float visibleHeight)
    {
        if (pendingScrollIndex >= 0)
        {
            var globalScale = ImGuiHelpers.GlobalScale;
            var rowTop = (float)pendingScrollIndex * rowHeight;
            var scrollY = ImGui.GetScrollY();
            var usableHeight = visibleHeight - 12f * globalScale;
            if (rowTop < scrollY)
            {
                ImGui.SetScrollY(rowTop);
            }
            else if (rowTop + rowHeight > scrollY + usableHeight)
            {
                ImGui.SetScrollY(rowTop + rowHeight - usableHeight);
            }
            pendingScrollIndex = -1;
        }
        else if (follow)
        {
            ImGui.SetScrollHereY(1f);
        }
    }

    private void DrawContextMenu()
    {
        using var scope = ContextMenu.Begin(MenuId);
        if (!scope.Open || contextIndex < 0 || contextIndex >= viewCount)
        {
            return;
        }
        var line = view[contextIndex];
        if (ImGui.MenuItem(Loc.T(L.Log.CopyLine)))
        {
            CopyRange(contextIndex, contextIndex);
        }
        var (low, high) = SelectionIndices();
        if (high > low && ImGui.MenuItem(Loc.T(L.Log.CopySelection) + " (" + Loc.Plural(L.Log.Selected, high - low + 1) + ")"))
        {
            CopyRange(low, high);
        }
        if (ImGui.MenuItem(Loc.T(L.Log.CopyToEnd)))
        {
            CopyRange(contextIndex, viewCount - 1);
        }
        ImGui.Separator();
        if (ImGui.MenuItem(Loc.T(L.Log.OnlySource, line.Source), selected: false, source == null))
        {
            source = line.Source;
            ClearSelection();
        }
    }

    private void DrawEmpty()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursor = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var pad = 10f * globalScale;
        var wrapWidth = MathF.Max(1f, width - pad * 2f);
        var y = cursor.Y + pad;
        if (bufferedCount > 0)
        {
            var noMatches = Loc.T(L.Log.NoMatches);
            TextDraw.Wrapped(noMatches, new Vector2(cursor.X + pad, y), wrapWidth, Styling.TextDim);
            y += TextDraw.MeasureWrapped(noMatches, wrapWidth).Y + pad;
            ImGui.SetCursorScreenPos(new Vector2(cursor.X + pad, y));
            if (PillButton.Draw("##fta_log_reset_empty", Loc.T(L.Log.ResetFilters), Styling.AccentViolet, PillButton.Emphasis.Tinted, FontAwesomeIcon.Undo, enabled: true, 30f))
            {
                ResetFilters();
            }
            return;
        }
        var empty = Loc.T(L.Log.Empty);
        TextDraw.Wrapped(empty, new Vector2(cursor.X + pad, y), wrapWidth, Styling.TextDim);
        y += TextDraw.MeasureWrapped(empty, wrapWidth).Y + pad * 0.6f;
        var footer = Loc.T(L.Log.Footer, "[FTA]");
        TextDraw.Wrapped(footer, new Vector2(cursor.X + pad, y), wrapWidth, Styling.TextMuted);
        y += TextDraw.MeasureWrapped(footer, wrapWidth).Y;
        ImGui.Dummy(new Vector2(width, y - cursor.Y));
    }

    private bool HasSelection => anchorSequence != 0;

    private bool IsSelected(long sequence)
    {
        if (HasSelection && sequence >= Math.Min(anchorSequence, focusSequence))
        {
            return sequence <= Math.Max(anchorSequence, focusSequence);
        }
        return false;
    }

    private void ClearSelection()
    {
        anchorSequence = 0L;
        focusSequence = 0L;
    }

    private void SelectSingle(int index)
    {
        anchorSequence = view[index].Sequence;
        focusSequence = anchorSequence;
    }

    private void ClickRow(int index, bool extend)
    {
        var sequence = view[index].Sequence;
        if (extend && HasSelection)
        {
            focusSequence = sequence;
        }
        else if (anchorSequence == sequence && focusSequence == sequence)
        {
            ClearSelection();
        }
        else
        {
            SelectSingle(index);
        }
    }

    private void SelectAll()
    {
        if (viewCount == 0)
        {
            return;
        }
        anchorSequence = view[0].Sequence;
        focusSequence = view[viewCount - 1].Sequence;
    }

    private void MoveSelection(int delta, bool extend)
    {
        if (viewCount == 0)
        {
            return;
        }
        var target = (HasSelection ? Math.Clamp(LowerBound(focusSequence) + delta, 0, viewCount - 1) : ((delta < 0) ? (viewCount - 1) : 0));
        if (extend && HasSelection)
        {
            focusSequence = view[target].Sequence;
        }
        else
        {
            SelectSingle(target);
        }
        pendingScrollIndex = target;
        if (target < viewCount - 1 && follow)
        {
            Detach();
        }
    }

    private (int Low, int High) SelectionIndices()
    {
        if (!HasSelection)
        {
            return (Low: 0, High: -1);
        }
        var low = LowerBound(Math.Min(anchorSequence, focusSequence));
        var high = LowerBound(Math.Max(anchorSequence, focusSequence) + 1) - 1;
        return (Low: low, High: high);
    }

    private int LowerBound(long sequence)
    {
        var lo = 0;
        var hi = viewCount;
        while (lo < hi)
        {
            var mid = lo + hi >> 1;
            if (view[mid].Sequence < sequence)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }
        return lo;
    }

    private int SingleSelectionIndex()
    {
        if (!HasSelection || anchorSequence != focusSequence)
        {
            return -1;
        }
        var index = LowerBound(anchorSequence);
        if (index >= viewCount || view[index].Sequence != anchorSequence)
        {
            return -1;
        }
        return index;
    }

    private void OnViewRebuilt(long previousTail, bool filterChanged)
    {
        if (filterChanged)
        {
            sourceColumn = 0f;
        }
        if (HasSelection)
        {
            var (low, high) = SelectionIndices();
            if (high < low)
            {
                ClearSelection();
            }
        }
        if (follow || viewCount == 0 || view[viewCount - 1].Sequence == previousTail)
        {
            return;
        }
        var unseen = 0;
        for (var i = viewCount - 1; i >= 0 && view[i].Sequence > detachedTail; i--)
        {
            unseen++;
        }
        newWhileDetached = unseen;
    }

    private void HandleShortcuts()
    {
        if (!ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
        {
            return;
        }
        var io = ImGui.GetIO();
        if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.F))
        {
            focusSearch = true;
        }
        else
        {
            if (io.WantTextInput || searchFocused)
            {
                return;
            }
            if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.C))
            {
                if (HasSelection)
                {
                    var (low, high) = SelectionIndices();
                    CopyRange(low, high);
                }
                else
                {
                    CopyView();
                }
            }
            else if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.A))
            {
                SelectAll();
            }
            else if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
            {
                MoveSelection(-1, io.KeyShift);
            }
            else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
            {
                MoveSelection(1, io.KeyShift);
            }
        }
    }

    private void CopyView()
    {
        if (viewCount == 0)
        {
            return;
        }
        ImGui.SetClipboardText(RunLogReport.WithHeader(view.AsSpan(0, viewCount), in builtFilter, bufferedCount));
        copiedAtMs = Environment.TickCount64;
        ShowNotice(Loc.Plural(L.Log.CopiedLines, viewCount));
    }

    private void CopyRange(int low, int high)
    {
        if (low < 0 || high < low || high >= viewCount)
        {
            return;
        }
        var count = high - low + 1;
        ImGui.SetClipboardText(RunLogReport.Plain(view.AsSpan(low, count)));
        ShowNotice(Loc.Plural(L.Log.CopiedLines, count));
    }
}
