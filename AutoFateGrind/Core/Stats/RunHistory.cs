using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AutoFateGrind.Core.Stats;

internal sealed class RunHistory
{
    public struct LifetimeTotals
    {
        public int Runs;

        public int Fates;

        public int Gemstones;

        public long Exp;

        public int Levels;

        public double Seconds;

        public readonly double FatesPerHour
        {
            get
            {
                if (!(Seconds > 0.0))
                {
                    return 0.0;
                }
                return (double)Fates / (Seconds / 3600.0);
            }
        }

        public readonly TimeSpan Duration => TimeSpan.FromSeconds(Seconds);
    }

    private const int MaxRecords = 500;

    public List<RunRecord> Records { get; private set; } = new List<RunRecord>();

    public LifetimeTotals Lifetime { get; private set; }

    public RunHistory()
    {
        Load();
        RecomputeLifetime();
    }

    private static string FilePath => Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, FileName);

    private void Load()
    {
        try
        {
            var filePath = FilePath;
            if (File.Exists(filePath))
            {
                List<RunRecord>? loaded = JsonConvert.DeserializeObject<List<RunRecord>>(File.ReadAllText(filePath));
                if (loaded != null)
                {
                    Records = loaded;
                }
            }
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "RunHistory load failed; starting empty");
        }
    }

    public void Append(RunRecord record)
    {
        Records.Add(record);
        Records.Sort((RunRecord a, RunRecord b) => b.EndedAtUtc.CompareTo(a.EndedAtUtc));
        if (Records.Count > MaxRecords)
        {
            Records.RemoveRange(MaxRecords, Records.Count - MaxRecords);
        }
        RecomputeLifetime();
        Save();
    }

    private const string FileName = "run-history.json";

    public void Clear()
    {
        Records.Clear();
        RecomputeLifetime();
        Save();
    }

    private void RecomputeLifetime()
    {
        var totals = new LifetimeTotals
        {
            Runs = Records.Count
        };
        foreach (var record in Records)
        {
            totals.Fates += record.FatesCompleted;
            totals.Gemstones += record.GemstonesEarned;
            totals.Exp += record.ExpEarned;
            totals.Levels += record.LevelsGained;
            totals.Seconds += record.DurationSeconds;
        }
        Lifetime = totals;
    }

    private void Save()
    {
        try
        {
            var configDirectory = Plugin.PluginInterface.ConfigDirectory;
            if (!configDirectory.Exists)
            {
                configDirectory.Create();
            }
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(Records, Formatting.Indented));
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "RunHistory save failed");
        }
    }
}
