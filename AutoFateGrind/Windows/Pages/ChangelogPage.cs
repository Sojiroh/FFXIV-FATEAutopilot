using System;
using System.Globalization;
using System.Numerics;
using AutoFateGrind.Core.Changelog;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AutoFateGrind.Windows.Pages;

internal sealed class ChangelogPage
{
    private const float RailWidth = 26f;

    private const float RailDotRadius = 5f;

    private const float CardGap = 14f;

    private const float CardPadX = 18f;

    private const float CardPadY = 16f;

    private const float HeaderGap = 10f;

    private const float SeparatorGap = 12f;

    private const float BulletColumn = 18f;

    private const float BulletRadius = 2.5f;

    private const float BulletGap = 8f;

    private const float PillPadX = 8f;

    private const float PillPadY = 3f;

    private const float PillGap = 10f;

    private const float RevealMs = 360f;

    private const float RevealStaggerMs = 80f;

    private const float RevealSlide = 10f;

    private const string DateFormat = "d MMM yyyy";

    private const string MetaSeparator = " · ";

    private readonly string[] versionLabels = new string[ChangelogData.Entries.Length];

    private readonly string[] metaLabels = new string[ChangelogData.Entries.Length];

    private LanguageInfo? labelsLanguage;

    private long visitTick = long.MinValue;

    private int unseenAtVisit;

    public void Draw(long shownTick)
    {
        BeginVisit(shownTick);
        RefreshLabels();
        PageHeader.Draw(Loc.T(L.Changelog.Title), Loc.T(L.Changelog.Subtitle));
        ChangelogEntry[] entries = ChangelogData.Entries;
        for (int i = 0; i < entries.Length; i++)
        {
            using (Motion.PushReveal(Motion.Reveal(shownTick, RevealMs, (float)i * RevealStaggerMs)))
            {
                DrawEntry(i, i == entries.Length - 1);
            }
        }
    }

    private void BeginVisit(long shownTick)
    {
        if (visitTick == shownTick)
        {
            return;
        }
        visitTick = shownTick;
        unseenAtVisit = ChangelogData.UnseenCount(Plugin.Cfg.LastSeenChangelogVersion);
        Plugin.Cfg.MarkChangelogSeen();
    }

    private void RefreshLabels()
    {
        if (labelsLanguage == Loc.Current)
        {
            return;
        }
        labelsLanguage = Loc.Current;
        ChangelogEntry[] entries = ChangelogData.Entries;
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            versionLabels[i] = Loc.T(L.Changelog.Version, entry.Version);
            metaLabels[i] = FormatDate(entry.Date) + MetaSeparator + Loc.Plural(L.Changelog.Changes, entry.Highlights.Length);
        }
    }

    private void DrawEntry(int index, bool isLast)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var entry = ChangelogData.Entries[index];
        var isUnseen = index < unseenAtVisit;
        var isLatest = index == 0;
        var cursor = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var railWidth = RailWidth * globalScale;
        var min = new Vector2(cursor.X + railWidth, cursor.Y);
        var cardWidth = width - railWidth;
        var padX = CardPadX * globalScale;
        var padY = CardPadY * globalScale;
        var textLeft = min.X + padX + BulletColumn * globalScale;
        var textWidth = MathF.Max(1f, min.X + cardWidth - padX - textLeft);
        float headlineHeight;
        using (Fonts.PushHeadline())
        {
            headlineHeight = TextDraw.LineHeight();
        }
        var bodyHeight = MeasureBody(entry.Highlights, textWidth);
        var separatorY = min.Y + padY + headlineHeight + SeparatorGap * globalScale;
        var cardHeight = separatorY - min.Y + SeparatorGap * globalScale + bodyHeight + padY;
        var max = new Vector2(min.X + cardWidth, min.Y + cardHeight);
        var drawList = ImGui.GetWindowDrawList();
        var rounding = 10f * globalScale;
        var headerMidY = min.Y + padY + headlineHeight * 0.5f;
        DrawRail(drawList, cursor, headerMidY, cardHeight, isUnseen | isLatest, isLast);
        if (isUnseen)
        {
            Paint.Glow(drawList, min, max, rounding, Styling.AccentViolet, 0.8f);
        }
        if (isLatest)
        {
            Paint.Glass(drawList, min, max, rounding, Styling.AccentViolet, 0.08f);
            Paint.Stroke(drawList, min, max, Styling.WithAlpha(Styling.AccentViolet, isUnseen ? 0.55f : 0.3f), rounding);
        }
        else
        {
            Paint.Surface(drawList, min, max, rounding, Styling.WithAlpha(Styling.Surface1, 0.75f), Styling.WithAlpha(Styling.BorderDim, 0.5f));
        }
        var headerX = min.X + padX;
        using (Fonts.PushHeadline())
        {
            TextDraw.At(versionLabels[index], new Vector2(headerX, min.Y + padY), isLatest ? Styling.AccentVioletSoft : Styling.TextStrong);
            headerX += TextDraw.Measure(versionLabels[index]).X + HeaderGap * globalScale;
        }
        if (isUnseen | isLatest)
        {
            headerX += DrawPill(drawList, isUnseen ? L.Changelog.New : L.Changelog.Latest, headerX, headerMidY, isUnseen) + PillGap * globalScale;
        }
        using (Fonts.PushCaption())
        {
            var meta = metaLabels[index];
            var metaSize = TextDraw.Measure(meta);
            var metaX = max.X - padX - metaSize.X;
            if (metaX > headerX)
            {
                TextDraw.At(meta, new Vector2(metaX, headerMidY - metaSize.Y * 0.5f), Styling.TextMuted);
            }
        }
        Paint.Hairline(drawList, new Vector2(min.X + padX, separatorY), new Vector2(max.X - padX, separatorY));
        DrawBody(drawList, entry.Highlights, min.X + padX, textLeft, textWidth, separatorY + SeparatorGap * globalScale);
        ImGui.SetCursorScreenPos(cursor);
        ImGui.Dummy(new Vector2(width, cardHeight + (isLast ? 0f : (CardGap * globalScale))));
    }

    private static void DrawRail(ImDrawListPtr dl, Vector2 origin, float dotY, float cardHeight, bool highlighted, bool isLast)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var dot = new Vector2(origin.X + RailWidth * globalScale * 0.5f - 2f * globalScale, dotY);
        var dotRadius = RailDotRadius * globalScale;
        if (!isLast)
        {
            var lineEndY = origin.Y + cardHeight + CardGap * globalScale + (dotY - origin.Y) - dotRadius;
            dl.AddLine(dot + new Vector2(0f, dotRadius + 2f * globalScale), new Vector2(dot.X, lineEndY), Paint.Col(Styling.WithAlpha(Styling.BorderDim, 0.8f)), 1.5f * globalScale);
        }
        if (highlighted)
        {
            Paint.Dot(dl, dot, dotRadius, Styling.AccentViolet, 0.28f);
        }
        else
        {
            dl.AddCircle(dot, dotRadius, Paint.Col(Styling.WithAlpha(Styling.TextDim, 0.8f)), 0, 1.5f * globalScale);
        }
    }

    private static float DrawPill(ImDrawListPtr dl, LocString label, float x, float midY, bool filled)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        using (Fonts.PushCaption())
        {
            var caption = TextDraw.Upper(Loc.T(label));
            var captionSize = TextDraw.Measure(caption);
            var min = new Vector2(x, midY - captionSize.Y * 0.5f - PillPadY * globalScale);
            var max = new Vector2(x + captionSize.X + PillPadX * 2f * globalScale, midY + captionSize.Y * 0.5f + PillPadY * globalScale);
            if (filled)
            {
                Paint.Pill(dl, min, max, Styling.AccentViolet, Styling.WithAlpha(Styling.AccentVioletSoft, 0.6f));
                TextDraw.At(caption, new Vector2(min.X + PillPadX * globalScale, midY - captionSize.Y * 0.5f), Styling.ForegroundOn(Styling.AccentViolet));
            }
            else
            {
                Paint.Pill(dl, min, max, Styling.WithAlpha(Styling.AccentViolet, 0.16f), Styling.WithAlpha(Styling.AccentViolet, 0.4f));
                TextDraw.At(caption, new Vector2(min.X + PillPadX * globalScale, midY - captionSize.Y * 0.5f), Styling.AccentVioletSoft);
            }
            return max.X - min.X;
        }
    }

    private static float MeasureBody(LocString[] highlights, float textWidth)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var height = 0f;
        for (int i = 0; i < highlights.Length; i++)
        {
            if (i > 0)
            {
                height += BulletGap * globalScale;
            }
            height += TextDraw.MeasureWrapped(Loc.T(highlights[i]), textWidth).Y;
        }
        return height;
    }

    private static void DrawBody(ImDrawListPtr dl, LocString[] highlights, float bulletX, float textLeft, float textWidth, float top)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var lineHeight = TextDraw.LineHeight();
        var y = top;
        for (int i = 0; i < highlights.Length; i++)
        {
            if (i > 0)
            {
                y += BulletGap * globalScale;
            }
            var bullet = Loc.T(highlights[i]);
            dl.AddCircleFilled(new Vector2(bulletX + BulletRadius * globalScale, y + lineHeight * 0.5f), BulletRadius * globalScale, Paint.Col(Styling.AccentViolet));
            TextDraw.Wrapped(bullet, new Vector2(textLeft, y), textWidth, Styling.TextSecondary);
            y += TextDraw.MeasureWrapped(bullet, textWidth).Y;
        }
    }

    private static string FormatDate(string isoDate)
    {
        if (!DateTime.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return isoDate;
        }
        return date.ToString(DateFormat, Loc.Culture);
    }
}
