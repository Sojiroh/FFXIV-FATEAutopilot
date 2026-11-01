using System;
using System.Numerics;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Modes;
using AutoFateGrind.Core.Tasks;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoFateGrind.Windows.Sections;

internal static class StopRow
{
    private enum Limit
    {
        GoalOnly,
        Fates,
        Minutes,
        Levels
    }

    private enum ZoneShare
    {
        AsManyAsItFinds,
        ANumberOfFates
    }

    private readonly record struct Columns(float Label, float Control, float Right, float SegmentedWidth);

    private readonly record struct StepperSpec(string Id, int Step, int Max, string Unit);

    private const float PadX = 18f;

    private const float PadY = 14f;

    private const float RowGap = 10f;

    private const float Gap = 10f;

    private const float LabelWidth = 104f;

    private const float StepperWidth = 124f;

    private const float SegmentHeight = 36f;

    private const int FateStep = 5;

    private const int MinuteStep = 5;

    private const int LevelStep = 1;

    private const int ZoneFateStep = 1;

    private static readonly AfterRunAction[] afterOrder = new AfterRunAction[]
    {
        AfterRunAction.StayLoggedIn,
        AfterRunAction.ReturnToInn,
        AfterRunAction.Logout,
        AfterRunAction.CloseGame
    };

    private static readonly (LocString Short, LocString Detail)[] afterChoices = new (LocString, LocString)[]
    {
        (L.Grind.AfterStayShort, L.Grind.AfterStayDetail),
        (L.Grind.AfterInnShort, L.Grind.AfterInnDetail),
        (L.Grind.AfterLogoutShort, L.Grind.AfterLogoutDetail),
        (L.Grind.AfterCloseShort, L.Grind.AfterCloseDetail)
    };

    private static readonly Segmented.Item[] limitItems = new Segmented.Item[4];

    private static readonly Segmented.Item[] zoneItems = new Segmented.Item[2];

    private static readonly Segmented.Item[] afterItems = new Segmented.Item[4];

    public static void Draw(Configuration cfg, AutoFateController ctrl)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var padX = PadX * scale;
        var padY = PadY * scale;
        var editable = !ctrl.Running;
        var drawList = ImGui.GetWindowDrawList();
        var left = origin.X + padX;
        var right = origin.X + width - padX;
        FillItems(cfg);
        var control = left + LabelColumnWidth();
        var segmentedWidth = MathF.Min(MathF.Max(Segmented.PreferredWidth(limitItems), MathF.Max(Segmented.PreferredWidth(zoneItems), Segmented.PreferredWidth(afterItems))), right - control);
        var columns = new Columns(left, control, right, segmentedWidth);
        drawList.ChannelsSplit(2);
        drawList.ChannelsSetCurrent(1);
        var y = origin.Y + padY;
        y = DrawLimitRow(cfg, editable, columns, y);
        y += RowGap * scale;
        y = DrawZoneRow(cfg, editable, columns, y);
        y += RowGap * scale;
        y = DrawAfterRow(cfg, editable, columns, y);
        var max = new Vector2(origin.X + width, y + padY);
        drawList.ChannelsSetCurrent(0);
        Paint.Glass(drawList, origin, max, 12f * scale, Styling.AccentViolet, 0.05f, 0f, elevated: true);
        drawList.ChannelsMerge();
        if (!editable && Hit.HoveringRect(origin, max))
        {
            Tooltip.Show(Loc.T(L.Grind.PlanLocked));
        }
        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, max.Y - origin.Y));
    }

    private static void FillItems(Configuration cfg)
    {
        var hasTarget = GoalSummary.HasTarget(cfg);
        limitItems[0] = new Segmented.Item(hasTarget ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.HandPaper, hasTarget ? Loc.T(L.Grind.UntilGoalDone) : Loc.T(L.Grind.UntilYouStopIt));
        limitItems[1] = new Segmented.Item(FontAwesomeIcon.ListOl, Loc.T(L.Grind.ANumberOfFates));
        limitItems[2] = new Segmented.Item(FontAwesomeIcon.Stopwatch, Loc.T(L.Grind.ALengthOfTime));
        limitItems[3] = new Segmented.Item(FontAwesomeIcon.LevelUpAlt, Loc.T(L.Grind.ANumberOfLevels));
        zoneItems[0] = new Segmented.Item(FontAwesomeIcon.Infinity, Loc.T(L.Grind.ZoneAsManyAsItFinds));
        zoneItems[1] = new Segmented.Item(FontAwesomeIcon.ListOl, Loc.T(L.Grind.ANumberOfFates));
        afterItems[0] = new Segmented.Item(FontAwesomeIcon.MapMarkerAlt, Loc.T(afterChoices[0].Short));
        afterItems[1] = new Segmented.Item(FontAwesomeIcon.Bed, Loc.T(afterChoices[1].Short));
        afterItems[2] = new Segmented.Item(FontAwesomeIcon.SignOutAlt, Loc.T(afterChoices[2].Short));
        afterItems[3] = new Segmented.Item(FontAwesomeIcon.PowerOff, Loc.T(afterChoices[3].Short));
    }

    private static float DrawLimitRow(Configuration cfg, bool editable, Columns columns, float y)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var rowHeight = SegmentHeight * scale;
        var limit = CurrentLimit(cfg);
        DrawRowLabel(Loc.T(L.Grind.HowLong), columns.Label, y, rowHeight);
        ImGui.SetCursorScreenPos(new Vector2(columns.Control, y));
        var selected = (int)limit;
        if (Segmented.Draw("##fta_limit", limitItems, ref selected, editable, SegmentHeight, columns.SegmentedWidth) & editable)
        {
            ApplyLimit(cfg, (Limit)selected);
            limit = (Limit)selected;
        }
        if (limit == Limit.GoalOnly)
        {
            return y + rowHeight;
        }
        var spec = limit switch
        {
            Limit.Fates => new StepperSpec("##fta_limit_fates", FateStep, RunLimits.MaxFates, Loc.T(L.Grind.UnitFates)),
            Limit.Minutes => new StepperSpec("##fta_limit_minutes", MinuteStep, RunLimits.MaxMinutes, Loc.T(L.Grind.UnitMinutes)),
            _ => new StepperSpec("##fta_limit_levels", LevelStep, RunLimits.MaxLevels, Loc.T(L.Grind.UnitLevels)),
        };
        var target = limit switch
        {
            Limit.Fates => cfg.TargetFateCount,
            Limit.Minutes => cfg.TargetMinutes,
            _ => cfg.TargetLevels,
        };
        var bottom = PlaceStepper(columns, y, rowHeight, in spec, ref target, editable, out var changed);
        if (!changed)
        {
            return bottom;
        }
        switch (limit)
        {
            case Limit.Fates:
                cfg.TargetFateCount = target;
                break;
            case Limit.Minutes:
                cfg.TargetMinutes = target;
                break;
            default:
                cfg.TargetLevels = target;
                break;
        }
        cfg.SaveDebounced();
        return bottom;
    }

    private static float DrawZoneRow(Configuration cfg, bool editable, Columns columns, float y)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var rowHeight = SegmentHeight * scale;
        DrawRowLabel(Loc.T(L.Grind.EachZone), columns.Label, y, rowHeight);
        ImGui.SetCursorScreenPos(new Vector2(columns.Control, y));
        var selected = (int)(cfg.FatesPerZoneEnabled ? ZoneShare.ANumberOfFates : ZoneShare.AsManyAsItFinds);
        if (Segmented.Draw("##fta_zone_share", zoneItems, ref selected, editable, SegmentHeight, columns.SegmentedWidth) & editable)
        {
            cfg.FatesPerZoneEnabled = selected == 1;
            cfg.SaveDebounced();
        }
        if (!cfg.FatesPerZoneEnabled)
        {
            return y + rowHeight;
        }
        var spec = new StepperSpec("##fta_zone_fates", ZoneFateStep, RunLimits.MaxFatesPerZone, Loc.T(L.Grind.UnitFates));
        var target = cfg.TargetFatesPerZone;
        var bottom = PlaceStepper(columns, y, rowHeight, in spec, ref target, editable, out var changed);
        if (!changed)
        {
            return bottom;
        }
        cfg.TargetFatesPerZone = target;
        cfg.SaveDebounced();
        return bottom;
    }

    private static float PlaceStepper(Columns columns, float y, float rowHeight, in StepperSpec spec, ref int value, bool editable, out bool changed)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var inlineX = columns.Control + columns.SegmentedWidth + Gap * scale;
        if (inlineX + StepperRowWidth(spec.Unit) <= columns.Right)
        {
            changed = DrawStepper(in spec, ref value, editable, inlineX, y, rowHeight);
            return y + rowHeight;
        }
        var wrappedY = y + rowHeight + RowGap * scale;
        var frameHeight = ImGui.GetFrameHeight();
        changed = DrawStepper(in spec, ref value, editable, columns.Control, wrappedY, frameHeight);
        return wrappedY + frameHeight;
    }

    private static float StepperRowWidth(string unit)
    {
        return 134f * ImGuiHelpers.GlobalScale + TextDraw.Measure(unit).X;
    }

    private static bool DrawStepper(in StepperSpec spec, ref int value, bool editable, float x, float y, float rowHeight)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var frameHeight = ImGui.GetFrameHeight();
        var midY = y + rowHeight * 0.5f;
        value = Math.Clamp(value, 1, spec.Max);
        ImGui.SetCursorScreenPos(new Vector2(x, midY - frameHeight * 0.5f));
        var changed = Stepper.Draw(spec.Id, ref value, spec.Step, 1, spec.Max, "%d", StepperWidth) & editable;
        if (changed)
        {
            value = Math.Clamp(value, 1, spec.Max);
        }
        var unitSize = TextDraw.Measure(spec.Unit);
        TextDraw.At(spec.Unit, new Vector2(x + StepperWidth * scale + Gap * scale, midY - unitSize.Y * 0.5f), Styling.TextDim);
        return changed;
    }

    private static float DrawAfterRow(Configuration cfg, bool editable, Columns columns, float y)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var rowHeight = SegmentHeight * scale;
        DrawRowLabel(Loc.T(L.Grind.Afterwards), columns.Label, y, rowHeight);
        ImGui.SetCursorScreenPos(new Vector2(columns.Control, y));
        var selected = Math.Max(0, Array.IndexOf(afterOrder, cfg.AfterRun));
        if (Segmented.Draw("##fta_after_run", afterItems, ref selected, editable, SegmentHeight, columns.SegmentedWidth) & editable)
        {
            cfg.AfterRun = afterOrder[selected];
            cfg.SaveDebounced();
        }
        y += rowHeight + 6f * scale;
        using (Fonts.PushCaption())
        {
            var detail = Loc.T(afterChoices[selected].Detail);
            var wrapWidth = columns.Right - columns.Control;
            TextDraw.Wrapped(detail, new Vector2(columns.Control, y), wrapWidth, Styling.TextMuted);
            return y + TextDraw.MeasureWrapped(detail, wrapWidth).Y;
        }
    }

    private static float LabelColumnWidth()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var textWidth = MathF.Max(TextDraw.Measure(Loc.T(L.Grind.HowLong)).X, MathF.Max(TextDraw.Measure(Loc.T(L.Grind.EachZone)).X, TextDraw.Measure(Loc.T(L.Grind.Afterwards)).X));
        return MathF.Max(LabelWidth * scale, textWidth + Gap * scale);
    }

    private static void DrawRowLabel(string label, float left, float y, float rowHeight)
    {
        var labelSize = TextDraw.Measure(label);
        TextDraw.At(label, new Vector2(left, y + (rowHeight - labelSize.Y) * 0.5f), Styling.TextSecondary);
    }

    private static Limit CurrentLimit(Configuration cfg)
    {
        if (cfg.StopAfterFatesEnabled)
        {
            return Limit.Fates;
        }
        if (cfg.StopAfterMinutesEnabled)
        {
            return Limit.Minutes;
        }
        if (cfg.StopAfterLevelsEnabled)
        {
            return Limit.Levels;
        }
        return Limit.GoalOnly;
    }

    private static void ApplyLimit(Configuration cfg, Limit limit)
    {
        cfg.StopAfterFatesEnabled = limit == Limit.Fates;
        cfg.StopAfterMinutesEnabled = limit == Limit.Minutes;
        cfg.StopAfterLevelsEnabled = limit == Limit.Levels;
        cfg.SaveDebounced();
    }
}
