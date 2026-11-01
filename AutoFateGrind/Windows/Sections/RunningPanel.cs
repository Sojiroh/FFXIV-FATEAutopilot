using System;
using System.Linq;
using System.Numerics;
using AutoFateGrind.Core.Game.Fates;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Tasks;
using AutoFateGrind.Core.Zones;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using clib.Utils;

namespace AutoFateGrind.Windows.Sections;

internal static class RunningPanel
{
    private const float PadX = 18f;

    private const int QueueLength = 5;

    public static void Draw(Configuration cfg, AutoFateController controller)
    {
        var paused = controller.Paused;
        var currentFate = PublicEvent.CurrentFate;
        var inFate = currentFate != null && currentFate.State == FateState.Running && !paused;
        var (accent, accentSoft, label) = PhasePalette(controller, inFate);
        DrawHeaderStrip(controller, accent, accentSoft, paused);
        Styling.VSpace(6f);
        DrawHeroCard(cfg, controller, currentFate, inFate, accent, accentSoft, label);
        Styling.VSpace(10f);
        DrawStatTiles(cfg, controller);
        Styling.VSpace(10f);
        DrawQueue(cfg);
    }

    private static void DrawHeaderStrip(AutoFateController controller, Vector4 accent, Vector4 accentSoft, bool paused)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var drawList = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var lineHeight = ImGui.GetTextLineHeight();
        var midY = origin.Y + lineHeight * 0.5f;
        var dotColor = paused ? accent : Styling.PulseColor(accent, accentSoft);
        var dotRadius = 4f * scale;
        Paint.Dot(drawList, new Vector2(origin.X + dotRadius + 3f * scale, midY), dotRadius, dotColor);
        var label = paused ? Loc.T(L.Shell.StatusPaused) : Loc.T(L.Shell.StatusRunning);
        var labelSize = TextDraw.SmallCapsSize(label);
        TextDraw.SmallCaps(label, new Vector2(origin.X + dotRadius * 2f + 12f * scale, midY - labelSize.Y * 0.5f), Styling.TextSecondary);
        var current = Svc.ClientState.TerritoryType;
        var currentZone = ZoneRegistry.Zones.FirstOrDefault(z => z.TerritoryId == current);
        var rotation = Loc.Plural(L.Run.Rotation, controller.ActiveZoneCount, currentZone?.Name ?? Loc.T(L.Run.SomewhereElse));
        using (Fonts.PushCaption())
        {
            var rotationSize = TextDraw.Measure(rotation);
            TextDraw.At(rotation, new Vector2(origin.X + width - rotationSize.X, midY - rotationSize.Y * 0.5f), Styling.TextMuted);
        }
        ImGui.Dummy(new Vector2(width, lineHeight));
    }

    private static void DrawHeroCard(Configuration cfg, AutoFateController controller, PublicEvent? fate, bool inFate, Vector4 accent, Vector4 accentSoft, string label)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, 134f * scale);
        var origin = ImGui.GetCursorScreenPos();
        var max = origin + size;
        var drawList = ImGui.GetWindowDrawList();
        var active = controller.Running && !controller.Paused;
        var rounding = 12f * scale;
        Paint.Glass(drawList, origin, max, rounding, accent, active ? 0.1f : 0.03f, 0f, elevated: true);
        if (active)
        {
            var strokeColor = Styling.PulseColor(Styling.WithAlpha(accent, 0.5f), accentSoft, inFate ? 600.0 : 800.0);
            Paint.Stroke(drawList, origin, max, strokeColor, rounding, 1.6f);
        }
        GoalProgress.Info goal = GoalProgress.Resolve(cfg, controller.SessionSnapshot);
        var padX = PadX * scale;
        var ringRadius = size.Y * 0.5f - 18f * scale;
        var center = new Vector2(origin.X + padX + ringRadius, origin.Y + size.Y * 0.5f);
        DrawGoalRing(center, ringRadius, accent, active, goal);
        var textX = center.X + ringRadius + 20f * scale;
        var rightX = max.X - padX;
        var textWidth = rightX - textX;
        var y = origin.Y + 16f * scale;
        var chipHeight = DrawPhaseChip(textX, y, label, accent, accentSoft, inFate && (fate?.HasBonus ?? false));
        y += chipHeight + 10f * scale;
        if (inFate)
        {
            using (Fonts.PushHeadline())
            {
                var fateName = TextDraw.Truncate(FateNameFormatter.Format(fate!), textWidth);
                var fateNameSize = TextDraw.Measure(fateName);
                TextDraw.At(fateName, new Vector2(textX, y), Styling.TextStrong);
                y += fateNameSize.Y + 9f * scale;
            }
            var barHeight = 10f * scale;
            var fraction = Motion.Approach(Motion.Key("##fta_fate_progress"), (float)fate!.Progress / 100f, 10f);
            Paint.Bar(drawList, new Vector2(textX, y), textWidth, barHeight, fraction, accent);
            y += barHeight + 8f * scale;
            using (Fonts.PushCaption())
            {
                TextDraw.At(Loc.T(L.Run.FateProgress, fate.Progress, Formatting.Time(FateClock.Remaining(fate))), new Vector2(textX, y), Styling.TextDim);
                TextDraw.Right(goal.Remaining, rightX, y, Styling.WithAlpha(accentSoft, 0.9f));
            }
        }
        else
        {
            var status = TextDraw.Truncate(string.IsNullOrWhiteSpace(controller.Status) ? Loc.T(L.Common.Working) : controller.Status, textWidth);
            var statusSize = TextDraw.Measure(status);
            TextDraw.At(status, new Vector2(textX, y), Styling.TextSecondary);
            y += statusSize.Y + 12f * scale;
            var barHeight = 8f * scale;
            if (active)
            {
                Paint.IndeterminateBar(drawList, new Vector2(textX, y), textWidth, barHeight, accent);
            }
            else
            {
                Paint.Bar(drawList, new Vector2(textX, y), textWidth, barHeight, 0f, accent);
            }
            y += barHeight + 8f * scale;
            using (Fonts.PushCaption())
            {
                TextDraw.At(goal.Remaining, new Vector2(textX, y), Styling.WithAlpha(accentSoft, 0.9f));
            }
        }
        ImGui.Dummy(size);
    }

    private static void DrawGoalRing(Vector2 center, float radius, Vector4 accent, bool active, GoalProgress.Info info)
    {
        var thickness = 6f * ImGuiHelpers.GlobalScale;
        ProgressRing.Track(center, radius, thickness, Styling.WithAlpha(Styling.BorderDim, 0.7f));
        if (info.Endless)
        {
            if (active)
            {
                ProgressRing.Sweep(center, radius, thickness, accent, 3400.0, 1.8849558f, 1f);
            }
        }
        else
        {
            var fraction = Motion.Approach(Motion.Key("##fta_goal_ring"), info.Fraction.GetValueOrDefault(), 6f);
            ProgressRing.Fill(center, radius, thickness, fraction, accent);
        }
        ProgressRing.CenterValue(center, info.CenterBig, info.CenterSmall, Styling.TextStrong, Styling.TextDim);
    }

    private static float DrawPhaseChip(float x, float y, string text, Vector4 accent, Vector4 accentSoft, bool bonus)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var drawList = ImGui.GetWindowDrawList();
        var padX = 9f * scale;
        var padY = 3f * scale;
        using (Fonts.PushCaption())
        {
            var upper = TextDraw.Upper(text);
            var textSize = TextDraw.Measure(upper);
            var bonusWidth = bonus ? TextDraw.IconSize(FontAwesomeIcon.Star).X + 6f * scale : 0f;
            var chipMin = new Vector2(x, y);
            var chipMax = chipMin + new Vector2(padX * 2f + textSize.X + bonusWidth, textSize.Y + padY * 2f);
            Paint.Pill(drawList, chipMin, chipMax, Styling.WithAlpha(accent, 0.28f), Styling.WithAlpha(accent, 0.65f));
            TextDraw.At(upper, new Vector2(x + padX, y + padY), accentSoft);
            if (bonus)
            {
                var starSize = TextDraw.IconSize(FontAwesomeIcon.Star);
                TextDraw.Icon(FontAwesomeIcon.Star, new Vector2(x + padX + textSize.X + 6f * scale, y + padY + (textSize.Y - starSize.Y) * 0.5f), Styling.AccentAmber);
            }
            return chipMax.Y - chipMin.Y;
        }
    }

    private static (Vector4 Accent, Vector4 AccentSoft, string Label) PhasePalette(AutoFateController controller, bool inFate)
    {
        if (!controller.Running)
        {
            return (Accent: Styling.TextDim, AccentSoft: Styling.TextSecondary, Label: Loc.T(L.Run.PhaseReady));
        }
        if (controller.Paused)
        {
            if (controller.PauseReason != PauseReason.InContent)
            {
                return (Accent: Styling.AccentAmber, AccentSoft: Styling.AccentAmberSoft, Label: Loc.T(L.Run.PhasePaused));
            }
            return (Accent: Styling.AccentAmber, AccentSoft: Styling.AccentAmberSoft, Label: Loc.T(L.Run.PhasePausedInContent));
        }
        return controller.Phase switch
        {
            AutoPhase.Trading => (Accent: Styling.AccentAmber, AccentSoft: Styling.AccentAmberSoft, Label: Loc.T(L.Run.PhaseTrading)),
            AutoPhase.Repairing => (Accent: Styling.TextStrong, AccentSoft: Styling.TextSecondary, Label: Loc.T(L.Run.PhaseRepairing)),
            AutoPhase.Humanizing => (Accent: Styling.AccentMint, AccentSoft: Styling.AccentMintSoft, Label: Loc.T(L.Run.PhaseBreak)),
            AutoPhase.Finishing => (Accent: Styling.AccentMint, AccentSoft: Styling.AccentMintSoft, Label: Loc.T(L.Run.PhaseFinishing)),
            AutoPhase.Grinding => (Accent: Styling.AccentBlue, AccentSoft: Styling.AccentBlueSoft, Label: inFate ? Loc.T(L.Run.PhaseEngaging) : Loc.T(L.Run.PhaseGrinding)),
            _ => (Accent: Styling.TextDim, AccentSoft: Styling.TextSecondary, Label: Loc.T(L.Run.PhaseStandingBy)),
        };
    }

    private static void DrawStatTiles(Configuration cfg, AutoFateController controller)
    {
        var session = controller.SessionSnapshot;
        var scale = ImGuiHelpers.GlobalScale;
        var availWidth = ImGui.GetContentRegionAvail().X;
        var gap = 8f * scale;
        var width = (availWidth - gap * 3f) / 4f;
        var completed = session?.CompletedCount ?? 0;
        var fatesPerHour = session?.FatesPerHour ?? 0.0;
        var gems = session?.GemstonesEarned ?? 0;
        var hours = session?.Elapsed.TotalHours ?? 0.0;
        var gemsPerHour = hours > 0.0 ? (double)gems / hours : 0.0;
        GoalProgress.Info goal = GoalProgress.Resolve(cfg, session);
        var elapsed = session == null ? Formatting.Elapsed(TimeSpan.Zero) : Formatting.Elapsed(session.Elapsed);
        var remaining = goal.Endless ? null : goal.Remaining;
        StatTile.Draw(Loc.T(L.Run.TileFates), completed.ToString(Loc.Culture), null, Styling.AccentBlue, width);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.Run.TileGems), gems.ToString(Loc.Culture), (gemsPerHour >= 1.0) ? Loc.T(L.Run.PerHour, gemsPerHour.ToString("F0", Loc.Culture)) : null, Styling.AccentAmber, width);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.Run.TileFatesPerHour), (fatesPerHour > 0.0) ? fatesPerHour.ToString("F1", Loc.Culture) : "—", null, Styling.AccentMint, width);
        ImGui.SameLine(0f, gap);
        StatTile.Draw(Loc.T(L.Run.TileElapsed), elapsed, remaining, Styling.AccentViolet, width);
    }

    private static void DrawQueue(Configuration cfg)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var label = Loc.T(L.Run.UpNext);
        var labelSize = TextDraw.SectionTitleSize(label);
        TextDraw.SectionTitle(label, origin, Styling.TextStrong);
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, labelSize.Y + 8f * scale));
        var player = Svc.Objects.LocalPlayer;
        if (player == null)
        {
            EmptyHint(Loc.T(L.Common.PlayerNotLoaded));
            return;
        }
        var current = PublicEvent.CurrentFate;
        PublicEvent[] fates = FateScanner.ApplySort(from f in PublicEvent.Fates ?? Enumerable.Empty<PublicEvent>()
                                                    where current == null || f.Id != current.Id
                                                    where FateScanner.IsEligible(f, cfg, null)
                                                    select f, cfg.FateSortOrder, player.Position).Take(QueueLength).ToArray();
        if (fates.Length == 0)
        {
            EmptyHint(Loc.T(L.Run.NoOtherFates));
            return;
        }
        for (int i = 0; i < fates.Length; i++)
        {
            DrawQueueRow(fates[i], player.Position, i == 0);
        }
    }

    private static void DrawQueueRow(PublicEvent fate, Vector3 playerPos, bool emphasize)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, 54f * scale);
        var origin = ImGui.GetCursorScreenPos();
        var max = origin + size;
        var drawList = ImGui.GetWindowDrawList();
        var accent = fate.HasBonus ? Styling.AccentAmber : Styling.AccentViolet;
        Paint.Glass(drawList, origin, max, 10f * scale, accent, emphasize ? 0.1f : 0.03f);
        var padX = 13f * scale;
        var top = origin.Y + 9f * scale;
        var icon = (fate.HasBonus ? 61445 : 61671);
        var iconColor = fate.HasBonus ? Styling.AccentAmber : Styling.TextDim;
        var iconSize = TextDraw.IconSize((FontAwesomeIcon)icon);
        TextDraw.Icon((FontAwesomeIcon)icon, new Vector2(origin.X + padX, top + (ImGui.GetTextLineHeight() - iconSize.Y) * 0.5f), iconColor);
        var distance = (int)Math.Round(Vector3.Distance(playerPos, fate.Position));
        var meta = Loc.T(L.Run.QueueMeta, fate.Progress, Formatting.Time(FateClock.Remaining(fate)), distance);
        Vector2 metaSize;
        using (Fonts.PushCaption())
        {
            metaSize = TextDraw.Measure(meta);
            TextDraw.At(meta, new Vector2(max.X - padX - metaSize.X, top + (ImGui.GetTextLineHeight() - metaSize.Y) * 0.5f), Styling.TextDim);
        }
        var nameX = origin.X + padX + iconSize.X + 10f * scale;
        TextDraw.At(TextDraw.Truncate(FateNameFormatter.Format(fate), max.X - padX - metaSize.X - 12f * scale - nameX), new Vector2(nameX, top), emphasize ? Styling.TextStrong : Styling.TextSecondary);
        Paint.Bar(drawList, new Vector2(origin.X + padX, max.Y - 13f * scale), size.X - padX * 2f, 5f * scale, (float)fate.Progress / 100f, accent);
        ImGui.Dummy(size);
    }

    private static void EmptyHint(string text)
    {
        var origin = ImGui.GetCursorScreenPos();
        TextDraw.At(text, origin, Styling.TextMuted);
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight()));
    }
}
