using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using AutoFateGrind.Core.Game.Watchers;
using AutoFateGrind.Core.Ipc;
using Dalamud.Game.ClientState.Conditions;
using clib.TaskSystem;

namespace AutoFateGrind.Core.Tasks;

public abstract partial class AutoCommon
{
    private async Task<bool> LandAndDismount(Vector3 around, string scope)
    {
        Status = "Landing";
        var spotCount = FindLandingSpots(around, landingSpots);
        if (spotCount == 0)
        {
            Diag($"{scope}: no landable floor within {LandingRingRadiusMeters:F0}m of {FormatPosition(around)}; descending where the flight ended");
            return await DescendAndDismount(scope) || await DescendInPlace(scope);
        }
        var attempts = Math.Min(spotCount, MaxLandingSpots);
        for (var spotIndex = 0; spotIndex < attempts; spotIndex++)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return false;
            }
            var spot = landingSpots[spotIndex];
            var legScope = $"{scope}-land#{spotIndex + 1}";
            Diag($"{legScope}: flying {DistanceTo(spot):F0}m to a landable spot at {FormatPosition(spot)}");
            var flight = new MoveOp((MoveOp move) => move.MoveInZone(spot, MovementConfig.Default.WithTolerance(LandingArriveMeters), null));
            await RunCancellable(flight, LandingFlightWatchdogMs, legScope, StuckDetector.MoveStallAbort(legScope));
            var fault = flight.Fault;
            if (fault != null)
            {
                Diag(legScope + ": the landing flight faulted: " + fault.Message);
            }
            if (await DescendAndDismount(legScope))
            {
                return true;
            }
            if (CancelToken.IsCancellationRequested)
            {
                return false;
            }
            await BackOffInFlight(legScope);
        }
        if (await DescendInPlace(scope))
        {
            return true;
        }
        Warn($"{scope}: could not land near {FormatPosition(around)} after {attempts} spot(s) ({ConditionTag()})");
        return false;
    }

    private async Task<bool> DescendAndDismount(string scope)
    {
        if (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounted])
        {
            return true;
        }
        if (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight])
        {
            return await GroundDismount(scope);
        }
        await DismountViaOp(scope, LandingDismountWatchdogMs, StuckDetector.AirborneFreezeAbort(scope));
        if (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounted])
        {
            return true;
        }
        Diag(scope + ": the descent did not land (" + ConditionTag() + ")");
        CancelDescent();
        return false;
    }

    private async Task<bool> DescendInPlace(string scope)
    {
        if (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight])
        {
            return await DescendAndDismount(scope);
        }
        var descentScope = scope + "-vertical";
        Diag(descentScope + ": descending straight down from " + FormatPosition(ECommons.DalamudServices.Svc.Objects.LocalPlayer?.Position ?? default(Vector3)));
        NavmeshIPC.Instance.Stop();
        var frozen = StuckDetector.AirborneFreezeAbort(descentScope);
        var deadline = Environment.TickCount64 + VerticalDescentWatchdogMs;
        var nextPressAtMs = 0L;
        while (ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight] && Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return false;
            }
            if (frozen())
            {
                break;
            }
            if (Environment.TickCount64 >= nextPressAtMs && GeneralActionStatus(DismountGeneralActionId) == 0)
            {
                UseGeneralAction(DismountGeneralActionId);
                nextPressAtMs = Environment.TickCount64 + VerticalDescentReissueMs;
            }
            await NextFrame();
        }
        if (ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight])
        {
            Diag(descentScope + ": still airborne after the straight descent (" + ConditionTag() + ")");
            CancelDescent();
            return false;
        }
        return await GroundDismount(descentScope);
    }

    private async Task<bool> GroundDismount(string scope)
    {
        await DismountViaOp(scope, GroundDismountWatchdogMs);
        if (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounted])
        {
            return true;
        }
        Diag(scope + ": still mounted after the dismount (" + ConditionTag() + ")");
        return false;
    }

    private Task<bool> DismountViaOp(string label, int watchdogMs, Func<bool>? abortIf = null) => RunCancellable(new MoveOp((MoveOp move) => move.DismountNow()), watchdogMs, label, abortIf);

    private static void CancelDescent()
    {
        if (ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight])
        {
            UseGeneralAction(JumpGeneralActionId);
        }
    }

    private async Task BackOffInFlight(string scope)
    {
        var localPlayer = ECommons.DalamudServices.Svc.Objects.LocalPlayer;
        if (localPlayer == null || !ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight])
        {
            return;
        }
        var backOffPoint = localPlayer.Position with
        {
            Y = localPlayer.Position.Y + LandingBackOffMeters
        };
        Diag($"{scope}: backing off {LandingBackOffMeters:F0}m upward");
        NavmeshIPC.Instance.MoveAlong(new List<Vector3>(1) { backOffPoint }, fly: true);
        await DelayMs(LandingBackOffMs);
        NavmeshIPC.Instance.Stop();
    }

    private static int FindLandingSpots(Vector3 around, Vector3[] spots)
    {
        var navmesh = NavmeshIPC.Instance;
        var spotCount = 0;
        if (LandableFloor(navmesh, around) is { } centerSpot)
        {
            spots[spotCount++] = centerSpot;
        }
        for (var i = 0; i < LandingRingPoints; i++)
        {
            var x = (float)Math.PI * 2f * (float)i / LandingRingPoints;
            var point = around + new Vector3(MathF.Cos(x) * LandingRingRadiusMeters, 0f, MathF.Sin(x) * LandingRingRadiusMeters);
            if (LandableFloor(navmesh, point) is { } ringSpot)
            {
                spots[spotCount++] = ringSpot;
            }
        }
        return spotCount;
    }

    private static Vector3? LandableFloor(NavmeshIPC navmesh, Vector3 point) => navmesh.PointOnFloor(point with { Y = point.Y + LandingProbeLiftMeters }, allowUnreachable: false, LandingFloorHalfExtentMeters);

    private static float DistanceTo(Vector3 target)
    {
        var localPlayer = ECommons.DalamudServices.Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return float.MaxValue;
        }
        return Vector3.Distance(localPlayer.Position, target);
    }

    private static string FormatPosition(Vector3 position) => $"({position.X:F0},{position.Y:F0},{position.Z:F0})";

    private protected TeleportRefusal? LastTeleportRefusal { get; private set; }
}
