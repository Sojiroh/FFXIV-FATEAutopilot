using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Zones;

internal static class CityAethernet
{
    private const float HopCostMeters = 100f;

    private static readonly Dictionary<uint, AethernetShard[]> shardsByTerritory = new Dictionary<uint, AethernetShard[]>();

    public static bool TryPlanHop(uint territoryId, Vector3 from, Vector3 to, out AethernetHop hop)
    {
        hop = default;
        var shards = ShardsIn(territoryId);
        if (shards.Length < 2)
        {
            return false;
        }
        var source = Nearest(shards, from);
        var destination = Nearest(shards, to);
        if (source.Id == destination.Id || source.Group != destination.Group)
        {
            return false;
        }
        if (!IsAttuned(source.Id) || !IsAttuned(destination.Id))
        {
            return false;
        }
        IGameObject? shardObject = FindShardObject(source.Id);
        if (shardObject == null)
        {
            return false;
        }
        source = source with
        {
            Position = shardObject.Position
        };
        var walkMeters = FlatDistance(from, to);
        var rideMeters = FlatDistance(from, source.Position) + HopCostMeters + FlatDistance(destination.Position, to);
        hop = new AethernetHop(source, destination, walkMeters, rideMeters);
        return true;
    }

    public static bool TryPlanEntry(uint fromTerritoryId, Vector3 from, uint toTerritoryId, Vector3 to, uint group, out AethernetHop hop)
    {
        hop = default;
        if (!TryFindNearestAttuned(ShardsIn(toTerritoryId), to, group, out var destination))
        {
            return false;
        }
        if (!TryFindNearestAttuned(ShardsIn(fromTerritoryId), from, group, out var source))
        {
            return false;
        }
        IGameObject? shardObject = FindShardObject(source.Id);
        if (shardObject == null)
        {
            return false;
        }
        source = source with
        {
            Position = shardObject.Position
        };
        var rideMeters = FlatDistance(from, source.Position) + HopCostMeters + FlatDistance(destination.Position, to);
        hop = new AethernetHop(source, destination, float.PositiveInfinity, rideMeters);
        return true;
    }

    public static bool HoldsShardOf(uint territoryId, uint group)
    {
        var shards = ShardsIn(territoryId);
        for (var i = 0; i < shards.Length; i++)
        {
            if (shards[i].Group == group && !shards[i].IsAetheryte)
            {
                return true;
            }
        }
        return false;
    }

    public static IGameObject? FindShardObject(uint aetheryteId)
    {
        foreach (IGameObject gameObject in Svc.Objects)
        {
            if (gameObject.ObjectKind == ObjectKind.Aetheryte && gameObject.BaseId == aetheryteId)
            {
                return gameObject;
            }
        }
        return null;
    }

    public static string ShardName(uint aetheryteId)
    {
        Aetheryte? aetheryte = Svc.Data.GetExcelSheet<Aetheryte>()?.GetRowOrDefault(aetheryteId);
        string? text = aetheryte is not { } row ? null : (row.IsAetheryte ? row.PlaceName.ValueNullable?.Name.ExtractText() : row.AethernetName.ValueNullable?.Name.ExtractText());
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text;
        }
        return $"aetheryte #{aetheryteId}";
    }

    private static AethernetShard Nearest(AethernetShard[] shards, Vector3 point)
    {
        var nearest = shards[0];
        var nearestDistance = FlatDistance(nearest.Position, point);
        for (var i = 1; i < shards.Length; i++)
        {
            var distance = FlatDistance(shards[i].Position, point);
            if (!(distance >= nearestDistance))
            {
                nearestDistance = distance;
                nearest = shards[i];
            }
        }
        return nearest;
    }

    private static bool TryFindNearestAttuned(AethernetShard[] shards, Vector3 point, uint group, out AethernetShard nearest)
    {
        nearest = default;
        var nearestDistance = float.MaxValue;
        for (var i = 0; i < shards.Length; i++)
        {
            var shard = shards[i];
            if (shard.Group == group && IsAttuned(shard.Id))
            {
                var distance = FlatDistance(shard.Position, point);
                if (!(distance >= nearestDistance))
                {
                    nearestDistance = distance;
                    nearest = shard;
                }
            }
        }
        return nearestDistance < float.MaxValue;
    }

    private static float FlatDistance(Vector3 first, Vector3 second) => Vector2.Distance(new Vector2(first.X, first.Z), new Vector2(second.X, second.Z));

    private unsafe static bool IsAttuned(uint aetheryteId)
    {
        var uiState = UIState.Instance();
        if (uiState != null)
        {
            return uiState->IsAetheryteUnlocked(aetheryteId);
        }
        return false;
    }

    private static AethernetShard[] ShardsIn(uint territoryId)
    {
        if (shardsByTerritory.TryGetValue(territoryId, out var cached))
        {
            return cached;
        }
        var shards = ResolveShards(territoryId);
        shardsByTerritory[territoryId] = shards;
        return shards;
    }

    private static AethernetShard[] ResolveShards(uint territoryId)
    {
        ExcelSheet<Aetheryte>? aetheryteSheet = Svc.Data.GetExcelSheet<Aetheryte>();
        if (aetheryteSheet == null)
        {
            return Array.Empty<AethernetShard>();
        }
        var shards = new List<AethernetShard>(8);
        foreach (var aetheryte in aetheryteSheet)
        {
            if (aetheryte.Territory.RowId == territoryId && aetheryte.AethernetGroup != 0 && (aetheryte.IsAetheryte || aetheryte.AethernetName.RowId != 0) && AetheryteGeometry.TryResolvePosition(aetheryte, out var position))
            {
                shards.Add(new AethernetShard(aetheryte.RowId, aetheryte.AethernetGroup, aetheryte.IsAetheryte, position));
            }
        }
        return shards.ToArray();
    }
}
