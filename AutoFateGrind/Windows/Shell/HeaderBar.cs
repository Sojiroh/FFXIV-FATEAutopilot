using System;
using System.Numerics;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Zones;
using AutoFateGrind.Windows.Components;
using AutoFateGrind.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using clib.Utils;

namespace AutoFateGrind.Windows.Shell;

internal static class HeaderBar
{
    private const string Title = "FATE Autopilot";

    private const float PadX = 16f;

    private const float IconBox = 26f;

    private const float ButtonSize = 30f;

    private const float ButtonGap = 6f;

    private const int ButtonCount = 3;

    private const float CompactBarWidth = 90f;

    public const float MinimumWidth = 172f;

    public static float ButtonsWidth()
    {
        return ((float)ButtonCount * ButtonSize + (float)(ButtonCount - 1) * ButtonGap) * ImGuiHelpers.GlobalScale;
    }

    public static bool HandleDrag(Vector2 windowPos, float width, float height)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var dragWidth = width - PadX * globalScale - ButtonsWidth() - 8f * globalScale;
        ImGui.SetCursorScreenPos(windowPos);
        ImGui.InvisibleButton("##fta_drag", new Vector2(MathF.Max(1f, dragWidth), height));
        var doubleClicked = ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);
        if (!ImGui.IsItemActive())
        {
            return doubleClicked;
        }
        var mouseDelta = ImGui.GetIO().MouseDelta;
        if (mouseDelta != Vector2.Zero)
        {
            ImGui.SetWindowPos(ImGui.GetWindowPos() + mouseDelta, ImGuiCond.Always);
        }
        return doubleClicked;
    }

    public static void Draw(AppWindow window, Plugin plugin, Vector2 origin, float width, float height, float windowRounding, bool compact)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var windowDrawList = ImGui.GetWindowDrawList();
        var end = origin + new Vector2(width, height);
        var padX = PadX * globalScale;
        var midY = origin.Y + height * 0.5f;
        Paint.Fill(windowDrawList, origin, end, Styling.WithAlpha(Styling.Surface1, 0.4f), windowRounding, compact ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersTop);
        if (!compact)
        {
            Paint.Hairline(windowDrawList, new Vector2(origin.X, end.Y - 0.5f), new Vector2(end.X, end.Y - 0.5f));
        }
        var iconSize = IconBox * globalScale;
        var iconMin = new Vector2(origin.X + padX, midY - iconSize * 0.5f);
        AppIcon.Draw(windowDrawList, iconMin, iconMin + new Vector2(iconSize, iconSize), 7f * globalScale);
        var rightLimit = end.X - padX - ButtonsWidth();
        var cursorX = iconMin.X + iconSize + 12f * globalScale;
        using (Fonts.PushHeadline())
        {
            var titleSize = TextDraw.Measure(Title);
            if (cursorX + titleSize.X <= rightLimit)
            {
                TextDraw.At(Title, new Vector2(cursorX, midY - titleSize.Y * 0.5f), Styling.TextStrong);
                cursorX += titleSize.X + 14f * globalScale;
            }
        }
        var info = ReadyState.Resolve(plugin.Configuration, plugin.Controller);
        var pillEnd = DrawStatusPill(windowDrawList, info, cursorX, rightLimit, midY);
        if (pillEnd > cursorX)
        {
            cursorX = pillEnd + 14f * globalScale;
        }
        if (compact)
        {
            DrawCompactStrip(plugin, info, cursorX, rightLimit - 14f * globalScale, midY);
        }
        DrawButtons(window, plugin, end, midY, compact);
    }

    private static void DrawCompactStrip(Plugin plugin, ReadyState.Info info, float x, float rightX, float midY)
    {
        if (WalletChip.TryRead(out var count))
        {
            var chipWidth = WalletChip.Width(count);
            if (rightX - x >= chipWidth)
            {
                WalletChip.Draw(plugin.Configuration, count, rightX, midY);
                rightX -= chipWidth + 14f * ImGuiHelpers.GlobalScale;
            }
        }
        DrawCompactInfo(plugin, info, x, rightX, midY);
    }

    private static void DrawButtons(AppWindow window, Plugin plugin, Vector2 end, float midY, bool compact)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var padRight = PadX * globalScale;
        var buttonSize = ButtonSize * globalScale;
        var step = buttonSize + ButtonGap * globalScale;
        var top = midY - buttonSize * 0.5f;
        ImGui.SetCursorScreenPos(new Vector2(end.X - padRight - buttonSize, top));
        if (IconButton.Draw(FontAwesomeIcon.Times, "##fta_close", buttonSize, tooltip: Loc.T(L.Common.Close)))
        {
            window.IsOpen = false;
        }
        ImGui.SetCursorScreenPos(new Vector2(end.X - padRight - buttonSize - step, top));
        var icon = compact ? 61559 : 61560;
        if (IconButton.Draw((FontAwesomeIcon)icon, "##fta_minimize", buttonSize, tooltip: compact ? Loc.T(L.Shell.Restore) : Loc.T(L.Shell.Minimize)))
        {
            window.ToggleCompact();
        }
        var configuration = plugin.Configuration;
        var showLivePopout = configuration.ShowLivePopout;
        ImGui.SetCursorScreenPos(new Vector2(end.X - padRight - buttonSize - step * 2f, top));
        if (IconButton.Draw(FontAwesomeIcon.ExternalLinkAlt, "##fta_popout", buttonSize, showLivePopout ? new Vector4?(Styling.AccentVioletSoft) : ((Vector4?)null), showLivePopout ? Loc.T(L.Shell.HideLiveTracker) : Loc.T(L.Shell.ShowLiveTracker)))
        {
            configuration.ShowLivePopout = !showLivePopout;
            plugin.LiveFateWindow.IsOpen = configuration.ShowLivePopout;
            configuration.Save();
        }
    }

    private static float DrawStatusPill(ImDrawListPtr dl, ReadyState.Info info, float x, float rightLimit, float midY)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var label = ReadyState.ShortLabel(info.Kind);
        var padX = 10f * globalScale;
        var dotRadius = 3.5f * globalScale;
        using (Fonts.PushCaption())
        {
            var labelSize = TextDraw.Measure(label);
            var height = labelSize.Y + 8f * globalScale;
            var min = new Vector2(x, midY - height * 0.5f);
            var max = min + new Vector2(padX * 2f + dotRadius * 2f + 6f * globalScale + labelSize.X, height);
            if (max.X > rightLimit)
            {
                return x;
            }
            Paint.Pill(dl, min, max, Styling.WithAlpha(info.Accent, 0.16f), Styling.WithAlpha(info.Accent, 0.45f));
            var running = info.Kind == ReadyState.Kind.Running;
            var color = running ? Styling.PulseColor(info.Accent, info.AccentSoft) : info.Accent;
            dl.AddCircleFilled(new Vector2(min.X + padX + dotRadius, midY), dotRadius, Paint.Col(color));
            TextDraw.At(label, new Vector2(min.X + padX + dotRadius * 2f + 6f * globalScale, midY - labelSize.Y * 0.5f), info.AccentSoft);
            return max.X;
        }
    }

    private static void DrawCompactInfo(Plugin plugin, ReadyState.Info info, float x, float rightX, float midY)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var windowDrawList = ImGui.GetWindowDrawList();
        var controller = plugin.Controller;
        var configuration = plugin.Configuration;
        if (rightX - x < CompactBarWidth * globalScale)
        {
            return;
        }
        if (!controller.Running)
        {
            var count = ZoneSelection.ResolveStartList(configuration).Count;
            if (count == 0)
            {
                return;
            }
            var startSub = GoalSummary.StartSub(configuration, count);
            using (Fonts.PushCaption())
            {
                var subSize = TextDraw.Measure(startSub);
                TextDraw.At(TextDraw.Truncate(startSub, rightX - x), new Vector2(x, midY - subSize.Y * 0.5f), Styling.TextDim);
            }
            return;
        }
        var currentFate = PublicEvent.CurrentFate;
        var fateRunning = currentFate != null && currentFate.State == FateState.Running && !controller.Paused;
        var barWidth = CompactBarWidth * globalScale;
        var barX = rightX - barWidth;
        var barHeight = 6f * globalScale;
        var origin = new Vector2(barX, midY - barHeight * 0.5f);
        if (fateRunning)
        {
            Paint.Bar(windowDrawList, origin, barWidth, barHeight, (float)currentFate!.Progress / 100f, info.Accent);
        }
        else if (controller.Paused)
        {
            Paint.Bar(windowDrawList, origin, barWidth, barHeight, 0f, info.Accent);
        }
        else
        {
            Paint.IndeterminateBar(windowDrawList, origin, barWidth, barHeight, info.Accent);
        }
        var textWidth = barX - 12f * globalScale - x;
        if (textWidth <= 0f)
        {
            return;
        }
        var phase = controller.Paused ? Loc.T(L.Run.PhasePaused) : ReadyState.PhaseLabel(controller.Phase);
        var line = fateRunning ? $"{phase}  ·  L{currentFate!.Level} {currentFate!.Name}" : (phase + "  ·  " + controller.Status);
        using (Fonts.PushCaption())
        {
            var lineSize = TextDraw.Measure(line);
            TextDraw.At(TextDraw.Truncate(line, textWidth), new Vector2(x, midY - lineSize.Y * 0.5f), Styling.TextSecondary);
        }
    }
}
