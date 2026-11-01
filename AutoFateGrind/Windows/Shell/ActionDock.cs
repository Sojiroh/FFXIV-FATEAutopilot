using System.Numerics;
using AutoFateGrind.Core.External;
using AutoFateGrind.Core.Game.Items;
using AutoFateGrind.Core.Game.Player;
using AutoFateGrind.Core.Game.SharedFates;
using AutoFateGrind.Core.Game.Yokai;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Zones;
using AutoFateGrind.Windows.Components;
using AutoFateGrind.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AutoFateGrind.Windows.Shell;

internal static class ActionDock
{
    private const float PadX = 18f;

    private const float ButtonGap = 8f;

    public static void Draw(Plugin plugin, Vector2 size, float windowRounding)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var end = cursorScreenPos + size;
        Dock.Background(ImGui.GetWindowDrawList(), cursorScreenPos, end, windowRounding);
        var padX = PadX * globalScale;
        var buttonHeight = Layout.HeroButtonHeight * globalScale;
        var innerWidth = size.X - padX * 2f;
        ImGui.SetCursorScreenPos(new Vector2(cursorScreenPos.X + padX, cursorScreenPos.Y + (size.Y - buttonHeight) * 0.5f));
        if (plugin.Controller.Running)
        {
            DrawRunControls(plugin, innerWidth);
        }
        else
        {
            DrawStart(plugin, innerWidth);
        }
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(size);
    }

    private static void DrawRunControls(Plugin plugin, float innerWidth)
    {
        var controller = plugin.Controller;
        var gap = ButtonGap * ImGuiHelpers.GlobalScale;
        var width = (innerWidth - gap) * 0.5f;
        if (PauseButton.Draw(controller.PauseReason, width))
        {
            controller.TogglePause();
        }
        ImGui.SameLine(0f, gap);
        var sessionSnapshot = controller.SessionSnapshot;
        var stateLabel = controller.Paused ? Loc.T(L.Grind.StatePaused) : Loc.T(L.Grind.StateRunning);
        if (StopButton.Draw(controller.StopPending ? Loc.T(L.Grind.StopAfterFate) : (sessionSnapshot == null ? stateLabel : Loc.T(L.Grind.StopSub, stateLabel, Formatting.Elapsed(sessionSnapshot.Elapsed))), width))
        {
            if (ImGui.GetIO().KeyCtrl && !controller.StopPending)
            {
                controller.StopWhenSafe();
            }
            else
            {
                controller.Stop();
            }
        }
        if (ImGui.IsItemHovered())
        {
            Tooltip.Show(Loc.T(controller.StopPending ? L.Grind.StopNowHint : L.Grind.StopSoftHint));
        }
    }

    private static void DrawStart(Plugin plugin, float innerWidth)
    {
        var configuration = plugin.Configuration;
        var controller = plugin.Controller;
        var startZones = ZoneSelection.ResolveStartList(configuration);
        var requiredInstalled = ExternalPlugins.AllRequiredInstalled();
        var yokaiGoal = ZoneSelection.IsYokaiGoal(configuration);
        var rankedGoal = ZoneSelection.GoalNeedsRankedZones(configuration);
        var missingWatch = yokaiGoal && !YokaiOps.OwnsWatch();
        var allMaxed = rankedGoal && SharedFateProgress.AllMaxed(startZones);
        var itemGoalReason = ItemGoalReason(configuration);
        var levelsReached = configuration.StopAfterLevelsEnabled && ClassSwitcher.LevelingJobAtMax(configuration);
        var enabled = ((startZones.Count > 0) & requiredInstalled) && !missingWatch && !allMaxed && itemGoalReason == null && !levelsReached;
        var disabledReason = !requiredInstalled ? Loc.T(L.Grind.ReasonInstall) : missingWatch ? Loc.T(L.Grind.ReasonNoWatch) : allMaxed ? Loc.T(L.Grind.ReasonRanksMaxed) : itemGoalReason != null ? itemGoalReason : levelsReached ? Loc.T(L.Grind.ReasonAtLevelCap) : startZones.Count > 0 ? string.Empty : yokaiGoal ? Loc.T(L.Grind.ReasonNoYokai) : rankedGoal ? Loc.T(L.Grind.ReasonNoRankedZones) : Loc.T(L.Grind.ReasonPickZone);
        if (StartButton.Draw(GoalSummary.StartSub(configuration, startZones.Count), enabled, disabledReason, innerWidth))
        {
            controller.RunAll(startZones);
        }
    }

    private static string? ItemGoalReason(Configuration cfg)
    {
        var itemGoalDefinition = ItemGoalCatalog.Find(cfg.ActiveMode.Id);
        if (itemGoalDefinition == null)
        {
            return null;
        }
        return ItemGoalProgress.Blocker(itemGoalDefinition, cfg) switch
        {
            ItemGoalBlocker.NeedQuest => Loc.T(L.Grind.ReasonNeedQuest),
            ItemGoalBlocker.NeedZenith => Loc.T(L.Grind.ReasonNeedZenith),
            ItemGoalBlocker.AllCollected => Loc.T(L.Grind.ReasonAllCollected),
            _ => null,
        };
    }
}
