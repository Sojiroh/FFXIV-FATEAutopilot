using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace AutoFateGrind.Core.Localization;

internal sealed class StringCatalog
{
    public static readonly StringCatalog Empty = new StringCatalog(new Dictionary<string, string>(0, StringComparer.Ordinal));

    private readonly Dictionary<string, string> entries;

    private StringCatalog(Dictionary<string, string> entries)
    {
        this.entries = entries;
    }

    public int Count => entries.Count;

    public bool TryGet(string key, out string value) => entries.TryGetValue(key, out value!);

    public bool Contains(string key) => entries.ContainsKey(key);

    public static StringCatalog Load(string path)
    {
        if (!File.Exists(path))
        {
            return Empty;
        }
        var target = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            Flatten(JObject.Parse(File.ReadAllText(path)), null, target);
        }
        catch (Exception exception)
        {
            RunLog.Error(exception, "Failed to load language catalog '" + path + "'");
            return Empty;
        }
        return new StringCatalog(target);
    }

    public static ushort[] ScanGlyphRanges(string path)
    {
        if (!File.Exists(path))
        {
            return new ushort[1];
        }
        string fileText;
        try
        {
            fileText = File.ReadAllText(path);
        }
        catch (Exception exception)
        {
            RunLog.Error(exception, "Failed to scan glyphs from '" + path + "'");
            return new ushort[1];
        }
        var present = new bool[GlyphRanges.CodepointCount];
        GlyphRanges.MarkText(present, fileText);
        return GlyphRanges.ToRanges(present);
    }

    private static void Flatten(JObject node, string? prefix, Dictionary<string, string> target)
    {
        foreach (var property in node.Properties())
        {
            var key = ((prefix == null) ? property.Name : (prefix + "." + property.Name));
            if (property.Value is JObject child)
            {
                Flatten(child, key, target);
            }
            else
            {
                target[key] = property.Value.ToString();
            }
        }
    }
}
