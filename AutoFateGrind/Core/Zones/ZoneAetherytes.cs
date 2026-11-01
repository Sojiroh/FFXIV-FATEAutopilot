using System;
using System.Collections.Generic;
using System.Numerics;
using ECommons.DalamudServices;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Zones;

internal readonly record struct ZoneGateway(uint AetheryteId, uint AethernetGroup, uint TerritoryId, string Name, Vector3 Position);

internal static class ZoneAetherytes
{
    private static readonly Dictionary<uint, ZoneAetheryte[]> byTerritory = new Dictionary<uint, ZoneAetheryte[]>();

    private static readonly Dictionary<uint, uint[]> attunableIdsByTerritory = new Dictionary<uint, uint[]>();

    private static readonly Dictionary<uint, ZoneGateway?> gatewayByTerritory = new Dictionary<uint, ZoneGateway?>();

    public static bool TryFindNearest(uint territoryId, Vector3 target, out ZoneAetheryte nearest)
    {
        var aetherytes = InTerritory(territoryId);
        nearest = default;
        var nearestDistance = float.MaxValue;
        for (var i = 0; i < aetherytes.Length; i++)
        {
            var distance = Vector3.DistanceSquared(aetherytes[i].Position, target);
            if (!(distance >= nearestDistance))
            {
                nearestDistance = distance;
                nearest = aetherytes[i];
            }
        }
        return nearestDistance < float.MaxValue;
    }

    private static ZoneAetheryte[] InTerritory(uint territoryId)
    {
        if (byTerritory.TryGetValue(territoryId, out var cached))
        {
            return cached;
        }
        var aetherytes = ResolveTeleportableAetherytes(territoryId);
        byTerritory[territoryId] = aetherytes;
        return aetherytes;
    }

    public static uint[] AttunableIdsIn(uint territoryId)
    {
        if (attunableIdsByTerritory.TryGetValue(territoryId, out var cached))
        {
            return cached;
        }
        var ids = ResolveAttunableIds(territoryId);
        attunableIdsByTerritory[territoryId] = ids;
        return ids;
    }

    public static bool TryFindGateway(uint territoryId, out ZoneGateway gateway)
    {
        if (!gatewayByTerritory.TryGetValue(territoryId, out var resolved))
        {
            resolved = ResolveGateway(territoryId);
            gatewayByTerritory[territoryId] = resolved;
        }
        gateway = resolved.GetValueOrDefault();
        return resolved.HasValue;
    }

    public static bool IsTeleportable(uint territoryId)
    {
        ZoneGateway gateway;
        if (AttunableIdsIn(territoryId).Length == 0)
        {
            return TryFindGateway(territoryId, out gateway);
        }
        return true;
    }

    private static ZoneGateway? ResolveGateway(uint territoryId)
    {
        if (AttunableIdsIn(territoryId).Length != 0)
        {
            return null;
        }
        if (Svc.Data.GetExcelSheet<TerritoryType>()?.GetRowOrDefault(territoryId) is not { } territoryType)
        {
            return null;
        }
        var aetheryteId = territoryType.Aetheryte.RowId;
        if (aetheryteId == 0)
        {
            return null;
        }
        if (Svc.Data.GetExcelSheet<Aetheryte>()?.GetRowOrDefault(aetheryteId) is not { } gatewayRow || !gatewayRow.IsAetheryte)
        {
            return null;
        }
        if (gatewayRow.Territory.RowId == 0 || gatewayRow.Territory.RowId == territoryId)
        {
            return null;
        }
        if (!CityAethernet.HoldsShardOf(territoryId, gatewayRow.AethernetGroup))
        {
            return null;
        }
        if (!TryResolvePosition(gatewayRow, out var position))
        {
            return null;
        }
        return new ZoneGateway(aetheryteId, gatewayRow.AethernetGroup, gatewayRow.Territory.RowId, ResolveName(gatewayRow), position);
    }

    private static uint[] ResolveAttunableIds(uint territoryId)
    {
        ExcelSheet<Aetheryte>? aetheryteSheet = Svc.Data.GetExcelSheet<Aetheryte>();
        if (aetheryteSheet == null)
        {
            return Array.Empty<uint>();
        }
        var ids = new List<uint>(4);
        foreach (var aetheryte in aetheryteSheet)
        {
            if (aetheryte.IsAetheryte && aetheryte.Territory.RowId == territoryId)
            {
                ids.Add(aetheryte.RowId);
            }
        }
        return ids.ToArray();
    }

    private static ZoneAetheryte[] ResolveTeleportableAetherytes(uint territoryId)
    {
        ExcelSheet<Aetheryte>? aetheryteSheet = Svc.Data.GetExcelSheet<Aetheryte>();
        if (aetheryteSheet == null)
        {
            return Array.Empty<ZoneAetheryte>();
        }
        var ids = AttunableIdsIn(territoryId);
        var aetherytes = new List<ZoneAetheryte>(ids.Length);
        for (var i = 0; i < ids.Length; i++)
        {
            if (aetheryteSheet.GetRowOrDefault(ids[i]) is { } aetheryte && TryResolvePosition(aetheryte, out var position))
            {
                aetherytes.Add(new ZoneAetheryte(aetheryte.RowId, ResolveName(aetheryte), position));
            }
        }
        return aetherytes.ToArray();
    }

    private static string ResolveName(Aetheryte row)
    {
        var name = row.PlaceName.ValueNullable?.Name.ExtractText();
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }
        return $"aetheryte #{row.RowId}";
    }

    private static bool TryResolvePosition(Aetheryte row, out Vector3 position)
    {
        if (AetheryteGeometry.TryResolvePosition(row, out position))
        {
            return true;
        }
        RunLog.Warning($"Could not resolve a position for aetheryte {row.RowId}; skipping it as a teleport target");
        return false;
    }
}
