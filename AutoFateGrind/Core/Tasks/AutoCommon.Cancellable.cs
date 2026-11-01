using System;
using System.Threading.Tasks;

namespace AutoFateGrind.Core.Tasks;

public abstract partial class AutoCommon
{
    private const int AethernetShortcutMs = 90000;

    internal async Task<bool> RunCancellable(MoveOp op, int timeoutMs, string label, Func<bool>? abortIf = null)
    {
        var tcs = new TaskCompletionSource();
        op.Run(() =>
        {
            tcs.TrySetResult();
        });
        var work = tcs.Task;
        using var reg = CancelToken.Register(() =>
        {
            TryCancel(op, label);
        });
        var deadline = Environment.TickCount64 + timeoutMs;
        var aborted = false;
        var abortThrew = false;
        while (!work.IsCompleted && Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested)
            {
                break;
            }
            if (abortIf != null)
            {
                bool shouldAbort;
                try
                {
                    shouldAbort = abortIf();
                }
                catch (Exception ex)
                {
                    if (!abortThrew)
                    {
                        Warn("RunCancellable '" + label + "' abortIf threw (abort disabled, watchdog still active): " + ex.Message);
                        abortThrew = true;
                    }
                    shouldAbort = false;
                }
                if (shouldAbort)
                {
                    aborted = true;
                    break;
                }
            }
            await NextFrame(4);
        }
        if (work.IsCompleted)
        {
            return true;
        }
        if (CancelToken.IsCancellationRequested)
        {
            TryCancel(op, label);
            return false;
        }
        Diag(aborted ? ("RunCancellable '" + label + "' aborting sub-task (abort condition met)") : $"WATCHDOG: '{label}' exceeded {timeoutMs / 1000}s; cancelling sub-task");
        TryCancel(op, label);
        var grace = Environment.TickCount64 + SubTaskUnwindGraceMs;
        while (!work.IsCompleted && Environment.TickCount64 < grace)
        {
            await NextFrame(4);
        }
        if (!work.IsCompleted)
        {
            Diag($"WATCHDOG: '{label}' did not unwind {SubTaskUnwindGraceMs / 1000}s after Cancel (unexpected)");
        }
        return false;
    }

    private void TryCancel(MoveOp op, string label)
    {
        try
        {
            op.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception ex)
        {
            Warn("RunCancellable '" + label + "' Cancel threw: " + ex.Message);
        }
    }
}
