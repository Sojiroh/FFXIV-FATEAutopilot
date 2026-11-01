using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using AutoFateGrind.Core.Ipc;
using AutoFateGrind.Core.Zones;
using ECommons.DalamudServices;
using clib.TaskSystem;

namespace AutoFateGrind.Core.Tasks;

internal sealed class AutoHumanize(BreakPlan plan, int durationMs) : AutoCommon
{
    private readonly BreakPlan plan = plan;

    private readonly int durationMs = durationMs;

    private const int TeleportWatchdogMs = 60000;

    private const int WalkWatchdogMs = 90000;

    private const int NavmeshReadyWaitMs = 60000;

    private const int PlayerWaitPollMs = 500;

    private const int RouteQueryTimeoutMs = 5000;

    private const int CandidateAttempts = 8;

    private const float ArrivalTolerance = 4f;

    private const float CandidateHalfExtentXZ = 10f;

    private const float CandidateHalfExtentY = 5f;

    private const float PartialRouteGapMeters = 0.75f;

    private const float MaxDetourRatio = 2f;

    private const int IdleStatusRefreshMs = 5000;

    private const float SpotArrivalTolerance = 1f;

    private const float SpotMountMinMeters = 50f;

    private static readonly Random rng = new Random();

    public bool BreakTaken { get; private set; }

    private static (int minMs, int maxMs) PauseRangeMs()
    {
        var cfg = Plugin.Cfg;
        var minSec = Math.Max(0, cfg.HumanizerPauseMinSec);
        var maxSec = Math.Max(minSec, cfg.HumanizerPauseMaxSec);
        return (minMs: minSec * 1000, maxMs: maxSec * 1000);
    }

    private static (float min, float max) WanderRange()
    {
        var cfg = Plugin.Cfg;
        var minMeters = Math.Max(1, cfg.HumanizerWanderMinMeters);
        var maxMeters = Math.Max(minMeters, cfg.HumanizerWanderMaxMeters);
        return (min: minMeters, max: maxMeters);
    }

    protected override async Task Execute()
    {
        var label = plan.Place;
        var minutes = Math.Max(1, durationMs / 60000);
        AutoCommon.Diag($"Humanize start: {plan.Describe()}, break {durationMs / 1000}s");
        Svc.Chat.Print($"[FTA] Humanize: taking a ~{minutes}m break in {label}.");
        if (Svc.ClientState.TerritoryType != plan.TerritoryId)
        {
            var reached = false;
            await RunWithStatusPinned("Teleporting to " + label, async () =>
            {
                reached = await TeleportToTerritory(plan.TerritoryId, TeleportTarget(), "humanize-teleport", TeleportWatchdogMs);
            });
            if (!reached)
            {
                AutoCommon.Diag($"Humanize aborted: could not reach {label} (still in {Svc.ClientState.TerritoryType}).");
                return;
            }
        }
        BreakTaken = true;
        await WaitForNavmeshReady(NavmeshReadyWaitMs);
        if (CancelToken.IsCancellationRequested)
        {
            return;
        }
        var deadline = Environment.TickCount64 + durationMs;
        if (plan.Wander)
        {
            await Wander(label, deadline);
            return;
        }
        await Idle(label, deadline);
    }

    private Vector3 TeleportTarget()
    {
        var spot = plan.Spot;
        if (spot == null)
        {
            return Vector3.Zero;
        }
        if (!ZoneAetherytes.TryFindNearest(plan.TerritoryId, spot.Position, out var nearest))
        {
            return spot.Position;
        }
        return nearest.Position;
    }

    private bool LeftBreakTerritory()
    {
        if (Svc.ClientState.TerritoryType == plan.TerritoryId)
        {
            return false;
        }
        AutoCommon.Diag($"Humanize: territory changed to {Svc.ClientState.TerritoryType} (expected {plan.TerritoryId}); ending early.");
        return true;
    }

    private async Task Idle(string label, long deadline)
    {
        var spot = plan.Spot;
        if (spot != null)
        {
            await WalkToSpot(spot.Position, deadline);
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
        }
        await SafeDismount("humanize-idle-dismount");
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            if (LeftBreakTerritory())
            {
                return;
            }
            var secondsLeft = Math.Max(0L, (deadline - Environment.TickCount64) / 1000);
            Status = $"Idling in {label} (~{secondsLeft}s left)";
            await IdleFor(IdleStatusRefreshMs, deadline);
        }
        AutoCommon.Diag("Humanize done: idled in " + label + ".");
    }

    private async Task WalkToSpot(Vector3 spot, long deadline)
    {
        await RideAethernetShortcut(spot, "humanize-aethernet");
        if (CancelToken.IsCancellationRequested)
        {
            return;
        }
        Status = "Walking to the idle spot in " + plan.Place;
        var territoryId = plan.TerritoryId;
        if (!(await WalkWithRetries(() => new MoveOp((MoveOp o) => o.Move(territoryId, spot, SpotMovement(spot), () => Environment.TickCount64 >= deadline || CancelToken.IsCancellationRequested)), WalkWatchdogMs, "humanize-spot-walk", () => AutoCommon.WithinReach(spot, SpotArrivalTolerance) || Environment.TickCount64 >= deadline)) && !CancelToken.IsCancellationRequested)
        {
            AutoCommon.Diag($"Humanize: could not reach the idle spot {spot}; idling where the walk ended.");
        }
    }

    private static MovementConfig SpotMovement(Vector3 spot)
    {
        var localPlayer = Svc.Objects.LocalPlayer;
        var farAway = localPlayer != null && Vector3.Distance(localPlayer.Position, spot) >= SpotMountMinMeters;
        return (farAway ? MovementConfig.Everything : MovementConfig.Default).WithTolerance(SpotArrivalTolerance);
    }

    private async Task Wander(string label, long deadline)
    {
        await SafeDismount("humanize-dismount");
        var hops = 0;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            if (LeftBreakTerritory())
            {
                return;
            }
            var player = Svc.Objects.LocalPlayer;
            if (player == null)
            {
                await DelayMs(PlayerWaitPollMs);
                continue;
            }
            var dest = await PickRandomDestination(player.Position);
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            if (!dest.HasValue)
            {
                Status = "Idling in " + label;
                await IdleFor(1500, deadline);
                continue;
            }
            hops++;
            var secondsLeft = Math.Max(0L, (deadline - Environment.TickCount64) / 1000);
            Status = $"Wandering in {label} (~{secondsLeft}s left)";
            AutoCommon.Diag($"Humanize hop {hops}: walking {Vector3.Distance(player.Position, dest.Value):F0}m to {dest.Value}");
            var walkTimeoutMs = (int)Math.Min(WalkWatchdogMs, deadline - Environment.TickCount64);
            if (walkTimeoutMs < 4000)
            {
                break;
            }
            var op = new MoveOp((MoveOp o) => o.Move(plan.TerritoryId, dest.Value, MovementConfig.Default.WithTolerance(ArrivalTolerance), () => Environment.TickCount64 >= deadline || CancelToken.IsCancellationRequested));
            await RunCancellable(op, walkTimeoutMs, $"humanize-walk-{hops}", StuckDetector.MoveStallAbort($"humanize-walk-{hops}"));
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            if (Environment.TickCount64 >= deadline)
            {
                break;
            }
            (int minMs, int maxMs) pauseRange = PauseRangeMs();
            var pauseMinMs = pauseRange.minMs;
            var pauseMaxMs = pauseRange.maxMs;
            var pauseMs = ((pauseMinMs == pauseMaxMs) ? pauseMinMs : rng.Next(pauseMinMs, pauseMaxMs + 1));
            if (pauseMs > 0)
            {
                await IdleFor(pauseMs, deadline);
            }
        }
        AutoCommon.Diag($"Humanize done in {label} after {hops} hop(s).");
    }

    private async Task IdleFor(int pauseMs, long deadline)
    {
        var milliseconds = (int)Math.Min(pauseMs, Math.Max(0L, deadline - Environment.TickCount64));
        await DelayMs(milliseconds);
    }

    private async Task<Vector3?> PickRandomDestination(Vector3 from)
    {
        (float, float) wanderRange = WanderRange();
        var minR = wanderRange.Item1;
        var maxR = wanderRange.Item2;
        for (var attempt = 0; attempt < CandidateAttempts; attempt++)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return null;
            }
            var angle = rng.NextDouble() * Math.PI * 2.0;
            var radius = minR + (float)rng.NextDouble() * (maxR - minR);
            var position = new Vector3(from.X + (float)Math.Cos(angle) * radius, from.Y, from.Z + (float)Math.Sin(angle) * radius);
            var snapped = NavmeshIPC.Instance.NearestPointReachable(position, CandidateHalfExtentXZ, CandidateHalfExtentY);
            if (!snapped.HasValue || Vector3.Distance(snapped.Value, from) < minR * 0.5f)
            {
                continue;
            }
            var rejection = RejectRoute(await QueryWalkRoute(from, snapped.Value), from, snapped.Value);
            if (rejection == null)
            {
                return snapped;
            }
            AutoCommon.Diag($"Humanize candidate {snapped.Value} rejected: {rejection}");
        }
        return null;
    }

    private async Task<List<Vector3>?> QueryWalkRoute(Vector3 from, Vector3 to)
    {
        using CancellationTokenSource cancel = CancellationTokenSource.CreateLinkedTokenSource(CancelToken);
        cancel.CancelAfter(RouteQueryTimeoutMs);
        var pending = NavmeshIPC.Instance.Pathfind(from, to, fly: false, cancel.Token);
        if (pending == null)
        {
            return null;
        }
        while (!pending.IsCompleted)
        {
            if (cancel.IsCancellationRequested)
            {
                return null;
            }
            await NextFrame();
        }
        return pending.IsCompletedSuccessfully ? pending.Result : null;
    }

    private static string? RejectRoute(List<Vector3>? route, Vector3 from, Vector3 dest)
    {
        if (route == null || route.Count < 2)
        {
            return "no route";
        }
        var tailMeters = Vector3.Distance(route[^2], route[^1]);
        if (tailMeters > PartialRouteGapMeters)
        {
            return $"route stops {tailMeters:F1}m short of the target (partial path)";
        }
        var pathMeters = Vector3.Distance(from, route[0]);
        for (var i = 1; i < route.Count; i++)
        {
            pathMeters += Vector3.Distance(route[i - 1], route[i]);
        }
        var straightMeters = Vector3.Distance(from, dest);
        if (pathMeters > straightMeters * MaxDetourRatio)
        {
            return $"detour of {pathMeters:F0}m for {straightMeters:F0}m straight";
        }
        return null;
    }
}
