using System;
using System.Numerics;
using System.Threading.Tasks;
using AutoFateGrind.Core.Game.Fates;
using AutoFateGrind.Core.Game.Ops;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using clib.Extensions;
using clib.TaskSystem;
using clib.Utils;

namespace AutoFateGrind.Core.Tasks;

public sealed partial class AutoFate
{
    private async Task ActivateFate(PublicEvent fate)
    {
        uint fateId = fate.Id;
        string fateName = fate.Name;
        Status = "Activating " + fateName;
        AutoCommon.Diag($"FATE {fateId} ({fateName}) in Preparation, heading to MotivationNpc {fate.MotivationNpcId:X}");
        try
        {
            IGameObject? gameObject = await WaitForStarterNpc(fateId);
            if (gameObject == null)
            {
                return;
            }
            await WalkToStarterNpc(fateId, fateName, gameObject.Position);
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                if (CancelToken.IsCancellationRequested || !AwaitingNpcStart(fateId))
                {
                    return;
                }
                if (!(await PrepareToTalk(fateId)))
                {
                    continue;
                }
                IGameObject? gameObject2 = ResolveStarterNpc(fateId);
                if (gameObject2 == null)
                {
                    AutoCommon.Diag($"Starter NPC for FATE {fateId} is gone or untargetable (attempt {attempt}/{3})");
                    await DelayMs(1000);
                }
                else if (await OpenNpcDialog(fateId, gameObject2, attempt, () => !AwaitingNpcStart(fateId), "activate"))
                {
                    if (await DriveStarterDialog(fateId, fateName))
                    {
                        return;
                    }
                }
            }
            if (!AwaitingNpcStart(fateId))
            {
                return;
            }
            AutoCommon.Diag($"FATE {fateId} ({fateName}) did not start after {3} NPC interactions; blacklisting for this session");
            sessionStuckFateIds.Add(fateId);
        }
        catch (Exception ex)
        {
            AutoCommon.Diag("ActivateFate caught: " + ex.Message);
        }
    }

    private static bool AwaitingNpcStart(uint fateId)
    {
        PublicEvent? fateById = PublicEvent.GetFateById(fateId);
        if (fateById != null)
        {
            return FateScanner.AwaitsNpcStart(fateById);
        }
        return false;
    }

    private static IGameObject? ResolveStarterNpc(uint fateId)
    {
        PublicEvent? fateById = PublicEvent.GetFateById(fateId);
        if (fateById == null)
        {
            return null;
        }
        IGameObject? motivationNpc = fateById.MotivationNpc;
        if (motivationNpc == null || !motivationNpc.IsTargetable)
        {
            return null;
        }
        return motivationNpc;
    }

    private async Task<IGameObject?> WaitForStarterNpc(uint fateId)
    {
        long deadline = Environment.TickCount64 + 30000;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested || !AwaitingNpcStart(fateId))
            {
                return null;
            }
            IGameObject? gameObject = ResolveStarterNpc(fateId);
            if (gameObject != null)
            {
                return gameObject;
            }
            await NextFrame(30);
        }
        AutoCommon.Diag($"NPC for FATE {fateId} never spawned within {30}s; skipping it until another FATE completes");
        sessionStuckFateIds.Add(fateId);
        DeferSkipUntilNextCompletion(fateId);
        return null;
    }

    private async Task WalkToStarterNpc(uint fateId, string fateName, Vector3 npcPos)
    {
        string label = "Activating " + fateName;
        await WalkWithRetries(() => new MoveOp((MoveOp o) => o.Move(zone.TerritoryId, npcPos, StarterNpcMovementConfig(npcPos), () =>
        {
            Status = label;
            return !AwaitingNpcStart(fateId);
        })), 60000, $"activate-move-{fateId}", () => !AwaitingNpcStart(fateId) || AutoCommon.WithinReach(npcPos, 3f));
    }

    private static MovementConfig StarterNpcMovementConfig(Vector3 npcPos)
    {
        MovementConfig config = MovementConfig.Everything.WithTolerance(3f);
        if (Svc.Condition[ConditionFlag.Mounted])
        {
            return config;
        }
        if (Svc.Condition[ConditionFlag.InCombat])
        {
            return MovementConfig.InteractRange;
        }
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer != null && Vector3.Distance(localPlayer.Position, npcPos) < 30f)
        {
            return MovementConfig.InteractRange;
        }
        return config;
    }

    private async Task<bool> PrepareToTalk(uint fateId)
    {
        if (Svc.Condition[ConditionFlag.InCombat])
        {
            await ClearBlockingCombat();
        }
        await SafeDismount($"dismount-activate-{fateId}");
        if (await WaitUntilTimed(NpcInteraction.PlayerReady, 10000, $"activate-ready-{fateId}", 2))
        {
            return true;
        }
        AutoCommon.Diag($"Cannot talk to the NPC for FATE {fateId} yet ({NpcInteraction.DescribeBlockers()}); retrying");
        return false;
    }

    private async Task<bool> OpenNpcDialog(uint fateId, IGameObject npc, int attempt, Func<bool> abandon, string scope)
    {
        if (!npc.IsInInteractRange())
        {
            Vector3 npcPos = npc.Position;
            string label = $"{scope}-approach-{fateId}";
            MoveOp op = new MoveOp((MoveOp o) => o.MoveInZone(npcPos, MovementConfig.InteractRange.WithTolerance(2f), () => npc.IsInInteractRange() || abandon()));
            await RunCancellable(op, 20000, label, StuckDetector.MoveStallAbort(label));
            if (!npc.IsInInteractRange())
            {
                AutoCommon.Diag($"Could not get within interact range of the NPC for FATE {fateId} (attempt {attempt}/{3})");
                return false;
            }
        }
        NpcInteraction.Target(npc);
        await WaitUntilTimed(() => NpcInteraction.IsTargeted(npc), 500, $"{scope}-target-{fateId}", 1);
        ulong result = NpcInteraction.Interact(npc);
        if (result == 0L)
        {
            AutoCommon.Diag($"The game rejected the interaction with NPC {npc.GameObjectId:X} for FATE {fateId} (attempt {attempt}/{3}, blockers: {NpcInteraction.DescribeBlockers()}); retrying");
            await DelayMs(1000);
            return false;
        }
        if (await WaitUntilTimed(() => NpcInteraction.DialogOpen() || abandon(), 2500, $"{scope}-dialog-{fateId}", 2))
        {
            AutoCommon.Trace($"NPC dialog for FATE {fateId} opened (interaction result {result}, attempt {attempt})");
            return true;
        }
        AutoCommon.Diag($"Interaction with NPC {npc.GameObjectId:X} for FATE {fateId} opened no dialog (result {result}, attempt {attempt}/{3}, blockers: {NpcInteraction.DescribeBlockers()}); retrying");
        await DelayMs(1000);
        return false;
    }

    private async Task<bool> DriveStarterDialog(uint fateId, string fateName)
    {
        Status = "Talking to the NPC for " + fateName;
        long deadline = Environment.TickCount64 + 30000;
        long lastDialogSeenMs = Environment.TickCount64;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return true;
            }
            PublicEvent? fateById = PublicEvent.GetFateById(fateId);
            if (fateById == null)
            {
                return true;
            }
            if (!FateScanner.AwaitsNpcStart(fateById))
            {
                if (fateById.State == FateState.Running)
                {
                    AutoCommon.Diag($"FATE {fateId} ({fateName}) started via its NPC");
                }
                await DismissLingeringDialog();
                return true;
            }
            bool dialogActive = NpcInteraction.DriveDialog() || NpcInteraction.DialogOpen();
            if (dialogActive)
            {
                lastDialogSeenMs = Environment.TickCount64;
            }
            else if (Environment.TickCount64 - lastDialogSeenMs > 2000)
            {
                AutoCommon.Diag($"NPC dialog for FATE {fateId} closed without starting it; retrying");
                return false;
            }
            await NextFrame(2);
        }
        AutoCommon.Diag($"NPC dialog for FATE {fateId} did not start it within {30}s; retrying");
        return false;
    }

    private async Task DismissLingeringDialog()
    {
        long deadline = Environment.TickCount64 + 3000;
        while (Environment.TickCount64 < deadline && NpcInteraction.DialogOpen())
        {
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            if (!NpcInteraction.DriveDialog())
            {
                NpcInteraction.CancelRequestDialog();
            }
            await NextFrame(2);
        }
    }

    private static int HandInBatch => Math.Max(1, Plugin.Cfg.CollectHandInBatch);
}
