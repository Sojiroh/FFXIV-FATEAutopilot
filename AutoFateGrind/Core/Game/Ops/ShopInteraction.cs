using ECommons;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoFateGrind.Core.Game.Ops;

internal static class ShopInteraction
{
    public unsafe static bool ShopExchangeCurrencyOpen() => GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.ShopExchangeCurrency, out var addon) && GenericHelpers.IsAddonReady(addon);

    public unsafe static bool SelectIconStringOpen() => GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.SelectIconString, out var addon) && GenericHelpers.IsAddonReady(addon);

    public unsafe static bool SelectYesnoOpen() => GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.SelectYesno, out var addon) && GenericHelpers.IsAddonReady(addon);

    public unsafe static int SelectIconStringEntryCount()
    {
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.SelectIconString, out var addon))
        {
            return 0;
        }
        if (!GenericHelpers.IsAddonReady(addon))
        {
            return 0;
        }
        return new AddonMaster.SelectIconString(addon).Entries.Length;
    }

    public unsafe static bool ClickSelectIconString(int index)
    {
        if (!EzThrottler.Throttle(AfgConstants.ThrottleKeys.ShopClickIconString))
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

    public unsafe static bool ClickSelectYesno()
    {
        if (!EzThrottler.Throttle(AfgConstants.ThrottleKeys.ShopClickYesno))
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

    public unsafe static int FindCurrencyShopSlot(uint targetItemId)
    {
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.ShopExchangeCurrency, out var addon))
        {
            return -1;
        }
        if (!GenericHelpers.IsAddonReady(addon))
        {
            return -1;
        }
        var shopMaster = new AddonMaster.ShopExchangeCurrency(addon);
        var shopItems = shopMaster.BasicShopItems;
        for (var i = 0; i < shopItems.Length; i++)
        {
            if (shopItems[i].ItemId == targetItemId)
            {
                return i;
            }
        }
        return -1;
    }

    public unsafe static bool BuyFromCurrencyShop(uint targetItemId, int quantity)
    {
        if (!EzThrottler.Throttle(AfgConstants.ThrottleKeys.ShopBuyCurrency))
        {
            return false;
        }
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.ShopExchangeCurrency, out var addon))
        {
            return false;
        }
        if (!GenericHelpers.IsAddonReady(addon))
        {
            return false;
        }
        var shopMaster = new AddonMaster.ShopExchangeCurrency(addon);
        var shopItems = shopMaster.BasicShopItems;
        foreach (var shopItemInfo in shopItems)
        {
            if (shopItemInfo.ItemId == targetItemId)
            {
                shopItemInfo.Select(quantity);
                RunLog.Info($"ShopExchangeCurrency.Select(item={targetItemId} qty={quantity} index={shopItemInfo.Index})");
                return true;
            }
        }
        return false;
    }

    public unsafe static bool CloseShop()
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.ShopExchangeCurrency, out var addon) && GenericHelpers.IsAddonReady(addon))
        {
            addon->Close(fireCallback: true);
            return true;
        }
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>(AfgConstants.AddonNames.Shop, out var shopAddon) && GenericHelpers.IsAddonReady(shopAddon))
        {
            shopAddon->Close(fireCallback: true);
            return true;
        }
        return false;
    }
}
