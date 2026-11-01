using System.Collections.Generic;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Zones;

internal static class TerritoryNames
{
    private static readonly Dictionary<uint, string> byTerritory = new Dictionary<uint, string>();

    public static string Of(uint territoryId)
    {
        if (byTerritory.TryGetValue(territoryId, out var name))
        {
            return name;
        }
        var placeName = Svc.Data.GetExcelSheet<TerritoryType>()?.GetRowOrDefault(territoryId)?.PlaceName.ValueNullable?.Name.ExtractText();
        var resolved = (string.IsNullOrWhiteSpace(placeName) ? $"territory {territoryId}" : placeName);
        byTerritory[territoryId] = resolved;
        return resolved;
    }
}
