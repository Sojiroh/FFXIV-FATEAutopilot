using System.Numerics;
using AutoFateGrind.Core.Ipc;

namespace AutoFateGrind.Core.Game.Fates;

internal static class FateGround
{
    private const float HalfExtentXZ = 5f;

    private const float NearHalfExtentY = 5f;

    private const float RingHalfExtentY = 20f;

    public static Vector3? Project(Vector3 point)
    {
        var navmesh = NavmeshIPC.Instance;
        return navmesh.NearestPointReachable(point, HalfExtentXZ, NearHalfExtentY) ?? navmesh.NearestPointReachable(point, HalfExtentXZ, RingHalfExtentY);
    }

    public static float HorizontalDistance(Vector3 from, Vector3 to) => Vector2.Distance(new Vector2(from.X, from.Z), new Vector2(to.X, to.Z));
}
