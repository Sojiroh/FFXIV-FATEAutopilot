using System;
using System.Numerics;
using AutoFateGrind.Core.Ipc;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;

namespace AutoFateGrind.Core.Tasks;

internal static class StuckDetector
{
    internal const float StuckMoveThresholdMeters = 1.5f;

    internal const int IdleStallTimeoutMs = 8000;

    internal const int NavWedgeTimeoutMs = 3000;

    internal const float ProgressEpsilonMeters = 1f;

    private const int AirborneFreezeMs = 2000;

    private const float AirborneFreezeMeters = 0.25f;

    internal static bool IsPositionFrozenLegit() => Svc.Condition[ConditionFlag.Casting] || Svc.Condition[ConditionFlag.Casting87] || Svc.Condition[ConditionFlag.Mounting] || Svc.Condition[ConditionFlag.Mounting71] || Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51] || Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent] || Svc.Condition[ConditionFlag.WatchingCutscene] || Svc.Condition[ConditionFlag.WatchingCutscene78];

    internal static Func<bool> MoveStallAbort(string label)
    {
        var tracker = new MoveStallTracker();
        return () =>
        {
            var stallKind = tracker.Check();
            if (stallKind == StallKind.None)
            {
                return false;
            }
            RunLog.Info($"{label} stalled ({stallKind}); aborting the move");
            return true;
        };
    }

    internal static Func<bool> IdleStallAbort(int timeoutMs)
    {
        Vector3? anchor = null;
        var idleSinceMs = Environment.TickCount64;
        return () =>
        {
            var localPlayer = Svc.Objects.LocalPlayer;
            if (localPlayer == null)
            {
                return false;
            }
            var tickCount = Environment.TickCount64;
            var position = localPlayer.Position;
            if (!anchor.HasValue || Vector3.Distance(anchor.Value, position) > StuckMoveThresholdMeters || NavmeshIPC.Instance.IsBusy() || IsPositionFrozenLegit())
            {
                anchor = position;
                idleSinceMs = tickCount;
                return false;
            }
            return tickCount - idleSinceMs >= timeoutMs;
        };
    }

    internal static Func<bool> AirborneFreezeAbort(string label)
    {
        Vector3? anchor = null;
        var frozenSinceMs = Environment.TickCount64;
        return () =>
        {
            var tickCount = Environment.TickCount64;
            var localPlayer = Svc.Objects.LocalPlayer;
            if (localPlayer == null || !Svc.Condition[ConditionFlag.InFlight])
            {
                anchor = null;
                frozenSinceMs = tickCount;
                return false;
            }
            var position = localPlayer.Position;
            if (!anchor.HasValue || Vector3.Distance(anchor.Value, position) > AirborneFreezeMeters)
            {
                anchor = position;
                frozenSinceMs = tickCount;
                return false;
            }
            if (tickCount - frozenSinceMs < AirborneFreezeMs)
            {
                return false;
            }
            RunLog.Info($"{label} froze in the air for {AirborneFreezeMs}ms at {position}; aborting the descent");
            return true;
        };
    }
}
