using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace AutoFateGrind.Core.Localization;

internal static class Loc
{
    private static readonly ushort[] NoGlyphs = new ushort[1];

    private static readonly Dictionary<string, string> UpperCache = new Dictionary<string, string>(StringComparer.Ordinal);

    private static readonly Dictionary<string, CompositeFormat> FormatCache = new Dictionary<string, CompositeFormat>(StringComparer.Ordinal);

    private static string directory = string.Empty;

    private static LanguageInfo current = Languages.English;

    private static CultureInfo culture = CultureInfo.InvariantCulture;

    private static StringCatalog catalog = StringCatalog.Empty;

    private static ushort[] catalogGlyphRanges = NoGlyphs;

    public static LanguageInfo Current => current;

    public static CultureInfo Culture => culture;

    public static ushort[] CatalogGlyphRanges => catalogGlyphRanges;

    public static void Initialize(string code, string localizationDirectory)
    {
        directory = localizationDirectory;
        Apply(Languages.Resolve(code));
    }

    public static void SetLanguage(string code)
    {
        var languageInfo = Languages.Resolve(code);
        if (languageInfo == current)
        {
            return;
        }
        Apply(languageInfo);
    }

    public static string Upper(string text)
    {
        if (UpperCache.TryGetValue(text, out var cached))
        {
            return cached;
        }
        var upper = culture.TextInfo.ToUpper(text);
        UpperCache[text] = upper;
        return upper;
    }

    public static string T(LocString entry)
    {
        if (!catalog.TryGet(entry.Key, out var translated))
        {
            return entry.Source;
        }
        return translated;
    }

    public static string T<T0>(LocString entry, T0 arg0) => string.Format(culture, FormatFor(T(entry)), arg0);

    public static string T<T0, T1>(LocString entry, T0 arg0, T1 arg1) => string.Format(culture, FormatFor(T(entry)), arg0, arg1);

    public static string T<T0, T1, T2>(LocString entry, T0 arg0, T1 arg1, T2 arg2) => string.Format(culture, FormatFor(T(entry)), arg0, arg1, arg2);

    public static string T(LocString entry, params object[] args) => string.Format(culture, T(entry), args);

    public static string Plural(LocPlural entry, int count) => string.Format(culture, FormatFor(PluralTemplate(entry, count)), count);

    public static string Plural<T1>(LocPlural entry, int count, T1 arg1) => string.Format(culture, FormatFor(PluralTemplate(entry, count)), count, arg1);

    public static string Plural<T1, T2>(LocPlural entry, int count, T1 arg1, T2 arg2) => string.Format(culture, FormatFor(PluralTemplate(entry, count)), count, arg1, arg2);

    private static string PluralTemplate(LocPlural entry, int count)
    {
        if (!IsOne(count))
        {
            return Resolve(entry.KeyBase + ".other", entry.OtherSource);
        }
        return Resolve(entry.KeyBase + ".one", entry.OneSource);
    }

    private static CompositeFormat FormatFor(string template)
    {
        if (FormatCache.TryGetValue(template, out var cached))
        {
            return cached;
        }
        var compositeFormat = CompositeFormat.Parse(template);
        FormatCache[template] = compositeFormat;
        return compositeFormat;
    }

    private static string Resolve(string key, string source)
    {
        if (!catalog.TryGet(key, out var translated))
        {
            return source;
        }
        return translated;
    }

    private static bool IsOne(int count)
    {
        var absoluteCount = Math.Abs(count);
        return current.PluralKind switch
        {
            PluralKind.French => absoluteCount is 0 or 1,
            _ => absoluteCount == 1,
        };
    }

    private static void Apply(LanguageInfo language)
    {
        current = language;
        culture = ResolveCulture(language.CultureName);
        UpperCache.Clear();
        FormatCache.Clear();
        var path = Path.Combine(directory, language.Code + ".json");
        catalog = ((language == Languages.English) ? StringCatalog.Empty : StringCatalog.Load(path));
        catalogGlyphRanges = ((language == Languages.English) ? NoGlyphs : StringCatalog.ScanGlyphRanges(path));
    }

    private static CultureInfo ResolveCulture(string name)
    {
        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException exception)
        {
            RunLog.Warning(exception, "Culture '" + name + "' is unavailable; falling back to the invariant culture");
            return CultureInfo.InvariantCulture;
        }
    }
}

internal readonly struct LocString(string key, string source)
{
    public readonly string Key = key;

    public readonly string Source = source;
}
