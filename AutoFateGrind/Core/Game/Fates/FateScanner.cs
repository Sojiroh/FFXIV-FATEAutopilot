using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using AutoFateGrind.Core.Zones;
using AutoFateGrind.Windows;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using clib.Utils;

namespace AutoFateGrind.Core.Game.Fates;

internal static class FateMobScanner
{
    private const float PullPackMeters = 8f;

    private const float PullPackBonusMeters = 3f;

    private const float PullMaxVerticalMeters = 8f;

    public const int EnmityListCapacity = 32;

    private static readonly List<IBattleNpc> pullCandidates = new List<IBattleNpc>();

    private static readonly uint[] enmityIds = new uint[EnmityListCapacity];

    private static int enmityCount;

    public static FateMobSurvey Survey(uint fateId, Vector3 from)
    {
        var mobCount = 0;
        Vector3 nearestPosition = default;
        var nearestHitboxRadius = 0f;
        var nearestDistance = float.MaxValue;
        var nearestVerticalDelta = 0f;
        var objects = Svc.Objects;
        for (var i = 0; i < objects.Length; i++)
        {
            if (objects[i] is IBattleNpc battleNpc && IsLiveMobOfFate(battleNpc, fateId))
            {
                mobCount++;
                var distance = DistanceToHitbox(from, battleNpc);
                if (!(distance >= nearestDistance))
                {
                    nearestDistance = distance;
                    nearestHitboxRadius = battleNpc.HitboxRadius;
                    nearestPosition = battleNpc.Position;
                    nearestVerticalDelta = battleNpc.Position.Y - from.Y;
                }
            }
        }
        if (mobCount != 0)
        {
            return new FateMobSurvey(mobCount, nearestPosition, nearestHitboxRadius, nearestDistance, nearestVerticalDelta);
        }
        return FateMobSurvey.Empty;
    }

    public static bool TryGetTargetedMob(uint fateId, Vector3 from, out float distanceToHitbox)
    {
        var found = TryGetTarget(fateId, from, out var target);
        distanceToHitbox = (found ? target.DistanceToHitbox : float.MaxValue);
        return found;
    }

    public static bool IsTargetingMobOf(uint fateId) => Svc.Targets.Target is IBattleNpc npc && IsLiveMobOfFate(npc, fateId);

    public static bool TargetNearest(uint fateId, Vector3 from)
    {
        IBattleNpc? nearest = null;
        var nearestDistance = float.MaxValue;
        var objects = Svc.Objects;
        for (var i = 0; i < objects.Length; i++)
        {
            if (objects[i] is IBattleNpc candidate && IsLiveMobOfFate(candidate, fateId))
            {
                var distance = DistanceToHitbox(from, candidate);
                if (!(distance >= nearestDistance))
                {
                    nearestDistance = distance;
                    nearest = candidate;
                }
            }
        }
        if (nearest == null)
        {
            return false;
        }
        Svc.Targets.Target = nearest;
        return true;
    }

    public static bool TryGetTarget(uint fateId, Vector3 from, out FateMobTarget target)
    {
        target = default;
        if (!(Svc.Targets.Target is IBattleNpc battleNpc))
        {
            return false;
        }
        if (!IsLiveMobOfFate(battleNpc, fateId))
        {
            return false;
        }
        target = new FateMobTarget(battleNpc.GameObjectId, battleNpc.Position, battleNpc.HitboxRadius, DistanceToHitbox(from, battleNpc));
        return true;
    }

    public static void DropUnpulledTarget(uint fateId)
    {
        if (Svc.Targets.Target is not IBattleNpc battleNpc || !IsLiveMobOfFate(battleNpc, fateId))
        {
            return;
        }
        if ((battleNpc.StatusFlags & StatusFlags.InCombat) != 0)
        {
            return;
        }
        Svc.Targets.Target = null;
    }

    public static FatePullSurvey SurveyPull(uint fateId, Vector3 from, Vector3 ringCentre, float ringRadius, float maxMeters, IReadOnlyDictionary<ulong, long> skipped)
    {
        SnapshotEnmityList();
        var onPlayerCount = 0;
        pullCandidates.Clear();
        var objects = Svc.Objects;
        for (var i = 0; i < objects.Length; i++)
        {
            if (objects[i] is IBattleNpc { CurrentHp: not 0u } battleNpc)
            {
                if (IsOnPlayer(battleNpc.EntityId))
                {
                    onPlayerCount++;
                }
                else if (IsLiveMobOfFate(battleNpc, fateId) && !skipped.ContainsKey(battleNpc.GameObjectId) && !(battleNpc.TargetObject is IPlayerCharacter) && !(DistanceToHitbox(from, battleNpc) > maxMeters) && !(MathF.Abs(battleNpc.Position.Y - from.Y) > PullMaxVerticalMeters) && (!(ringRadius > 0f) || !(FlatDistance(battleNpc.Position, ringCentre) > ringRadius)))
                {
                    pullCandidates.Add(battleNpc);
                }
            }
        }
        if (pullCandidates.Count == 0)
        {
            return new FatePullSurvey(onPlayerCount, 0, default);
        }
        var bestIndex = 0;
        var bestScore = float.MaxValue;
        var distanceToHitbox = 0f;
        for (var j = 0; j < pullCandidates.Count; j++)
        {
            var candidate = pullCandidates[j];
            var distance = DistanceToHitbox(from, candidate);
            var score = distance - PullPackBonusMeters * (float)PackNeighbours(candidate, j);
            if (!(score >= bestScore))
            {
                bestScore = score;
                distanceToHitbox = distance;
                bestIndex = j;
            }
        }
        var best = pullCandidates[bestIndex];
        var next = new FateMobTarget(best.GameObjectId, best.Position, best.HitboxRadius, distanceToHitbox);
        return new FatePullSurvey(onPlayerCount, pullCandidates.Count, next);
    }

    public static FatePullTagState PullTagState(uint fateId, ulong gameObjectId)
    {
        if (!(Svc.Objects.SearchById(gameObjectId) is IBattleNpc battleNpc) || !IsLiveMobOfFate(battleNpc, fateId))
        {
            return FatePullTagState.Gone;
        }
        SnapshotEnmityList();
        if (IsOnPlayer(battleNpc.EntityId))
        {
            return FatePullTagState.OnPlayer;
        }
        if (!(battleNpc.TargetObject is IPlayerCharacter))
        {
            return FatePullTagState.Waiting;
        }
        return FatePullTagState.Gone;
    }

    public static bool TryGetPullTarget(uint fateId, ulong gameObjectId, out IBattleNpc npc)
    {
        npc = null!;
        if (!(Svc.Objects.SearchById(gameObjectId) is IBattleNpc battleNpc) || !IsLiveMobOfFate(battleNpc, fateId))
        {
            return false;
        }
        npc = battleNpc;
        return true;
    }

    private static int PackNeighbours(IBattleNpc candidate, int candidateIndex)
    {
        var maxDistanceSquared = PullPackMeters * PullPackMeters;
        var neighbours = 0;
        for (var i = 0; i < pullCandidates.Count; i++)
        {
            if (i != candidateIndex && Vector3.DistanceSquared(candidate.Position, pullCandidates[i].Position) <= maxDistanceSquared)
            {
                neighbours++;
            }
        }
        return neighbours;
    }

    private unsafe static void SnapshotEnmityList()
    {
        var hater = &UIState.Instance()->Hater;
        enmityCount = Math.Clamp(hater->HaterCount, 0, EnmityListCapacity);
        for (var i = 0; i < enmityCount; i++)
        {
            enmityIds[i] = hater->Haters[i].EntityId;
        }
    }

    private static bool IsOnPlayer(uint entityId) => Array.IndexOf(enmityIds, entityId, 0, enmityCount) >= 0;

    private static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.X, a.Z), new Vector2(b.X, b.Z));

    public static bool HasPickupWithin(uint fateId, Vector3 from, float maxMeters)
    {
        var maxDistanceSquared = maxMeters * maxMeters;
        var objects = Svc.Objects;
        for (var i = 0; i < objects.Length; i++)
        {
            IGameObject gameObject = objects[i]!;
            if (IsPickupOfFate(gameObject, fateId) && Vector3.DistanceSquared(from, gameObject.Position) <= maxDistanceSquared)
            {
                return true;
            }
        }
        return false;
    }

    public static bool TryFindNearestPickup(uint fateId, Vector3 from, out IGameObject pickup, out float distanceToHitbox)
    {
        pickup = null!;
        distanceToHitbox = float.MaxValue;
        var objects = Svc.Objects;
        for (var i = 0; i < objects.Length; i++)
        {
            IGameObject gameObject = objects[i]!;
            if (IsPickupOfFate(gameObject, fateId))
            {
                var distance = FlatDistanceToHitbox(from, gameObject);
                if (!(distance >= distanceToHitbox))
                {
                    distanceToHitbox = distance;
                    pickup = gameObject;
                }
            }
        }
        return distanceToHitbox < float.MaxValue;
    }

    public static bool TryGetPickup(uint fateId, ulong gameObjectId, out IGameObject pickup)
    {
        var candidate = Svc.Objects.SearchById(gameObjectId);
        pickup = candidate!;
        return IsPickupOfFate(candidate, fateId);
    }

    private unsafe static bool IsPickupOfFate(IGameObject? candidate, uint fateId) => candidate != null && candidate.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj && candidate.IsTargetable && ((GameObject*)candidate.Address)->FateId == fateId;

    private static float FlatDistanceToHitbox(Vector3 from, IGameObject target) => MathF.Max(0f, Vector2.Distance(new Vector2(from.X, from.Z), new Vector2(target.Position.X, target.Position.Z)) - target.HitboxRadius);

    private unsafe static bool IsLiveMobOfFate(IBattleNpc npc, uint fateId)
    {
        if (!npc.IsTargetable)
        {
            return false;
        }
        if (npc.CurrentHp == 0)
        {
            return false;
        }
        var gameObject = (GameObject*)npc.Address;
        if (gameObject->FateId != fateId)
        {
            return false;
        }
        if (gameObject->BattleNpcSubKind != FFXIVClientStructs.FFXIV.Client.Game.Object.BattleNpcSubKind.Combatant)
        {
            return false;
        }
        return ActionManager.ClassifyTarget((Character*)npc.Address) == ActionManager.TargetCategory.Enemy;
    }

    private static float DistanceToHitbox(Vector3 from, IBattleNpc npc) => MathF.Max(0f, Vector3.Distance(from, npc.Position) - npc.HitboxRadius);
}

internal static class FateNameFormatter
{
    private enum Token : byte
    {
        Literal,
        Level,
        Name,
        Id,
        Progress,
        TimeRemaining,
        Distance,
        State
    }

    private readonly record struct Segment(Token Token, string Literal);

    public const string DefaultFormat = "L{Level}   {Name}";

    public const int MaxFormatLength = 64;

    private static readonly (string Name, Token Token)[] TokenNames = new (string, Token)[]
    {
        ("Level", Token.Level),
        ("Name", Token.Name),
        ("Id", Token.Id),
        ("Progress", Token.Progress),
        ("TimeRemaining", Token.TimeRemaining),
        ("Distance", Token.Distance),
        ("State", Token.State)
    };

    private static readonly List<Segment> segments = new List<Segment>();

    private static readonly StringBuilder builder = new StringBuilder();

    private static string parsedFormat = string.Empty;

    public static string Format(PublicEvent fate)
    {
        var fateFormat = Plugin.Cfg.FateNameFormat;
        if (string.IsNullOrWhiteSpace(fateFormat))
        {
            fateFormat = DefaultFormat;
        }
        if (!string.Equals(fateFormat, parsedFormat, StringComparison.Ordinal))
        {
            Parse(fateFormat);
        }
        builder.Clear();
        for (var i = 0; i < segments.Count; i++)
        {
            Append(segments[i], fate);
        }
        return builder.ToString();
    }

    private static void Append(Segment segment, PublicEvent fate)
    {
        switch (segment.Token)
        {
            case Token.Level:
                builder.Append(fate.Level);
                break;
            case Token.Name:
                builder.Append(fate.Name);
                break;
            case Token.Id:
                builder.Append(fate.Id);
                break;
            case Token.Progress:
                builder.Append(fate.Progress).Append('%');
                break;
            case Token.TimeRemaining:
                builder.Append(Formatting.Time(FateClock.Remaining(fate)));
                break;
            case Token.Distance:
                AppendDistance(fate);
                break;
            case Token.State:
                builder.Append(fate.State);
                break;
            default:
                builder.Append(segment.Literal);
                break;
        }
    }

    private static void AppendDistance(PublicEvent fate)
    {
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            builder.Append('?');
        }
        else
        {
            builder.Append((int)MathF.Round(Vector3.Distance(localPlayer.Position, fate.Position))).Append('y');
        }
    }

    private static void Parse(string format)
    {
        segments.Clear();
        parsedFormat = format;
        var literalStart = 0;
        var cursor = 0;
        while (cursor < format.Length)
        {
            if (format[cursor] != '{')
            {
                cursor++;
                continue;
            }
            var closeIndex = format.IndexOf('}', cursor + 1);
            if (closeIndex < 0)
            {
                break;
            }
            var token = ResolveToken(format.AsSpan(cursor + 1, closeIndex - cursor - 1));
            if (token == Token.Literal)
            {
                cursor = closeIndex + 1;
                continue;
            }
            if (cursor > literalStart)
            {
                segments.Add(new Segment(Token.Literal, format[literalStart..cursor]));
            }
            segments.Add(new Segment(token, string.Empty));
            cursor = closeIndex + 1;
            literalStart = cursor;
        }
        if (literalStart < format.Length)
        {
            segments.Add(new Segment(Token.Literal, format.Substring(literalStart)));
        }
    }

    private static Token ResolveToken(ReadOnlySpan<char> name)
    {
        for (var i = 0; i < TokenNames.Length; i++)
        {
            if (MemoryExtensions.Equals(name, TokenNames[i].Name, StringComparison.OrdinalIgnoreCase))
            {
                return TokenNames[i].Token;
            }
        }
        return Token.Literal;
    }
}

internal static class FateScanner
{
    private const int UrgentTimeThresholdSec = 240;

    private const uint TwistOfFateStatusId = 1288u;

    private const int MaxEquivalentPicks = 3;

    private const float EquivalentDistanceRatio = 1.2f;

    private const float EquivalentDistanceSlackMeters = 30f;

    private const int EquivalentProgressPoints = 20;

    public const uint NoMotivationNpcId = 3758096384u;

    public static bool AwaitsNpcStart(PublicEvent f) => f.State == FateState.Preparing && f.MotivationNpcId != NoMotivationNpcId;

    public static PublicEvent? PickNext(Configuration cfg, Vector3 playerPos, IReadOnlySet<uint>? sessionBlacklist = null, uint? forcedReturnId = null)
    {
        IEnumerable<PublicEvent>? fates = PublicEvent.Fates;
        if (fates == null)
        {
            return null;
        }
        if (forcedReturnId is { } returnId)
        {
            PublicEvent? returnFate = PublicEvent.GetFateById(returnId);
            if (returnFate != null && returnFate.Progress < 100 && !IsExcluded(returnFate, cfg, sessionBlacklist))
            {
                return returnFate;
            }
        }
        return ApplySort(fates.Where(f => IsEligible(f, cfg, sessionBlacklist)), cfg.FateSortOrder, playerPos).FirstOrDefault();
    }

    public static void CollectEquivalentPicks(Configuration cfg, Vector3 playerPos, IReadOnlySet<uint>? sessionBlacklist, List<PublicEvent> picks)
    {
        picks.Clear();
        IEnumerable<PublicEvent>? fates = PublicEvent.Fates;
        if (fates == null)
        {
            return;
        }
        var order = EffectiveOrder(cfg.FateSortOrder);
        var sorted = ApplySort(fates.Where(f => IsEligible(f, cfg, sessionBlacklist)), order, playerPos).ToList();
        if (sorted.Count == 0)
        {
            return;
        }
        var best = sorted[0];
        picks.Add(best);
        var leadingCount = IndexOfCriterion(order, FateSortCriterion.Distance);
        if (leadingCount < 0)
        {
            return;
        }
        var maxDistance = Vector3.Distance(best.Position, playerPos) * EquivalentDistanceRatio + EquivalentDistanceSlackMeters;
        for (var i = 1; i < sorted.Count && picks.Count < MaxEquivalentPicks; i++)
        {
            var candidate = sorted[i];
            if (!(Vector3.Distance(candidate.Position, playerPos) > maxDistance) && RanksAlike(best, candidate, order, leadingCount, playerPos))
            {
                picks.Add(candidate);
            }
        }
    }

    private static bool RanksAlike(PublicEvent best, PublicEvent candidate, IReadOnlyList<FateSortEntry> order, int leadingCount, Vector3 playerPos)
    {
        for (var i = 0; i < leadingCount; i++)
        {
            var criterion = order[i].Criterion;
            if (criterion == FateSortCriterion.Progress)
            {
                if (Math.Abs(best.Progress - candidate.Progress) > EquivalentProgressPoints)
                {
                    return false;
                }
                continue;
            }
            var keySelector = KeyFor(criterion, playerPos);
            if (keySelector(best).CompareTo(keySelector(candidate)) != 0)
            {
                return false;
            }
        }
        return true;
    }

    private static int IndexOfCriterion(IReadOnlyList<FateSortEntry> order, FateSortCriterion criterion)
    {
        for (var i = 0; i < order.Count; i++)
        {
            if (order[i].Criterion == criterion)
            {
                return i;
            }
        }
        return -1;
    }

    private static IReadOnlyList<FateSortEntry> EffectiveOrder(IReadOnlyList<FateSortEntry> sortOrder)
    {
        if (sortOrder == null || sortOrder.Count <= 0)
        {
            return DefaultSortOrder;
        }
        return sortOrder;
    }

    public static bool IsEligible(PublicEvent f, Configuration cfg, IReadOnlySet<uint>? sessionBlacklist) => Ineligibility(f, cfg, sessionBlacklist) == FateExclusion.None;

    public static FateExclusion Ineligibility(PublicEvent f, Configuration cfg, IReadOnlySet<uint>? sessionBlacklist)
    {
        var awaitsNpcStart = AwaitsNpcStart(f);
        if (f.State != FateState.Running && !awaitsNpcStart)
        {
            return FateExclusion.NotStarted;
        }
        var exclusion = ExclusionFor(f, cfg, sessionBlacklist);
        if (exclusion != FateExclusion.None)
        {
            return exclusion;
        }
        if (!awaitsNpcStart && FateClock.Remaining(f) < (float)cfg.MinTimeRemainingSec)
        {
            return FateExclusion.TooLittleTime;
        }
        if (f.Progress >= 100)
        {
            return FateExclusion.Finished;
        }
        if (f.Progress > cfg.MaxProgressPct)
        {
            return FateExclusion.TooMuchProgress;
        }
        if (!FateClock.IsOnMap(f))
        {
            return FateExclusion.NotOnMap;
        }
        return FateExclusion.None;
    }

    public static bool IsExcluded(PublicEvent f, Configuration cfg, IReadOnlySet<uint>? sessionBlacklist) => ExclusionFor(f, cfg, sessionBlacklist) != FateExclusion.None;

    public static FateExclusion ExclusionFor(PublicEvent f, Configuration cfg, IReadOnlySet<uint>? sessionBlacklist)
    {
        if (FateBlacklist.Contains(cfg, f))
        {
            return FateExclusion.Blacklisted;
        }
        if (sessionBlacklist != null && sessionBlacklist.Contains(f.Id))
        {
            return FateExclusion.SessionStuck;
        }
        if (cfg.SkippedFateRules.Contains((int)f.Rule))
        {
            return FateExclusion.SkippedRule;
        }
        if (cfg.LevelRangeFilterEnabled && !ZoneSelection.GoalPlansZones(cfg) && !IsWithinLevelRange(f, cfg))
        {
            return FateExclusion.OutsideLevelBand;
        }
        if (cfg.LevelWindowEnabled && f.Level > 0 && (f.Level < cfg.MinFateLevel || f.Level > cfg.MaxFateLevel))
        {
            return FateExclusion.OutsideLevelWindow;
        }
        if (cfg.MaxDurationFilterEnabled && f.Duration > cfg.MaxFateDurationSec)
        {
            return FateExclusion.TooLong;
        }
        return FateExclusion.None;
    }

    public static string DescribeExclusion(PublicEvent f, FateExclusion exclusion, Configuration cfg)
    {
        return exclusion switch
        {
            FateExclusion.Blacklisted => "blacklisted",
            FateExclusion.SessionStuck => "skipped for this session",
            FateExclusion.SkippedRule => $"a skipped FATE type ({f.Rule})",
            FateExclusion.OutsideLevelBand => $"Lv {f.Level}, outside the Lv {LevelBandFloor(cfg)}-{LevelBandCeiling(cfg)} band",
            FateExclusion.OutsideLevelWindow => $"Lv {f.Level}, outside the Lv {cfg.MinFateLevel}-{cfg.MaxFateLevel} window",
            FateExclusion.TooLong => $"lasts {f.Duration / 60} min, over the {cfg.MaxFateDurationSec / 60} min limit",
            FateExclusion.NotStarted => "not started yet",
            FateExclusion.TooLittleTime => $"{FateClock.Remaining(f):F0}s left, under the {cfg.MinTimeRemainingSec}s minimum",
            FateExclusion.TooMuchProgress => $"{f.Progress}% done, over the {cfg.MaxProgressPct}% limit",
            FateExclusion.Finished => "already finished",
            FateExclusion.NotOnMap => "not on the map yet",
            _ => "eligible",
        };
    }

    private static bool IsWithinLevelRange(PublicEvent f, Configuration cfg)
    {
        var playerLevel = Svc.PlayerState.Level;
        if (playerLevel <= 0 || f.Level <= 0)
        {
            return true;
        }
        return f.Level >= playerLevel - cfg.MaxLevelBelow && f.Level <= playerLevel + cfg.MaxLevelAbove;
    }

    private static int LevelBandFloor(Configuration cfg) => Math.Max(1, Svc.PlayerState.Level - cfg.MaxLevelBelow);

    private static int LevelBandCeiling(Configuration cfg) => Svc.PlayerState.Level + cfg.MaxLevelAbove;

    public static IOrderedEnumerable<PublicEvent> ApplySort(IEnumerable<PublicEvent> source, IReadOnlyList<FateSortEntry> sortOrder, Vector3 playerPos)
    {
        var effectiveOrder = EffectiveOrder(sortOrder);
        IOrderedEnumerable<PublicEvent>? ordered = null;
        foreach (var entry in effectiveOrder)
        {
            var keySelector = KeyFor(entry.Criterion, playerPos);
            ordered = ordered == null ? (entry.Descending ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector)) : (entry.Descending ? ordered.ThenByDescending(keySelector) : ordered.ThenBy(keySelector));
        }
        return ordered ?? source.OrderBy(_ => 0);
    }

    public static readonly IReadOnlyList<FateSortEntry> DefaultSortOrder =
    [
        new FateSortEntry
        {
            Criterion = FateSortCriterion.HasBonusWithTwist,
            Descending = true
        },
        new FateSortEntry
        {
            Criterion = FateSortCriterion.Progress,
            Descending = true
        },
        new FateSortEntry
        {
            Criterion = FateSortCriterion.HasBonus,
            Descending = true
        },
        new FateSortEntry
        {
            Criterion = FateSortCriterion.TimeRemainingUrgent,
            Descending = true
        },
        new FateSortEntry
        {
            Criterion = FateSortCriterion.Distance,
            Descending = false
        },
        new FateSortEntry
        {
            Criterion = FateSortCriterion.TimeRemaining,
            Descending = false
        }
    ];

    private static Func<PublicEvent, IComparable> KeyFor(FateSortCriterion c, Vector3 playerPos)
    {
        return c switch
        {
            FateSortCriterion.HasBonusWithTwist => (Func<PublicEvent, IComparable>)((PublicEvent f) => f.HasBonus && !PlayerHasTwistOfFate()),
            FateSortCriterion.Progress => (PublicEvent f) => f.Progress,
            FateSortCriterion.HasBonus => (PublicEvent f) => f.HasBonus,
            FateSortCriterion.TimeRemainingUrgent => (PublicEvent f) => IsUrgent(f),
            FateSortCriterion.Distance => (PublicEvent f) => Vector3.DistanceSquared(f.Position, playerPos),
            FateSortCriterion.TimeRemaining => (PublicEvent f) => IsUrgent(f) ? FateClock.Remaining(f) : UrgentTimeThresholdSec,
            FateSortCriterion.Level => (PublicEvent f) => f.Level,
            FateSortCriterion.Name => (PublicEvent f) => f.Name ?? string.Empty,
            _ => (PublicEvent _) => 0,
        };
    }

    private static bool IsUrgent(PublicEvent f)
    {
        if (!AwaitsNpcStart(f))
        {
            var remaining = FateClock.Remaining(f);
            return remaining >= 0f && remaining < UrgentTimeThresholdSec;
        }
        return false;
    }

    public static bool PlayerHasTwistOfFate()
    {
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return false;
        }
        foreach (var status in localPlayer.StatusList)
        {
            if (status.StatusId == TwistOfFateStatusId)
            {
                return true;
            }
        }
        return false;
    }
}
