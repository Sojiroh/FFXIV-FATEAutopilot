using System;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using ECommons.Automation;
using ECommons.DalamudServices;

namespace AutoFateGrind.Core.Game.Watchers;

internal sealed class GmAlertWatcher : IDisposable
{
    private const uint GmStatusMin = 1u;

    private const uint GmStatusMax = 3u;

    private bool fired;

    public GmAlertWatcher()
    {
        Svc.Framework.Update += OnUpdate;
    }

    public void Dispose()
    {
        Svc.Framework.Update -= OnUpdate;
    }

    private void OnUpdate(IFramework _)
    {
        var cfg = Plugin.Cfg;
        if (!AnyActionEnabled(cfg))
        {
            fired = false;
            return;
        }
        IPlayerCharacter? localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer == null)
        {
            fired = false;
            return;
        }
        IPlayerCharacter? gmCharacter = null;
        foreach (var gameObject in Svc.Objects)
        {
            if (gameObject is IPlayerCharacter candidate && candidate.EntityId != localPlayer.EntityId)
            {
                var onlineStatusId = candidate.OnlineStatus.RowId;
                if (onlineStatusId >= GmStatusMin && onlineStatusId <= GmStatusMax)
                {
                    gmCharacter = candidate;
                    break;
                }
            }
        }
        if (gmCharacter == null)
        {
            fired = false;
            return;
        }
        if (fired)
        {
            return;
        }
        fired = true;
        FireAlerts(cfg, gmCharacter);
    }

    private static bool AnyActionEnabled(Configuration cfg) => cfg.GmAlertStopRun || cfg.GmAlertToast || cfg.GmAlertChat || cfg.GmAlertSound || cfg.GmAlertKillGame || cfg.GmAlertCommands.Count > 0;

    private static void FireAlerts(Configuration cfg, IPlayerCharacter gm)
    {
        string textValue = gm.Name.TextValue;
        RunLog.Warning($"GM detected nearby: {textValue} (OnlineStatus {gm.OnlineStatus.RowId}). Firing GM alert.");
        if (cfg.GmAlertStopRun)
        {
            try
            {
                Plugin.Instance.Controller.Stop();
            }
            catch (Exception exception)
            {
                RunLog.Warning(exception, "GM alert: Controller.Stop threw.");
            }
        }
        if (cfg.GmAlertToast)
        {
            try
            {
                Svc.Toasts.ShowNormal("GM " + textValue + " is nearby!");
            }
            catch (Exception exception2)
            {
                RunLog.Warning(exception2, "GM alert: toast threw.");
            }
        }
        if (cfg.GmAlertChat)
        {
            try
            {
                Svc.Chat.PrintError("[FTA] GM " + textValue + " is nearby!");
            }
            catch (Exception exception3)
            {
                RunLog.Warning(exception3, "GM alert: chat print threw.");
            }
        }
        if (cfg.GmAlertSound)
        {
            PlayBeeps(cfg.GmAlertBeepCount, cfg.GmAlertBeepFrequencyHz, cfg.GmAlertBeepDurationMs);
        }
        foreach (string gmAlertCommand in cfg.GmAlertCommands)
        {
            try
            {
                Chat.ExecuteCommand(gmAlertCommand);
            }
            catch (Exception exception4)
            {
                RunLog.Warning(exception4, "GM alert: command '" + gmAlertCommand + "' threw.");
            }
        }
        if (cfg.GmAlertKillGame)
        {
            try
            {
                Chat.ExecuteCommand("/xlkill");
            }
            catch (Exception exception5)
            {
                RunLog.Warning(exception5, "GM alert: /xlkill threw.");
            }
        }
    }

    public static void PlayBeeps(int count, int freqHz, int durationMs)
    {
        int n = Math.Clamp(count, 1, 100);
        int f = Math.Clamp(freqHz, 37, 32767);
        int d = Math.Clamp(durationMs, 1, 5000);
        Task.Run(() =>
        {
            for (var i = 0; i < n; i++)
            {
                try
                {
                    Console.Beep(f, d);
                }
                catch (Exception ex)
                {
                    RunLog.Debug("Console.Beep failed: " + ex.Message);
                    break;
                }
            }
        });
    }
}
