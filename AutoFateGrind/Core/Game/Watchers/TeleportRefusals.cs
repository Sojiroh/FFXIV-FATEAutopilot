using System;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Game.Chat;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace AutoFateGrind.Core.Game.Watchers;

internal static class TeleportRefusals
{
    public const uint NotAttunedId = 1663u;

    public const uint AlreadyUnderwayId = 1665u;

    public const uint InsufficientGilId = 1669u;

    private static readonly string[] RefusalPrefixes = new string[] { "Unable to teleport", "Unable to execute", "Cannot execute" };

    private static Dictionary<uint, string> refusalTexts = new Dictionary<uint, string>();

    private static volatile TeleportRefusal? latest;

    public static void Initialize()
    {
        refusalTexts = BuildRefusalTexts();
        Svc.Chat.LogMessage += OnLogMessage;
    }

    public static void Shutdown()
    {
        Svc.Chat.LogMessage -= OnLogMessage;
    }

    public static TeleportRefusal? Since(long tick)
    {
        TeleportRefusal? current = latest;
        if (current is null || current.Tick < tick)
        {
            return null;
        }
        return current;
    }

    private static void OnLogMessage(ILogMessage message)
    {
        if (!refusalTexts.TryGetValue(message.LogMessageId, out string? text))
        {
            return;
        }
        latest = new TeleportRefusal(message.LogMessageId, text, Environment.TickCount64);
    }

    private static Dictionary<uint, string> BuildRefusalTexts()
    {
        var texts = new Dictionary<uint, string>();
        foreach (var logMessage in Svc.Data.GetExcelSheet<LogMessage>(ClientLanguage.English))
        {
            var text = logMessage.Text.ExtractText();
            if (StartsWithRefusalPrefix(text))
            {
                texts[logMessage.RowId] = text;
            }
        }
        return texts;
    }

    private static bool StartsWithRefusalPrefix(string text)
    {
        for (var i = 0; i < RefusalPrefixes.Length; i++)
        {
            if (text.StartsWith(RefusalPrefixes[i], StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }
}
