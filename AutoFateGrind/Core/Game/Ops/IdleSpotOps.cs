using System.Numerics;
using AutoFateGrind.Core.Zones;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;

namespace AutoFateGrind.Core.Game.Ops;

internal static class IdleSpotOps
{
    private const float TargetNameRangeMeters = 15f;

    public static IdleSpotCapture TryCapture(out IdleSpot? spot)
    {
        spot = null;
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null || Svc.Condition[ConditionFlag.BetweenAreas])
        {
            return IdleSpotCapture.NotLoaded;
        }
        if (Svc.Condition[ConditionFlag.InFlight] || Svc.Condition[ConditionFlag.Diving])
        {
            return IdleSpotCapture.Airborne;
        }
        var territoryType = Svc.ClientState.TerritoryType;
        if (!ZoneAetherytes.IsTeleportable(territoryType))
        {
            return IdleSpotCapture.NoAetheryte;
        }
        var position = localPlayer.Position;
        spot = new IdleSpot
        {
            TerritoryId = territoryType,
            X = position.X,
            Y = position.Y,
            Z = position.Z,
            Name = NearbyTargetName(position)
        };
        return IdleSpotCapture.Saved;
    }

    private static string NearbyTargetName(Vector3 position)
    {
        IGameObject? target = Svc.Targets.Target;
        if (target == null || target.ObjectKind == ObjectKind.Pc)
        {
            return "";
        }
        if (!(Vector3.Distance(target.Position, position) <= TargetNameRangeMeters))
        {
            return "";
        }
        return target.Name.TextValue;
    }
}
