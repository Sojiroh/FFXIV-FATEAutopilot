using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoFateGrind.Windows;

internal static class Styling
{
    private sealed class ChromeScope(IDisposable style, IDisposable color) : IDisposable
    {
        public void Dispose()
        {
            color.Dispose();
            style.Dispose();
        }
    }

    public static readonly Vector4 AccentViolet = new Vector4(0.62f, 0.42f, 0.96f, 1f);

    public static readonly Vector4 AccentVioletSoft = new Vector4(0.78f, 0.6f, 1f, 1f);

    public static readonly Vector4 AccentPink = new Vector4(0.95f, 0.45f, 0.78f, 1f);

    public static readonly Vector4 AccentMint = new Vector4(0.46f, 0.86f, 0.66f, 1f);

    public static readonly Vector4 AccentMintSoft = new Vector4(0.66f, 0.96f, 0.8f, 1f);

    public static readonly Vector4 AccentAmber = new Vector4(0.92f, 0.74f, 0.34f, 1f);

    public static readonly Vector4 AccentAmberSoft = new Vector4(1f, 0.86f, 0.52f, 1f);

    public static readonly Vector4 AccentRose = new Vector4(0.93f, 0.42f, 0.5f, 1f);

    public static readonly Vector4 AccentRoseSoft = new Vector4(1f, 0.62f, 0.68f, 1f);

    public static readonly Vector4 AccentBlue = new Vector4(0.4f, 0.68f, 0.98f, 1f);

    public static readonly Vector4 AccentBlueSoft = new Vector4(0.62f, 0.82f, 1f, 1f);

    public static readonly Vector4 WindowBg = new Vector4(0.05f, 0.054f, 0.076f, 0.985f);

    public static readonly Vector4 Surface0 = new Vector4(0.082f, 0.09f, 0.118f, 1f);

    public static readonly Vector4 Surface1 = new Vector4(0.108f, 0.118f, 0.152f, 1f);

    public static readonly Vector4 Surface2 = new Vector4(0.142f, 0.155f, 0.196f, 1f);

    public static readonly Vector4 Surface3 = new Vector4(0.18f, 0.196f, 0.244f, 1f);

    public static readonly Vector4 CardBg = new Vector4(0.082f, 0.09f, 0.118f, 0.9f);

    public static readonly Vector4 CardBgSoft = new Vector4(0.108f, 0.118f, 0.152f, 0.62f);

    public static readonly Vector4 CardBgHover = new Vector4(0.142f, 0.155f, 0.196f, 0.95f);

    public static readonly Vector4 SliderBg = new Vector4(0.16f, 0.175f, 0.22f, 1f);

    public static readonly Vector4 BorderDim = new Vector4(0.235f, 0.262f, 0.33f, 1f);

    public static readonly Vector4 TextStrong = new Vector4(0.965f, 0.965f, 0.975f, 1f);

    public static readonly Vector4 InkOnAccent = new Vector4(0.078f, 0.059f, 0.133f, 1f);

    public static readonly Vector4 TextSecondary = new Vector4(0.78f, 0.8f, 0.84f, 1f);

    public static readonly Vector4 TextDim = new Vector4(0.56f, 0.59f, 0.64f, 1f);

    public static readonly Vector4 TextMuted = new Vector4(0.4f, 0.42f, 0.47f, 1f);

    public static readonly Vector4 Hairline = new Vector4(1f, 1f, 1f, 0.055f);

    public static readonly Vector4 Highlight = new Vector4(1f, 1f, 1f, 0.075f);

    public const float WindowRounding = 14f;

    public const float PanelRounding = 12f;

    public const float CardRounding = 10f;

    public const float FrameRounding = 7f;

    public const double PulseFast = 600.0;

    public const double PulseMedium = 800.0;

    public const double PulseBreath = 2600.0;

    public const double PulseCalm = 1900.0;

    public const double PulseOrbit = 3400.0;

    public static float Pulse(double periodMs = PulseMedium)
    {
        return (float)((Math.Sin((double)Environment.TickCount % periodMs / periodMs * Math.PI * 2.0) + 1.0) * 0.5);
    }

    public static Vector4 PulseColor(Vector4 a, Vector4 b, double periodMs = PulseMedium)
    {
        return Vector4.Lerp(a, b, Pulse(periodMs));
    }

    public static float Phase(double periodMs) => (float)((double)Environment.TickCount % periodMs / periodMs);

    public static Vector4 WithAlpha(Vector4 c, float a)
    {
        var colored = c;
        colored.W = a;
        return colored;
    }

    public static Vector4 ForegroundOn(Vector4 fill)
    {
        if (!(Luminance(fill) > 0.65f))
        {
            return TextStrong;
        }
        return InkOnAccent;
    }

    private static float Luminance(Vector4 color) => 0.2126f * Linear(color.X) + 0.7152f * Linear(color.Y) + 0.0722f * Linear(color.Z);

    private static float Linear(float channel)
    {
        if (!(channel <= 0.04045f))
        {
            return MathF.Pow((channel + 0.055f) / 1.055f, 2.4f);
        }
        return channel / 12.92f;
    }

    public static Vector4 Lighten(Vector4 c, float t)
    {
        var lightened = Vector4.Lerp(c, Vector4.One, t);
        lightened.W = c.W;
        return lightened;
    }

    public static Vector4 Darken(Vector4 c, float t)
    {
        var darkened = Vector4.Lerp(c, Vector4.Zero, t);
        darkened.W = c.W;
        return darkened;
    }

    public static Vector4 Tint(Vector4 baseColor, Vector4 accent, float amount)
    {
        var tinted = Vector4.Lerp(baseColor, accent, amount);
        tinted.W = baseColor.W;
        return tinted;
    }

    public static void TextCentered(string text, Vector4 color)
    {
        var textWidth = ImGui.CalcTextSize(text).X;
        var availableWidth = ImGui.GetContentRegionAvail().X;
        if (availableWidth > textWidth)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (availableWidth - textWidth) * 0.5f);
        }
        using (ImRaii.PushColor(ImGuiCol.Text, color))
        {
            ImGui.TextUnformatted(text);
        }
    }

    public static void VSpace(float pixels)
    {
        ImGui.Dummy(new Vector2(0f, pixels * ImGuiHelpers.GlobalScale));
    }

    public static void CenterNextItem(float width)
    {
        var availableWidth = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0f, (availableWidth - width) * 0.5f));
    }

    public static void SectionLabel(string label)
    {
        using (Fonts.PushHeadline())
        {
            using (ImRaii.PushColor(ImGuiCol.Text, TextStrong))
            {
                ImGui.TextUnformatted(label);
            }
        }
    }

    public static IDisposable PushChrome(Vector2 windowPadding)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var style = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, WindowRounding * globalScale).Push(ImGuiStyleVar.WindowBorderSize, 1f).Push(ImGuiStyleVar.WindowPadding, windowPadding * globalScale)
            .Push(ImGuiStyleVar.ChildRounding, CardRounding * globalScale)
            .Push(ImGuiStyleVar.ChildBorderSize, 0f)
            .Push(ImGuiStyleVar.PopupRounding, CardRounding * globalScale)
            .Push(ImGuiStyleVar.PopupBorderSize, 1f)
            .Push(ImGuiStyleVar.FrameRounding, FrameRounding * globalScale)
            .Push(ImGuiStyleVar.FramePadding, new Vector2(10f, 6f) * globalScale)
            .Push(ImGuiStyleVar.FrameBorderSize, 0f)
            .Push(ImGuiStyleVar.ItemSpacing, new Vector2(10f, 8f) * globalScale)
            .Push(ImGuiStyleVar.ItemInnerSpacing, new Vector2(6f, 4f) * globalScale)
            .Push(ImGuiStyleVar.ScrollbarSize, 9f * globalScale)
            .Push(ImGuiStyleVar.ScrollbarRounding, 9f * globalScale)
            .Push(ImGuiStyleVar.GrabRounding, 6f * globalScale)
            .Push(ImGuiStyleVar.GrabMinSize, 12f * globalScale);
        var windowColors = ImRaii.PushColor(ImGuiCol.WindowBg, WindowBg).Push(ImGuiCol.ChildBg, Vector4.Zero);
        var color = windowColors.Push(ImGuiCol.PopupBg, Surface1 with { W = 0.985f }).Push(ImGuiCol.Border, new Vector4(1f, 1f, 1f, 0.09f)).Push(ImGuiCol.BorderShadow, Vector4.Zero)
            .Push(ImGuiCol.FrameBg, SliderBg)
            .Push(ImGuiCol.FrameBgHovered, Surface2)
            .Push(ImGuiCol.FrameBgActive, Surface3)
            .Push(ImGuiCol.ScrollbarBg, Vector4.Zero)
            .Push(ImGuiCol.ScrollbarGrab, new Vector4(1f, 1f, 1f, 0.12f))
            .Push(ImGuiCol.ScrollbarGrabHovered, new Vector4(1f, 1f, 1f, 0.2f))
            .Push(ImGuiCol.ScrollbarGrabActive, new Vector4(1f, 1f, 1f, 0.28f))
            .Push(ImGuiCol.Button, Surface1)
            .Push(ImGuiCol.ButtonHovered, Surface2)
            .Push(ImGuiCol.ButtonActive, Tint(Surface2, AccentViolet, 0.35f))
            .Push(ImGuiCol.Header, Tint(Surface1, AccentViolet, 0.3f))
            .Push(ImGuiCol.HeaderHovered, Surface2)
            .Push(ImGuiCol.HeaderActive, Tint(Surface2, AccentViolet, 0.4f))
            .Push(ImGuiCol.CheckMark, AccentVioletSoft)
            .Push(ImGuiCol.SliderGrab, AccentViolet)
            .Push(ImGuiCol.SliderGrabActive, AccentVioletSoft)
            .Push(ImGuiCol.Text, TextStrong)
            .Push(ImGuiCol.TextDisabled, TextMuted)
            .Push(ImGuiCol.Separator, Hairline)
            .Push(ImGuiCol.ResizeGrip, Vector4.Zero)
            .Push(ImGuiCol.ResizeGripHovered, Vector4.Zero)
            .Push(ImGuiCol.ResizeGripActive, Vector4.Zero)
            .Push(ImGuiCol.TextSelectedBg, WithAlpha(AccentViolet, 0.35f));
        return new ChromeScope(style, color);
    }
}
