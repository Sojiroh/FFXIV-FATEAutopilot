using AutoFateGrind.Core.Game.Items;
using AutoFateGrind.Core.Tasks;
using AutoFateGrind.Core.Zones;
using AutoFateGrind.Windows.Sections;
using AutoFateGrind.Windows.Shell;

namespace AutoFateGrind.Windows.Pages;

internal sealed class GrindPage
{
    private const float SwitchRevealMs = 320f;

    public void Draw(Plugin plugin, AppWindow window)
    {
        Configuration configuration = plugin.Configuration;
        AutoFateController controller = plugin.Controller;
        bool running = controller.Running;
        using (Motion.PushSwitch("##fta_grind_state", running, SwitchRevealMs))
        {
            if (running)
            {
                RunningPanel.Draw(configuration, controller);
            }
            else
            {
                DrawIdle(plugin, window, configuration, controller);
            }
        }
    }

    private static void DrawIdle(Plugin plugin, AppWindow window, Configuration cfg, AutoFateController ctrl)
    {
        switch (Headline.Draw(cfg, ctrl, plugin.History))
        {
            case Headline.Action.OpenPlugins:
                window.Show(AppWindow.Page.Plugins);
                break;
            case Headline.Action.AutoPick:
                ZoneSelection.AutoPick(cfg);
                break;
        }
        Styling.VSpace(20f);
        int count = ZoneSelection.ResolveStartList(cfg).Count;
        GoalSection.Draw(cfg, ctrl, count);
        Styling.VSpace(14f);
        StopRow.Draw(cfg, ctrl);
        Styling.VSpace(26f);
        if (ItemGoalCatalog.IsItemGoal(cfg.ActiveMode.Id))
        {
            ItemGoalRoster.Draw(cfg, ctrl, scrollIntoView: false);
        }
        else if (ZoneSelection.IsYokaiGoal(cfg))
        {
            YokaiRoster.Draw(cfg, ctrl, scrollIntoView: false);
        }
        else
        {
            ZoneLibrary.Draw(cfg, ctrl, scrollIntoView: false);
        }
        Styling.VSpace(12f);
    }
}
