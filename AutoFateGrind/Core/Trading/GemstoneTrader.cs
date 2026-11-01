using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AutoFateGrind.Core.Zones;
using ECommons.DalamudServices;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Trading;

public sealed record TraderLocation(string Name, uint EnpcBaseId, uint TerritoryId, Vector3 Position, ExpansionKind Expansion, bool IsHub);

public enum TraderAvailability
{
    Reachable,
    NoSeller,
    AllLocked
}

public static class GemstoneTrader
{
    public static readonly TraderLocation[] Traders = new TraderLocation[24]
    {
        new TraderLocation("Siulmet", 1027385u, 813u, new Vector3(701.898f, 21.7958f, -45.9079f), ExpansionKind.ShB, IsHub: false),
        new TraderLocation("Zumutt", 1027497u, 814u, new Vector3(-482.859f, 417.19f, -629.042f), ExpansionKind.ShB, IsHub: false),
        new TraderLocation("Halden", 1027892u, 815u, new Vector3(-541.649f, 45.4565f, -217.395f), ExpansionKind.ShB, IsHub: false),
        new TraderLocation("Sul Lad", 1027665u, 816u, new Vector3(-253.926f, 40.324f, 466.046f), ExpansionKind.ShB, IsHub: false),
        new TraderLocation("Nacille", 1027709u, 817u, new Vector3(323.079f, 33.7629f, -162.036f), ExpansionKind.ShB, IsHub: false),
        new TraderLocation("Goushs Ooan", 1027766u, 818u, new Vector3(586.613f, 348.98f, -173.632f), ExpansionKind.ShB, IsHub: false),
        new TraderLocation("Gramsol", 1027998u, 819u, new Vector3(-6.7598f, -7.7f, 118.517f), ExpansionKind.ShB, IsHub: true),
        new TraderLocation("Pedronille", 1027538u, 820u, new Vector3(-32.8222f, 84.184f, 49.3019f), ExpansionKind.ShB, IsHub: true),
        new TraderLocation("Faezbroes", 1037484u, 956u, new Vector3(423.562f, 166.283f, -423.983f), ExpansionKind.EW, IsHub: false),
        new TraderLocation("Mahveydah", 1037635u, 957u, new Vector3(218.646f, 4.7637f, 658.839f), ExpansionKind.EW, IsHub: false),
        new TraderLocation("Zawawa", 1037724u, 958u, new Vector3(-426.858f, 22.3626f, 430.132f), ExpansionKind.EW, IsHub: false),
        new TraderLocation("Tradingway", 1037793u, 959u, new Vector3(19.6384f, -132.952f, -460.746f), ExpansionKind.EW, IsHub: false),
        new TraderLocation("Aisara", 1037909u, 961u, new Vector3(146.861f, 10.3859f, 100.747f), ExpansionKind.EW, IsHub: false),
        new TraderLocation("N-1499", 1038004u, 960u, new Vector3(468.98f, 437.002f, 330.62f), ExpansionKind.EW, IsHub: false),
        new TraderLocation("Gadfrid", 1037055u, 962u, new Vector3(78.3673f, 5.1423f, -36.7838f), ExpansionKind.EW, IsHub: true),
        new TraderLocation("Sajareen", 1037304u, 963u, new Vector3(-4.7563f, 0.9002f, -52.8448f), ExpansionKind.EW, IsHub: true),
        new TraderLocation("Tepli", 1048628u, 1187u, new Vector3(301.503f, -172.282f, -484.55f), ExpansionKind.DT, IsHub: false),
        new TraderLocation("Kunuhali", 1048778u, 1188u, new Vector3(-200.336f, 6.3003f, -522.772f), ExpansionKind.DT, IsHub: false),
        new TraderLocation("Rral Wuruq", 1048933u, 1189u, new Vector3(-381.561f, 23.5356f, -436.932f), ExpansionKind.DT, IsHub: false),
        new TraderLocation("Mitepe", 1049283u, 1190u, new Vector3(357.748f, -1.4908f, 469.419f), ExpansionKind.DT, IsHub: false),
        new TraderLocation("Toashana", 1049438u, 1191u, new Vector3(-258.373f, 30f, -594.195f), ExpansionKind.DT, IsHub: false),
        new TraderLocation("Clerk PX-0029", 1049528u, 1192u, new Vector3(30.3197f, 53.2f, 802.792f), ExpansionKind.DT, IsHub: false),
        new TraderLocation("Kajeel Ja", 1048383u, 1185u, new Vector3(-23.9029f, -10f, 105.344f), ExpansionKind.DT, IsHub: true),
        new TraderLocation("Beryl", 1049082u, 1186u, new Vector3(-198.779f, 0.9002f, -4.3314f), ExpansionKind.DT, IsHub: true)
    };

    private static Dictionary<uint, TraderLocation[]>? shopToTraders;

    private static readonly Dictionary<uint, uint> ShbFateShopFallback = new Dictionary<uint, uint>
    {
        { 1027998u, 1769957u },
        { 1027538u, 1769958u },
        { 1027385u, 1769959u },
        { 1027497u, 1769960u },
        { 1027892u, 1769961u },
        { 1027665u, 1769962u },
        { 1027709u, 1769963u },
        { 1027766u, 1769964u }
    };

    private static Dictionary<uint, TraderLocation[]> ShopToTraders => shopToTraders ?? (shopToTraders = BuildShopToTraders());

    private static Dictionary<uint, TraderLocation[]> BuildShopToTraders()
    {
        ExcelSheet<FateShop>? fateShopSheet = Svc.Data.GetExcelSheet<FateShop>();
        if (fateShopSheet == null)
        {
            return new Dictionary<uint, TraderLocation[]>();
        }
        Dictionary<uint, List<TraderLocation>> accum = new Dictionary<uint, List<TraderLocation>>();
        void Attach(uint shopId, TraderLocation trader)
        {
            if (shopId == 0)
            {
                return;
            }
            if (!accum.TryGetValue(shopId, out var shopTraders))
            {
                shopTraders = new List<TraderLocation>();
                accum[shopId] = shopTraders;
            }
            if (!shopTraders.Contains(trader))
            {
                shopTraders.Add(trader);
            }
        }
        var shopCountByTrader = new Dictionary<TraderLocation, int>();
        var traders = Traders;
        foreach (var trader in traders)
        {
            var shopCount = 0;
            if (fateShopSheet.GetRowOrDefault(trader.EnpcBaseId) is { } fateShopRow)
            {
                foreach (RowRef<SpecialShop> shop in fateShopRow.SpecialShop)
                {
                    if (shop.RowId != 0)
                    {
                        Attach(shop.RowId, trader);
                        shopCount++;
                    }
                }
            }
            if (shopCount == 0 && ShbFateShopFallback.TryGetValue(trader.EnpcBaseId, out var fallbackShopId))
            {
                Attach(fallbackShopId, trader);
                shopCount = 1;
            }
            shopCountByTrader[trader] = shopCount;
        }
        var uniqueShops = accum.Count;
        var mappedTraders = Enumerable.Count(shopCountByTrader, kv => kv.Value > 0);
        RunLog.Info($"Bicolor catalog: {mappedTraders}/{Traders.Length} traders mapped, {uniqueShops} unique shops.");
        foreach (var unmapped in shopCountByTrader.Where(kv => kv.Value == 0))
        {
            RunLog.Warning($"Bicolor trader {unmapped.Key.Name} (ENpcBase {unmapped.Key.EnpcBaseId}) mapped to 0 shops.");
        }
        return accum.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
    }

    public static TraderLocation? PickForItem(uint itemId, uint? preferTerritoryId, ExpansionKind? preferExpansion, out TraderAvailability availability)
    {
        availability = TraderAvailability.NoSeller;
        GemstoneTradeItem? tradeItem = GemstoneCatalog.FindById(itemId);
        if (tradeItem is null)
        {
            return null;
        }
        var sellers = SellersOf(tradeItem);
        if (sellers.Count == 0)
        {
            return null;
        }
        if (ZoneStateReader.AnyAetheryteAttuned())
        {
            sellers.RemoveAll(t => !ZoneStateReader.IsTerritoryUnlocked(t.TerritoryId));
            if (sellers.Count == 0)
            {
                availability = TraderAvailability.AllLocked;
                return null;
            }
        }
        availability = TraderAvailability.Reachable;
        if (preferTerritoryId is { } tid)
        {
            TraderLocation? territoryTrader = sellers.FirstOrDefault(t => t.TerritoryId == tid);
            if (territoryTrader is not null)
            {
                return territoryTrader;
            }
        }
        if (preferExpansion is { } exp)
        {
            TraderLocation? expansionHub = sellers.FirstOrDefault(t => t.Expansion == exp && t.IsHub);
            if (expansionHub is not null)
            {
                return expansionHub;
            }
            TraderLocation? expansionTrader = sellers.FirstOrDefault(t => t.Expansion == exp);
            if (expansionTrader is not null)
            {
                return expansionTrader;
            }
        }
        return sellers.FirstOrDefault(t => t.IsHub) ?? sellers[0];
    }

    private const int ListedZoneLimit = 3;

    public static bool Sells(TraderLocation trader, GemstoneTradeItem item)
    {
        for (var i = 0; i < item.ShopRowIds.Length; i++)
        {
            if (ShopToTraders.TryGetValue(item.ShopRowIds[i], out var shopTraders) && Array.IndexOf(shopTraders, trader) >= 0)
            {
                return true;
            }
        }
        return false;
    }

    public static string DescribeSellerZones(uint itemId)
    {
        GemstoneTradeItem? tradeItem = GemstoneCatalog.FindById(itemId);
        if (tradeItem is null)
        {
            return "";
        }
        var sellers = SellersOf(tradeItem);
        var zoneNames = new List<string>(ListedZoneLimit);
        for (var i = 0; i < sellers.Count && zoneNames.Count < ListedZoneLimit; i++)
        {
            var zoneName = TerritoryNames.Of(sellers[i].TerritoryId);
            if (!zoneNames.Contains(zoneName))
            {
                zoneNames.Add(zoneName);
            }
        }
        return string.Join(", ", zoneNames);
    }

    private static List<TraderLocation> SellersOf(GemstoneTradeItem item)
    {
        var sellers = new List<TraderLocation>();
        var shopRowIds = item.ShopRowIds;
        foreach (var shopRowId in shopRowIds)
        {
            if (!ShopToTraders.TryGetValue(shopRowId, out var shopTraders))
            {
                continue;
            }
            var traders = shopTraders;
            foreach (var trader in traders)
            {
                if (!sellers.Contains(trader))
                {
                    sellers.Add(trader);
                }
            }
        }
        return sellers;
    }
}

public readonly record struct TradeOrder(GemstoneTradeItem Item, int StopAtCount);

public sealed record TradePlan(TraderLocation Trader, TradeOrder[] Orders)
{
    public string DescribeItems()
    {
        var names = new string[Orders.Length];
        for (var i = 0; i < Orders.Length; i++)
        {
            names[i] = Orders[i].Item.ItemName;
        }
        return string.Join(", ", names);
    }
}
