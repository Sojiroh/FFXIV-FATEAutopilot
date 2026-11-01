using System;
using System.Collections.Generic;
using System.Globalization;
using Dalamud.Game;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Game.Player;

internal readonly record struct MountOption(uint Id, string Name);

internal static class MountOps
{
    public const uint Roulette = 0u;

    public static bool HasPreferred => IsUsable(Plugin.Cfg.PreferredMountId);

    public static bool TerritoryAllowsMount()
    {
        var territory = Svc.Data.GetExcelSheet<TerritoryType>().GetRowOrDefault(Svc.ClientState.TerritoryType);
        return territory is { } territoryRow && territoryRow.Mount;
    }

    public unsafe static bool IsUsable(uint mountId)
    {
        if (mountId == 0)
        {
            return false;
        }
        if (Svc.Data.GetExcelSheet<Mount>().GetRowOrDefault(mountId) is not { } mount || !IsSelectable(mount))
        {
            return false;
        }
        var playerState = PlayerState.Instance();
        return playerState != null && playerState->IsMountUnlocked(mountId);
    }

    public unsafe static bool TrySummonPreferred()
    {
        uint preferredMountId = Plugin.Cfg.PreferredMountId;
        if (!IsUsable(preferredMountId))
        {
            return false;
        }
        var actionManager = ActionManager.Instance();
        if (actionManager == null || actionManager->GetActionStatus(ActionType.Mount, preferredMountId, 3758096384uL, checkRecastActive: true, checkCastingActive: true, null) != 0)
        {
            return false;
        }
        return actionManager->UseAction(ActionType.Mount, preferredMountId, 3758096384uL, 0u, ActionManager.UseActionMode.None, 0u, null);
    }

    public unsafe static MountOption[] OwnedMounts()
    {
        var playerState = PlayerState.Instance();
        if (playerState == null)
        {
            return Array.Empty<MountOption>();
        }
        var options = new List<MountOption>();
        foreach (var mount in Svc.Data.GetExcelSheet<Mount>())
        {
            var row = mount;
            if (IsSelectable(in row) && playerState->IsMountUnlocked(row.RowId))
            {
                var mountName = row.Singular.ExtractText();
                if (mountName.Length != 0)
                {
                    options.Add(new MountOption(row.RowId, DisplayName(mountName)));
                }
            }
        }
        var sorted = options.ToArray();
        Array.Sort(sorted, (MountOption left, MountOption right) => string.Compare(left.Name, right.Name, StringComparison.CurrentCultureIgnoreCase));
        return sorted;
    }

    public static string NameOf(uint mountId)
    {
        if (Svc.Data.GetExcelSheet<Mount>().GetRowOrDefault(mountId) is not { } mount)
        {
            return string.Empty;
        }
        return DisplayName(mount.Singular.ExtractText());
    }

    private static bool IsSelectable(in Mount row) => row.Order >= 0 && row.IsFlying != 0 && !row.IsImmobile;

    private static string DisplayName(string name)
    {
        if (name.Length == 0)
        {
            return name;
        }
        if (Svc.ClientState.ClientLanguage == ClientLanguage.English)
        {
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name);
        }
        if (!char.IsLower(name[0]))
        {
            return name;
        }
        return string.Concat(char.ToUpperInvariant(name[0]).ToString(), name.AsSpan(1));
    }
}
