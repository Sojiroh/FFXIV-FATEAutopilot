using System;
using AutoFateGrind.Core.Tasks;

namespace AutoFateGrind.Core.Ipc;

internal static class BossModMovementTuning
{
    public const string Module = "BossMod.Autorotation.MiscAI.NormalMovement";

    private const string SeparateDodgeDelayTrack = "SeparateDodgeDelay";

    private const string DodgeDelayTrack = "DodgeDelayMovement";

    private const string MoveDelayTrack = "DelayMovement";

    private const string CushionTrack = "ForbiddenZoneCushion";

    private const string EnabledOption = "Enabled";

    public static readonly string[] DelayOptions = new string[] { "None", "Short", "Long" };

    public static readonly string[] CushionOptions = new string[] { "None", "Small", "Medium", "Large" };

    private static readonly string[] tracks = new string[] { SeparateDodgeDelayTrack, DodgeDelayTrack, MoveDelayTrack, CushionTrack };

    private static string? overridePreset;

    private static bool failureLogged;

    public static bool? PresetHasModule(string preset)
    {
        var presetText = BossModIPC.Instance.GetPreset(preset);
        if (presetText == null)
        {
            return null;
        }
        return presetText.Contains(Module, StringComparison.Ordinal);
    }

    public static void Apply(string preset, uint fateId)
    {
        var cfg = Plugin.Cfg;
        if (!cfg.CombatMovementEnabled || !BossModIPC.Instance.CanClearTransientStrategy)
        {
            ClearOverrides();
            return;
        }
        if (overridePreset != preset)
        {
            ClearOverrides();
        }
        var dodgeDelay = Roll(DelayOptions, cfg.CombatDodgeDelayMin, cfg.CombatDodgeDelayMax);
        var moveDelay = Roll(DelayOptions, cfg.CombatMoveDelayMin, cfg.CombatMoveDelayMax);
        var cushion = Roll(CushionOptions, cfg.CombatCushionMin, cfg.CombatCushionMax);
        var bossMod = BossModIPC.Instance;
        if ((bossMod.AddTransientStrategy(preset, Module, SeparateDodgeDelayTrack, EnabledOption) && bossMod.AddTransientStrategy(preset, Module, DodgeDelayTrack, dodgeDelay) && bossMod.AddTransientStrategy(preset, Module, MoveDelayTrack, moveDelay) && bossMod.AddTransientStrategy(preset, Module, CushionTrack, cushion)) is false)
        {
            var trackNames = tracks;
            foreach (var track in trackNames)
            {
                bossMod.ClearTransientStrategy(preset, Module, track);
            }
            overridePreset = null;
            if (!failureLogged)
            {
                failureLogged = true;
                RunLog.Warning("Combat movement variation not applied to '" + preset + "' (no NormalMovement module in the preset, or a BossMod build without these options).");
            }
            return;
        }
        overridePreset = preset;
        RunLog.Info($"Combat movement for FATE {fateId} on '{preset}': dodge delay {dodgeDelay}, movement delay {moveDelay}, cushion {cushion}.");
    }

    public static void Release()
    {
        ClearOverrides();
        failureLogged = false;
    }

    private static void ClearOverrides()
    {
        if (overridePreset == null)
        {
            return;
        }
        var trackNames = tracks;
        foreach (var track in trackNames)
        {
            BossModIPC.Instance.ClearTransientStrategy(overridePreset, Module, track);
        }
        RunLog.Info("Combat movement overrides cleared from '" + overridePreset + "'.");
        overridePreset = null;
    }

    private static string Roll(string[] options, int minimum, int maximum)
    {
        var max = options.Length - 1;
        return options[Pacing.RollBetween(Math.Clamp(minimum, 0, max), Math.Clamp(maximum, 0, max))];
    }
}
