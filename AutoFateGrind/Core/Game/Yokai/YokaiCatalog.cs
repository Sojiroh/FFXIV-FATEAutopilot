namespace AutoFateGrind.Core.Game.Yokai;

internal readonly record struct YokaiEntry(uint MinionId, uint MedalItemId, uint WeaponItemId, uint[] ZoneIds);

internal static class YokaiCatalog
{
    public const uint WatchItemId = 15222u;

    public const uint PlainMedalItemId = 15167u;

    private const uint MiddleLaNoscea = 134u;

    private const uint LowerLaNoscea = 135u;

    private const uint WesternLaNoscea = 138u;

    private const uint UpperLaNoscea = 139u;

    private const uint WesternThanalan = 140u;

    private const uint CentralThanalan = 141u;

    private const uint EasternThanalan = 145u;

    private const uint SouthernThanalan = 146u;

    private const uint CentralShroud = 148u;

    private const uint EastShroud = 152u;

    private const uint SouthShroud = 153u;

    private const uint NorthShroud = 154u;

    private const uint OuterLaNoscea = 180u;

    private const uint CoerthasWesternHighlands = 397u;

    private const uint DravanianForelands = 398u;

    private const uint DravanianHinterlands = 399u;

    private const uint ChurningMists = 400u;

    private const uint SeaOfClouds = 401u;

    private const uint AzysLla = 402u;

    private const uint Fringes = 612u;

    private const uint RubySea = 613u;

    private const uint Yanxia = 614u;

    private const uint Peaks = 620u;

    private const uint Lochs = 621u;

    private const uint AzimSteppe = 622u;

    private const uint Jibanyan = 200u;

    private const uint Komasan = 201u;

    private const uint Whisper = 202u;

    private const uint Blizzaria = 203u;

    private const uint Kyubi = 204u;

    private const uint Komajiro = 205u;

    private const uint Manjimutt = 206u;

    private const uint Noko = 207u;

    private const uint Venoct = 208u;

    private const uint Shogunyan = 209u;

    private const uint Hovernyan = 210u;

    private const uint Robonyan = 211u;

    private const uint Usapyon = 212u;

    private const uint LordEnma = 390u;

    private const uint LordAnanta = 391u;

    private const uint Zazel = 392u;

    private const uint Damona = 393u;

    private static readonly uint[] HeavenswardZones = new uint[] { CoerthasWesternHighlands, DravanianForelands, DravanianHinterlands, ChurningMists, SeaOfClouds, AzysLla };

    private static readonly uint[] StormbloodZones = new uint[] { Fringes, RubySea, Yanxia, Peaks, Lochs, AzimSteppe };

    public static readonly YokaiEntry[] Entries = new YokaiEntry[]
    {
        new YokaiEntry(Jibanyan, 15168u, 15210u, new uint[] { CentralShroud, LowerLaNoscea, CentralThanalan }),
        new YokaiEntry(Komasan, 15169u, 15216u, new uint[] { EastShroud, WesternLaNoscea, EasternThanalan }),
        new YokaiEntry(Whisper, 15170u, 15212u, new uint[] { SouthShroud, UpperLaNoscea, SouthernThanalan }),
        new YokaiEntry(Blizzaria, 15171u, 15217u, new uint[] { NorthShroud, OuterLaNoscea, MiddleLaNoscea }),
        new YokaiEntry(Kyubi, 15172u, 15213u, new uint[] { WesternThanalan, CentralShroud, LowerLaNoscea }),
        new YokaiEntry(Komajiro, 15173u, 15219u, new uint[] { CentralThanalan, EastShroud, WesternLaNoscea }),
        new YokaiEntry(Manjimutt, 15174u, 15218u, new uint[] { EasternThanalan, SouthShroud, UpperLaNoscea }),
        new YokaiEntry(Noko, 15175u, 15220u, new uint[] { SouthernThanalan, NorthShroud, OuterLaNoscea }),
        new YokaiEntry(Venoct, 15176u, 15211u, new uint[] { MiddleLaNoscea, WesternThanalan, CentralShroud }),
        new YokaiEntry(Shogunyan, 15177u, 15221u, new uint[] { LowerLaNoscea, CentralThanalan, EastShroud }),
        new YokaiEntry(Hovernyan, 15178u, 15214u, new uint[] { WesternLaNoscea, EasternThanalan, SouthShroud }),
        new YokaiEntry(Robonyan, 15179u, 15215u, new uint[] { UpperLaNoscea, SouthernThanalan, NorthShroud }),
        new YokaiEntry(Usapyon, 15180u, 15209u, new uint[] { OuterLaNoscea, MiddleLaNoscea, WesternThanalan }),
        new YokaiEntry(LordEnma, 30805u, 30809u, StormbloodZones),
        new YokaiEntry(LordAnanta, 30804u, 30808u, HeavenswardZones),
        new YokaiEntry(Zazel, 30803u, 30807u, HeavenswardZones),
        new YokaiEntry(Damona, 30806u, 30810u, StormbloodZones)
    };

    public static int IndexOfMinion(uint minionId)
    {
        if (minionId == 0)
        {
            return -1;
        }
        for (var i = 0; i < Entries.Length; i++)
        {
            if (Entries[i].MinionId == minionId)
            {
                return i;
            }
        }
        return -1;
    }
}
