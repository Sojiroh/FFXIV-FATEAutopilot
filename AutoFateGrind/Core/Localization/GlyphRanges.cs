using System.Collections.Generic;

namespace AutoFateGrind.Core.Localization;

internal static class GlyphRanges
{
    public const int CodepointCount = 65536;

    public const int FirstNonAsciiCodepoint = 128;

    public static bool MarkText(bool[] present, string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }
        var anyAdded = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c >= FirstNonAsciiCodepoint && !char.IsSurrogate(c) && !present[(uint)c])
            {
                present[(uint)c] = true;
                anyAdded = true;
            }
        }
        return anyAdded;
    }

    public static void MarkRanges(bool[] present, ushort[]? ranges)
    {
        if (ranges == null)
        {
            return;
        }
        for (var i = 0; i + 1 < ranges.Length; i += 2)
        {
            if (ranges[i] == 0)
            {
                return;
            }
            for (int codepoint = ranges[i]; codepoint <= ranges[i + 1]; codepoint++)
            {
                present[codepoint] = true;
            }
        }
    }

    public static ushort[] ToRanges(bool[] present)
    {
        var ranges = new List<ushort>();
        var rangeStart = -1;
        for (var i = 1; i < CodepointCount; i++)
        {
            if (present[i])
            {
                if (rangeStart < 0)
                {
                    rangeStart = i;
                }
            }
            else if (rangeStart >= 0)
            {
                ranges.Add((ushort)rangeStart);
                ranges.Add((ushort)(i - 1));
                rangeStart = -1;
            }
        }
        if (rangeStart >= 0)
        {
            ranges.Add((ushort)rangeStart);
            ranges.Add(ushort.MaxValue);
        }
        ranges.Add(0);
        return ranges.ToArray();
    }
}
