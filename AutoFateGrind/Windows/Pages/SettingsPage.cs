using System.Numerics;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Windows.Components;
using AutoFateGrind.Windows.Sections.Config;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoFateGrind.Windows.Pages;

internal sealed class SettingsPage
{
    private enum Tab
    {
        General,
        Travel,
        Filters,
        Classes,
        Gemstones,
        Repair,
        Consumables,
        Humanize,
        PartyInvites,
        GmAlert
    }

    private readonly record struct Entry(Tab Tab, LocString Label, FontAwesomeIcon Icon, LocString Subtitle);

    private static readonly Entry[] entries = new Entry[10]
    {
        new Entry(Tab.General, L.Settings.CatGeneral, FontAwesomeIcon.Cog, L.Settings.CatGeneralSub),
        new Entry(Tab.Travel, L.Settings.CatTravel, FontAwesomeIcon.Route, L.Settings.CatTravelSub),
        new Entry(Tab.Filters, L.Settings.CatFilters, FontAwesomeIcon.Filter, L.Settings.CatFiltersSub),
        new Entry(Tab.Classes, L.Settings.CatClasses, FontAwesomeIcon.UserShield, L.Settings.CatClassesSub),
        new Entry(Tab.Gemstones, L.Settings.CatGemstones, FontAwesomeIcon.Gem, L.Settings.CatGemstonesSub),
        new Entry(Tab.Repair, L.Settings.CatRepair, FontAwesomeIcon.Wrench, L.Settings.CatRepairSub),
        new Entry(Tab.Consumables, L.Settings.CatConsumables, FontAwesomeIcon.Utensils, L.Settings.CatConsumablesSub),
        new Entry(Tab.Humanize, L.Settings.CatHumanizer, FontAwesomeIcon.Walking, L.Settings.CatHumanizerSub),
        new Entry(Tab.PartyInvites, L.Settings.CatPartyInvites, FontAwesomeIcon.UserSlash, L.Settings.CatPartyInvitesSub),
        new Entry(Tab.GmAlert, L.Settings.CatGmAlert, FontAwesomeIcon.UserSecret, L.Settings.CatGmAlertSub)
    };

    private Tab activeTab;

    private bool resetScroll;

    public void Draw(Plugin plugin)
    {
        var configuration = plugin.Configuration;
        var globalScale = ImGuiHelpers.GlobalScale;
        var navWidth = 196f * globalScale;
        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero))
        {
            using (var navChild = ImRaii.Child("##fta_settings_nav", new Vector2(navWidth, -1f), border: false, ImGuiWindowFlags.NoScrollbar))
            {
                if ((bool)navChild)
                {
                    DrawNav();
                }
            }
            ImGui.SameLine(0f, 18f * globalScale);
            using (var contentChild = ImRaii.Child("##fta_settings_content", new Vector2(-1f, -1f), border: false, ImGuiWindowFlags.None))
            {
                if ((bool)contentChild)
                {
                    DrawContent(configuration);
                }
            }
        }
    }

    private void DrawNav()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var cursorScreenPos = ImGui.GetCursorScreenPos();
        var title = Loc.T(L.Settings.Title);
        using (Fonts.PushTitle())
        {
            TextDraw.At(title, new Vector2(cursorScreenPos.X + 6f * globalScale, cursorScreenPos.Y), Styling.TextStrong);
            ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, TextDraw.Measure(title).Y + 10f * globalScale));
        }
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (SidebarTab.Draw(Loc.T(entry.Label), entry.Icon, Styling.AccentViolet, activeTab == entry.Tab))
            {
                Select(entry.Tab);
            }
        }
    }

    private void Select(Tab tab)
    {
        if (activeTab == tab)
        {
            return;
        }
        activeTab = tab;
        resetScroll = true;
    }

    private void DrawContent(Configuration cfg)
    {
        if (resetScroll)
        {
            ImGui.SetScrollY(0f);
            resetScroll = false;
        }
        var entry = entries[(int)activeTab];
        var globalScale = ImGuiHelpers.GlobalScale;
        using (Motion.PushSwitch("##fta_settings_tab", (int)activeTab))
        {
            using (ImRaii.Group())
            {
                ImGui.Dummy(new Vector2(0f, 2f * globalScale));
                PageHeader.Draw(Loc.T(entry.Label), Loc.T(entry.Subtitle));
                switch (activeTab)
                {
                    case Tab.General:
                        GeneralSettings.Draw(cfg);
                        break;
                    case Tab.Travel:
                        TravelSettings.Draw(cfg);
                        break;
                    case Tab.Filters:
                        FilterSettings.Draw(cfg);
                        break;
                    case Tab.Classes:
                        ClassSettings.Draw(cfg);
                        break;
                    case Tab.Gemstones:
                        GemstoneSettings.Draw(cfg);
                        break;
                    case Tab.Repair:
                        RepairSettings.Draw(cfg);
                        break;
                    case Tab.Consumables:
                        ConsumableSettings.Draw(cfg);
                        break;
                    case Tab.Humanize:
                        HumanizerSettings.Draw(cfg);
                        break;
                    case Tab.PartyInvites:
                        PartyInviteSettings.Draw(cfg);
                        break;
                    case Tab.GmAlert:
                        GmAlertSettings.Draw(cfg);
                        break;
                }
            }
        }
    }
}
