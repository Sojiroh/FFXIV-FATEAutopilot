using System;
using System.IO;
using AutoFateGrind.Core;
using AutoFateGrind.Core.Localization;
using Dalamud;
using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoFateGrind.Windows;

internal static class Fonts
{
    private sealed class NoOpScope : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private const float CaptionScale = 1f;

    private const float BodyScale = 1.125f;

    private const float HeadlineScale = 1.25f;

    private const float TitleScale = 1.625f;

    private const float IconScale = 1.125f;

    private const float IconLargeScale = 1.625f;

    private const float IconDisplayScale = 2.25f;

    private const string LatinFontFile = "NotoSans-Medium-Latin.ttf";

    private const int LatinBlocksEnd = 879;

    private const int LatinAdditionalStart = 7680;

    private const int LatinAdditionalEnd = 7935;

    private const int NotoCjkJapaneseFace = 0;

    private const int NotoCjkSimplifiedChineseFace = 2;

    private static readonly ushort[] LatinBlocks = [32, 255, 256, 383, 384, 591, 8192, 8303];

    private static readonly ushort[] SymbolBlocks = [8592, 8703, 8704, 8959];

    private static readonly NoOpScope noOp = new NoOpScope();

    private static IUiBuilder? builder;

    private static IFontAtlas? atlas;

    private static byte[]? latinFont;

    private static ushort[] latinRanges = new ushort[1];

    private static ushort[] mergeRanges = new ushort[1];

    private static int cjkFace = 0;

    private static float unitPx = UiBuilder.DefaultFontSizePx;

    private static int bakedGameTextVersion;

    private static IFontHandle? body;

    private static IFontHandle? title;

    private static IFontHandle? headline;

    private static IFontHandle? caption;

    private static IFontHandle? icon;

    private static IFontHandle? iconLarge;

    public static void Initialize(IUiBuilder uiBuilder, string pluginDirectory)
    {
        builder = uiBuilder;
        atlas = uiBuilder.FontAtlas;
        latinFont = LoadLatinFont(Path.Combine(pluginDirectory, "Fonts", LatinFontFile));
        RefreshRanges();
        body = TextHandle(BodyScale);
        title = TextHandle(TitleScale);
        headline = TextHandle(HeadlineScale);
        caption = TextHandle(CaptionScale);
        icon = IconHandle(IconScale);
        iconLarge = IconHandle(IconLargeScale);
        iconDisplay = IconHandle(IconDisplayScale);
        uiBuilder.DefaultFontChanged += Rebuild;
        uiBuilder.Draw += RebuildOnNewGameText;
        if (atlas.AutoRebuildMode == FontAtlasAutoRebuildMode.Disable)
        {
            atlas.BuildFontsAsync();
        }
    }

    public static void OnLanguageChanged()
    {
        Rebuild();
    }

    public static void Dispose()
    {
        if (builder != null)
        {
            builder.DefaultFontChanged -= Rebuild;
            builder.Draw -= RebuildOnNewGameText;
        }
        body?.Dispose();
        title?.Dispose();
        headline?.Dispose();
        caption?.Dispose();
        icon?.Dispose();
        iconLarge?.Dispose();
        iconDisplay?.Dispose();
        body = (title = (headline = (caption = (icon = (iconLarge = (iconDisplay = null))))));
        builder = null;
        atlas = null;
        latinFont = null;
    }

    public static IDisposable PushBody() => body?.Push() ?? noOp;

    public static IDisposable PushTitle() => title?.Push() ?? noOp;

    public static IDisposable PushHeadline() => headline?.Push() ?? noOp;

    public static IDisposable PushCaption() => caption?.Push() ?? noOp;

    public static IDisposable PushIcon() => icon?.Push() ?? ImRaii.PushFont(UiBuilder.IconFont);

    public static IDisposable PushIconLarge() => iconLarge?.Push() ?? ImRaii.PushFont(UiBuilder.IconFont);

    public static IDisposable PushIconDisplay() => iconDisplay?.Push() ?? ImRaii.PushFont(UiBuilder.IconFont);

    public static IDisposable PushIconFor(float targetHeight)
    {
        var scaledUnit = unitPx * ImGuiHelpers.GlobalScale;
        if (targetHeight > scaledUnit * IconLargeScale)
        {
            return PushIconDisplay();
        }
        if (targetHeight > scaledUnit * IconScale)
        {
            return PushIconLarge();
        }
        return PushIcon();
    }

    private static byte[]? LoadLatinFont(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
            RunLog.Warning("Latin font missing at '" + path + "'; falling back to the Dalamud default font, Latin Extended letters will not render");
        }
        catch (Exception exception)
        {
            RunLog.Error(exception, "Failed to read the Latin font");
        }
        return null;
    }

    private static IFontHandle TextHandle(float scale)
    {
        return atlas!.NewDelegateFontHandle((IFontAtlasBuildToolkit e) =>
        {
            e.OnPreBuild((IFontAtlasBuildToolkitPreBuild tk) =>
            {
                var sizePx = unitPx * scale;
                var baseFont = (latinFont != null) ? tk.AddFontFromMemory(latinFont, new SafeFontConfig
                {
                    SizePx = sizePx,
                    GlyphRanges = latinRanges
                }, LatinFontFile) : tk.AddDalamudDefaultFont(sizePx, latinRanges);
                tk.Font = baseFont;
                if (mergeRanges.Length <= 1)
                {
                    return;
                }
                tk.AddDalamudAssetFont(DalamudAsset.NotoSansCjkRegular, new SafeFontConfig
                {
                    SizePx = sizePx,
                    GlyphRanges = mergeRanges,
                    MergeFont = baseFont,
                    FontNo = cjkFace
                });
            });
        });
    }

    private static IFontHandle IconHandle(float scale)
    {
        return atlas!.NewDelegateFontHandle((IFontAtlasBuildToolkit e) =>
        {
            e.OnPreBuild((IFontAtlasBuildToolkitPreBuild tk) =>
            {
                tk.AddFontAwesomeIconFont(new SafeFontConfig
                {
                    SizePx = unitPx * scale
                });
            });
        });
    }

    private static IFontHandle? iconDisplay;

    private static void RebuildOnNewGameText()
    {
        if (bakedGameTextVersion != GameTextGlyphs.Version)
        {
            Rebuild();
        }
    }

    private static void Rebuild()
    {
        RefreshRanges();
        if (atlas != null)
        {
            atlas.BuildFontsAsync();
        }
    }

    private static void RefreshRanges()
    {
        unitPx = builder?.DefaultFontSpec.SizePx ?? UiBuilder.DefaultFontSizePx;
        var latinGlyphs = new bool[GlyphRanges.CodepointCount];
        var mergeGlyphs = new bool[GlyphRanges.CodepointCount];
        var extraGlyphs = new bool[GlyphRanges.CodepointCount];
        GlyphRanges.MarkRanges(latinGlyphs, LatinBlocks);
        GlyphRanges.MarkRanges(mergeGlyphs, SymbolBlocks);
        GlyphRanges.MarkRanges(extraGlyphs, Loc.Current.ExtraGlyphRanges);
        GlyphRanges.MarkRanges(extraGlyphs, Loc.CatalogGlyphRanges);
        MarkNativeNames(extraGlyphs);
        ReadOnlySpan<bool> present = GameTextGlyphs.Present;
        for (var i = GlyphRanges.FirstNonAsciiCodepoint; i < GlyphRanges.CodepointCount; i++)
        {
            if (!latinGlyphs[i] && (extraGlyphs[i] || present[i]))
            {
                if (IsLatinCodepoint(i))
                {
                    latinGlyphs[i] = true;
                }
                else
                {
                    mergeGlyphs[i] = true;
                }
            }
        }
        latinRanges = GlyphRanges.ToRanges(latinGlyphs);
        mergeRanges = GlyphRanges.ToRanges(mergeGlyphs);
        cjkFace = ((Loc.Current == Languages.Chinese) ? NotoCjkSimplifiedChineseFace : NotoCjkJapaneseFace);
        bakedGameTextVersion = GameTextGlyphs.Version;
    }

    private static bool IsLatinCodepoint(int codepoint)
    {
        if (codepoint > LatinBlocksEnd)
        {
            if (codepoint >= LatinAdditionalStart)
            {
                return codepoint <= LatinAdditionalEnd;
            }
            return false;
        }
        return true;
    }

    private static void MarkNativeNames(bool[] extra)
    {
        var languages = Languages.All;
        for (var i = 0; i < languages.Length; i++)
        {
            GlyphRanges.MarkText(extra, languages[i].NativeName);
        }
    }
}
