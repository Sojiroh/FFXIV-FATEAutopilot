using System;
using System.Threading.Tasks;
using AutoFateGrind.Core.Game.Ops;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using clib.TaskSystem;

namespace AutoFateGrind.Core.Tasks;

public sealed class AutoRepair : AutoCommon
{
    private const int TeleportWatchdogMs = 60000;

    private const int MoveWatchdogMs = 120000;

    private const float WalkToleranceMeters = 4f;

    private const int InteractWaitMs = 15000;

    private const int RepairAddonWaitMs = 10000;

    private const int YesnoWaitMs = 5000;

    private const int RepairCompleteWaitMs = 30000;

    private const float SelfRepairSuccessMarginPct = 5f;

    private const float RepairCompleteConditionPct = 95f;

    protected override async Task Execute()
    {
        var cfg = Plugin.Cfg;
        ErrorIf(!cfg.AutoRepair, "Auto-repair is disabled.");
        var before = RepairOps.LowestEquippedConditionPct();
        AutoCommon.Diag($"AutoRepair start: lowest condition {before:F1}%, threshold {cfg.AutoRepairThresholdPct}%, mode {cfg.RepairMode}");
        Svc.Chat.Print($"[FTA] Repairing gear (lowest at {before:F0}%).");
        var allowSelf = cfg.RepairMode is RepairMode.SelfThenNpc or RepairMode.SelfOnly;
        var allowNpc = cfg.RepairMode is RepairMode.SelfThenNpc or RepairMode.NpcOnly;
        if (allowSelf && RepairOps.HasDarkMatterForAllEquipped())
        {
            if (await TrySelfRepair())
            {
                ReportDone(before);
                return;
            }
            AutoCommon.Diag("Self-repair did not bring condition above threshold; falling through.");
        }
        else if (allowSelf)
        {
            AutoCommon.Diag("Skipping self-repair: missing required Dark Matter for at least one equipped item.");
        }
        if (!allowNpc)
        {
            Svc.Chat.PrintError("[FTA] Repair: no Dark Matter and NPC fallback disabled; skipping.");
            return;
        }
        await RepairAtGcMender();
        ReportDone(before);
    }

    private void ReportDone(float before)
    {
        var repairedPct = RepairOps.LowestEquippedConditionPct();
        Svc.Chat.Print($"[FTA] Repair done. Lowest condition {before:F0}% → {repairedPct:F0}%.");
    }

    private async Task<bool> TrySelfRepair()
    {
        await SafeDismount("repair-dismount");
        Status = "Opening Repair";
        AutoCommon.Diag("Triggering Repair general action");
        if (!RepairOps.TriggerRepairGeneralAction())
        {
            AutoCommon.Diag("UseAction(GeneralAction, Repair) call returned without dispatching.");
            return false;
        }
        if (!(await WaitUntilTimed(RepairOps.RepairAddonOpen, RepairAddonWaitMs, "self-wait-repair-addon")))
        {
            AutoCommon.Diag("Repair addon never opened after Repair general action.");
            return false;
        }
        return await DriveRepairAddon();
    }

    private async Task RepairAtGcMender()
    {
        var gcMender = RepairOps.ResolveRepairNpc(out var repairIndex);
        ErrorIf(!gcMender.HasValue, "Auto-repair NPC fallback needs a Grand Company affiliation or a custom repair NPC. Join a Grand Company, set a custom repair NPC, or enable Self-only repair.");
        var m = gcMender!.Value;
        AutoCommon.Diag($"NPC repair: heading to {m.Name} (territory {m.TerritoryId}, DataId {m.DataId})");
        if (Svc.ClientState.TerritoryType != m.TerritoryId)
        {
            var reached = false;
            await RunWithStatusPinned("Teleporting to " + m.Name, async () =>
            {
                reached = await TeleportToTerritory(m.TerritoryId, m.Position, "repair-teleport", TeleportWatchdogMs);
            });
            ErrorIf(!reached, $"Could not reach {m.Name}'s zone (still in {Svc.ClientState.TerritoryType}); aborting repair.");
        }
        await RunWithStatusPinned("Walking to " + m.Name, async () =>
        {
            var menderPos = m.Position;
            var menderTerr = m.TerritoryId;
            await RideAethernetShortcut(menderPos, "repair-aethernet");
            await WalkWithRetries(() => new MoveOp((MoveOp o) => o.Move(menderTerr, menderPos, MovementConfig.Everything.WithTolerance(WalkToleranceMeters), null)), MoveWatchdogMs, "repair-walk", () => AutoCommon.WithinReach(menderPos, WalkToleranceMeters));
        });
        await SafeDismount("repair-dismount-mender");
        var npc = RepairOps.FindObjectByBaseId(m.DataId);
        ErrorIf(npc == null, $"Could not find {m.Name} (BaseId {m.DataId}) near {m.Position}.");
        Status = "Talking to " + m.Name;
        AutoCommon.Diag("Interacting with " + m.Name);
        var interact = new MoveOp((MoveOp o) => o.Interact(npc!, () => RepairOps.RepairAddonOpen() || RepairOps.SelectIconStringOpen(), UiSkipOptions.Talk));
        await RunCancellable(interact, InteractWaitMs, "repair-interact");
        ErrorIf(interact.Fault is not null, "Interacting with " + m.Name + " failed: " + interact.Fault?.Message);
        ErrorIf(!(await WaitUntilTimed(() => RepairOps.RepairAddonOpen() || RepairOps.SelectIconStringOpen(), RepairAddonWaitMs, "npc-wait-menu-or-repair")), $"{m.Name} did not respond with a repair menu within {RepairAddonWaitMs / 1000}s.");
        if (!RepairOps.RepairAddonOpen() && RepairOps.SelectIconStringOpen())
        {
            var detectedIndex = RepairOps.FindRepairMenuEntry();
            var menuIndex = ((detectedIndex >= 0) ? detectedIndex : repairIndex);
            AutoCommon.Diag($"Talk menu open; selecting repair entry {menuIndex} (auto-detected: {detectedIndex >= 0}).");
            ErrorIf(!RepairOps.ClickSelectIconString(menuIndex), $"Could not select repair option {menuIndex} in {m.Name}'s menu.");
            ErrorIf(!(await WaitUntilTimed(RepairOps.RepairAddonOpen, RepairAddonWaitMs, "npc-wait-repair-after-menu")), m.Name + " did not open the Repair window after menu selection.");
        }
        await DriveRepairAddon();
    }

    private async Task<bool> DriveRepairAddon()
    {
        Status = "Repairing gear";
        if (!RepairOps.ClickRepairAll())
        {
            AutoCommon.Diag("ClickRepairAll returned false; addon disappeared between checks.");
            RepairOps.HideRepairAgent();
            return false;
        }
        if (!(await WaitUntilTimed(RepairOps.SelectYesnoOpen, YesnoWaitMs, "wait-repair-yesno")))
        {
            AutoCommon.Diag("SelectYesno never appeared — RepairAll button likely disabled (no materials/affordability).");
            RepairOps.HideRepairAgent();
            return false;
        }
        if (!RepairOps.ClickSelectYesno())
        {
            AutoCommon.Diag("ClickSelectYesno returned false; addon disappeared between checks.");
            RepairOps.HideRepairAgent();
            return false;
        }
        var deadline = Environment.TickCount64 + RepairCompleteWaitMs;
        var sawAnim = false;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return false;
            }
            if (Svc.Condition[ConditionFlag.Occupied39])
            {
                sawAnim = true;
            }
            else if (sawAnim)
            {
                break;
            }
            if (!RepairOps.RepairAddonOpen() && RepairOps.LowestEquippedConditionPct() > RepairCompleteConditionPct)
            {
                break;
            }
            await NextFrame(60);
        }
        RepairOps.HideRepairAgent();
        return RepairOps.LowestEquippedConditionPct() > (float)Plugin.Cfg.AutoRepairThresholdPct + SelfRepairSuccessMarginPct;
    }
}
