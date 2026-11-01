using System.Collections.Generic;
using AutoFateGrind.Core.Game.Items;
using AutoFateGrind.Core.Game.SharedFates;
using AutoFateGrind.Core.Game.Yokai;
using AutoFateGrind.Core.Trading;
using AutoFateGrind.Core.Zones;

namespace AutoFateGrind.Core.Modes;

public sealed class MaxGemstonesMode : IFateGrindMode
{
    public const string ModeId = "maxgemstones";

    public string Id => ModeId;

    public string DisplayName => "Farm Gemstones";

    public string Description => "Stops when Bicolor Gemstones hit your target. Auto-trade resumes the grind.";

    public bool IsComplete(ModeContext ctx) => GemstoneCatalog.CurrentWalletCount() >= Plugin.Cfg.TargetGemstoneCount;

    public string? GetRemainingDisplay(ModeContext ctx)
    {
        var walletCount = GemstoneCatalog.CurrentWalletCount();
        var targetGemstoneCount = Plugin.Cfg.TargetGemstoneCount;
        if (walletCount >= targetGemstoneCount)
        {
            return null;
        }
        return $"{walletCount} / {targetGemstoneCount} gems";
    }
}

public sealed class PlainFatesMode : IFateGrindMode
{
    public const string ModeId = "plainfates";

    public string Id => ModeId;

    public string DisplayName => "Just FATEs";

    public string Description => "Grinds your zones with no target of its own; a FATE or time limit, or Stop, ends the run.";

    public bool IsComplete(ModeContext ctx) => false;
}

public sealed class SharedFateRanksMode : IFateGrindMode
{
    public const string ModeId = "sharedfateranks";

    public string Id => ModeId;

    public string DisplayName => "Shared FATE Ranks";

    public string Description => "Grinds each selected zone until its Shared FATE rank is maxed, then moves on to the next one, and stops once every zone with ranks is maxed.";

    public bool IsComplete(ModeContext ctx) => SharedFateProgress.AllMaxed(ctx.Zones);

    public string? GetRemainingDisplay(ModeContext ctx)
    {
        var unmaxedCount = SharedFateProgress.CountUnmaxed(ctx.Zones);
        if (unmaxedCount <= 0)
        {
            return null;
        }
        return $"{unmaxedCount} zone{((unmaxedCount == 1) ? "" : "s")} left";
    }

    public bool AcceptsZone(uint territoryId) => SharedFateCatalog.HasRanks(territoryId);

    public bool IsZoneDone(uint territoryId) => SharedFateProgress.IsMaxed(territoryId);

    public string ZoneDoneReason(uint territoryId) => "Shared FATE rank maxed";
}

internal sealed class ItemGoalMode(ItemGoalDefinition definition) : IFateGrindMode
{
    public ItemGoalDefinition Definition => definition;

    public string Id => definition.ModeId;

    public string DisplayName => definition.DisplayName;

    public string Description => "Grinds the zones where the items still drop, moves on once a zone has nothing left to give, and stops when every item is collected.";

    public bool IsComplete(ModeContext ctx)
    {
        if (ItemGoalProgress.IsAvailable(definition))
        {
            return ItemGoalProgress.AllCollected(definition, Plugin.Cfg);
        }
        return true;
    }

    public string? GetRemainingDisplay(ModeContext ctx)
    {
        var itemsLeft = ItemGoalProgress.ItemsLeft(definition, Plugin.Cfg);
        if (itemsLeft <= 0)
        {
            return null;
        }
        return $"{itemsLeft} item{((itemsLeft == 1) ? "" : "s")} left";
    }

    public bool PlansZones => true;

    public IReadOnlyList<ZoneInfo> PlanZones(Configuration cfg) => ItemGoalProgress.PlanZones(definition, cfg);

    public bool AcceptsZone(uint territoryId) => ItemGoalProgress.DropsHere(definition, territoryId);

    public bool IsZoneDone(uint territoryId) => ItemGoalProgress.IsZoneDone(definition, territoryId, Plugin.Cfg);

    public string ZoneDoneReason(uint territoryId) => "every item that drops here is collected";
}

public sealed class YokaiMedalsMode : IFateGrindMode
{
    public const string ModeId = "yokaimedals";

    public string Id => ModeId;

    public string DisplayName => "Yo-kai Medals";

    public string Description => "Farms Legendary Medals for every Yo-kai minion you own, summoning each minion and travelling to its zones, and stops once all of them reach your target.";

    public bool IsComplete(ModeContext ctx) => YokaiProgress.IsComplete(Plugin.Cfg);

    public string? GetRemainingDisplay(ModeContext ctx)
    {
        var (medals, target) = YokaiProgress.Totals(Plugin.Cfg);
        if (medals >= target)
        {
            return null;
        }
        return $"{target - medals} medals left";
    }

    public bool PlansZones => true;

    public IReadOnlyList<ZoneInfo> PlanZones(Configuration cfg) => YokaiProgress.ZonesFor(YokaiProgress.ResolveTargetIndex(cfg, 0u));
}

public static class FateGrindModes
{
    public const string RetiredRunCountId = "runcount";

    public const string RetiredTimeBoxedId = "timeboxed";

    public const string RetiredEndlessId = "endless";

    private static readonly List<IFateGrindMode> registered = Build();

    private static List<IFateGrindMode> Build()
    {
        var definitions = ItemGoalCatalog.Definitions;
        var modes = new List<IFateGrindMode>(4 + definitions.Length)
        {
            new MaxGemstonesMode(),
            new PlainFatesMode(),
            new SharedFateRanksMode()
        };
        for (var i = 0; i < definitions.Length; i++)
        {
            modes.Add(new ItemGoalMode(definitions[i]));
        }
        modes.Add(new YokaiMedalsMode());
        return modes;
    }

    public static IReadOnlyList<IFateGrindMode> All => registered;

    public static IFateGrindMode Default => registered[0];

    public static IFateGrindMode? GetById(string? id)
    {
        if (id == null)
        {
            return null;
        }
        for (var i = 0; i < registered.Count; i++)
        {
            if (registered[i].Id == id)
            {
                return registered[i];
            }
        }
        return null;
    }

    public static string IdForLegacy(GrindMode legacy)
    {
        return legacy switch
        {
            GrindMode.MaxGemstones => MaxGemstonesMode.ModeId,
            GrindMode.RunCount => PlainFatesMode.ModeId,
            GrindMode.Endless => PlainFatesMode.ModeId,
            GrindMode.MaxFates => SharedFateRanksMode.ModeId,
            _ => MaxGemstonesMode.ModeId,
        };
    }
}
