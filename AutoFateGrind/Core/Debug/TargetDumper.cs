using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Debug;

internal static class TargetDumper
{
    public unsafe static void Dump()
    {
        var territoryType = Svc.ClientState.TerritoryType;
        var placeName = Svc.Data.GetExcelSheet<TerritoryType>()?.GetRowOrDefault(territoryType)?.PlaceName.Value.Name.ToString() ?? "?";
        Svc.Chat.Print($"{AfgConstants.LogPrefix} Territory: {territoryType} ({placeName})");
        var target = TargetSystem.Instance()->Target;
        if (target == null)
        {
            Svc.Chat.Print("[FTA] No target. Click an NPC or FATE marker first, then re-run /fta target.");
            return;
        }
        var baseId = target->BaseId;
        var nameString = target->NameString;
        var npcName = Svc.Data.GetExcelSheet<ENpcResident>()?.GetRowOrDefault(baseId)?.Singular.ToString() ?? nameString;
        Svc.Chat.Print($"{AfgConstants.LogPrefix} Target: BaseId={baseId}  Name=\"{npcName}\"");
        RunLog.Info($"territory={territoryType} BaseId={baseId} name='{npcName}'");
    }
}
