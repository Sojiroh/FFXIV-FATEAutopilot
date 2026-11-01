using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoFateGrind.Core.Ipc;

internal sealed class NavmeshIPC
{
    private static NavmeshIPC? instance;

    private const float BuildIdle = -1f;

    private readonly ICallGateSubscriber<bool> pathIsRunning;

    private readonly ICallGateSubscriber<bool> simpleMovePathfindInProgress;

    private readonly ICallGateSubscriber<bool> navPathfindInProgress;

    private readonly ICallGateSubscriber<bool> navIsReady;

    private readonly ICallGateSubscriber<float> navBuildProgress;

    private readonly ICallGateSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>> navPathfind;

    private readonly ICallGateSubscriber<Vector3, Vector3, bool, CancellationToken, Task<List<Vector3>>> navPathfindCancelable;

    private readonly ICallGateSubscriber<Vector3, float, float, Vector3?> nearestPointReachable;

    private readonly ICallGateSubscriber<Vector3, bool, float, Vector3?> pointOnFloor;

    private readonly ICallGateSubscriber<object> pathStop;

    private readonly ICallGateSubscriber<List<Vector3>, bool, object> pathMoveTo;

    private readonly ICallGateSubscriber<Vector3, bool, object> simpleMovePathfindAndMoveTo;

    private readonly ICallGateSubscriber<Vector3, bool, float, bool> simpleMovePathfindAndMoveCloseTo;

    private readonly ICallGateSubscriber<int> pathNumWaypoints;

    private readonly ICallGateSubscriber<List<Vector3>> pathListWaypoints;

    private readonly ICallGateSubscriber<float> pathGetTolerance;

    private readonly Func<bool> isRunningCall;

    private readonly Func<bool> isMovePendingCall;

    private readonly Func<bool> isPathfindingCall;

    public const int WaypointsUnavailable = -1;

    public static NavmeshIPC Instance => instance ?? (instance = new NavmeshIPC());

    private NavmeshIPC()
    {
        pathIsRunning = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Path.IsRunning");
        simpleMovePathfindInProgress = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.SimpleMove.PathfindInProgress");
        navPathfindInProgress = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.PathfindInProgress");
        navIsReady = Svc.PluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
        navBuildProgress = Svc.PluginInterface.GetIpcSubscriber<float>("vnavmesh.Nav.BuildProgress");
        navPathfind = Svc.PluginInterface.GetIpcSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>>("vnavmesh.Nav.Pathfind");
        navPathfindCancelable = Svc.PluginInterface.GetIpcSubscriber<Vector3, Vector3, bool, CancellationToken, Task<List<Vector3>>>("vnavmesh.Nav.PathfindCancelable");
        nearestPointReachable = Svc.PluginInterface.GetIpcSubscriber<Vector3, float, float, Vector3?>("vnavmesh.Query.Mesh.NearestPointReachable");
        pointOnFloor = Svc.PluginInterface.GetIpcSubscriber<Vector3, bool, float, Vector3?>("vnavmesh.Query.Mesh.PointOnFloor");
        pathStop = Svc.PluginInterface.GetIpcSubscriber<object>("vnavmesh.Path.Stop");
        pathMoveTo = Svc.PluginInterface.GetIpcSubscriber<List<Vector3>, bool, object>("vnavmesh.Path.MoveTo");
        simpleMovePathfindAndMoveTo = Svc.PluginInterface.GetIpcSubscriber<Vector3, bool, object>("vnavmesh.SimpleMove.PathfindAndMoveTo");
        simpleMovePathfindAndMoveCloseTo = Svc.PluginInterface.GetIpcSubscriber<Vector3, bool, float, bool>("vnavmesh.SimpleMove.PathfindAndMoveCloseTo");
        pathNumWaypoints = Svc.PluginInterface.GetIpcSubscriber<int>("vnavmesh.Path.NumWaypoints");
        pathListWaypoints = Svc.PluginInterface.GetIpcSubscriber<List<Vector3>>("vnavmesh.Path.ListWaypoints");
        pathGetTolerance = Svc.PluginInterface.GetIpcSubscriber<float>("vnavmesh.Path.GetTolerance");
        isRunningCall = pathIsRunning.InvokeFunc;
        isMovePendingCall = simpleMovePathfindInProgress.InvokeFunc;
        isPathfindingCall = navPathfindInProgress.InvokeFunc;
    }

    public float GetTolerance() => IpcGate.Invoke(pathGetTolerance.HasFunction, pathGetTolerance.InvokeFunc, 0f, "GetTolerance failed");

    public bool IsReady() => IpcGate.Invoke(navIsReady.HasFunction, navIsReady.InvokeFunc, fallback: true, "IsReady failed");

    public float BuildProgress() => IpcGate.Invoke(navBuildProgress.HasFunction, navBuildProgress.InvokeFunc, BuildIdle, "BuildProgress failed");

    public bool IsRunning() => IpcGate.Invoke(pathIsRunning.HasFunction, isRunningCall, fallback: false, "IsRunning failed");

    public bool IsBusy() => IsRunning() || IsMovePending() || IsPathfinding();

    public bool IsMovePending() => IpcGate.Invoke(simpleMovePathfindInProgress.HasFunction, isMovePendingCall, fallback: false, "PathfindInProgress(SimpleMove) failed");

    public bool IsPathfinding() => IpcGate.Invoke(navPathfindInProgress.HasFunction, isPathfindingCall, fallback: false, "PathfindInProgress(Nav) failed");

    public bool PathfindAndMoveCloseTo(Vector3 destination, bool fly, float arriveWithin) => IpcGate.Invoke(simpleMovePathfindAndMoveCloseTo.HasFunction, () => simpleMovePathfindAndMoveCloseTo.InvokeFunc(destination, fly, arriveWithin), fallback: false, "SimpleMove.PathfindAndMoveCloseTo failed");

    public Task<List<Vector3>>? Pathfind(Vector3 from, Vector3 to, bool fly, CancellationToken cancel)
    {
        if (navPathfindCancelable.HasFunction)
        {
            return IpcGate.Invoke<Task<List<Vector3>>?>(hasFunction: true, () => navPathfindCancelable.InvokeFunc(from, to, fly, cancel), null, "PathfindCancelable failed");
        }
        return IpcGate.Invoke<Task<List<Vector3>>?>(navPathfind.HasFunction, () => navPathfind.InvokeFunc(from, to, fly), null, "Pathfind failed");
    }

    public Vector3? NearestPointReachable(Vector3 position, float halfExtentXZ = 5f, float halfExtentY = 5f)
    {
        return IpcGate.Invoke(nearestPointReachable.HasFunction, () => nearestPointReachable.InvokeFunc(position, halfExtentXZ, halfExtentY), null, "NearestPointReachable failed");
    }

    public Vector3? PointOnFloor(Vector3 position, bool allowUnreachable, float halfExtentXZ) => IpcGate.Invoke(pointOnFloor.HasFunction, () => pointOnFloor.InvokeFunc(position, allowUnreachable, halfExtentXZ), null, "PointOnFloor failed");

    public int NumWaypoints() => IpcGate.Invoke(pathNumWaypoints.HasFunction, pathNumWaypoints.InvokeFunc, WaypointsUnavailable, "NumWaypoints failed");

    public Vector3? CurrentWaypoint()
    {
        List<Vector3>? waypoints = IpcGate.Invoke(pathListWaypoints.HasFunction, pathListWaypoints.InvokeFunc, null, "ListWaypoints failed");
        if (waypoints == null || waypoints.Count <= 0)
        {
            return null;
        }
        return waypoints[0];
    }

    public void Stop()
    {
        IpcGate.Run(pathStop.HasAction, pathStop.InvokeAction, "Stop failed");
    }

    public void MoveAlong(List<Vector3> waypoints, bool fly)
    {
        IpcGate.Run(pathMoveTo.HasAction, () =>
        {
            pathMoveTo.InvokeAction(waypoints, fly);
        }, "Path.MoveTo failed");
    }

    public void PathfindAndMoveTo(Vector3 destination, bool fly)
    {
        IpcGate.Run(simpleMovePathfindAndMoveTo.HasAction, () =>
        {
            simpleMovePathfindAndMoveTo.InvokeAction(destination, fly);
        }, "SimpleMove.PathfindAndMoveTo failed");
    }
}
