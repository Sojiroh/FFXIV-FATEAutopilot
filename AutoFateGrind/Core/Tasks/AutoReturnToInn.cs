using System;
using System.Numerics;
using System.Threading.Tasks;
using AutoFateGrind.Core.Game.Ops;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using clib.TaskSystem;

namespace AutoFateGrind.Core.Tasks;

public sealed class AutoReturnToInn : AutoCommon
{
    private readonly record struct Inn(uint CityTerritory, uint InnTerritory, uint InnkeeperDataId, Vector3 InnkeeperPos, string City);

    private const int TeleportWatchdogMs = 60000;

    private const int WalkWatchdogMs = 120000;

    private const int NavmeshReadyWaitMs = 60000;

    private const int EnterInnTimeoutMs = 60000;

    private const int ApproachWatchdogMs = 25000;

    private const int InteractWatchdogMs = 8000;

    private const int DialogPollMs = 250;

    private const int InteractRetryMs = 500;

    private const float ArrivalTolerance = 3.5f;

    private const float InnkeeperApproachDistance = 5f;

    private const float InnkeeperStepTolerance = 2.5f;

    private unsafe static Inn Resolve()
    {
        PlayerState* ptr = PlayerState.Instance();
        int grandCompany = (ptr == null) ? 0 : ptr->GrandCompany;
        return grandCompany switch
        {
            2 => new Inn(132u, 179u, 1000102u, new Vector3(25.6627f, -8f, 99.74237f), "Gridania"),
            3 => new Inn(130u, 178u, 1001976u, new Vector3(28.85994f, 6.999999f, -80.12716f), "Ul'dah"),
            _ => new Inn(128u, 177u, 1000974u, new Vector3(15.42688f, 39.99999f, 12.466553f), "Limsa Lominsa"),
        };
    }

    protected override async Task Execute()
    {
        var inn = Resolve();
        AutoCommon.Diag($"Return to inn: city {inn.City} (territory {inn.CityTerritory}), inn territory {inn.InnTerritory}.");
        if (Svc.ClientState.TerritoryType == inn.InnTerritory)
        {
            AutoCommon.Diag("Already inside the inn; nothing to do.");
            return;
        }
        Svc.Chat.Print("[FTA] Run complete — retiring to the inn in " + inn.City + ".");
        if (Svc.ClientState.TerritoryType != inn.CityTerritory)
        {
            var reached = false;
            await RunWithStatusPinned("Teleporting to " + inn.City, async () =>
            {
                reached = await TeleportToTerritory(inn.CityTerritory, inn.InnkeeperPos, "inn-teleport", TeleportWatchdogMs);
            });
            if (!reached)
            {
                AutoCommon.Diag($"Return to inn aborted: could not reach {inn.City} (still in {Svc.ClientState.TerritoryType}).");
                return;
            }
        }
        await WaitForNavmeshReady(NavmeshReadyWaitMs);
        if (CancelToken.IsCancellationRequested)
        {
            return;
        }
        await SafeDismount("inn-dismount");
        await RideAethernetShortcut(inn.InnkeeperPos, "inn-aethernet");
        if (CancelToken.IsCancellationRequested)
        {
            return;
        }
        Status = "Walking to the innkeeper";
        var op = new MoveOp((MoveOp o) => o.Move(inn.CityTerritory, inn.InnkeeperPos, MovementConfig.Everything.WithTolerance(ArrivalTolerance), () => CancelToken.IsCancellationRequested));
        await RunCancellable(op, WalkWatchdogMs, "inn-walk", StuckDetector.MoveStallAbort("inn-walk"));
        if (CancelToken.IsCancellationRequested)
        {
            return;
        }
        await EnterInn(inn);
    }

    private async Task EnterInn(Inn inn)
    {
        Status = "Entering the inn in " + inn.City;
        var deadline = Environment.TickCount64 + EnterInnTimeoutMs;
        while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
        {
            if (Svc.ClientState.TerritoryType == inn.InnTerritory)
            {
                AutoCommon.Diag("Reached the inn.");
                return;
            }
            if (NpcInteraction.DriveDialog())
            {
                await DelayMs(DialogPollMs);
                continue;
            }
            var npc = RepairOps.FindObjectByBaseId(inn.InnkeeperDataId);
            if (npc == null)
            {
                await DelayMs(DialogPollMs);
                continue;
            }
            var localPlayer = Svc.Objects.LocalPlayer;
            if (localPlayer != null && Vector3.Distance(localPlayer.Position, npc.Position) > InnkeeperApproachDistance)
            {
                var op = new MoveOp((MoveOp o) => o.Move(inn.CityTerritory, npc.Position, MovementConfig.Everything.WithTolerance(InnkeeperStepTolerance), () => CancelToken.IsCancellationRequested));
                await RunCancellable(op, ApproachWatchdogMs, "inn-approach", StuckDetector.MoveStallAbort("inn-approach"));
                continue;
            }
            var interact = new MoveOp((MoveOp o) => o.Interact(npc, NpcInteraction.DialogAddonOpen, UiSkipOptions.Talk));
            await RunCancellable(interact, InteractWatchdogMs, "inn-interact");
            var fault = interact.Fault;
            if (fault != null)
            {
                AutoCommon.Diag("Innkeeper interaction failed: " + fault.Message + "; retrying");
            }
            await DelayMs(InteractRetryMs);
        }
        AutoCommon.Diag("Return to inn: timed out before entering the inn room.");
    }
}
