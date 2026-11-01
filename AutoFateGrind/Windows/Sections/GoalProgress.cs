using System;
using AutoFateGrind.Core.Game.Items;
using AutoFateGrind.Core.Game.SharedFates;
using AutoFateGrind.Core.Game.Yokai;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Modes;
using AutoFateGrind.Core.Tasks;
using AutoFateGrind.Core.Trading;
using ECommons.DalamudServices;

namespace AutoFateGrind.Windows.Sections;

internal static class GoalProgress
{
    public readonly record struct Info(float? Fraction, string CenterBig, string CenterSmall, string Remaining, bool Endless);

    private const string Separator = "  ·  ";

    public static Info Resolve(Configuration cfg, AutoFateSession? session)
    {
        var completed = session?.CompletedCount ?? 0;
        var itemGoal = ItemGoalCatalog.Find(cfg.ActiveMode.Id);
        if (itemGoal != null)
        {
            return ResolveItems(cfg, itemGoal);
        }
        switch (cfg.ActiveMode.Id)
        {
            case MaxGemstonesMode.ModeId:
                {
                    var gems = session?.GemstoneCurrent ?? GemstoneCatalog.CurrentWalletCount();
                    var gemTarget = Math.Max(1, cfg.TargetGemstoneCount);
                    var gemsLeft = Math.Max(0, gemTarget - gems);
                    return new Info(Math.Clamp((float)gems / (float)gemTarget, 0f, 1f), gems.ToString(Loc.Culture), Loc.T(L.Run.GoalOf, gemTarget), (gemsLeft > 0) ? Loc.T(L.Run.GemsToGo, gemsLeft) : Loc.T(L.Run.TargetReached), Endless: false);
                }
            case PlainFatesMode.ModeId:
                return ResolveLimits(cfg, session, completed);
            case YokaiMedalsMode.ModeId:
                return ResolveYokai(cfg, session);
            case SharedFateRanksMode.ModeId:
                return ResolveSharedFates(cfg);
            default:
                return new Info(null, completed.ToString(Loc.Culture), Loc.T(L.Run.Done), Loc.T(L.Run.UntilYouStop), Endless: true);
        }
    }

    private static Info ResolveLimits(Configuration cfg, AutoFateSession? session, int completed)
    {
        if (cfg.StopAfterFatesEnabled)
        {
            var fateTarget = RunLimits.FateCap(cfg);
            var fatesLeft = Math.Max(0, fateTarget - completed);
            return new Info(Math.Clamp((float)completed / (float)fateTarget, 0f, 1f), completed.ToString(Loc.Culture), Loc.T(L.Run.GoalOf, fateTarget), (fatesLeft > 0) ? Loc.T(L.Run.FatesLeft, fatesLeft) : Loc.T(L.Run.TargetReached), Endless: false);
        }
        if (cfg.StopAfterMinutesEnabled)
        {
            var minuteTarget = RunLimits.MinuteCap(cfg);
            var elapsed = session?.Elapsed ?? TimeSpan.Zero;
            var timeLeft = TimeSpan.FromMinutes(minuteTarget) - elapsed;
            var remaining = timeLeft > TimeSpan.Zero
                ? (timeLeft.TotalHours >= 1.0 ? Loc.T(L.Run.HoursLeft, (int)timeLeft.TotalHours, timeLeft.Minutes) : Loc.T(L.Run.MinutesLeft, timeLeft.Minutes, timeLeft.Seconds))
                : Loc.T(L.Run.TimeReached);
            return new Info(Math.Clamp((float)(elapsed.TotalMinutes / (double)minuteTarget), 0f, 1f), Loc.T(L.Run.GoalMinutes, (int)elapsed.TotalMinutes), Loc.T(L.Run.GoalOfMinutes, minuteTarget), remaining, Endless: false);
        }
        if (cfg.StopAfterLevelsEnabled)
        {
            var levelTarget = RunLimits.LevelCap(cfg);
            var levelsGained = session?.LevelsGained ?? 0;
            var levelsLeft = Math.Max(0, levelTarget - levelsGained);
            return new Info(Math.Clamp((float)levelsGained / (float)levelTarget, 0f, 1f), levelsGained.ToString(Loc.Culture), Loc.T(L.Run.GoalOf, levelTarget), (levelsLeft > 0) ? Loc.T(L.Run.LevelsLeft, levelsLeft) : Loc.T(L.Run.TargetReached), Endless: false);
        }
        return new Info(null, completed.ToString(Loc.Culture), Loc.T(L.Run.Done), Loc.T(L.Run.UntilYouStop), Endless: true);
    }

    private static Info ResolveItems(Configuration cfg, ItemGoalDefinition goal)
    {
        var (have, need) = ItemGoalProgress.Totals(goal, cfg);
        var itemsLeft = Math.Max(0, need - have);
        return new Info((need > 0) ? Math.Clamp((float)have / (float)need, 0f, 1f) : 1f, have.ToString(Loc.Culture), Loc.T(L.Run.GoalOf, need), (itemsLeft > 0) ? Loc.Plural(L.Run.ItemsToGo, itemsLeft) : Loc.T(L.Run.ItemsCollected), Endless: false);
    }

    private static Info ResolveSharedFates(Configuration cfg)
    {
        var (maxed, ranked) = SharedFateProgress.CountMaxed(cfg.SelectedZones);
        var zonesLeft = Loc.Plural(L.Run.SharedZonesLeft, Math.Max(0, ranked - maxed));
        var territoryType = Svc.ClientState.TerritoryType;
        if (!SharedFateCatalog.HasRanks(territoryType))
        {
            return new Info((ranked > 0) ? ((float)maxed / (float)ranked) : 0f, maxed.ToString(Loc.Culture), Loc.T(L.Run.GoalOf, ranked), zonesLeft, Endless: false);
        }
        if (!SharedFateProgress.TryGet(territoryType, out var rank))
        {
            return new Info(0f, "?", Loc.T(L.Run.GoalOf, 66), Loc.T(L.Run.SharedSyncing) + Separator + zonesLeft, Endless: false);
        }
        var rankText = rank.IsMaxed ? Loc.T(L.Run.SharedMaxedHere) : Loc.T(L.Run.SharedRank, rank.Rank, rank.MaxRank, rank.RankProgress, rank.RankSize);
        return new Info(rank.Fraction, rank.Completed.ToString(Loc.Culture), Loc.T(L.Run.GoalOf, rank.Total), rankText + Separator + zonesLeft, Endless: false);
    }

    private static Info ResolveYokai(Configuration cfg, AutoFateSession? session)
    {
        var medalTarget = YokaiProgress.MedalTarget(cfg);
        var targetIndex = YokaiProgress.ResolveTargetIndex(cfg, session?.YokaiTargetMinionId ?? 0);
        if (targetIndex < 0)
        {
            return new Info(1f, medalTarget.ToString(Loc.Culture), Loc.T(L.Run.GoalOf, medalTarget), Loc.T(L.Run.TargetReached), Endless: false);
        }
        var medals = Math.Min(YokaiProgress.Statuses[targetIndex].Medals, medalTarget);
        return new Info(Math.Clamp((float)medals / (float)medalTarget, 0f, 1f), medals.ToString(Loc.Culture), Loc.T(L.Run.GoalOf, medalTarget), Loc.T(L.Run.YokaiToGo, YokaiProgress.MinionName(targetIndex), medalTarget - medals), Endless: false);
    }
}
