using System.Numerics;
using AutoFateGrind.Core.External;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace AutoFateGrind.Windows.Pages;

internal sealed class PluginsPage
{
    private const float PadX = 16f;

    private const float DiscRadius = 17f;

    public void Draw()
    {
        var missingRequired = 0;
        foreach (var plugin in ExternalPlugins.All)
        {
            if (ExternalPlugins.Catalog[plugin].Required && !ExternalPlugins.IsInstalled(plugin))
            {
                missingRequired++;
            }
        }
        var subtitle = ((missingRequired == 0) ? Loc.T(L.Plugins.AllInstalled) : Loc.Plural(L.Plugins.Missing, missingRequired));
        PageHeader.Draw(Loc.T(L.Plugins.Title), subtitle, (missingRequired == 0) ? Styling.AccentMint : Styling.AccentRose);
        foreach (var externalPlugin in ExternalPlugins.All)
        {
            DrawCard(externalPlugin);
            Styling.VSpace(4f);
        }
        Styling.VSpace(6f);
        using (Fonts.PushCaption())
        {
            var cursor = ImGui.GetCursorScreenPos();
            var width = ImGui.GetContentRegionAvail().X;
            var footer = Loc.T(L.Plugins.Footer);
            TextDraw.Wrapped(footer, cursor, width, Styling.TextMuted);
            ImGui.Dummy(new Vector2(width, TextDraw.MeasureWrapped(footer, width).Y));
        }
    }

    private static string Purpose(ExternalPlugin plugin)
    {
        return plugin switch
        {
            ExternalPlugin.Vnavmesh => Loc.T(L.Plugins.PurposeVnavmesh),
            ExternalPlugin.BossMod => Loc.T(L.Plugins.PurposeBossMod),
            ExternalPlugin.TextAdvance => Loc.T(L.Plugins.PurposeTextAdvance),
            _ => ExternalPlugins.Catalog[plugin].Purpose,
        };
    }

    private static void DrawCard(ExternalPlugin plugin)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var info = ExternalPlugins.Catalog[plugin];
        var installed = ExternalPlugins.IsInstalled(plugin);
        var disabled = ExternalPlugins.IsInstalledButDisabled(plugin);
        var installing = PluginInstaller.IsInstalling(plugin);
        var (icon, accent) = (installed, disabled, info.Required) switch
        {
            (true, true, _) => (FontAwesomeIcon.ExclamationCircle, Styling.AccentAmber),
            (true, false, _) => (FontAwesomeIcon.CheckCircle, Styling.AccentMint),
            (false, _, true) => (FontAwesomeIcon.TimesCircle, Styling.AccentRose),
            _ => (FontAwesomeIcon.Circle, Styling.TextDim),
        };
        var size = new Vector2(ImGui.GetContentRegionAvail().X, 76f * globalScale);
        var min = ImGui.GetCursorScreenPos();
        var max = min + size;
        var drawList = ImGui.GetWindowDrawList();
        Paint.Glass(drawList, min, max, 10f * globalScale, accent, 0.06f);
        var padX = PadX * globalScale;
        var midY = min.Y + size.Y * 0.5f;
        var discRadius = DiscRadius * globalScale;
        var center = new Vector2(min.X + padX + discRadius, midY);
        ProgressRing.Disc(center, discRadius, Styling.Tint(Styling.Surface1, accent, 0.3f));
        ProgressRing.Track(center, discRadius, 1.2f * globalScale, Styling.WithAlpha(accent, 0.7f));
        ProgressRing.CenterIcon(center, icon, accent, discRadius * 0.95f);
        var actionWidth = DrawAction(plugin, installed, disabled, installing, max, midY);
        var purpose = Purpose(plugin);
        var textX = center.X + discRadius + 16f * globalScale;
        var maxWidth = max.X - padX - actionWidth - textX;
        float nameHeight;
        using (Fonts.PushHeadline())
        {
            nameHeight = TextDraw.Measure(info.DisplayName).Y;
        }
        float purposeHeight;
        using (Fonts.PushCaption())
        {
            purposeHeight = TextDraw.Measure(purpose).Y;
        }
        var textY = midY - (nameHeight + 3f * globalScale + purposeHeight) * 0.5f;
        Vector2 nameSize;
        using (Fonts.PushHeadline())
        {
            nameSize = TextDraw.Measure(info.DisplayName);
            TextDraw.At(info.DisplayName, new Vector2(textX, textY), Styling.TextStrong);
        }
        DrawRequirementTag(drawList, info.Required, textX + nameSize.X + 10f * globalScale, textY + nameHeight * 0.5f);
        using (Fonts.PushCaption())
        {
            TextDraw.At(TextDraw.Truncate(purpose, maxWidth), new Vector2(textX, textY + nameHeight + 3f * globalScale), Styling.TextDim);
        }
        var nameMin = new Vector2(textX, textY);
        var nameMax = nameMin + nameSize;
        if (Hit.HoveringRect(nameMin, nameMax))
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            Tooltip.Show(Loc.T(L.Plugins.RepoHint, info.RepoUrl));
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                UrlActions.OpenInBrowser(info.RepoUrl);
            }
            else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                ImGui.SetClipboardText(info.RepoUrl);
            }
        }
        ImGui.SetCursorScreenPos(min);
        ImGui.Dummy(size);
    }

    private static void DrawRequirementTag(ImDrawListPtr dl, bool required, float x, float midY)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var label = Loc.Upper(required ? Loc.T(L.Plugins.Required) : Loc.T(L.Plugins.Optional));
        using (Fonts.PushCaption())
        {
            var textSize = TextDraw.Measure(label);
            var min = new Vector2(x, midY - textSize.Y * 0.5f - 3f * globalScale);
            var max = min + textSize + new Vector2(14f * globalScale, 6f * globalScale);
            var baseColor = (required ? Styling.AccentViolet : Styling.TextDim);
            Paint.Pill(dl, min, max, Styling.WithAlpha(baseColor, 0.18f), Styling.WithAlpha(baseColor, 0.45f));
            TextDraw.Middle(label, min, max, required ? Styling.AccentVioletSoft : Styling.TextSecondary);
        }
    }

    private static float DrawAction(ExternalPlugin plugin, bool installed, bool disabled, bool installing, Vector2 end, float midY)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var padX = PadX * globalScale;
        if (installed)
        {
            string statusText;
            Vector4 color;
            FontAwesomeIcon icon;
            (statusText, color, icon) = (disabled ? (Loc.T(L.Plugins.Disabled), Styling.AccentAmber, FontAwesomeIcon.ExclamationTriangle) : (Loc.T(L.Plugins.Installed), Styling.AccentMint, FontAwesomeIcon.Check));
            var textSize = TextDraw.Measure(statusText);
            var iconSize = TextDraw.IconSize(icon);
            var textX = end.X - padX - textSize.X;
            TextDraw.At(statusText, new Vector2(textX, midY - textSize.Y * 0.5f), color);
            var iconX = textX - 6f * globalScale - iconSize.X;
            TextDraw.Icon(icon, new Vector2(iconX, midY - iconSize.Y * 0.5f), color);
            if (disabled && Hit.HoveringRect(new Vector2(iconX, midY - textSize.Y), new Vector2(end.X - padX, midY + textSize.Y)))
            {
                Tooltip.Show(Loc.T(L.Plugins.TextAdvanceDisabled));
            }
            return end.X - padX - iconX + 12f * globalScale;
        }
        var label = (installing ? Loc.T(L.Plugins.Installing) : Loc.T(L.Plugins.Install));
        var buttonWidth = PillButton.Width(label, FontAwesomeIcon.Download);
        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - buttonWidth, midY - 15f * globalScale));
        ImGui.PushID((nint)(plugin + 1));
        if (PillButton.Draw("##install", label, Styling.AccentViolet, PillButton.Emphasis.Filled, FontAwesomeIcon.Download, !installing, 30f))
        {
            _ = PluginInstaller.Install(plugin);
        }
        ImGui.PopID();
        return buttonWidth + 12f * globalScale;
    }
}
