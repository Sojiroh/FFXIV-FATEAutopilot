using System;
using System.Numerics;
using AutoFateGrind.Core.Ipc;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;

namespace AutoFateGrind.Core.Tasks;

internal enum StallKind
{
    None,
    NavWedge,
    Idle
}

internal sealed class MoveStallTracker
{
    private Vector3? anchor;

    private Vector3? target;

    private float bestTargetDistance = float.MaxValue;

    private int lastWaypointCount;

    private bool followedOnce;

    private bool pathInterrupted;

    private bool jumpedOutOfWedge;

    private long navWedgeSinceMs = Environment.TickCount64;

    private long idleSinceMs = Environment.TickCount64;

    public StallKind Check()
    {
        var localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return StallKind.None;
        }
        var tickCount = Environment.TickCount64;
        var position = localPlayer.Position;
        var navmesh = NavmeshIPC.Instance;
        var followingPath = navmesh.IsRunning();
        var navBusy = followingPath || navmesh.IsBusy();
        var frozenLegit = StuckDetector.IsPositionFrozenLegit();
        if (frozenLegit | navBusy)
        {
            idleSinceMs = tickCount;
        }
        else if (tickCount - idleSinceMs >= StuckDetector.IdleStallTimeoutMs)
        {
            return StallKind.Idle;
        }
        if (frozenLegit)
        {
            navWedgeSinceMs = tickCount;
            anchor = position;
            return StallKind.None;
        }
        if (!followingPath)
        {
            if (followedOnce)
            {
                pathInterrupted = true;
            }
            if (!followedOnce || !navBusy)
            {
                navWedgeSinceMs = tickCount;
            }
            return StallKind.None;
        }
        followedOnce = true;
        var waypointCount = navmesh.NumWaypoints();
        if ((waypointCount == -1) ? DisplacedFromAnchor(position) : ClosedOnWaypoint(navmesh, waypointCount, position))
        {
            navWedgeSinceMs = tickCount;
            return StallKind.None;
        }
        if (tickCount - navWedgeSinceMs < StuckDetector.NavWedgeTimeoutMs)
        {
            return StallKind.None;
        }
        if (!jumpedOutOfWedge && CanJumpOutOfWedge())
        {
            jumpedOutOfWedge = true;
            navWedgeSinceMs = tickCount;
            AutoCommon.UseGeneralAction(AutoCommon.JumpGeneralActionId);
            RunLog.Info($"No progress toward the next waypoint in {StuckDetector.NavWedgeTimeoutMs / 1000}s at {position}; jumping once before giving up on the move");
            return StallKind.None;
        }
        return StallKind.NavWedge;
    }

    private static bool CanJumpOutOfWedge() => !Svc.Condition[ConditionFlag.InCombat] && !Svc.Condition[ConditionFlag.InFlight] && !Svc.Condition[ConditionFlag.Swimming] && !Svc.Condition[ConditionFlag.Diving] && !Svc.Condition[ConditionFlag.Jumping] && !Svc.Condition[ConditionFlag.Jumping61];

    private bool DisplacedFromAnchor(Vector3 pos)
    {
        var anchorValue = anchor;
        if (anchorValue.HasValue && Vector3.Distance(anchor!.Value, pos) <= StuckDetector.StuckMoveThresholdMeters)
        {
            return false;
        }
        anchor = pos;
        return true;
    }

    private bool ClosedOnWaypoint(NavmeshIPC nav, int count, Vector3 pos)
    {
        var reachedWaypoint = target is not null && !pathInterrupted && count < lastWaypointCount;
        var retargeted = target is null || pathInterrupted || count > lastWaypointCount;
        if (reachedWaypoint | retargeted)
        {
            pathInterrupted = false;
            lastWaypointCount = count;
            target = nav.CurrentWaypoint();
            bestTargetDistance = target is { } targetPoint ? Vector3.Distance(pos, targetPoint) : float.MaxValue;
            if (target is null)
            {
                return DisplacedFromAnchor(pos);
            }
            return reachedWaypoint;
        }
        if (target is null)
        {
            return DisplacedFromAnchor(pos);
        }
        var newDistance = Vector3.Distance(pos, target!.Value);
        if (newDistance >= bestTargetDistance - StuckDetector.ProgressEpsilonMeters)
        {
            return false;
        }
        bestTargetDistance = newDistance;
        return true;
    }
}
