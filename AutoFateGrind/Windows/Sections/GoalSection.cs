using System;
using System.Collections.Generic;
using System.Numerics;
using AutoFateGrind.Core.Game.Items;
using AutoFateGrind.Core.Game.SharedFates;
using AutoFateGrind.Core.Game.Yokai;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Modes;
using AutoFateGrind.Core.Tasks;
using AutoFateGrind.Core.Trading;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoFateGrind.Windows.Sections;

internal static class GoalSection
{
    private enum Category
    {
        Farm,
        Ranks,
        Relics,
        Events
    }

    private const float Gap = 8f;

    private const float CardMinWidth = 300f;

    private const float CardHeight = 60f;

    private const float PadX = 18f;

    private const float PadY = 14f;

    private const float ListSlide = 8f;

    private const float StepperGap = 10f;

    private const int GemstoneStep = 50;

    private const int MaxYokaiMedals = 99;

    private const string Separator = "  ·  ";

    private static readonly Segmented.Item[] categoryItems = new Segmented.Item[4];

    private static readonly List<IFateGrindMode> cardScratch = new List<IFateGrindMode>();

    private static int currentCategory = -1;

    public static void Draw(Configuration cfg, AutoFateController ctrl, int zoneCount)
    {
        DrawHeader(cfg, zoneCount);
        Styling.VSpace(10f);
        if (currentCategory < 0)
        {
            currentCategory = (int)CategoryOf(cfg.ActiveMode.Id);
        }
        RefreshCategoryItems();
        Segmented.Draw("##fta_goal_categories", categoryItems, ref currentCategory);
        Styling.VSpace(8f);
        using (Motion.PushSwitch("##fta_goal_cards", currentCategory, 240f, ListSlide))
        {
            DrawCards(cfg, ctrl, (Category)currentCategory);
        }
        Styling.VSpace(10f);
        DrawDetail(cfg, ctrl);
    }

    private static void DrawHeader(Configuration cfg, int zoneCount)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var rowHeight = 36f * scale;
        var midY = origin.Y + rowHeight * 0.5f;
        var label = Loc.T(L.Grind.GoalTitle);
        var titleSize = TextDraw.SectionTitleSize(label);
        TextDraw.SectionTitle(label, new Vector2(origin.X, midY - titleSize.Y * 0.5f), Styling.TextStrong);
        using (Fonts.PushCaption())
        {
            var sentence = TextDraw.Truncate(GoalSummary.Sentence(cfg, zoneCount), width - titleSize.X - 16f * scale);
            var sentenceSize = TextDraw.Measure(sentence);
            TextDraw.At(sentence, new Vector2(origin.X + width - sentenceSize.X, midY - sentenceSize.Y * 0.5f), Styling.TextDim);
        }
        ImGui.Dummy(new Vector2(width, rowHeight));
    }

    private static void RefreshCategoryItems()
    {
        categoryItems[0] = new Segmented.Item(FontAwesomeIcon.Gem, Loc.T(L.Grind.CatFarm));
        categoryItems[1] = new Segmented.Item(FontAwesomeIcon.Trophy, Loc.T(L.Grind.CatRanks));
        categoryItems[2] = new Segmented.Item(FontAwesomeIcon.Cubes, Loc.T(L.Grind.CatRelics));
        categoryItems[3] = new Segmented.Item(FontAwesomeIcon.Ghost, Loc.T(L.Grind.CatEvents));
    }

    private static void DrawCards(Configuration cfg, AutoFateController ctrl, Category category)
    {
        cardScratch.Clear();
        var modes = FateGrindModes.All;
        for (int i = 0; i < modes.Count; i++)
        {
            if (CategoryOf(modes[i].Id) == category)
            {
                cardScratch.Add(modes[i]);
            }
        }
        var scale = ImGuiHelpers.GlobalScale;
        var gap = Gap * scale;
        var availWidth = ImGui.GetContentRegionAvail().X;
        var columns = Math.Max(1, (int)MathF.Floor((availWidth + gap) / (CardMinWidth * scale + gap)));
        var width = (availWidth - gap * (float)(columns - 1)) / (float)columns;
        using var spacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(gap, gap));
        for (int j = 0; j < cardScratch.Count; j++)
        {
            if (j % columns != 0)
            {
                ImGui.SameLine(0f, gap);
            }
            DrawCard(cfg, ctrl, cardScratch[j], width);
        }
    }

    private static void DrawCard(Configuration cfg, AutoFateController ctrl, IFateGrindMode mode, float width)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(width, CardHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var max = origin + size;
        var selected = mode.Id == cfg.ActiveMode.Id;
        var editable = !ctrl.Running;
        ImGui.PushID(mode.Id);
        Hit.Result hit = Hit.Area("##goal", size, editable);
        var hover = Motion.Hover(Motion.Key("##goal"), hit.Hovered);
        var selectedAmount = Motion.Approach(Motion.Key("##goal", 1), selected ? 1f : 0f);
        ImGui.PopID();
        if (hit.Clicked && !selected)
        {
            cfg.ModeId = mode.Id;
            cfg.SaveDebounced();
        }
        var drawList = ImGui.GetWindowDrawList();
        Paint.Glass(drawList, origin, max, 10f * scale, Styling.AccentViolet, 0.02f + 0.16f * selectedAmount, hover);
        var midY = origin.Y + size.Y * 0.5f;
        var radius = 9f * scale;
        var center = new Vector2(origin.X + 14f * scale + radius, midY);
        DrawSelector(drawList, center, radius, selectedAmount);
        var lineHeight = ImGui.GetTextLineHeight();
        var icon = IconFor(mode.Id);
        var iconSize = TextDraw.IconSize(icon);
        var textX = center.X + radius + 12f * scale;
        var textY = origin.Y + 11f * scale;
        var rightX = max.X - 12f * scale;
        TextDraw.Icon(icon, new Vector2(textX, textY + (lineHeight - iconSize.Y) * 0.5f), selected ? Styling.AccentVioletSoft : Styling.TextDim);
        var nameX = textX + iconSize.X + 8f * scale;
        var color = Vector4.Lerp(Styling.TextSecondary, Styling.TextStrong, MathF.Max(selectedAmount, hover));
        TextDraw.At(TextDraw.Truncate(NameFor(mode), rightX - nameX), new Vector2(nameX, textY), color);
        var (status, statusColor) = StatusFor(cfg, mode);
        using (Fonts.PushCaption())
        {
            TextDraw.At(TextDraw.Truncate(status, rightX - textX), new Vector2(textX, textY + lineHeight + 3f * scale), statusColor);
        }
        if (!editable && Hit.HoveringRect(origin, max))
        {
            Tooltip.Show(Loc.T(L.Grind.PlanLocked));
        }
        else if (hit.Hovered)
        {
            Tooltip.Show(NoteFor(cfg, mode));
        }
    }

    private static void DrawSelector(ImDrawListPtr dl, Vector2 center, float radius, float active)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var color = Vector4.Lerp(Styling.WithAlpha(Styling.BorderDim, 0.9f), Styling.AccentVioletSoft, active);
        dl.AddCircle(center, radius, Paint.Col(color), 0, 1.4f * scale);
        if (active <= 0.01f)
        {
            return;
        }
        dl.AddCircleFilled(center, radius * active, Paint.Col(Styling.AccentViolet));
        if (active > 0.5f)
        {
            Paint.Check(dl, center, radius * 1.1f, Styling.WithAlpha(Styling.TextStrong, (active - 0.5f) * 2f), 1.8f * scale);
        }
    }

    private static void DrawDetail(Configuration cfg, AutoFateController ctrl)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var availWidth = ImGui.GetContentRegionAvail().X;
        var padX = PadX * scale;
        var padY = PadY * scale;
        var width = availWidth - padX * 2f;
        var editable = !ctrl.Running;
        var activeMode = cfg.ActiveMode;
        var drawList = ImGui.GetWindowDrawList();
        drawList.ChannelsSplit(2);
        drawList.ChannelsSetCurrent(1);
        var contentX = origin.X + padX;
        var contentY = origin.Y + padY;
        var label = NameFor(activeMode);
        var labelSize = TextDraw.SmallCapsSize(label);
        TextDraw.SmallCaps(label, new Vector2(contentX, contentY), Styling.TextSecondary);
        contentY += labelSize.Y + 10f * scale;
        ItemGoalDefinition? itemGoal = ItemGoalCatalog.Find(activeMode.Id);
        if (itemGoal != null)
        {
            contentY = DrawItemDetail(cfg, itemGoal, contentX, contentY, width, editable);
        }
        else
        {
            contentY = activeMode.Id switch
            {
                MaxGemstonesMode.ModeId => DrawGemstoneDetail(cfg, contentX, contentY, width, editable),
                SharedFateRanksMode.ModeId => DrawCaption(SharedFateNote(cfg), contentX, contentY, width, Styling.TextMuted),
                YokaiMedalsMode.ModeId => DrawYokaiDetail(cfg, contentX, contentY, width, editable),
                _ => DrawCaption(Loc.T(L.Grind.NotePlain), contentX, contentY, width, Styling.TextMuted),
            };
        }
        var max = new Vector2(origin.X + availWidth, contentY + padY);
        drawList.ChannelsSetCurrent(0);
        Paint.Glass(drawList, origin, max, 12f * scale, Styling.AccentViolet, 0.07f, 0f, elevated: true);
        drawList.ChannelsMerge();
        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(availWidth, max.Y - origin.Y));
    }

    private static float DrawGemstoneDetail(Configuration cfg, float x, float y, float width, bool editable)
    {
        var target = cfg.TargetGemstoneCount;
        y = DrawStepperRow("##fta_target_gems", Loc.T(L.Grind.StopAt), ref target, GemstoneStep, 1, 1500, Loc.T(L.Grind.UnitGemstones), x, y, editable);
        if (target != cfg.TargetGemstoneCount)
        {
            cfg.TargetGemstoneCount = target;
            cfg.SaveDebounced();
        }
        y += 8f * ImGuiHelpers.GlobalScale;
        return DrawCaption(Loc.T(L.Grind.NoteGemstones, GemstoneCatalog.CurrentWalletCount().ToString("N0", Loc.Culture)), x, y, width, Styling.TextMuted);
    }

    private static float DrawItemDetail(Configuration cfg, ItemGoalDefinition goal, float x, float y, float width, bool editable)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var count = ItemGoalProgress.WeaponCount(cfg);
        y = DrawStepperRow("##fta_relic_count", Loc.T(L.Grind.WeaponsToMake), ref count, 1, 1, ItemGoalCatalog.MaxWeaponCount, Loc.T(L.Grind.UnitWeapons), x, y, editable);
        if (count != cfg.TargetRelicCount)
        {
            cfg.TargetRelicCount = count;
            cfg.SaveDebounced();
        }
        y += 8f * scale;
        y = DrawCaption(Loc.T(goal.Note), x, y, width, Styling.TextMuted);
        string? gateNote = goal.Gate switch
        {
            ItemGoalGate.QuestAccepted => Loc.T(L.Grind.ItemGoalQuest, ItemGoalProgress.QuestName(goal.QuestId)),
            ItemGoalGate.ZenithEquipped => Loc.T(L.Grind.ItemGoalZenith),
            _ => null,
        };
        if (gateNote != null)
        {
            y += 4f * scale;
            y = DrawCaption(gateNote, x, y, width, ItemGoalProgress.IsAvailable(goal) ? Styling.TextMuted : Styling.AccentAmber);
        }
        var (have, need) = ItemGoalProgress.Totals(goal, cfg);
        y += 4f * scale;
        return DrawCaption(Loc.T(L.Grind.ItemsSummary, have, need), x, y, width, Styling.TextDim);
    }

    private static float DrawYokaiDetail(Configuration cfg, float x, float y, float width, bool editable)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var target = cfg.TargetYokaiMedals;
        y = DrawStepperRow("##fta_yokai_target", Loc.T(L.Grind.StopAt), ref target, 1, 1, MaxYokaiMedals, Loc.T(L.Grind.UnitYokaiMedals), x, y, editable);
        if (target != cfg.TargetYokaiMedals)
        {
            cfg.TargetYokaiMedals = target;
            cfg.SaveDebounced();
        }
        y += 8f * scale;
        y = DrawCaption(Loc.T(L.Grind.NoteYokai), x, y, width, Styling.TextMuted);
        var (owned, weaponsLeft) = YokaiProgress.Ownership();
        var watchMissing = !YokaiOps.OwnsWatch();
        var watchStatus = YokaiOps.IsWatchEquipped()
            ? Loc.T(L.Grind.YokaiEventWatchEquipped)
            : (watchMissing ? Loc.T(L.Grind.YokaiEventWatchMissing) : Loc.T(L.Grind.YokaiEventWatchStored));
        var summary = string.Concat(Loc.T(L.Grind.YokaiEventOwned, owned, YokaiCatalog.Entries.Length), Separator, Loc.Plural(L.Grind.YokaiEventWeaponsLeft, weaponsLeft), Separator, watchStatus);
        y += 4f * scale;
        return DrawCaption(summary, x, y, width, watchMissing ? Styling.AccentAmber : Styling.TextDim);
    }

    private static float DrawStepperRow(string id, string label, ref int value, int step, int min, int max, string unit, float x, float y, bool editable)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var gap = StepperGap * scale;
        var frameHeight = ImGui.GetFrameHeight();
        var labelSize = TextDraw.Measure(label);
        TextDraw.At(label, new Vector2(x, y + (frameHeight - labelSize.Y) * 0.5f), Styling.TextSecondary);
        var stepperX = x + labelSize.X + gap;
        ImGui.SetCursorScreenPos(new Vector2(stepperX, y));
        var stepperValue = value;
        if (Stepper.Draw(id, ref stepperValue, step, min, max, "%d") & editable)
        {
            value = Math.Clamp(stepperValue, min, max);
        }
        var unitSize = TextDraw.Measure(unit);
        TextDraw.At(unit, new Vector2(stepperX + 168f * scale + gap, y + (frameHeight - unitSize.Y) * 0.5f), Styling.TextDim);
        return y + frameHeight;
    }

    private static float DrawCaption(string text, float x, float y, float width, Vector4 color)
    {
        using (Fonts.PushCaption())
        {
            TextDraw.Wrapped(text, new Vector2(x, y), width, color);
            return y + TextDraw.MeasureWrapped(text, width).Y;
        }
    }

    private static string SharedFateNote(Configuration cfg)
    {
        var (maxed, ranked) = SharedFateProgress.CountMaxed(cfg.SelectedZones);
        var note = Loc.T(L.Grind.NoteSharedFates);
        if (ranked != 0)
        {
            return note + " " + Loc.T(L.Grind.NoteSharedFatesSummary, maxed, ranked);
        }
        return note;
    }

    private static Category CategoryOf(string modeId)
    {
        if (ItemGoalCatalog.IsItemGoal(modeId))
        {
            return Category.Relics;
        }
        return modeId switch
        {
            SharedFateRanksMode.ModeId => Category.Ranks,
            YokaiMedalsMode.ModeId => Category.Events,
            _ => Category.Farm,
        };
    }

    private static FontAwesomeIcon IconFor(string modeId)
    {
        return modeId switch
        {
            MaxGemstonesMode.ModeId => FontAwesomeIcon.Gem,
            PlainFatesMode.ModeId => FontAwesomeIcon.Bolt,
            SharedFateRanksMode.ModeId => FontAwesomeIcon.Trophy,
            ItemGoalCatalog.AtmaModeId => FontAwesomeIcon.Sun,
            ItemGoalCatalog.LuminousModeId => FontAwesomeIcon.Snowflake,
            ItemGoalCatalog.MemoriesModeId => FontAwesomeIcon.Book,
            ItemGoalCatalog.LawsOrderModeId => FontAwesomeIcon.Skull,
            ItemGoalCatalog.DemiatmaModeId => FontAwesomeIcon.Cubes,
            ItemGoalCatalog.PasteModeId => FontAwesomeIcon.Flask,
            YokaiMedalsMode.ModeId => FontAwesomeIcon.Ghost,
            _ => FontAwesomeIcon.Flag,
        };
    }

    private static string NameFor(IFateGrindMode mode)
    {
        ItemGoalDefinition? itemGoal = ItemGoalCatalog.Find(mode.Id);
        if (itemGoal != null)
        {
            return Loc.T(itemGoal.Name);
        }
        return mode.Id switch
        {
            MaxGemstonesMode.ModeId => Loc.T(L.Grind.CardGemstones),
            PlainFatesMode.ModeId => Loc.T(L.Grind.CardPlain),
            SharedFateRanksMode.ModeId => Loc.T(L.Grind.CardShared),
            YokaiMedalsMode.ModeId => Loc.T(L.Grind.CardYokai),
            _ => mode.DisplayName,
        };
    }

    private static string NoteFor(Configuration cfg, IFateGrindMode mode)
    {
        ItemGoalDefinition? itemGoal = ItemGoalCatalog.Find(mode.Id);
        if (itemGoal != null)
        {
            return Loc.T(itemGoal.Note);
        }
        return mode.Id switch
        {
            MaxGemstonesMode.ModeId => Loc.T(L.Grind.NoteGemstones, GemstoneCatalog.CurrentWalletCount().ToString("N0", Loc.Culture)),
            SharedFateRanksMode.ModeId => Loc.T(L.Grind.NoteSharedFates),
            YokaiMedalsMode.ModeId => Loc.T(L.Grind.NoteYokai),
            _ => Loc.T(L.Grind.NotePlain),
        };
    }

    private static (string Text, Vector4 Color) StatusFor(Configuration cfg, IFateGrindMode mode)
    {
        ItemGoalDefinition? itemGoal = ItemGoalCatalog.Find(mode.Id);
        if (itemGoal != null)
        {
            switch (ItemGoalProgress.Blocker(itemGoal, cfg))
            {
                case ItemGoalBlocker.NeedQuest:
                    return (Text: Loc.T(L.Grind.StatusQuestMissing), Color: Styling.AccentAmber);
                case ItemGoalBlocker.NeedZenith:
                    return (Text: Loc.T(L.Grind.StatusZenithMissing), Color: Styling.AccentAmber);
                default:
                    var (itemsHave, itemsNeed) = ItemGoalProgress.Totals(itemGoal, cfg);
                    return (Text: Loc.T(L.Grind.ItemsSummary, itemsHave, itemsNeed), Color: (itemsHave >= itemsNeed) ? Styling.AccentMint : Styling.TextDim);
            }
        }
        switch (mode.Id)
        {
            case MaxGemstonesMode.ModeId:
                {
                    var gems = GemstoneCatalog.CurrentWalletCount();
                    var gemTarget = Math.Max(1, cfg.TargetGemstoneCount);
                    return (Text: Loc.T(L.Grind.StatusGems, gems.ToString("N0", Loc.Culture), gemTarget.ToString("N0", Loc.Culture)), Color: (gems >= gemTarget) ? Styling.AccentMint : Styling.TextDim);
                }
            case SharedFateRanksMode.ModeId:
                {
                    var (ranksMaxed, ranksTotal) = SharedFateProgress.CountMaxed(cfg.SelectedZones);
                    if (ranksTotal == 0)
                    {
                        return (Text: Loc.T(L.Grind.StatusRanksNoZones), Color: Styling.AccentAmber);
                    }
                    return (Text: Loc.T(L.Grind.StatusRanks, ranksMaxed, ranksTotal), Color: (ranksMaxed >= ranksTotal) ? Styling.AccentMint : Styling.TextDim);
                }
            case YokaiMedalsMode.ModeId:
                {
                    if (!YokaiOps.OwnsWatch())
                    {
                        return (Text: Loc.T(L.Grind.YokaiEventWatchMissing), Color: Styling.AccentAmber);
                    }
                    var (medals, medalTarget) = YokaiProgress.Totals(cfg);
                    if (medalTarget == 0)
                    {
                        return (Text: Loc.T(L.Grind.ZonesYokaiNone), Color: Styling.AccentMint);
                    }
                    return (Text: Loc.T(L.Grind.YokaiSummary, medals, medalTarget), Color: (medals >= medalTarget) ? Styling.AccentMint : Styling.TextDim);
                }
            default:
                return (Text: Loc.T(L.Grind.StatusPlain), Color: Styling.TextDim);
        }
    }
}
