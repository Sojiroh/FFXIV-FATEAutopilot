using System.Numerics;
using AutoFateGrind.Core;
using AutoFateGrind.Core.External;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoFateGrind.Windows.Shell;

internal static class NavRail
{
    private readonly record struct Entry(AppWindow.Page Page, FontAwesomeIcon Icon, string Id, LocString Label);

    private const float TopPad = 12f;

    private const float Gap = 8f;

    private static readonly Entry[] entries = new Entry[]
    {
        new Entry(AppWindow.Page.Grind, FontAwesomeIcon.Play, "##fta_nav_grind", L.Shell.NavGrind),
        new Entry(AppWindow.Page.Settings, FontAwesomeIcon.SlidersH, "##fta_nav_settings", L.Shell.NavSettings),
        new Entry(AppWindow.Page.History, FontAwesomeIcon.ChartLine, "##fta_nav_history", L.Shell.NavHistory),
        new Entry(AppWindow.Page.Plugins, FontAwesomeIcon.Plug, "##fta_nav_plugins", L.Shell.NavPlugins),
        new Entry(AppWindow.Page.Log, FontAwesomeIcon.Terminal, "##fta_nav_log", L.Shell.NavLog),
        new Entry(AppWindow.Page.Changelog, FontAwesomeIcon.Newspaper, "##fta_nav_changelog", L.Shell.NavChangelog),
        new Entry(AppWindow.Page.About, FontAwesomeIcon.InfoCircle, "##fta_nav_about", L.Shell.NavAbout)
    };

    public static AppWindow.Page? Draw(AppWindow.Page current, Plugin plugin)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var buttonSize = Layout.RailButton * globalScale;
        var gap = Gap * globalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var buttonX = cursorScreenPos.X + (width - buttonSize) * 0.5f;
        var top = cursorScreenPos.Y + TopPad * globalScale;
        var windowDrawList = ImGui.GetWindowDrawList();
        var currentIndex = 0;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].Page == current)
            {
                currentIndex = i;
            }
        }
        var indicator = Motion.Approach(Motion.Key("##fta_rail_indicator"), currentIndex, 16f);
        var indicatorY = top + (buttonSize + gap) * indicator;
        var indicatorMin = new Vector2(buttonX, indicatorY);
        var indicatorMax = indicatorMin + new Vector2(buttonSize, buttonSize);
        Paint.Glass(windowDrawList, indicatorMin, indicatorMax, 12f * globalScale, Styling.AccentViolet, 0.3f);
        Paint.Fill(windowDrawList, new Vector2(cursorScreenPos.X, indicatorY + buttonSize * 0.25f), new Vector2(cursorScreenPos.X + 3f * globalScale, indicatorY + buttonSize * 0.75f), Styling.AccentViolet, 2f * globalScale);
        var missingPlugins = !ExternalPlugins.AllRequiredInstalled();
        var running = plugin.Controller.Running;
        AppWindow.Page? clicked = null;
        for (int j = 0; j < entries.Length; j++)
        {
            var entry = entries[j];
            var buttonY = top + (buttonSize + gap) * (float)j;
            ImGui.SetCursorScreenPos(new Vector2(buttonX, buttonY));
            var hit = Hit.Area(entry.Id, new Vector2(buttonSize, buttonSize));
            var hover = Motion.Hover(Motion.Key(entry.Id), hit.Hovered);
            var isCurrent = j == currentIndex;
            if (!isCurrent && hover > 0.01f)
            {
                Paint.Fill(windowDrawList, new Vector2(buttonX, buttonY), new Vector2(buttonX + buttonSize, buttonY + buttonSize), Styling.WithAlpha(Styling.Surface2, 0.8f * hover), 12f * globalScale);
            }
            var center = new Vector2(buttonX + buttonSize * 0.5f, buttonY + buttonSize * 0.5f);
            var color = isCurrent ? Styling.TextStrong : Vector4.Lerp(Styling.TextDim, Styling.TextSecondary, hover);
            TextDraw.IconCentered(entry.Icon, center, color);
            DrawBadge(windowDrawList, entry.Page, current, center, buttonSize, missingPlugins, running);
            if (hit.Hovered)
            {
                Tooltip.Show(Loc.T(entry.Label));
            }
            if (hit.Clicked)
            {
                clicked = entry.Page;
            }
        }
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(new Vector2(width, TopPad * globalScale + (buttonSize + gap) * (float)entries.Length));
        return clicked;
    }

    private static void DrawBadge(ImDrawListPtr dl, AppWindow.Page page, AppWindow.Page current, Vector2 center, float button, bool missingPlugins, bool running)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var badgeCenter = center + new Vector2(button * 0.3f, -button * 0.3f);
        var badgeRadius = 3.5f * globalScale;
        if ((page == AppWindow.Page.Plugins) & missingPlugins)
        {
            dl.AddCircleFilled(badgeCenter, badgeRadius + 1.5f * globalScale, Paint.Col(Styling.WindowBg));
            dl.AddCircleFilled(badgeCenter, badgeRadius, Paint.Col(Styling.AccentRose));
        }
        else if ((page == AppWindow.Page.Grind) & running)
        {
            dl.AddCircleFilled(badgeCenter, badgeRadius + 1.5f * globalScale, Paint.Col(Styling.WindowBg));
            dl.AddCircleFilled(badgeCenter, badgeRadius, Paint.Col(Styling.PulseColor(Styling.AccentBlue, Styling.AccentBlueSoft)));
        }
        else if (page == AppWindow.Page.Changelog && current != AppWindow.Page.Changelog && Plugin.Cfg.HasUnseenChangelog)
        {
            dl.AddCircleFilled(badgeCenter, badgeRadius + 1.5f * globalScale, Paint.Col(Styling.WindowBg));
            dl.AddCircleFilled(badgeCenter, badgeRadius, Paint.Col(Styling.PulseColor(Styling.AccentViolet, Styling.AccentVioletSoft)));
        }
        else if (page == AppWindow.Page.Log && current != AppWindow.Page.Log && RunLog.Unseen is { } level)
        {
            dl.AddCircleFilled(badgeCenter, badgeRadius + 1.5f * globalScale, Paint.Col(Styling.WindowBg));
            dl.AddCircleFilled(badgeCenter, badgeRadius, Paint.Col((level == RunLogLevel.Error) ? Styling.AccentRose : Styling.AccentAmber));
        }
    }
}
