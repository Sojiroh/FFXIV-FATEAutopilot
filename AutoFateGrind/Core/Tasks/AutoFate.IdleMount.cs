using System;
using AutoFateGrind.Core.Game.Player;
using AutoFateGrind.Core.Zones;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;

namespace AutoFateGrind.Core.Tasks;

public sealed partial class AutoFate
{
    private ulong ringChaseGivenUpTargetId;

    private const uint MountRouletteActionId = 9u;

    private const int MountRetryMs = 5000;

    private long idleMountFirstAttemptAtMs;

    private long nextIdleMountRetryAtMs;

    private bool idleMountRefusalLogged;

    private void BeginIdleMountPeriod()
    {
        int reactionDelayMs = Pacing.ReactionDelayMs();
        idleMountFirstAttemptAtMs = Environment.TickCount64 + reactionDelayMs;
        nextIdleMountRetryAtMs = 0L;
        idleMountRefusalLogged = false;
        if (reactionDelayMs > 0)
        {
            AutoCommon.Diag($"Pacing: idle mount; waiting {(double)reactionDelayMs / 1000.0:F1}s before the first request");
        }
    }

    private void EndIdleMountPeriod()
    {
        idleMountFirstAttemptAtMs = 0L;
        nextIdleMountRetryAtMs = 0L;
        idleMountRefusalLogged = false;
    }

    private void TryIdleMount()
    {
        if (!Plugin.Cfg.MountWhileWaitingForFates || ZoneSelection.IsYokaiGoal(Plugin.Cfg))
        {
            EndIdleMountPeriod();
            return;
        }
        if (idleMountFirstAttemptAtMs == 0L)
        {
            BeginIdleMountPeriod();
        }
        long tickCount = Environment.TickCount64;
        if (tickCount < idleMountFirstAttemptAtMs || tickCount < nextIdleMountRetryAtMs || !CanIdleMount() || Svc.Condition[ConditionFlag.Mounted] || Svc.Condition[ConditionFlag.RidingPillion])
        {
            return;
        }
        nextIdleMountRetryAtMs = tickCount + 5000;
        if (MountOps.TrySummonPreferred())
        {
            AutoCommon.Diag("Idle mount requested while waiting for FATEs (" + MountOps.NameOf(Plugin.Cfg.PreferredMountId) + ")");
            return;
        }
        uint? actionStatus = AutoCommon.GeneralActionStatus(9u);
        if (actionStatus == 0 && AutoCommon.UseGeneralAction(9u))
        {
            AutoCommon.Diag("Idle mount requested while waiting for FATEs");
        }
        else if (!idleMountRefusalLogged)
        {
            string refusalReason = !actionStatus.HasValue ? "ActionManager unavailable" : ((actionStatus == 0) ? "UseAction returned false" : $"action status {actionStatus}");
            AutoCommon.Diag("Idle mount refused while waiting for FATEs (" + refusalReason + ")");
            idleMountRefusalLogged = true;
        }
    }
}
