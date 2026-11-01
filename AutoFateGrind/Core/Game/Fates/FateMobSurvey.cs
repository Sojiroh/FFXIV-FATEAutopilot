using System.Numerics;

namespace AutoFateGrind.Core.Game.Fates;

internal readonly record struct FateMobSurvey(int LiveCount, Vector3 NearestPosition, float NearestHitboxRadius, float NearestDistanceToHitbox, float NearestVerticalDelta)
{
    public bool Any => LiveCount > 0;

    public static readonly FateMobSurvey Empty = new FateMobSurvey(0, default, 0f, float.MaxValue, 0f);
}
