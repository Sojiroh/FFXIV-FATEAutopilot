namespace AutoFateGrind.Core.Zones;

public static class ExpansionKindExtensions
{
    public static string DisplayName(this ExpansionKind exp)
    {
        return exp switch
        {
            ExpansionKind.ARR => "A Realm Reborn 2.0",
            ExpansionKind.HW => "Heavensward 3.0",
            ExpansionKind.SB => "Stormblood 4.0",
            ExpansionKind.ShB => "Shadowbringers 5.0",
            ExpansionKind.EW => "Endwalker 6.0",
            ExpansionKind.DT => "Dawntrail 7.0",
            _ => exp.ToString(),
        };
    }

    public static string ShortName(this ExpansionKind exp)
    {
        return exp switch
        {
            ExpansionKind.ARR => "A Realm Reborn",
            ExpansionKind.HW => "Heavensward",
            ExpansionKind.SB => "Stormblood",
            ExpansionKind.ShB => "Shadowbringers",
            ExpansionKind.EW => "Endwalker",
            ExpansionKind.DT => "Dawntrail",
            _ => exp.ToString(),
        };
    }

    public static ExpansionKind FromExVersion(uint exVersion)
    {
        return exVersion switch
        {
            0u => ExpansionKind.ARR,
            1u => ExpansionKind.HW,
            2u => ExpansionKind.SB,
            3u => ExpansionKind.ShB,
            4u => ExpansionKind.EW,
            5u => ExpansionKind.DT,
            _ => ExpansionKind.DT,
        };
    }
}
