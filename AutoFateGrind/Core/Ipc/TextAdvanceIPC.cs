using System;
using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoFateGrind.Core.Ipc;

public class ExternalTerritoryConfig
{
    public bool? EnableTalkSkip;

    public bool? EnableRequestFill;

    public bool? EnableRequestHandin;
}

internal static class TextAdvanceIPC
{
    private static ICallGateSubscriber<bool>? isEnabled;

    private static ICallGateSubscriber<string, ExternalTerritoryConfig, bool>? enableExternalControl;

    private static ICallGateSubscriber<string, bool>? disableExternalControl;

    private static bool initialized;

    private static void EnsureInit()
    {
        if (initialized)
        {
            return;
        }
        initialized = true;
        try
        {
            isEnabled = Svc.PluginInterface.GetIpcSubscriber<bool>("TextAdvance.IsEnabled");
            enableExternalControl = Svc.PluginInterface.GetIpcSubscriber<string, ExternalTerritoryConfig, bool>("TextAdvance.EnableExternalControl");
            disableExternalControl = Svc.PluginInterface.GetIpcSubscriber<string, bool>("TextAdvance.DisableExternalControl");
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "subscribe failed");
        }
    }

    public static bool IsPluginEnabled()
    {
        EnsureInit();
        try
        {
            return isEnabled?.HasFunction != true || isEnabled.InvokeFunc();
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "IsEnabled failed");
            return true;
        }
    }

    public static bool EnableExternalControl(string callerName, bool talkSkip, bool requestFill, bool requestHandin)
    {
        EnsureInit();
        if (enableExternalControl?.HasFunction != true)
        {
            return false;
        }
        try
        {
            var config = new ExternalTerritoryConfig
            {
                EnableTalkSkip = talkSkip,
                EnableRequestFill = requestFill,
                EnableRequestHandin = requestHandin
            };
            return enableExternalControl.InvokeFunc(callerName, config);
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "EnableExternalControl failed");
            return false;
        }
    }

    public static void DisableExternalControl(string callerName)
    {
        EnsureInit();
        try
        {
            disableExternalControl?.InvokeFunc(callerName);
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "DisableExternalControl failed");
        }
    }
}
