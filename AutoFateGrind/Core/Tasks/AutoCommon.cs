using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AutoFateGrind.Core.Ipc;
using Dalamud.Game.ClientState.Conditions;
using clib.Services;

namespace AutoFateGrind.Core.Tasks;

public abstract partial class AutoCommon : AutoTask
{
    protected static void Diag(string message, [CallerFilePath] string callerFile = "") => RunLog.Info(message, callerFile);

    protected static void Warn(string message, [CallerFilePath] string callerFile = "") => RunLog.Warning(message, callerFile);

    protected static void Trace(string message, [CallerFilePath] string callerFile = "") => RunLog.Debug(message, callerFile);

    protected async Task RunWithStatusPinned(string label, Func<Task> work)
    {
        Status = label;
        ECommons.DalamudServices.Svc.Framework.Update += Pin;
        try
        {
            await work();
        }
        finally
        {
            ECommons.DalamudServices.Svc.Framework.Update -= Pin;
        }
        void Pin(object _)
        {
            Status = label;
        }
    }

    protected const int SubTaskUnwindGraceMs = 5000;

    protected new async Task DelayMs(int milliseconds)
    {
        var deadline = Environment.TickCount64 + milliseconds;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            await NextFrame(DelayPollFrames);
        }
    }

    protected async Task<bool> WaitUntilTimed(Func<bool> condition, int timeoutMs, string scope, int checkFrames = 30)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        var threw = false;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return false;
            }
            bool satisfied;
            try
            {
                satisfied = condition();
            }
            catch (Exception ex)
            {
                if (!threw)
                {
                    Warn("WaitUntilTimed '" + scope + "' condition threw (treating as unsatisfied; will retry until timeout): " + ex.Message);
                    threw = true;
                }
                satisfied = false;
            }
            if (satisfied)
            {
                return true;
            }
            await NextFrame(checkFrames);
        }
        Diag($"WAIT TIMEOUT: '{scope}' not satisfied within {timeoutMs / 1000}s");
        return false;
    }

    protected async Task WaitForNavmeshReady(int timeoutMs, int pollFrames = 120)
    {
        if (NavmeshIPC.Instance.IsReady())
        {
            return;
        }
        var deadline = Environment.TickCount64 + timeoutMs;
        while (!NavmeshIPC.Instance.IsReady())
        {
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            if (Environment.TickCount64 >= deadline)
            {
                Diag($"WAIT TIMEOUT: navmesh not ready within {timeoutMs / 1000}s; proceeding anyway");
                return;
            }
            var buildProgress = NavmeshIPC.Instance.BuildProgress();
            Status = ((buildProgress >= 0f && buildProgress <= 1f) ? $"Please wait — navmesh is loading ({buildProgress * 100f:F0}%)" : "Please wait — navmesh is loading…");
            await NextFrame(pollFrames);
        }
    }

    private const int DelayPollFrames = 2;

    private const int MaxLandingSpots = 3;

    private const float LandingRingRadiusMeters = 6f;

    private const int LandingRingPoints = 6;

    private const int LandingCandidateCount = 7;

    private const float LandingFloorHalfExtentMeters = 3f;

    private const float LandingProbeLiftMeters = 30f;

    private const float LandingArriveMeters = 1.5f;

    private const int LandingFlightWatchdogMs = 20000;

    private const int LandingDismountWatchdogMs = 6000;

    private const int GroundDismountWatchdogMs = 8000;

    private const float LandingBackOffMeters = 8f;

    private const int LandingBackOffMs = 1500;

    internal const uint JumpGeneralActionId = 2u;

    internal const uint DismountGeneralActionId = 23u;

    private const int VerticalDescentWatchdogMs = 15000;

    private const int VerticalDescentReissueMs = 250;

    protected async Task<bool> SafeDismount(string scope)
    {
        if (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.Mounted])
        {
            return true;
        }
        if (!ECommons.DalamudServices.Svc.Condition[ConditionFlag.InFlight])
        {
            return await GroundDismount(scope);
        }
        var localPlayer = ECommons.DalamudServices.Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return false;
        }
        return await LandAndDismount(localPlayer.Position, scope);
    }
}
