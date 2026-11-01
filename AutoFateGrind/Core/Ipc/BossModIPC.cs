using System;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoFateGrind.Core.Ipc;

internal sealed class BossModIPC
{
    private static BossModIPC? instance;

    private const string QualityIsBadProperty = "IsBad";

    private readonly ICallGateSubscriber<string, bool> setActive;

    private readonly ICallGateSubscriber<bool> clearActive;

    private readonly ICallGateSubscriber<string> getActive;

    private readonly ICallGateSubscriber<string, string?> getPreset;

    private readonly ICallGateSubscriber<string, string, string, string, bool> addTransient;

    private readonly ICallGateSubscriber<string, string, string, bool> clearTransient;

    private readonly ICallGateSubscriber<string, bool, bool> createPreset;

    private readonly ICallGateSubscriber<Vector3, float, bool, bool> obstacleGenerate;

    private readonly ICallGateSubscriber<TaskStatus> obstacleGetStatus;

    private readonly ICallGateSubscriber<bool> obstacleHasTempMap;

    private readonly ICallGateSubscriber<bool> obstacleClearTempMap;

    private readonly ICallGateSubscriber<object?> obstacleEvaluateQuality;

    public static BossModIPC Instance => instance ?? (instance = new BossModIPC());

    private BossModIPC()
    {
        setActive = Svc.PluginInterface.GetIpcSubscriber<string, bool>("BossMod.Presets.SetActive");
        clearActive = Svc.PluginInterface.GetIpcSubscriber<bool>("BossMod.Presets.ClearActive");
        getActive = Svc.PluginInterface.GetIpcSubscriber<string>("BossMod.Presets.GetActive");
        getPreset = Svc.PluginInterface.GetIpcSubscriber<string, string?>("BossMod.Presets.Get");
        addTransient = Svc.PluginInterface.GetIpcSubscriber<string, string, string, string, bool>("BossMod.Presets.AddTransientStrategy");
        clearTransient = Svc.PluginInterface.GetIpcSubscriber<string, string, string, bool>("BossMod.Presets.ClearTransientStrategy");
        createPreset = Svc.PluginInterface.GetIpcSubscriber<string, bool, bool>("BossMod.Presets.Create");
        obstacleGenerate = Svc.PluginInterface.GetIpcSubscriber<Vector3, float, bool, bool>("BossMod.ObstacleMap.Generate");
        obstacleGetStatus = Svc.PluginInterface.GetIpcSubscriber<TaskStatus>("BossMod.ObstacleMap.GetGenerationStatus");
        obstacleHasTempMap = Svc.PluginInterface.GetIpcSubscriber<bool>("BossMod.ObstacleMap.HasTempMap");
        obstacleClearTempMap = Svc.PluginInterface.GetIpcSubscriber<bool>("BossMod.ObstacleMap.ClearTempMap");
        obstacleEvaluateQuality = Svc.PluginInterface.GetIpcSubscriber<object?>("BossMod.ObstacleMap.EvaluateTempMapQuality");
    }

    public bool IsAvailable => setActive.HasFunction;

    public bool SetActive(string presetName) => IpcGate.Invoke(setActive.HasFunction, () => setActive.InvokeFunc(presetName), fallback: false, "SetActive failed");

    public bool ClearActive() => IpcGate.Invoke(clearActive.HasFunction, clearActive.InvokeFunc, fallback: false, "ClearActive failed");

    public string? GetActive() => IpcGate.Invoke(getActive.HasFunction, getActive.InvokeFunc, null, "GetActive failed");

    public string? GetPreset(string name) => IpcGate.Invoke<string?>(getPreset.HasFunction, () => getPreset.InvokeFunc(name), null, "GetPreset failed");

    public bool AddTransientStrategy(string preset, string module, string track, string option) => IpcGate.Invoke(addTransient.HasFunction, () => addTransient.InvokeFunc(preset, module, track, option), fallback: false, "AddTransientStrategy failed");

    public bool CanClearTransientStrategy => clearTransient.HasFunction;

    public bool ClearTransientStrategy(string preset, string module, string track) => IpcGate.Invoke(clearTransient.HasFunction, () => clearTransient.InvokeFunc(preset, module, track), fallback: false, "ClearTransientStrategy failed");

    public bool CreatePreset(string serialized, bool overwrite) => IpcGate.Invoke(createPreset.HasFunction, () => createPreset.InvokeFunc(serialized, overwrite), fallback: false, "CreatePreset failed");

    public bool GenerateObstacleMap(Vector3 center, float radius, bool writeToFile = false)
    {
        return IpcGate.Invoke(obstacleGenerate.HasFunction, () => obstacleGenerate.InvokeFunc(center, radius, writeToFile), fallback: false, "GenerateObstacleMap failed");
    }

    public TaskStatus? GetObstacleMapStatus() => IpcGate.Invoke(obstacleGetStatus.HasFunction, (Func<TaskStatus?>)(() => obstacleGetStatus.InvokeFunc()), (TaskStatus?)null, "GetObstacleMapStatus failed");

    public bool HasTempObstacleMap() => IpcGate.Invoke(obstacleHasTempMap.HasFunction, obstacleHasTempMap.InvokeFunc, fallback: false, "HasTempObstacleMap failed");

    public bool ClearTempObstacleMap() => IpcGate.Invoke(obstacleClearTempMap.HasFunction, obstacleClearTempMap.InvokeFunc, fallback: false, "ClearTempObstacleMap failed");

    public bool EvaluateTempMapQualityIsBad()
    {
        return IpcGate.Invoke(obstacleEvaluateQuality.HasFunction, () =>
        {
            object? quality = obstacleEvaluateQuality.InvokeFunc();
            if (quality == null)
            {
                return false;
            }
            PropertyInfo? isBadProperty = quality.GetType().GetProperty(QualityIsBadProperty);
            if (isBadProperty is null)
            {
                return false;
            }
            return isBadProperty.GetValue(quality) is bool isBad && isBad;
        }, fallback: false, "EvaluateTempMapQuality failed");
    }
}
