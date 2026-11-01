using System;

namespace AutoFateGrind.Core.Game.SharedFates;

internal readonly record struct SharedFateZone(uint TerritoryId, uint AchievementId, byte TabIndex);

internal readonly record struct SharedFateRank(bool Known, int Completed, int Total, int Rank, int MaxRank, int RankProgress, int RankSize)
{
    public bool IsMaxed => Known && Completed >= Total;

    public float Fraction
    {
        get
        {
            if (Total <= 0)
            {
                return 0f;
            }
            return Math.Clamp((float)Completed / (float)Total, 0f, 1f);
        }
    }
}

internal static class SharedFateCatalog
{
    public const int TotalFates = 66;

    public const int TabCount = 3;

    public const byte ShadowbringersTab = 0;

    public const byte EndwalkerTab = 1;

    public const byte DawntrailTab = 2;

    private static readonly int[] ThreeRankSizes = new int[] { 6, 60 };

    private static readonly int[] FourRankSizes = new int[] { 6, 20, 40 };

    public static readonly SharedFateZone[] Entries = new SharedFateZone[]
    {
        new SharedFateZone(813u, 2343u, ShadowbringersTab),
        new SharedFateZone(814u, 2344u, ShadowbringersTab),
        new SharedFateZone(815u, 2345u, ShadowbringersTab),
        new SharedFateZone(816u, 2346u, ShadowbringersTab),
        new SharedFateZone(817u, 2347u, ShadowbringersTab),
        new SharedFateZone(818u, 2348u, ShadowbringersTab),
        new SharedFateZone(956u, 3022u, EndwalkerTab),
        new SharedFateZone(957u, 3023u, EndwalkerTab),
        new SharedFateZone(958u, 3024u, EndwalkerTab),
        new SharedFateZone(959u, 3025u, EndwalkerTab),
        new SharedFateZone(961u, 3026u, EndwalkerTab),
        new SharedFateZone(960u, 3027u, EndwalkerTab),
        new SharedFateZone(1187u, 3559u, DawntrailTab),
        new SharedFateZone(1188u, 3560u, DawntrailTab),
        new SharedFateZone(1189u, 3561u, DawntrailTab),
        new SharedFateZone(1190u, 3562u, DawntrailTab),
        new SharedFateZone(1191u, 3563u, DawntrailTab),
        new SharedFateZone(1192u, 3564u, DawntrailTab)
    };

    public static bool HasRanks(uint territoryId) => IndexOfTerritory(territoryId) >= 0;

    public static int IndexOfTerritory(uint territoryId)
    {
        for (var i = 0; i < Entries.Length; i++)
        {
            if (Entries[i].TerritoryId == territoryId)
            {
                return i;
            }
        }
        return -1;
    }

    public static int IndexOfAchievement(uint achievementId)
    {
        for (var i = 0; i < Entries.Length; i++)
        {
            if (Entries[i].AchievementId == achievementId)
            {
                return i;
            }
        }
        return -1;
    }

    public static int MaxRank(byte tabIndex) => RankSizes(tabIndex).Length + 1;

    public static SharedFateRank Resolve(int entryIndex, int completed, int total)
    {
        var rankSizes = RankSizes(Entries[entryIndex].TabIndex);
        var maxRank = rankSizes.Length + 1;
        var totalFates = total > 0 ? total : TotalFates;
        var completedClamped = Math.Clamp(completed, 0, totalFates);
        var remaining = completedClamped;
        for (var i = 0; i < rankSizes.Length; i++)
        {
            if (remaining < rankSizes[i])
            {
                return new SharedFateRank(Known: true, completedClamped, totalFates, i + 1, maxRank, remaining, rankSizes[i]);
            }
            remaining -= rankSizes[i];
        }
        return new SharedFateRank(Known: true, completedClamped, totalFates, maxRank, maxRank, 0, 0);
    }

    private static int[] RankSizes(byte tabIndex)
    {
        if (tabIndex != DawntrailTab)
        {
            return ThreeRankSizes;
        }
        return FourRankSizes;
    }
}
