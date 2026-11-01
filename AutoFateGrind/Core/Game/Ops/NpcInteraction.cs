using System.Text;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoFateGrind.Core.Game.Ops;

internal static class NpcInteraction
{
    private readonly record struct BlockingFlag(ConditionFlag Flag, string Label);

    private static readonly BlockingFlag[] BlockingFlags = new BlockingFlag[]
    {
        new BlockingFlag(ConditionFlag.Mounted, "mounted"),
        new BlockingFlag(ConditionFlag.MountOrOrnamentTransition, "mount transition"),
        new BlockingFlag(ConditionFlag.Jumping, "jumping"),
        new BlockingFlag(ConditionFlag.Jumping61, "airborne"),
        new BlockingFlag(ConditionFlag.InCombat, "in combat"),
        new BlockingFlag(ConditionFlag.Casting, "casting")
    };

    private const int RequestFirstSlot = 0;

    private const int RequestOpenSlotMenuEvent = 2;

    private const int RequestMenuHandOverEntry = 1021003;

    public static bool PlayerReady()
    {
        if (Svc.Objects.LocalPlayer == null)
        {
            return false;
        }
        for (var i = 0; i < BlockingFlags.Length; i++)
        {
            if (Svc.Condition[BlockingFlags[i].Flag])
            {
                return false;
            }
        }
        return !ECommons.GameHelpers.Player.IsAnimationLocked && !GenericHelpers.IsOccupied() && ECommons.GameHelpers.Player.Interactable;
    }

    public static string DescribeBlockers()
    {
        var blockers = new StringBuilder();
        for (var i = 0; i < BlockingFlags.Length; i++)
        {
            if (Svc.Condition[BlockingFlags[i].Flag])
            {
                AppendBlocker(blockers, BlockingFlags[i].Label);
            }
        }
        if (ECommons.GameHelpers.Player.IsAnimationLocked)
        {
            AppendBlocker(blockers, "animation lock");
        }
        if (GenericHelpers.IsOccupied())
        {
            AppendBlocker(blockers, "occupied");
        }
        if (!ECommons.GameHelpers.Player.Interactable)
        {
            AppendBlocker(blockers, "player not interactable");
        }
        if (blockers.Length != 0)
        {
            return blockers.ToString();
        }
        return "no blocker detected";
    }

    private static void AppendBlocker(StringBuilder builder, string label)
    {
        if (builder.Length > 0)
        {
            builder.Append(", ");
        }
        builder.Append(label);
    }

    public static bool IsTargeted(IGameObject npc) => Svc.Targets.Target?.GameObjectId == npc.GameObjectId;

    public static void Target(IGameObject npc)
    {
        Svc.Targets.Target = npc;
    }

    public unsafe static ulong Interact(IGameObject npc) => TargetSystem.Instance()->InteractWithObject(npc.Struct(), checkLineOfSight: false);

    public static bool DialogOpen() => Svc.Condition[ConditionFlag.OccupiedInQuestEvent] || Svc.Condition[ConditionFlag.OccupiedInEvent] || DialogAddonOpen();

    public unsafe static bool DialogAddonOpen() => AddonReady(AfgConstants.AddonNames.Talk, out var addon) || AddonReady(AfgConstants.AddonNames.SelectYesno, out addon) || AddonReady(AfgConstants.AddonNames.SelectString, out addon) || RequestDialogOpen();

    public unsafe static bool RequestDialogOpen()
    {
        AtkUnitBase* addon;
        return AddonReady(AfgConstants.AddonNames.Request, out addon);
    }

    public unsafe static bool AddonOpen(string name)
    {
        AtkUnitBase* addon;
        return AddonReady(name, out addon);
    }

    public unsafe static bool DriveRequestDialog(bool fillOurselves)
    {
        if (!AddonReady(AfgConstants.AddonNames.Request, out var addon))
        {
            return false;
        }
        var request = new AddonMaster.Request(addon);
        if (request.IsHandOverEnabled)
        {
            if (EzThrottler.Throttle(AfgConstants.ThrottleKeys.RequestHandOver))
            {
                request.HandOver();
            }
            return true;
        }
        if (!fillOurselves)
        {
            return true;
        }
        if (AddonReady(AfgConstants.AddonNames.ContextIconMenu, out var contextMenu) && contextMenu->IsVisible)
        {
            if (EzThrottler.Throttle(AfgConstants.ThrottleKeys.RequestFillPick))
            {
                Callback.Fire(contextMenu, false, 0, 0, RequestMenuHandOverEntry, 0, 0);
            }
            return true;
        }
        if (EzThrottler.Throttle(AfgConstants.ThrottleKeys.RequestFillOpen))
        {
            Callback.Fire(addon, false, RequestOpenSlotMenuEvent, RequestFirstSlot, 0, 0);
        }
        return true;
    }

    public unsafe static bool CancelRequestDialog()
    {
        if (!AddonReady(AfgConstants.AddonNames.Request, out var addon))
        {
            return false;
        }
        if (EzThrottler.Throttle(AfgConstants.ThrottleKeys.RequestHandOver))
        {
            new AddonMaster.Request(addon).Cancel();
        }
        return true;
    }

    public unsafe static bool DriveDialog()
    {
        if (AddonReady(AfgConstants.AddonNames.SelectString, out var addon))
        {
            if (EzThrottler.Throttle(AfgConstants.ThrottleKeys.NpcSelectString))
            {
                SelectFirstEntry(addon);
            }
            return true;
        }
        if (AddonReady(AfgConstants.AddonNames.SelectYesno, out var yesNo))
        {
            if (EzThrottler.Throttle(AfgConstants.ThrottleKeys.NpcSelectYesno))
            {
                new AddonMaster.SelectYesno(yesNo).Yes();
            }
            return true;
        }
        if (AddonReady(AfgConstants.AddonNames.Talk, out var talk))
        {
            if (EzThrottler.Throttle(AfgConstants.ThrottleKeys.NpcTalk))
            {
                new AddonMaster.Talk(talk).Click();
            }
            return true;
        }
        return false;
    }

    private unsafe static void SelectFirstEntry(AtkUnitBase* selectString)
    {
        var selectStringMaster = new AddonMaster.SelectString(selectString);
        if (selectStringMaster.EntryCount == 0)
        {
            return;
        }
        var entry = selectStringMaster.Entries[0];
        RunLog.Debug("SelectString: choosing '" + entry.Text + "'");
        entry.Select();
    }

    private unsafe static bool AddonReady(string name, out AtkUnitBase* addon) => GenericHelpers.TryGetAddonByName<AtkUnitBase>(name, out addon) && GenericHelpers.IsAddonReady(addon);
}
