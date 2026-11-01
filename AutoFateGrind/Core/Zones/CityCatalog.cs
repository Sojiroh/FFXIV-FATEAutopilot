using System.Collections.Generic;

namespace AutoFateGrind.Core.Zones;

public sealed class CityInfo
{
    public required uint TerritoryId { get; init; }

    public required string Name { get; init; }

    public required ExpansionKind Expansion { get; init; }
}

public static class CityCatalog
{
    public const uint SolutionNineTerritoryId = 1186u;

    public const uint LegacySolutionNineTerritoryId = 1205u;

    public static readonly CityInfo[] All = new CityInfo[4]
    {
        new CityInfo
        {
            TerritoryId = 129u,
            Name = "Limsa Lominsa Lower Decks",
            Expansion = ExpansionKind.ARR
        },
        new CityInfo
        {
            TerritoryId = 132u,
            Name = "New Gridania",
            Expansion = ExpansionKind.ARR
        },
        new CityInfo
        {
            TerritoryId = 1185u,
            Name = "Tuliyollal",
            Expansion = ExpansionKind.DT
        },
        new CityInfo
        {
            TerritoryId = SolutionNineTerritoryId,
            Name = "Solution Nine",
            Expansion = ExpansionKind.DT
        }
    };

    public static CityInfo? Find(uint territoryId)
    {
        var cities = All;
        foreach (var city in cities)
        {
            if (city.TerritoryId == territoryId)
            {
                return city;
            }
        }
        return null;
    }

    public static bool MigrateSelection(HashSet<uint> selectedCities)
    {
        if (!selectedCities.Remove(LegacySolutionNineTerritoryId))
        {
            return false;
        }
        selectedCities.Add(SolutionNineTerritoryId);
        return true;
    }
}
