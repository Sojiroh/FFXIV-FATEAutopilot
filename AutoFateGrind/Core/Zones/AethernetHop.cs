using System.Numerics;
using ECommons.DalamudServices;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Zones;

internal static class AetheryteGeometry
{
    private const byte AetheryteMarkerType = 3;

    private const byte AethernetMarkerType = 4;

    private const float MapCentrePixels = 1024f;

    private const float SizeFactorToScale = 0.01f;

    public static bool TryResolvePosition(Aetheryte row, out Vector3 position)
    {
        if (row.Level[0].ValueNullable is { } level)
        {
            position = new Vector3(level.X, level.Y, level.Z);
            return true;
        }
        position = default;
        if (row.Territory.ValueNullable is not { } territory)
        {
            return false;
        }
        if (territory.Map.ValueNullable is { } primaryMap && TryResolveOnMap(row, primaryMap, out position))
        {
            return true;
        }
        ExcelSheet<Map>? mapSheet = Svc.Data.GetExcelSheet<Map>();
        if (mapSheet == null)
        {
            return false;
        }
        foreach (var map in mapSheet)
        {
            if (map.TerritoryType.RowId == territory.RowId && map.RowId != territory.Map.RowId && TryResolveOnMap(row, map, out position))
            {
                return true;
            }
        }
        return false;
    }

    private static bool TryResolveOnMap(Aetheryte row, Map map, out Vector3 position)
    {
        position = default;
        if (!TryFindMarker(row, map.MapMarkerRange, out var marker))
        {
            return false;
        }
        float scale = (float)(int)map.SizeFactor * SizeFactorToScale;
        position = new Vector3(PixelToWorld(marker.X, scale, map.OffsetX), 0f, PixelToWorld(marker.Y, scale, map.OffsetY));
        return true;
    }

    private static bool TryFindMarker(Aetheryte row, uint markerRange, out MapMarker marker)
    {
        marker = default;
        if (Svc.Data.GetSubrowExcelSheet<MapMarker>()?.GetRowOrDefault(markerRange) is not { } markers)
        {
            return false;
        }
        for (var i = 0; i < markers.Count; i++)
        {
            if (markers[i].DataType == AetheryteMarkerType && markers[i].DataKey.RowId == row.RowId)
            {
                marker = markers[i];
                return true;
            }
        }
        if (row.AethernetName.RowId == 0)
        {
            return false;
        }
        for (var j = 0; j < markers.Count; j++)
        {
            if (markers[j].DataType == AethernetMarkerType && markers[j].DataKey.RowId == row.AethernetName.RowId)
            {
                marker = markers[j];
                return true;
            }
        }
        return false;
    }

    private static float PixelToWorld(short pixel, float scale, short offset) => (pixel - MapCentrePixels) / scale - offset;
}

internal readonly record struct AethernetShard(uint Id, uint Group, bool IsAetheryte, Vector3 Position);

internal readonly record struct AethernetHop(AethernetShard Source, AethernetShard Destination, float WalkMeters, float RideMeters)
{
    public bool SavesDistance => RideMeters < WalkMeters;
}
