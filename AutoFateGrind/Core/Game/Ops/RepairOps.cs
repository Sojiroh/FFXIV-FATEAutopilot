using System;
using AutoFateGrind.Core.Game.Player;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Game.Ops;

internal static class RepairOps
{
    public record struct GcMender(uint TerritoryId, System.Numerics.Vector3 Position, uint DataId, string Name);

    public const uint RepairGeneralActionId = 6u;

    private const float ConditionPerPercent = 300f;

    private const uint MaxConditionRaw = 30000u;

    private const uint EquippedSlotCount = 13u;

    private const string RepairMenuKeyword = "repair";

    public unsafe static float LowestEquippedConditionPct()
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
        {
            return 100f;
        }
        var equipped = inventoryManager->GetInventoryContainer(InventoryType.EquippedItems);
        if (equipped == null || !equipped->IsLoaded)
        {
            return 100f;
        }
        var lowestCondition = MaxConditionRaw;
        var anyEquipped = false;
        for (var slot = 0u; slot < EquippedSlotCount; slot++)
        {
            var item = equipped->Items[slot];
            if (item.ItemId != 0)
            {
                anyEquipped = true;
                if (item.Condition < lowestCondition)
                {
                    lowestCondition = item.Condition;
                }
            }
        }
        if (!anyEquipped)
        {
            return 100f;
        }
        return (float)lowestCondition / ConditionPerPercent;
    }

    public static bool NeedsRepair(int thresholdPct) => LowestEquippedConditionPct() <= (float)thresholdPct;

    public unsafe static bool HasDarkMatterForAllEquipped()
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
        {
            return false;
        }
        var equipped = inventoryManager->GetInventoryContainer(InventoryType.EquippedItems);
        if (equipped == null || !equipped->IsLoaded)
        {
            return false;
        }
        var itemSheet = Svc.Data.GetExcelSheet<Item>();
        if (itemSheet == null)
        {
            return false;
        }
        for (var slot = 0u; slot < EquippedSlotCount; slot++)
        {
            var item = equipped->Items[slot];
            if (item.ItemId == 0 || !itemSheet.TryGetRow(item.ItemId, out var row))
            {
                continue;
            }
            var repairResource = row.ItemRepair.ValueNullable;
            if (!repairResource.HasValue)
            {
                continue;
            }
            var darkMatterId = repairResource.Value.Item.RowId;
            if (darkMatterId != 0 && !HasDarkMatterOrBetter(darkMatterId, inventoryManager))
            {
                return false;
            }
        }
        return true;
    }

    private unsafe static bool HasDarkMatterOrBetter(uint requiredDmId, InventoryManager* im)
    {
        var repairSheet = Svc.Data.GetExcelSheet<ItemRepairResource>();
        if (repairSheet == null)
        {
            return false;
        }
        foreach (var resource in repairSheet)
        {
            if (resource.Item.RowId >= requiredDmId && im->GetInventoryItemCount(resource.Item.RowId, isHq: false, checkEquipped: true, checkArmory: true, 0) > 0)
            {
                return true;
            }
        }
        return false;
    }

    public unsafe static bool TriggerRepairGeneralAction()
    {
        if (!EzThrottler.Throttle(AfgConstants.ThrottleKeys.RepairTrigger))
        {
            return false;
        }
        var actionManager = ActionManager.Instance();
        if (actionManager == null)
        {
            return false;
        }
        actionManager->UseAction(ActionType.GeneralAction, RepairGeneralActionId, 3758096384uL, 0u, ActionManager.UseActionMode.None, 0u, null);
        return true;
    }

    public unsafe static bool RepairAddonOpen() => GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.Repair, out var addon) && GenericHelpers.IsAddonReady(addon);

    public unsafe static bool SelectYesnoOpen() => GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.SelectYesno, out var addon) && GenericHelpers.IsAddonReady(addon);

    public unsafe static bool SelectIconStringOpen() => GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.SelectIconString, out var addon) && GenericHelpers.IsAddonReady(addon);

    public unsafe static bool ClickRepairAll()
    {
        if (!EzThrottler.Throttle(AfgConstants.ThrottleKeys.RepairAll))
        {
            return false;
        }
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.Repair, out var addon))
        {
            return false;
        }
        if (!GenericHelpers.IsAddonReady(addon))
        {
            return false;
        }
        new AddonMaster.Repair(addon).RepairAll();
        return true;
    }

    public unsafe static bool ClickSelectYesno()
    {
        if (!EzThrottler.Throttle(AfgConstants.ThrottleKeys.RepairYesno))
        {
            return false;
        }
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.SelectYesno, out var addon))
        {
            return false;
        }
        if (!GenericHelpers.IsAddonReady(addon))
        {
            return false;
        }
        new AddonMaster.SelectYesno(addon).Yes();
        return true;
    }

    public unsafe static bool ClickSelectIconString(int index)
    {
        if (!EzThrottler.Throttle(AfgConstants.ThrottleKeys.RepairIconString))
        {
            return false;
        }
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.SelectIconString, out var addon))
        {
            return false;
        }
        if (!GenericHelpers.IsAddonReady(addon))
        {
            return false;
        }
        var entries = new AddonMaster.SelectIconString(addon).Entries;
        if (index < 0 || index >= entries.Length)
        {
            return false;
        }
        entries[index].Select();
        return true;
    }

    public unsafe static int FindRepairMenuEntry()
    {
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.SelectIconString, out var addon))
        {
            return -1;
        }
        if (!GenericHelpers.IsAddonReady(addon))
        {
            return -1;
        }
        var entries = new AddonMaster.SelectIconString(addon).Entries;
        for (var i = 0; i < entries.Length; i++)
        {
            var entryText = entries[i].Text;
            if (!string.IsNullOrEmpty(entryText) && entryText.Contains(RepairMenuKeyword, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    public unsafe static void HideRepairAgent()
    {
        var repairAgent = AgentModule.Instance()->GetAgentByInternalId(AgentId.Repair);
        if (repairAgent == null)
        {
            return;
        }
        repairAgent->Hide();
    }

    public unsafe static GcMender? GetGrandCompanyMender()
    {
        var playerState = PlayerState.Instance();
        if (playerState == null)
        {
            return null;
        }
        return playerState->GrandCompany switch
        {
            GrandCompanyId.Maelstrom => (GcMender?)new GcMender(128u, new System.Numerics.Vector3(17.715698f, 40.200005f, 3.9520264f), 1003251u, "Maelstrom Mender"),
            GrandCompanyId.TwinAdder => new GcMender(132u, new System.Numerics.Vector3(24.826416f, -8f, 93.18677f), 1000394u, "Twin Adder Mender"),
            GrandCompanyId.ImmortalFlames => new GcMender(130u, new System.Numerics.Vector3(32.85266f, 6.999999f, -81.31531f), 1004416u, "Flame Mender"),
            _ => null,
        };
    }

    public static IGameObject? FindObjectByBaseId(uint baseId)
    {
        foreach (var gameObject in Svc.Objects)
        {
            if (gameObject.BaseId == baseId)
            {
                return gameObject;
            }
        }
        return null;
    }

    public static IGameObject? FindNearestObjectByBaseId(uint baseId)
    {
        IGameObject? nearest = null;
        var nearestDistance = float.MaxValue;
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        var playerPosition = localPlayer?.Position ?? System.Numerics.Vector3.Zero;
        foreach (var gameObject in Svc.Objects)
        {
            if (gameObject.BaseId == baseId)
            {
                var distance = localPlayer == null ? 0f : System.Numerics.Vector3.Distance(gameObject.Position, playerPosition);
                if (distance < nearestDistance)
                {
                    nearest = gameObject;
                    nearestDistance = distance;
                }
            }
        }
        return nearest;
    }

    public static GcMender? ResolveRepairNpc(out int repairIndex)
    {
        RepairNpc? preferredRepairNpc = Plugin.Cfg.PreferredRepairNpc;
        if (preferredRepairNpc != null)
        {
            repairIndex = preferredRepairNpc.RepairIndex;
            return new GcMender(preferredRepairNpc.TerritoryId, new System.Numerics.Vector3(preferredRepairNpc.X, preferredRepairNpc.Y, preferredRepairNpc.Z), preferredRepairNpc.DataId, preferredRepairNpc.Name);
        }
        repairIndex = 0;
        return GetGrandCompanyMender();
    }

    public unsafe static RepairNpc? CaptureCurrentTargetAsRepairNpc()
    {
        var target = TargetSystem.Instance()->Target;
        if (target == null)
        {
            return null;
        }
        var baseId = target->BaseId;
        string? npcName = Svc.Data.GetExcelSheet<ENpcResident>()?.GetRowOrDefault(baseId)?.Singular.ToString();
        if (string.IsNullOrEmpty(npcName))
        {
            npcName = target->NameString;
        }
        var position = target->Position;
        return new RepairNpc
        {
            TerritoryId = Svc.ClientState.TerritoryType,
            X = position.X,
            Y = position.Y,
            Z = position.Z,
            DataId = baseId,
            Name = npcName
        };
    }
}
