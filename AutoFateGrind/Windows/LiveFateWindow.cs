using System;
using System.Linq;
using System.Numerics;
using AutoFateGrind.Core.Game.Fates;
using AutoFateGrind.Core.Ipc;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Tasks;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using clib.Utils;

namespace AutoFateGrind.Windows;

public sealed class LiveFateWindow : Window, IDisposable
{
    private const float ContentWidth = 330f;

    private const float HeaderHeight = 30f;

    private const float RowHeight = 26f;

    private const int QueueLength = 3;

    private const int SkippedLength = 3;

    private readonly Plugin plugin;

    private IDisposable? chrome;

    private IDisposable? bodyFont;

    public LiveFateWindow(Plugin plugin)
        : base("Live FATEs###FateAutopilotLive", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(100f, 60f),
            MaximumSize = new Vector2(600f, 700f)
        };
        RespectCloseHotkey = true;
        AllowPinning = false;
        AllowClickthrough = false;
    }

    public void Dispose()
    {
    }

    public override void OnClose()
    {
        if (!Plugin.Cfg.ShowLivePopout)
        {
            return;
        }
        Plugin.Cfg.ShowLivePopout = false;
        Plugin.Cfg.Save();
    }

    public override void PreDraw()
    {
        bodyFont = Fonts.PushBody();
        chrome = Styling.PushChrome(new Vector2(14f, 12f));
    }

    public override void PostDraw()
    {
        chrome?.Dispose();
        chrome = null;
        bodyFont?.Dispose();
        bodyFont = null;
    }

    public override void Draw()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var width = ContentWidth * globalScale;
        var controller = plugin.Controller;
        PublicEvent? currentFate = PublicEvent.CurrentFate;
        var hasActiveFate = currentFate != null && currentFate.State == FateState.Running;
        DrawHeader(width, controller);
        Styling.VSpace(4f);
        if (hasActiveFate)
        {
            DrawActive(currentFate!, width);
        }
        else
        {
            DrawIdle(controller, width);
        }
        Paint.Divider();
        DrawQueue(width);
        Paint.Divider();
        DrawSession(controller, width);
    }

    private void DrawHeader(float width, AutoFateController controller)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var headerHeight = HeaderHeight * globalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var windowDrawList = ImGui.GetWindowDrawList();
        var buttonSize = 24f * globalScale;
        var centerY = cursorScreenPos.Y + headerHeight * 0.5f;
        var dotColor = (controller.Paused ? Styling.AccentAmber : (controller.Running ? Styling.AccentBlue : Styling.TextDim));
        var dotFill = (controller.Running && !controller.Paused) ? Styling.PulseColor(dotColor, Styling.AccentBlueSoft) : dotColor;
        Paint.Dot(windowDrawList, new Vector2(cursorScreenPos.X + 6f * globalScale, centerY), 3.5f * globalScale, dotFill);
        var label = Loc.T(L.Live.Title);
        var labelSize = TextDraw.SmallCapsSize(label);
        TextDraw.SmallCaps(label, new Vector2(cursorScreenPos.X + 18f * globalScale, centerY - labelSize.Y * 0.5f), Styling.TextSecondary);
        ImGui.SetCursorScreenPos(new Vector2(cursorScreenPos.X + width - buttonSize, centerY - buttonSize * 0.5f));
        if (IconButton.Draw(FontAwesomeIcon.Times, "##fta_live_close", buttonSize, tooltip: Loc.T(L.Live.Hide)))
        {
            IsOpen = false;
        }
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(new Vector2(width, headerHeight));
    }

    private static void DrawActive(PublicEvent fate, float width)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var windowDrawList = ImGui.GetWindowDrawList();
        var y = cursorScreenPos.Y;
        var blacklisted = FateBlacklist.Contains(Plugin.Cfg, fate);
        var label = Loc.T(blacklisted ? L.Live.Blacklisted : L.Live.Engaging);
        var labelSize = TextDraw.SmallCapsSize(label);
        TextDraw.SmallCaps(label, new Vector2(cursorScreenPos.X, y), blacklisted ? Styling.AccentRose : Styling.AccentBlueSoft);
        y += labelSize.Y + 4f * globalScale;
        var buttonSize = 22f * globalScale;
        var titleY = y;
        float titleHeight;
        using (Fonts.PushHeadline())
        {
            var starWidth = (fate.HasBonus ? (TextDraw.IconSize(FontAwesomeIcon.Star).X + 8f * globalScale) : 0f);
            var name = TextDraw.Truncate(FateNameFormatter.Format(fate), width - buttonSize - 8f * globalScale - starWidth);
            var nameSize = TextDraw.Measure(name);
            TextDraw.At(name, new Vector2(cursorScreenPos.X, y), Styling.TextStrong);
            if (fate.HasBonus)
            {
                var starSize = TextDraw.IconSize(FontAwesomeIcon.Star);
                TextDraw.Icon(FontAwesomeIcon.Star, new Vector2(cursorScreenPos.X + nameSize.X + 8f * globalScale, y + (nameSize.Y - starSize.Y) * 0.5f), Styling.AccentAmber);
            }
            titleHeight = nameSize.Y;
            y += nameSize.Y + 8f * globalScale;
        }
        ImGui.SetCursorScreenPos(new Vector2(cursorScreenPos.X + width - buttonSize, titleY + (titleHeight - buttonSize) * 0.5f));
        ImGui.PushID((nint)fate.Id);
        if (IconButton.Draw(FontAwesomeIcon.Ban, "##ban_active", buttonSize, Styling.AccentRose, Loc.T(blacklisted ? L.Settings.RemoveFromBlacklist : L.Live.Ban)))
        {
            FateBlacklist.ToggleId(Plugin.Cfg, fate);
        }
        ImGui.PopID();
        var barHeight = 9f * globalScale;
        Paint.Bar(windowDrawList, new Vector2(cursorScreenPos.X, y), width, barHeight, (float)fate.Progress / 100f, Styling.AccentBlue);
        y += barHeight + 6f * globalScale;
        using (Fonts.PushCaption())
        {
            var progressText = Loc.T(L.Run.FateProgress, fate.Progress, Formatting.Time(FateClock.Remaining(fate)));
            TextDraw.At(progressText, new Vector2(cursorScreenPos.X, y), Styling.TextDim);
            y += TextDraw.Measure(progressText).Y;
        }
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(new Vector2(width, y - cursorScreenPos.Y));
    }

    private static void DrawIdle(AutoFateController controller, float width)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var label = controller.Paused ? Loc.T(L.Live.Paused) : (controller.Running ? Loc.T(L.Live.StandingBy) : Loc.T(L.Live.Ready));
        var color = (controller.Paused ? Styling.AccentAmberSoft : Styling.TextDim);
        var labelSize = TextDraw.SmallCapsSize(label);
        TextDraw.SmallCaps(label, cursorScreenPos, color);
        var status = TextDraw.Truncate(controller.Running ? controller.Status : Loc.T(L.Live.NoFate), width);
        var statusSize = TextDraw.Measure(status);
        TextDraw.At(status, new Vector2(cursorScreenPos.X, cursorScreenPos.Y + labelSize.Y + 4f * globalScale), Styling.TextSecondary);
        ImGui.Dummy(new Vector2(width, labelSize.Y + 4f * globalScale + statusSize.Y));
    }

    private void DrawQueue(float width)
    {
        var cfg = plugin.Configuration;
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            Hint(Loc.T(L.Common.PlayerNotLoaded), width);
            return;
        }
        PublicEvent? current = PublicEvent.CurrentFate;
        var others = (PublicEvent.Fates ?? Enumerable.Empty<PublicEvent>()).Where(f => current == null || f.Id != current.Id).ToList();
        var eligible = FateScanner.ApplySort(others.Where(f => FateScanner.IsEligible(f, cfg, null)), cfg.FateSortOrder, localPlayer.Position).Take(QueueLength).ToArray();
        var ineligible = others.Where(f => !FateScanner.IsEligible(f, cfg, null)).Take(SkippedLength).ToArray();
        if (eligible.Length == 0 && ineligible.Length == 0)
        {
            Hint(Loc.T(L.Live.NoOtherFates), width);
            return;
        }
        var idle = !plugin.Controller.Running;
        for (var i = 0; i < eligible.Length; i++)
        {
            DrawCompactRow(eligible[i], width, FateExclusion.None, idle);
        }
        for (var j = 0; j < ineligible.Length; j++)
        {
            DrawCompactRow(ineligible[j], width, FateScanner.Ineligibility(ineligible[j], cfg, null), idle);
        }
    }

    private static void DrawCompactRow(PublicEvent fate, float width, FateExclusion reason, bool idle)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var rowHeight = RowHeight * globalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var centerY = cursorScreenPos.Y + rowHeight * 0.5f;
        var buttonSize = 22f * globalScale;
        var excluded = reason != FateExclusion.None;
        ImGui.PushID((nint)fate.Id);
        var rowHit = Hit.Area("##row", new Vector2(width - buttonSize - 6f * globalScale, rowHeight), idle);
        ImGui.PopID();
        if (rowHit.Clicked)
        {
            WalkTo(fate);
        }
        var icon = (excluded ? FontAwesomeIcon.Minus : (fate.HasBonus ? FontAwesomeIcon.Star : FontAwesomeIcon.Bolt));
        var color = (excluded ? Styling.TextMuted : (fate.HasBonus ? Styling.AccentAmber : Styling.TextDim));
        var iconSize = TextDraw.IconSize(icon);
        TextDraw.Icon(icon, new Vector2(cursorScreenPos.X, centerY - iconSize.Y * 0.5f), color);
        ImGui.SetCursorScreenPos(new Vector2(cursorScreenPos.X + width - buttonSize, centerY - buttonSize * 0.5f));
        ImGui.PushID((nint)fate.Id);
        if (IconButton.Draw(FontAwesomeIcon.Ban, "##ban", buttonSize, Styling.AccentRose, Loc.T(L.Live.Ban)))
        {
            FateBlacklist.ToggleId(Plugin.Cfg, fate);
        }
        ImGui.PopID();
        Vector2 metaSize;
        using (Fonts.PushCaption())
        {
            var meta = Loc.T(L.Live.QueueMeta, fate.Progress, Formatting.Time(FateClock.Remaining(fate)));
            metaSize = TextDraw.Measure(meta);
            TextDraw.At(meta, new Vector2(cursorScreenPos.X + width - buttonSize - 8f * globalScale - metaSize.X, centerY - metaSize.Y * 0.5f), excluded ? Styling.TextMuted : Styling.TextDim);
        }
        var nameX = cursorScreenPos.X + iconSize.X + 8f * globalScale;
        var name = TextDraw.Truncate(FateNameFormatter.Format(fate), cursorScreenPos.X + width - buttonSize - 14f * globalScale - metaSize.X - nameX);
        var nameSize = TextDraw.Measure(name);
        TextDraw.At(name, new Vector2(nameX, centerY - nameSize.Y * 0.5f), excluded ? Styling.TextMuted : Styling.TextSecondary);
        if (rowHit.Hovered || (Hit.HoveringRect(cursorScreenPos, new Vector2(cursorScreenPos.X + width - buttonSize - 6f * globalScale, cursorScreenPos.Y + rowHeight)) && !idle))
        {
            Tooltip.Show(RowTooltip(reason, idle));
        }
        ImGui.SetCursorScreenPos(cursorScreenPos);
        ImGui.Dummy(new Vector2(width, rowHeight));
    }

    private static string RowTooltip(FateExclusion reason, bool idle)
    {
        var title = ((reason == FateExclusion.None) ? Loc.T(L.Live.EligibleTitle) : Loc.T(L.Live.ReasonTitle, ReasonText(reason)));
        if (!idle)
        {
            return title;
        }
        return title + "\n" + Loc.T(L.Live.WalkHint);
    }

    private static string ReasonText(FateExclusion reason)
    {
        return reason switch
        {
            FateExclusion.Blacklisted => Loc.T(L.Live.ReasonBlacklisted),
            FateExclusion.SessionStuck => Loc.T(L.Live.ReasonSessionStuck),
            FateExclusion.SkippedRule => Loc.T(L.Live.ReasonSkippedRule),
            FateExclusion.OutsideLevelBand => Loc.T(L.Live.ReasonLevelBand),
            FateExclusion.OutsideLevelWindow => Loc.T(L.Live.ReasonLevelWindow),
            FateExclusion.TooLong => Loc.T(L.Live.ReasonTooLong),
            FateExclusion.NotStarted => Loc.T(L.Live.ReasonNotStarted),
            FateExclusion.TooLittleTime => Loc.T(L.Live.ReasonTooLittleTime),
            FateExclusion.TooMuchProgress => Loc.T(L.Live.ReasonTooMuchProgress),
            FateExclusion.Finished => Loc.T(L.Live.ReasonFinished),
            FateExclusion.NotOnMap => Loc.T(L.Live.ReasonNotOnMap),
            _ => string.Empty,
        };
    }

    private static void WalkTo(PublicEvent fate)
    {
        var instance = NavmeshIPC.Instance;
        if (instance.IsRunning())
        {
            instance.Stop();
            return;
        }
        var destination = FateGround.Project(fate.Position) ?? fate.Position;
        instance.PathfindAndMoveTo(destination, Svc.Condition[ConditionFlag.InFlight]);
    }

    private void DrawSession(AutoFateController controller, float width)
    {
        AutoFateSession? sessionSnapshot = controller.SessionSnapshot;
        var summary = ((sessionSnapshot == null) ? Loc.T(L.Live.NoSession) : Loc.T(L.Live.Session, sessionSnapshot.CompletedCount, sessionSnapshot.GemstonesEarned, Formatting.Elapsed(sessionSnapshot.Elapsed)));
        var color = ((sessionSnapshot == null) ? Styling.TextMuted : Styling.TextDim);
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var summarySize = TextDraw.Measure(summary);
        var availableWidth = width;
        if (WalletChip.TryRead(out var count))
        {
            var chipWidth = WalletChip.Width(count);
            WalletChip.Draw(plugin.Configuration, count, cursorScreenPos.X + width, cursorScreenPos.Y + summarySize.Y * 0.5f);
            availableWidth -= chipWidth + 12f * globalScale;
        }
        TextDraw.At(TextDraw.Truncate(summary, availableWidth), cursorScreenPos, color);
        var height = summarySize.Y;
        if (sessionSnapshot == null)
        {
            ImGui.Dummy(new Vector2(width, height));
            return;
        }
        if (sessionSnapshot.ExpEarned > 0)
        {
            using (Fonts.PushCaption())
            {
                var expText = Loc.T(L.Live.Exp, Formatting.Exp(sessionSnapshot.ExpEarned), Formatting.Exp((long)sessionSnapshot.ExpPerHour));
                TextDraw.At(expText, new Vector2(cursorScreenPos.X, cursorScreenPos.Y + height + 2f * globalScale), Styling.TextMuted);
                height += 2f * globalScale + TextDraw.Measure(expText).Y;
            }
        }
        ImGui.Dummy(new Vector2(width, height));
    }

    private static void Hint(string text, float width)
    {
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        TextDraw.At(text, cursorScreenPos, Styling.TextMuted);
        ImGui.Dummy(new Vector2(width, TextDraw.Measure(text).Y));
    }
}
