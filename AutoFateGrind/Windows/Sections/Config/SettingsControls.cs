using System;
using System.Numerics;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoFateGrind.Windows.Sections.Config;

internal static class SettingsControls
{
    internal static class Choices
    {
        public readonly record struct Choice(LocString Name, LocString Detail);

        private const float PanelWidth = 320f;

        private static string[] names = Array.Empty<string>();

        private static string[] details = Array.Empty<string>();

        public static void DrawCombo(string id, Choice[] options, int selected, Action<int> onSelect, float width = RowComboWidth)
        {
            Resolve(options);
            var selected2 = selected;
            if (Dropdown.DrawDetailed(id, names.AsSpan(0, options.Length), details.AsSpan(0, options.Length), ref selected2, width, PanelWidth))
            {
                onSelect(selected2);
            }
        }

        private static void Resolve(Choice[] options)
        {
            if (names.Length < options.Length)
            {
                names = new string[options.Length];
                details = new string[options.Length];
            }
            for (int i = 0; i < options.Length; i++)
            {
                names[i] = Loc.T(options[i].Name);
                details[i] = Loc.T(options[i].Detail);
            }
        }
    }

    public const float ToggleWidth = 40f;

    public const float RowSliderWidth = 180f;

    public const float RowComboWidth = 170f;

    private const float RangeDragWidth = 62f;

    private const float RangeDashSlot = 14f;

    private const float RangeDragSpeed = 0.25f;

    private const float RangeComboWidth = 84f;

    public static float RangeInlineWidth()
    {
        return RangeDragWidth * 2f + RangeDashSlot;
    }

    public static void DrawToggle(Configuration cfg, Func<bool> getter, Action<bool> setter, string id)
    {
        var enabled = getter();
        if (ToggleSwitch.Draw(id, ref enabled))
        {
            setter(enabled);
            cfg.SaveDebounced();
        }
    }

    public static void DrawIntSlider(Configuration cfg, string id, Func<int> getter, Action<int> setter, int minimum, int maximum, string format = "%d", float width = RowSliderWidth)
    {
        var current = getter();
        ImGui.SetNextItemWidth(width * ImGuiHelpers.GlobalScale);
        using (PushFrameColors())
        {
            if (ImGui.SliderInt(id, ref current, minimum, maximum, format))
            {
                setter(Math.Clamp(current, minimum, maximum));
                cfg.SaveDebounced();
            }
        }
    }

    public static bool DrawPlainCombo(string id, ref int index, string[] labels, float width)
    {
        return Dropdown.Draw(id, labels, ref index, width);
    }

    public static void DrawLanguageCombo(Configuration cfg, float width = RowComboWidth)
    {
        LanguageInfo[] all = Languages.All;
        var selected = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == Loc.Current)
            {
                selected = i;
            }
        }
        if (Dropdown.Draw("##fta_language", languageLabels, ref selected, width))
        {
            ApplyLanguage(cfg, all[selected].Code);
        }
    }

    private static string[] BuildLanguageLabels()
    {
        LanguageInfo[] all = Languages.All;
        string[] labels = new string[all.Length];
        for (int i = 0; i < all.Length; i++)
        {
            labels[i] = all[i].NativeName;
        }
        return labels;
    }

    private static void ApplyLanguage(Configuration cfg, string code)
    {
        cfg.Language = code;
        cfg.Save();
        Loc.SetLanguage(code);
        Fonts.OnLanguageChanged();
        Plugin.Instance.OnLanguageChanged();
    }

    public static bool DrawSearchableCombo(string id, string[] labels, ref int selectedIndex, float width, string? hint = null)
    {
        return Dropdown.Draw(id, labels, ref selectedIndex, width, width, searchable: true, hint);
    }

    public static void DrawRangeInline(Configuration cfg, string minId, string maxId, Func<int> getMin, Action<int> setMin, Func<int> getMax, Action<int> setMax, int maxValue, int minValue = 0, string format = "%d s")
    {
        using (PushFrameColors())
        {
            DrawRangeBound(cfg, minId, getMin, setMin, minValue, maxValue, format, value =>
            {
                if (value > getMax())
                {
                    setMax(value);
                }
            });
            DrawRangeDash();
            DrawRangeBound(cfg, maxId, getMax, setMax, minValue, maxValue, format, value =>
            {
                if (value < getMin())
                {
                    setMin(value);
                }
            });
        }
    }

    private static readonly string[] languageLabels = BuildLanguageLabels();

    public static float ComboRangeWidth()
    {
        return RangeComboWidth * 2f + RangeDashSlot;
    }

    public static void DrawComboRange(Configuration cfg, string minId, string maxId, string[] labels, Func<int> getMin, Action<int> setMin, Func<int> getMax, Action<int> setMax)
    {
        var selected = getMin();
        if (Dropdown.Draw(minId, labels, ref selected, RangeComboWidth))
        {
            setMin(selected);
            if (selected > getMax())
            {
                setMax(selected);
            }
            cfg.SaveDebounced();
        }
        DrawRangeDash();
        var selected2 = getMax();
        if (Dropdown.Draw(maxId, labels, ref selected2, RangeComboWidth))
        {
            setMax(selected2);
            if (selected2 < getMin())
            {
                setMin(selected2);
            }
            cfg.SaveDebounced();
        }
    }

    private static void DrawRangeBound(Configuration cfg, string id, Func<int> getter, Action<int> setter, int minValue, int maxValue, string format, Action<int> onChanged)
    {
        var current = getter();
        ImGui.SetNextItemWidth(RangeDragWidth * ImGuiHelpers.GlobalScale);
        if (!ImGui.DragInt(id, ref current, RangeDragSpeed, minValue, maxValue, format))
        {
            return;
        }
        current = Math.Clamp(current, minValue, maxValue);
        setter(current);
        onChanged(current);
        cfg.SaveDebounced();
    }

    private static void DrawRangeDash()
    {
        var dashWidth = RangeDashSlot * ImGuiHelpers.GlobalScale;
        ImGui.SameLine(0f, 0f);
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var dashSize = ImGui.CalcTextSize("-");
        ImGui.SetCursorScreenPos(cursorScreenPos + new Vector2((dashWidth - dashSize.X) * 0.5f, (ImGui.GetFrameHeight() - dashSize.Y) * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
        {
            ImGui.TextUnformatted("-");
        }
        ImGui.SameLine(0f, 0f);
        ImGui.SetCursorScreenPos(cursorScreenPos with { X = cursorScreenPos.X + dashWidth });
    }

    public static IDisposable PushFrameColors()
    {
        return ImRaii.PushColor(ImGuiCol.SliderGrab, Styling.AccentViolet).Push(ImGuiCol.SliderGrabActive, Styling.AccentVioletSoft).Push(ImGuiCol.FrameBg, Styling.SliderBg)
            .Push(ImGuiCol.FrameBgHovered, Styling.CardBgHover)
            .Push(ImGuiCol.FrameBgActive, Styling.CardBgHover);
    }
}
