using System;
using System.Numerics;
using System.Threading.Tasks;
using AutoFateGrind.Core.Game.Watchers;
using AutoFateGrind.Core.Zones;

namespace AutoFateGrind.Core.Tasks;

public abstract partial class AutoCommon
{
    internal readonly record struct TeleportOutcome(bool Completed, Exception? Fault, TeleportRefusal? Refusal)
    {
        public bool Succeeded => Completed && Fault == null;
    }

    internal async Task RideAethernetShortcut(Vector3 destination, string scope)
    {
        var localPlayer = ECommons.DalamudServices.Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return;
        }
        if (!CityAethernet.TryPlanHop(ECommons.DalamudServices.Svc.ClientState.TerritoryType, localPlayer.Position, destination, out var hop))
        {
            return;
        }
        var hopName = CityAethernet.ShardName(hop.Source.Id) + " → " + CityAethernet.ShardName(hop.Destination.Id);
        if (!hop.SavesDistance)
        {
            Diag($"{scope}: walking; aethernet {hopName} costs ~{hop.RideMeters:F0}m against a {hop.WalkMeters:F0}m walk");
            return;
        }
        Status = "Riding the aethernet to " + CityAethernet.ShardName(hop.Destination.Id);
        Diag($"{scope}: aethernet {hopName} (~{hop.RideMeters:F0}m against a {hop.WalkMeters:F0}m walk)");
        var op = new MoveOp((MoveOp o) => o.RideAethernet(hop));
        await RunCancellable(op, AethernetShortcutMs, scope);
        var fault = op.Fault;
        if (fault != null)
        {
            Diag(scope + ": aethernet ride faulted: " + fault.Message + "; walking from here");
        }
    }
}
