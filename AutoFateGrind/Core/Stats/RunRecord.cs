using System;
using System.Collections.Generic;

namespace AutoFateGrind.Core.Stats;

[Serializable]
public sealed class RunRecord
{
    public DateTime StartedAtUtc { get; set; }

    public DateTime EndedAtUtc { get; set; }

    public double DurationSeconds { get; set; }

    public int FatesCompleted { get; set; }

    public int GemstonesEarned { get; set; }

    public long ExpEarned { get; set; }

    public int LevelsGained { get; set; }

    public int StartLevel { get; set; }

    public int EndLevel { get; set; }

    public uint JobId { get; set; }

    public string JobAbbr { get; set; } = "";

    public string ModeName { get; set; } = "";

    public List<string> ZoneNames { get; set; } = new List<string>();

    public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);

    public double FatesPerHour
    {
        get
        {
            if (!(DurationSeconds > 0.0))
            {
                return 0.0;
            }
            return (double)FatesCompleted / (DurationSeconds / 3600.0);
        }
    }

    public double ExpPerHour
    {
        get
        {
            if (!(DurationSeconds > 0.0))
            {
                return 0.0;
            }
            return (double)ExpEarned / (DurationSeconds / 3600.0);
        }
    }
}
