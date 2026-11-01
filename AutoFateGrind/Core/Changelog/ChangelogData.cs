using System;
using AutoFateGrind.Core.Localization;

namespace AutoFateGrind.Core.Changelog;

internal static class ChangelogData
{
    public static readonly ChangelogEntry[] Entries = new ChangelogEntry[]
    {
        new ChangelogEntry("2.22.0.0", "2026-10-21", L.Changelog.Release22200),
        new ChangelogEntry("2.21.3.0", "2026-10-21", L.Changelog.Release22130),
        new ChangelogEntry("2.21.2.0", "2026-10-19", L.Changelog.Release22120),
        new ChangelogEntry("2.21.1.0", "2026-10-18", L.Changelog.Release22110),
        new ChangelogEntry("2.21.0.0", "2026-10-18", L.Changelog.Release22100),
        new ChangelogEntry("2.20.0.0", "2026-10-17", L.Changelog.Release22000),
        new ChangelogEntry("2.19.0.0", "2026-10-13", L.Changelog.Release21900),
        new ChangelogEntry("2.18.0.0", "2026-10-10", L.Changelog.Release21800),
        new ChangelogEntry("2.17.0.0", "2026-09-29", L.Changelog.Release21700),
        new ChangelogEntry("2.16.0.0", "2026-09-29", L.Changelog.Release21600),
        new ChangelogEntry("2.15.0.0", "2026-09-27", L.Changelog.Release21500),
        new ChangelogEntry("2.14.0.0", "2026-09-27", L.Changelog.Release21400),
        new ChangelogEntry("2.13.0.0", "2026-09-24", L.Changelog.Release21300)
    };

    public static string LatestVersion => Entries[0].Version;

    public static int UnseenCount(string lastSeenVersion)
    {
        for (var i = 0; i < Entries.Length; i++)
        {
            if (string.Equals(Entries[i].Version, lastSeenVersion, StringComparison.Ordinal))
            {
                return i;
            }
        }
        return Entries.Length;
    }
}
