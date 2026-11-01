using System.Numerics;

namespace AutoFateGrind.Core.Zones;

public sealed class ZoneInfo
{
    public bool Unlocked;

    public int ActiveFateCount;

    public int CompletedThisRun;

    public required uint TerritoryId { get; init; }

    public required string Name { get; init; }

    public required ExpansionKind Expansion { get; init; }

    public required int MinLevel { get; init; }

    public Vector3 CentralLanding { get; init; }

    public string? IconFile { get; init; }
}
