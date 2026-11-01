using System.Collections.Generic;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using Lumina.Excel.Sheets;
using clib.Utils;

namespace AutoFateGrind.Core.Game.Fates;

internal static class FateItems
{
    private static readonly Dictionary<uint, uint> turnInItemByFateId = new Dictionary<uint, uint>();

    public static uint TurnInItemId(uint fateId)
    {
        if (turnInItemByFateId.TryGetValue(fateId, out var cachedItemId))
        {
            return cachedItemId;
        }
        var fate = Svc.Data.GetExcelSheet<Fate>()?.GetRowOrDefault(fateId);
        var itemId = fate?.EventItem.RowId ?? 0;
        turnInItemByFateId[fateId] = itemId;
        return itemId;
    }

    public unsafe static int HandedInCount(PublicEvent fate)
    {
        if (fate.FateType != FateType.Normal || fate.Address == 0)
        {
            return 0;
        }
        return ((FateContext*)fate.Address)->HandInCount;
    }

    public unsafe static int HeldCount(uint eventItemId)
    {
        if (eventItemId == 0)
        {
            return 0;
        }
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
        {
            return 0;
        }
        var keyItems = inventoryManager->GetInventoryContainer(InventoryType.KeyItems);
        if (keyItems == null || !keyItems->IsLoaded)
        {
            return 0;
        }
        var held = 0;
        for (var i = 0; i < keyItems->Size; i++)
        {
            var slot = keyItems->GetInventorySlot(i);
            if (slot != null && slot->GetItemId() == eventItemId)
            {
                held += (int)slot->GetQuantity();
            }
        }
        return held;
    }
}
