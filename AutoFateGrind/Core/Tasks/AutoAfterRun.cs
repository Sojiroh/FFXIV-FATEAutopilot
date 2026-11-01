using System;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoFateGrind.Core.Tasks;

public sealed class AutoAfterRun(AfterRunAction action, string commandBlock) : AutoCommon
{
    private readonly AfterRunAction action = action;

    private readonly string commandBlock = commandBlock;

    private const int ReadyWaitMs = 20000;

    private const int YesnoWaitMs = 6000;

    private const int PreCommandSettleMs = 800;

    protected override async Task Execute()
    {
        if (!(await WaitUntilTimed(IsSafeToFinish, ReadyWaitMs, "afterrun-ready")))
        {
            if (!CancelToken.IsCancellationRequested)
            {
                AutoCommon.Warn($"After-run chat commands and {action} skipped: no safe state (combat/casting/zone transition) within {ReadyWaitMs / 1000}s.");
            }
            return;
        }
        await SendPostRunCommands();
        if (CancelToken.IsCancellationRequested)
        {
            return;
        }
        switch (action)
        {
            case AfterRunAction.Logout:
                Status = "Logging out";
                AutoCommon.Diag("After-run: logging out.");
                await DelayMs(PreCommandSettleMs);
                Chat.ExecuteCommand("/logout");
                if (await WaitUntilTimed(SelectYesnoOpen, YesnoWaitMs, "logout-yesno"))
                {
                    ClickYes();
                    AutoCommon.Diag("Logout confirmation accepted.");
                    break;
                }
                AutoCommon.Warn($"Logout confirmation did not appear within {YesnoWaitMs / 1000}s; re-issuing /logout.");
                await DelayMs(PreCommandSettleMs);
                Chat.ExecuteCommand("/logout");
                if (await WaitUntilTimed(SelectYesnoOpen, YesnoWaitMs, "logout-yesno-retry"))
                {
                    ClickYes();
                }
                else
                {
                    AutoCommon.Warn("Logout confirmation still absent after retry; character may remain logged in.");
                }
                break;
            case AfterRunAction.CloseGame:
                Status = "Closing the game";
                AutoCommon.Diag("After-run: closing the game (/xlkill).");
                await DelayMs(PreCommandSettleMs);
                Chat.ExecuteCommand("/xlkill");
                break;
        }
    }

    private async Task SendPostRunCommands()
    {
        var lines = commandBlock.Split('\n');
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            var commandLine = lines[lineIndex].Trim();
            if (commandLine.Length != 0)
            {
                if (!commandLine.StartsWith('/'))
                {
                    AutoCommon.Warn("Post-run line skipped, commands must start with '/': " + commandLine);
                    continue;
                }
                Status = "Sending chat commands";
                SendPostRunCommand(commandLine);
                await DelayMs(AfgConstants.PostRunCommandSpacingMs);
            }
        }
    }

    private static void SendPostRunCommand(string command)
    {
        try
        {
            AutoCommon.Diag("Post-run command: " + command);
            Chat.ExecuteCommand(command);
        }
        catch (Exception exception)
        {
            RunLog.Error(exception, "Post-run command '" + command + "' failed.");
        }
    }

    private static bool IsSafeToFinish() => Svc.Objects.LocalPlayer != null && !Svc.Condition[ConditionFlag.InCombat] && !Svc.Condition[ConditionFlag.BetweenAreas] && !Svc.Condition[ConditionFlag.Casting];

    private unsafe static bool SelectYesnoOpen() => GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.SelectYesno, out var AddonPtr) && GenericHelpers.IsAddonReady(AddonPtr);

    private unsafe static void ClickYes()
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.SelectYesno, out var AddonPtr) && GenericHelpers.IsAddonReady(AddonPtr))
        {
            new AddonMaster.SelectYesno((nint)AddonPtr).Yes();
        }
    }
}
