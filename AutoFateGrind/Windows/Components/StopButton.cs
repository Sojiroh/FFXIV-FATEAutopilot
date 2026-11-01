using AutoFateGrind.Core.Localization;
using Dalamud.Interface;

namespace AutoFateGrind.Windows.Components;

internal static class StopButton
{
    public static bool Draw(string? sublabel, float width = 0f)
    {
        return HeroButton.Draw(FontAwesomeIcon.Stop, Loc.T(L.Grind.Stop), sublabel, Styling.AccentRose, enabled: true, null, width);
    }
}
