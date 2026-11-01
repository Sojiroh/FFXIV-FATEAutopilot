using System;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace AutoFateGrind.Core.Game.Yokai;

internal static class YokaiOps
{
    private const ushort WristsEquipSlot = 10;

    private const uint ActionReady = 0u;

    private static readonly InventoryType[] watchContainers = new InventoryType[]
    {
        InventoryType.ArmoryWrist,
        InventoryType.Inventory1,
        InventoryType.Inventory2,
        InventoryType.Inventory3,
        InventoryType.Inventory4
    };

    public unsafe static uint SummonedMinionId()
    {
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            return 0u;
        }
        var companionObject = ((Character*)localPlayer.Address)->CompanionData.CompanionObject;
        if (companionObject == null)
        {
            return 0u;
        }
        return companionObject->BaseId;
    }

    public unsafe static uint PendingCompanionId()
    {
        if (Svc.Objects.LocalPlayer is { } player)
        {
            return ((Character*)player.Address)->CompanionData.CompanionId;
        }
        return 0u;
    }

    public unsafe static int PlainMedalCount()
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager != null)
        {
            return inventoryManager->GetInventoryItemCount(YokaiCatalog.PlainMedalItemId, isHq: false, checkEquipped: true, checkArmory: true, 0);
        }
        return 0;
    }

    public static bool IsWatchEquipped()
    {
        ushort slot;
        return FindItem(InventoryType.EquippedItems, YokaiCatalog.WatchItemId, out slot);
    }

    public unsafe static bool OwnsWatch()
    {
        var inventoryManager = InventoryManager.Instance();
        return inventoryManager != null && inventoryManager->GetInventoryItemCount(YokaiCatalog.WatchItemId, isHq: false, checkEquipped: true, checkArmory: true, 0) > 0;
    }

    public unsafe static bool TrySummon(uint minionId)
    {
        var actionManager = ActionManager.Instance();
        if (actionManager == null)
        {
            return false;
        }
        if (actionManager->GetActionStatus(ActionType.Companion, minionId, 3758096384uL, checkRecastActive: true, checkCastingActive: true, null) != ActionReady)
        {
            return false;
        }
        return actionManager->UseAction(ActionType.Companion, minionId, 3758096384uL, 0u, ActionManager.UseActionMode.None, 0u, null);
    }

    public static bool IsFashionAccessoryDeployed() => Svc.Condition[ConditionFlag.UsingFashionAccessory];

    public static bool TryWithdrawFashionAccessory()
    {
        try
        {
            GameMain.ExecuteCommand(110);
            return true;
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "GameMain.ExecuteCommand withdraw fashion accessory failed");
            return false;
        }
    }

    public unsafe static bool CanIssueSummon()
    {
        var actionManager = ActionManager.Instance();
        if (actionManager == null || actionManager->AnimationLock > 0f)
        {
            return false;
        }
        var condition = Svc.Condition;
        return !condition[ConditionFlag.Casting] && !condition[ConditionFlag.Casting87] && !condition[ConditionFlag.BetweenAreas] && !condition[ConditionFlag.BetweenAreas51] && !condition[ConditionFlag.MountOrOrnamentTransition] && !condition[ConditionFlag.UsingFashionAccessory] && !condition[ConditionFlag.Occupied] && !condition[ConditionFlag.OccupiedInEvent] && !condition[ConditionFlag.OccupiedInCutSceneEvent] && !condition[ConditionFlag.Jumping];
    }

    public unsafe static bool TryEquipWatch()
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
        {
            return false;
        }
        for (var i = 0; i < watchContainers.Length; i++)
        {
            if (FindItem(watchContainers[i], YokaiCatalog.WatchItemId, out var slot))
            {
                inventoryManager->MoveItemSlot(watchContainers[i], slot, InventoryType.EquippedItems, WristsEquipSlot, a6: true);
                return true;
            }
        }
        return false;
    }

    private unsafe static bool FindItem(InventoryType containerType, uint itemId, out ushort slot)
    {
        slot = 0;
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
        {
            return false;
        }
        var container = inventoryManager->GetInventoryContainer(containerType);
        if (container == null || !container->IsLoaded)
        {
            return false;
        }
        for (var i = 0; i < container->Size; i++)
        {
            var inventorySlot = container->GetInventorySlot(i);
            if (inventorySlot != null && inventorySlot->ItemId == itemId)
            {
                slot = (ushort)i;
                return true;
            }
        }
        return false;
    }
}
