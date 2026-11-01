using System.Numerics;

namespace AutoFateGrind.Core.Game.Fates;

internal readonly record struct FateMobTarget(ulong GameObjectId, Vector3 Position, float HitboxRadius, float DistanceToHitbox);
