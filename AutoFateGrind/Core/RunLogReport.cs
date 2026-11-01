using System;
using System.Globalization;
using System.Text;
using Dalamud.Plugin;
using ECommons.DalamudServices;

namespace AutoFateGrind.Core;

public static class RunLogReport
{
    private const int MaxSourceWidth = 24;

    private const string DetailIndent = "    ";

    private const string Separator = " | ";

    private static readonly string[] levelNames = new string[RunLog.LevelCount] { "Verbose", "Debug", "Info", "Warning", "Error" };

    public static string WithHeader(ReadOnlySpan<RunLogLine> lines, in RunLogFilter filter, int bufferedCount)
    {
        var stringBuilder = new StringBuilder(256 + lines.Length * 112);
        AppendEnvironment(stringBuilder);
        AppendScope(stringBuilder, lines.Length, in filter, bufferedCount);
        stringBuilder.AppendLine();
        AppendLines(stringBuilder, lines);
        return stringBuilder.ToString();
    }

    public static string Plain(ReadOnlySpan<RunLogLine> lines)
    {
        var stringBuilder = new StringBuilder(lines.Length * 112);
        AppendLines(stringBuilder, lines);
        return stringBuilder.ToString();
    }

    private static void AppendEnvironment(StringBuilder builder)
    {
        var manifest = Svc.PluginInterface.Manifest;
        var version = typeof(IDalamudPluginInterface).Assembly.GetName().Version;
        var now = DateTimeOffset.Now;
        builder.Append(manifest.Name).Append(' ').Append(manifest.AssemblyVersion)
            .Append(Separator)
            .Append("Dalamud ")
            .Append(version)
            .Append(Separator)
            .Append(now.ToString("yyyy-MM-dd HH:mm:ss 'UTC'zzz", CultureInfo.InvariantCulture))
            .Append(Separator)
            .Append("Territory ")
            .Append(Svc.ClientState.TerritoryType)
            .AppendLine();
    }

    private static void AppendScope(StringBuilder builder, int shown, in RunLogFilter filter, int bufferedCount)
    {
        builder.Append(shown).Append(" of ").Append(bufferedCount)
            .Append(" lines");
        if (filter.LevelMask != RunLogFilter.AllLevels)
        {
            builder.Append(Separator).Append("levels: ");
            var isFirst = true;
            for (var i = 0; i < RunLog.LevelCount; i++)
            {
                if (filter.Shows((RunLogLevel)i))
                {
                    if (!isFirst)
                    {
                        builder.Append(", ");
                    }
                    builder.Append(levelNames[i]);
                    isFirst = false;
                }
            }
        }
        if (filter.Search.Length > 0)
        {
            builder.Append(Separator).Append("search: \"").Append(filter.Search)
                .Append('"');
        }
        if (filter.Source != null)
        {
            builder.Append(Separator).Append("source: ").Append(filter.Source);
        }
        builder.AppendLine();
    }

    private static void AppendLines(StringBuilder builder, ReadOnlySpan<RunLogLine> lines)
    {
        var widest = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            widest = Math.Max(widest, lines[i].Source.Length);
        }
        widest = Math.Min(widest, MaxSourceWidth);
        for (var j = 0; j < lines.Length; j++)
        {
            AppendLine(builder, in lines[j], widest);
        }
    }

    private static void AppendLine(StringBuilder builder, in RunLogLine line, int sourceWidth)
    {
        builder.Append(RunLog.Time(line.AtUtc)).Append(' ').Append(RunLog.Tag(line.Level))
            .Append(' ')
            .Append(line.Source.PadRight(sourceWidth))
            .Append("  ")
            .Append(line.Message);
        if (line.Repeat > 1)
        {
            builder.Append(" (x").Append(line.Repeat).Append(')');
        }
        builder.AppendLine();
        if (line.Detail == null)
        {
            return;
        }
        foreach (var detailLine in line.Detail.EnumerateLines())
        {
            builder.Append(DetailIndent).Append(detailLine).AppendLine();
        }
    }
}
