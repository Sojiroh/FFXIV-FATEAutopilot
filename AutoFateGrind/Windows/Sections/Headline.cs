using System;
using System.Numerics;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Stats;
using AutoFateGrind.Core.Tasks;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoFateGrind.Windows.Sections;

internal static class Headline
{
    public enum Action
    {
        None,
        OpenPlugins,
        AutoPick
    }

    private const float GreetingGap = 8f;

    private const float DetailGap = 6f;

    private const float RightGap = 18f;

    public static Action Draw(Configuration cfg, AutoFateController ctrl, RunHistory history)
    {
        var scale = ImGuiHelpers.GlobalScale;
        ReadyState.Info ready = ReadyState.Resolve(cfg, ctrl);
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var y = origin.Y;
        var (icon, color, greeting) = Greeting();
        using (Fonts.PushCaption())
        {
            var greetingSize = TextDraw.Measure(greeting);
            var iconSize = TextDraw.IconSize(icon);
            TextDraw.Icon(icon, new Vector2(origin.X + 1f * scale, y + (greetingSize.Y - iconSize.Y) * 0.5f), color);
            TextDraw.At(greeting, new Vector2(origin.X + iconSize.X + 8f * scale, y), Styling.TextDim);
            y += greetingSize.Y + GreetingGap * scale;
        }
        float titleHeight;
        using (Fonts.PushTitle())
        {
            titleHeight = ImGui.GetTextLineHeight();
        }
        var detailHeight = ImGui.GetTextLineHeight();
        var blockHeight = titleHeight + DetailGap * scale + detailHeight;
        var midY = y + blockHeight * 0.5f;
        var rightWidth = DrawRightColumn(ready, history, origin.X + width, midY, out var action);
        var maxWidth = width - rightWidth - RightGap * scale;
        using (Fonts.PushTitle())
        {
            TextDraw.At(TextDraw.Truncate(ready.Title + Loc.T(L.Grind.SentenceEnd), maxWidth), new Vector2(origin.X, y), Styling.TextStrong);
        }
        y += titleHeight + DetailGap * scale;
        TextDraw.At(TextDraw.Truncate(ready.Detail, maxWidth), new Vector2(origin.X, y), Styling.TextDim);
        y += detailHeight;
        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, y - origin.Y));
        return action;
    }

    private static (FontAwesomeIcon Icon, Vector4 Color, string Greeting) Greeting()
    {
        return DateTime.Now.Hour switch
        {
            >= 5 and < 12 => (FontAwesomeIcon.Sun, Styling.AccentAmber, Loc.T(L.Shell.GreetingMorning)),
            >= 12 and < 17 => (FontAwesomeIcon.Sun, Styling.AccentAmber, Loc.T(L.Shell.GreetingAfternoon)),
            >= 17 and < 22 => (FontAwesomeIcon.CloudMoon, Styling.AccentVioletSoft, Loc.T(L.Shell.GreetingEvening)),
            _ => (FontAwesomeIcon.Moon, Styling.AccentBlue, Loc.T(L.Shell.GreetingNight)),
        };
    }

    private static float DrawRightColumn(ReadyState.Info info, RunHistory history, float rightX, float midY, out Action action)
    {
        var scale = ImGuiHelpers.GlobalScale;
        action = Action.None;
        if (info.Kind == ReadyState.Kind.SetupNeeded)
        {
            var setupLabel = Loc.T(L.Grind.OpenPlugins);
            var setupWidth = PillButton.Width(setupLabel, FontAwesomeIcon.Plug);
            ImGui.SetCursorScreenPos(new Vector2(rightX - setupWidth, midY - 30f * scale * 0.5f));
            if (PillButton.Draw("##fta_open_plugins", setupLabel, Styling.AccentRose, PillButton.Emphasis.Tinted, FontAwesomeIcon.Plug, enabled: true, 30f))
            {
                action = Action.OpenPlugins;
            }
            return setupWidth;
        }
        if (info.Kind == ReadyState.Kind.PickZones)
        {
            var pickLabel = Loc.T(L.Grind.PickForMe);
            var pickWidth = PillButton.Width(pickLabel, FontAwesomeIcon.WandMagicSparkles);
            ImGui.SetCursorScreenPos(new Vector2(rightX - pickWidth, midY - 30f * scale * 0.5f));
            if (PillButton.Draw("##fta_auto_pick", pickLabel, Styling.AccentViolet, PillButton.Emphasis.Tinted, FontAwesomeIcon.WandMagicSparkles, enabled: true, 30f))
            {
                action = Action.AutoPick;
            }
            if (ImGui.IsItemHovered())
            {
                Tooltip.Show(Loc.T(L.Grind.PickForMeHint));
            }
            return pickWidth;
        }
        var (lastTitle, lastDetail) = LastRun(history);
        using (Fonts.PushCaption())
        {
            var titleSize = TextDraw.Measure(lastTitle);
            var detailSize = TextDraw.Measure(lastDetail);
            var lineGap = 3f * scale;
            var top = midY - (titleSize.Y + lineGap + detailSize.Y) * 0.5f;
            TextDraw.At(lastTitle, new Vector2(rightX - titleSize.X, top), Styling.TextSecondary);
            TextDraw.At(lastDetail, new Vector2(rightX - detailSize.X, top + titleSize.Y + lineGap), Styling.TextDim);
            return MathF.Max(titleSize.X, detailSize.X);
        }
    }

    private static (string Title, string Detail) LastRun(RunHistory history)
    {
        var records = history.Records;
        if (records.Count == 0)
        {
            return (Title: Loc.T(L.Grind.NoRunsYet), Detail: Loc.T(L.Grind.StatsAppearHere));
        }
        var runRecord = records[0];
        return (Title: Loc.T(L.Grind.LastRun, runRecord.FatesCompleted), Detail: Loc.T(L.Grind.LastRunDetail, Formatting.Elapsed(runRecord.Duration), runRecord.GemstonesEarned));
    }
}
