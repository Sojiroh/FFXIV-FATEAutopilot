using System;
using System.Diagnostics;
using AutoFateGrind.Core.Game.Ops;
using AutoFateGrind.Core.Game.Yokai;
using AutoFateGrind.Core.Stats;
using AutoFateGrind.Core.Trading;
using ECommons.DalamudServices;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

namespace AutoFateGrind.Core.Localization;

internal static class GameTextGlyphs
{
    private static readonly bool[] present = new bool[GlyphRanges.CodepointCount];

    public static int Version { get; private set; }

    public static ReadOnlySpan<bool> Present => present;

    public static void Collect(Configuration configuration, RunHistory history)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            ScanSheets();
            ScanCatalogs();
            ScanSaved(configuration, history);
        }
        catch (Exception exception)
        {
            RunLog.Error(exception, "Failed to collect game text glyphs; some game names may render as missing glyphs");
            return;
        }
        RunLog.Info($"Collected {CountGlyphs()} game text glyphs in {stopwatch.ElapsedMilliseconds} ms");
    }

    public static void Add(string? text)
    {
        if (GlyphRanges.MarkText(present, text))
        {
            Version++;
        }
    }

    private static void ScanSheets()
    {
        ScanSheet((PlaceName row) => row.Name);
        ScanSheet((Fate row) => row.Name);
        ScanSheet((DynamicEvent row) => row.Name);
        ScanSheet((WKSMechaEventData row) => row.Name);
        ScanSheet((ClassJob row) => row.Abbreviation);
        ScanSheet((ClassJob row) => row.Name);
    }

    private static void ScanSheet<T>(Func<T, ReadOnlySeString> text) where T : struct, IExcelRow<T>
    {
        var sheet = Svc.Data.GetExcelSheet<T>();
        for (var i = 0; i < sheet.Count; i++)
        {
            Add(text(sheet.GetRowAt(i)).ExtractText());
        }
    }

    private static void ScanCatalogs()
    {
        var catalog = FoodOps.Catalog;
        for (var i = 0; i < catalog.Count; i++)
        {
            Add(catalog[i].Name);
        }
        var gemstoneItems = GemstoneCatalog.All;
        for (var j = 0; j < gemstoneItems.Length; j++)
        {
            Add(gemstoneItems[j].ItemName);
        }
        for (var k = 0; k < YokaiCatalog.Entries.Length; k++)
        {
            Add(YokaiProgress.MinionName(k));
        }
    }

    private static void ScanSaved(Configuration configuration, RunHistory history)
    {
        Add(configuration.PreferredRepairNpc?.Name);
        var humanizerIdleSpots = configuration.HumanizerIdleSpots;
        for (var i = 0; i < humanizerIdleSpots.Count; i++)
        {
            Add(humanizerIdleSpots[i].Name);
        }
        var autoConsumeItems = configuration.AutoConsumeItems;
        for (var j = 0; j < autoConsumeItems.Count; j++)
        {
            Add(autoConsumeItems[j].Name);
        }
        var records = history.Records;
        for (var k = 0; k < records.Count; k++)
        {
            var runRecord = records[k];
            Add(runRecord.JobAbbr);
            for (var l = 0; l < runRecord.ZoneNames.Count; l++)
            {
                Add(runRecord.ZoneNames[l]);
            }
        }
    }

    private static int CountGlyphs()
    {
        var glyphCount = 0;
        for (var i = 0; i < present.Length; i++)
        {
            if (present[i])
            {
                glyphCount++;
            }
        }
        return glyphCount;
    }
}
