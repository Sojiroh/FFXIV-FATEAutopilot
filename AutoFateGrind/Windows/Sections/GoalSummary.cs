using System.Text;
using AutoFateGrind.Core.Game.Items;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Modes;

namespace AutoFateGrind.Windows.Sections;

internal static class GoalSummary
{
    private static readonly StringBuilder builder = new StringBuilder();

    private static readonly (AfterRunAction Action, LocString Token)[] afterTokens = new (AfterRunAction, LocString)[]
    {
        (AfterRunAction.StayLoggedIn, L.Grind.AfterStayToken),
        (AfterRunAction.ReturnToInn, L.Grind.AfterInnToken),
        (AfterRunAction.Logout, L.Grind.AfterLogoutToken),
        (AfterRunAction.CloseGame, L.Grind.AfterCloseToken)
    };

    public static string Sentence(Configuration cfg, int zoneCount)
    {
        return Loc.T(L.Grind.Summary, Verb(cfg), Loc.Plural(L.Grind.ZonesCount, zoneCount), Until(cfg), AfterToken(cfg));
    }

    public static string StartSub(Configuration cfg, int zoneCount)
    {
        return Loc.T(L.Grind.StartSub, Loc.Plural(L.Grind.ZonesCount, zoneCount), Loc.T(L.Grind.UntilShort, Until(cfg)));
    }

    public static string Until(Configuration cfg)
    {
        builder.Clear();
        var target = Target(cfg);
        if (target != null)
        {
            builder.Append(target);
        }
        if (cfg.StopAfterFatesEnabled)
        {
            Join(Loc.T(L.Grind.GoalFates, RunLimits.FateCap(cfg)));
        }
        if (cfg.StopAfterMinutesEnabled)
        {
            Join(Loc.T(L.Grind.GoalMinutes, RunLimits.MinuteCap(cfg)));
        }
        if (cfg.StopAfterLevelsEnabled)
        {
            Join(Loc.T(L.Grind.GoalLevels, RunLimits.LevelCap(cfg)));
        }
        if (cfg.FatesPerZoneEnabled)
        {
            Join(Loc.T(L.Grind.GoalPerZone, RunLimits.FatesPerZoneCap(cfg)));
        }
        if (builder.Length != 0)
        {
            return builder.ToString();
        }
        return Loc.T(L.Grind.GoalEndless);
    }

    public static bool HasTarget(Configuration cfg)
    {
        return cfg.ActiveMode.Id != PlainFatesMode.ModeId;
    }

    private static void Join(string part)
    {
        if (builder.Length > 0)
        {
            builder.Append(Loc.T(L.Grind.Or));
        }
        builder.Append(part);
    }

    private static string? Target(Configuration cfg)
    {
        var itemGoal = ItemGoalCatalog.Find(cfg.ActiveMode.Id);
        if (itemGoal != null)
        {
            return Loc.T(itemGoal.GoalToken);
        }
        return cfg.ActiveMode.Id switch
        {
            MaxGemstonesMode.ModeId => Loc.T(L.Grind.GoalGemstones, cfg.TargetGemstoneCount.ToString("N0", Loc.Culture)),
            SharedFateRanksMode.ModeId => Loc.T(L.Grind.GoalSharedFates),
            YokaiMedalsMode.ModeId => Loc.T(L.Grind.GoalYokai, cfg.TargetYokaiMedals),
            _ => null,
        };
    }

    private static string Verb(Configuration cfg)
    {
        var itemGoal = ItemGoalCatalog.Find(cfg.ActiveMode.Id);
        if (itemGoal != null)
        {
            return Loc.T(L.Grind.SummaryCollect, Loc.T(itemGoal.Name));
        }
        return cfg.ActiveMode.Id switch
        {
            MaxGemstonesMode.ModeId => Loc.T(L.Grind.SummaryGems),
            SharedFateRanksMode.ModeId => Loc.T(L.Grind.SummaryShared),
            YokaiMedalsMode.ModeId => Loc.T(L.Grind.SummaryYokai),
            _ => Loc.T(L.Grind.SummaryPlain),
        };
    }

    private static string AfterToken(Configuration cfg)
    {
        for (int i = 0; i < afterTokens.Length; i++)
        {
            if (afterTokens[i].Action == cfg.AfterRun)
            {
                return Loc.T(afterTokens[i].Token);
            }
        }
        return Loc.T(afterTokens[0].Token);
    }
}
