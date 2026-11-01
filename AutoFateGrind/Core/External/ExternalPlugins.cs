using System;
using System.Collections.Generic;
using System.Linq;
using AutoFateGrind.Core.Ipc;
using ECommons.DalamudServices;

namespace AutoFateGrind.Core.External;

public static class ExternalPlugins
{
    public static readonly IReadOnlyDictionary<ExternalPlugin, ExternalPluginInfo> Catalog = new Dictionary<ExternalPlugin, ExternalPluginInfo>
    {
        [ExternalPlugin.Vnavmesh] = new ExternalPluginInfo("vnavmesh", "vnavmesh", "https://puni.sh/api/repository/veyn", "Pathfinding and movement to FATEs.", Required: true),
        [ExternalPlugin.BossMod] = new ExternalPluginInfo("BossMod", "BossMod", "https://puni.sh/api/repository/veyn", "Auto-rotation, targeting, and dodging during FATE combat.", Required: true, new string[] { "BossModReborn" }),
        [ExternalPlugin.TextAdvance] = new ExternalPluginInfo("TextAdvance", "TextAdvance", "https://raw.githubusercontent.com/NightmareXIV/MyDalamudPlugins/main/pluginmaster.json", "Talk-skip during Collect FATE turn-ins (scoped, only enabled mid-Collect).", Required: true)
    };

    public static IEnumerable<ExternalPlugin> All => Catalog.Keys;

    public static bool IsInstalled(ExternalPlugin plugin)
    {
        var info = Catalog[plugin];
        return Svc.PluginInterface.InstalledPlugins.Any(p => p.IsLoaded && (p.InternalName == info.InternalName || (info.Aliases != null && Array.IndexOf(info.Aliases, p.InternalName) >= 0)));
    }

    public static bool AllRequiredInstalled() => All.Where((ExternalPlugin p) => Catalog[p].Required).All(IsInstalled);

    public static bool IsInstalledButDisabled(ExternalPlugin plugin) => plugin == ExternalPlugin.TextAdvance && IsInstalled(plugin) && !TextAdvanceIPC.IsPluginEnabled();
}
