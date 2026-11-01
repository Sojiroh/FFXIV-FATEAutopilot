using System.Numerics;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Tasks;
using AutoFateGrind.Windows.Components;
using AutoFateGrind.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using clib.Utils;

namespace AutoFateGrind.Windows.Shell;

internal static class MiniPlayer
{
    private const float PadX = 18f;

    private const float ButtonSize = 34f;

    private const float ButtonGap = 8f;

    private const float BarWidth = 160f;

    private const float BarHeight = 8f;

    public static bool Draw(Plugin plugin, Vector2 size, float windowRounding)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var end = cursorScreenPos + size;
        var windowDrawList = ImGui.GetWindowDrawList();
        var configuration = plugin.Configuration;
        var controller = plugin.Controller;
        var currentFate = PublicEvent.CurrentFate;
        var fateRunning = currentFate != null && currentFate.State == FateState.Running && !controller.Paused;
        var info = ReadyState.Resolve(configuration, controller);
        Dock.Background(windowDrawList, cursorScreenPos, end, windowRounding);
        var padX = PadX * globalScale;
        var buttonSize = ButtonSize * globalScale;
        var buttonsWidth = buttonSize * 2f + ButtonGap * globalScale;
        ImGui.SetCursorScreenPos(cursorScreenPos);
        var hit = Hit.Area("##fta_mini_open", new Vector2(size.X - padX - buttonsWidth - ButtonGap * globalScale, size.Y));
        var hover = Motion.Hover(Motion.Key("##fta_mini_open"), hit.Hovered);
        if (hover > 0.01f)
        {
            Paint.Fill(windowDrawList, cursorScreenPos, end, Styling.WithAlpha(Styling.Surface2, 0.35f * hover), windowRounding, ImDrawFlags.RoundCornersBottom);
        }
        var centerY = cursorScreenPos.Y + size.Y * 0.5f;
        var color = controller.Paused ? info.Accent : Styling.PulseColor(info.Accent, info.AccentSoft);
        Paint.Dot(windowDrawList, new Vector2(cursorScreenPos.X + padX + 4f * globalScale, centerY), 4f * globalScale, color);
        var barWidth = BarWidth * globalScale;
        var barX = end.X - padX - buttonsWidth - 16f * globalScale - barWidth;
        var barY = centerY - BarHeight * globalScale * 0.5f;
        if (fateRunning)
        {
            Paint.Bar(windowDrawList, new Vector2(barX, barY), barWidth, BarHeight * globalScale, (float)currentFate!.Progress / 100f, info.Accent);
        }
        else if (controller.Paused)
        {
            Paint.Bar(windowDrawList, new Vector2(barX, barY), barWidth, BarHeight * globalScale, 0f, info.Accent);
        }
        else
        {
            Paint.IndeterminateBar(windowDrawList, new Vector2(barX, barY), barWidth, BarHeight * globalScale, info.Accent);
        }
        var textX = cursorScreenPos.X + padX + 22f * globalScale;
        var label = controller.Paused ? Loc.T(L.Run.PhasePaused) : ReadyState.PhaseLabel(controller.Phase);
        var labelSize = TextDraw.SmallCapsSize(label);
        var textLineHeight = ImGui.GetTextLineHeight();
        var lineGap = 2f * globalScale;
        var textTop = centerY - (labelSize.Y + lineGap + textLineHeight) * 0.5f;
        TextDraw.SmallCaps(label, new Vector2(textX, textTop), info.AccentSoft);
        TextDraw.At(TextDraw.Truncate(fateRunning ? $"L{currentFate!.Level}   {currentFate!.Name}" : controller.Status, barX - 16f * globalScale - textX), new Vector2(textX, textTop + labelSize.Y + lineGap), Styling.TextStrong);
        var resumeBlocked = controller.Paused && controller.PauseReason == PauseReason.InContent;
        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - buttonSize * 2f - ButtonGap * globalScale, centerY - buttonSize * 0.5f));
        if (IconButton.Draw((FontAwesomeIcon)(controller.Paused ? 61515 : 61516), "##fta_mini_pause", buttonSize, controller.Paused ? Styling.AccentMintSoft : Styling.AccentAmberSoft, resumeBlocked ? Loc.T(L.Shell.ResumeBlocked) : (controller.Paused ? Loc.T(L.Common.Resume) : Loc.T(L.Common.Pause)), !resumeBlocked))
        {
            controller.TogglePause();
        }
        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - buttonSize, centerY - buttonSize * 0.5f));
        if (IconButton.Draw(FontAwesomeIcon.Stop, "##fta_mini_stop", buttonSize, Styling.AccentRose, Loc.T(L.Common.StopRun)))
        {
            controller.Stop();
        }
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(size);
        return hit.Clicked;
    }
}
