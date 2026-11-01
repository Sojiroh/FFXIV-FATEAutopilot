using System;
using System.Threading.Tasks;

namespace AutoFateGrind.Core.Tasks;

public abstract partial class AutoCommon
{
    private const int WalkAttempts = 3;

    private const float WalkArrivalSlackMeters = 2f;

    internal async Task<bool> WalkWithRetries(Func<MoveOp> makeOp, int watchdogMs, string label, Func<bool> arrived)
    {
        for (var attempt = 1; attempt <= WalkAttempts; attempt++)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return false;
            }
            var scope = $"{label}#{attempt}";
            var op = makeOp();
            var completed = await RunCancellable(op, watchdogMs, scope, StuckDetector.MoveStallAbort(scope));
            if (arrived())
            {
                return true;
            }
            if (CancelToken.IsCancellationRequested)
            {
                return false;
            }
            var fault = op.Fault;
            if (fault != null)
            {
                Diag(scope + " faulted: " + fault.Message + "; re-pathing from here");
            }
            else if (completed)
            {
                Diag(scope + " ended short of the destination; re-pathing from here");
            }
            else
            {
                Diag(scope + " stalled; re-pathing from here");
            }
        }
        return arrived();
    }
}
