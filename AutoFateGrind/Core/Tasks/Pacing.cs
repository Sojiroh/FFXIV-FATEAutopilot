using System;

namespace AutoFateGrind.Core.Tasks;

internal static class Pacing
{
    private const double ReactionSkewExponent = 2.0;

    private const double DistractionChance = 0.05;

    private const int DistractionMinMs = 8000;

    private const int DistractionMaxMs = 20000;

    private const int MaxReactionSec = 30;

    private const float TeleportShortcutMinSavingMeters = 220f;

    private const float TeleportShortcutMaxSavingMeters = 450f;

    private const double IdleSwapSpreadLow = 0.7;

    private const double IdleSwapSpreadHigh = 1.5;

    private const int FollowUpExtraMaxMs = 10000;

    private const int ReviveMinMs = 2000;

    private const int ReviveMaxMs = 8000;

    private const double BreakIntervalSpread = 0.3;

    private static readonly Random random = new Random();

    public static bool Active => Plugin.Cfg.PacingEnabled;

    public static bool PickVarietyActive => Plugin.Cfg.PacingEnabled && Plugin.Cfg.PacingPickVariety;

    public static int ReactionDelayMs()
    {
        if (!Active)
        {
            return 0;
        }
        var cfg = Plugin.Cfg;
        var minMs = Math.Clamp(cfg.PacingReactionMinSec, 0, MaxReactionSec) * 1000;
        var maxMs = Math.Max(minMs, Math.Clamp(cfg.PacingReactionMaxSec, 0, MaxReactionSec) * 1000);
        var reactionMs = minMs + (int)((double)(maxMs - minMs) * Math.Pow(random.NextDouble(), ReactionSkewExponent));
        if (random.NextDouble() < DistractionChance)
        {
            reactionMs += Roll(DistractionMinMs, DistractionMaxMs);
        }
        return reactionMs;
    }

    public static float TeleportShortcutSavingMeters(float baselineMeters)
    {
        if (!Active)
        {
            return baselineMeters;
        }
        return TeleportShortcutMinSavingMeters + (float)random.NextDouble() * (TeleportShortcutMaxSavingMeters - TeleportShortcutMinSavingMeters);
    }

    public static int IdleWaitBeforeSwapMs(int baselineMs)
    {
        if (!Active)
        {
            return baselineMs;
        }
        return Roll((int)((double)baselineMs * IdleSwapSpreadLow), (int)((double)baselineMs * IdleSwapSpreadHigh));
    }

    public static int FollowUpWatchMs(int baselineMs)
    {
        if (!Active)
        {
            return baselineMs;
        }
        return baselineMs + Roll(0, FollowUpExtraMaxMs);
    }

    public static int ReviveDelayMs()
    {
        if (!Active)
        {
            return 0;
        }
        return Roll(ReviveMinMs, ReviveMaxMs);
    }

    public static int FatesBeforeBreak(int configured)
    {
        var baseline = Math.Max(1, configured);
        if (!Active)
        {
            return baseline;
        }
        var jitter = 1.0 + (random.NextDouble() * 2.0 - 1.0) * BreakIntervalSpread;
        return Math.Max(1, (int)Math.Round((double)baseline * jitter));
    }

    public static int RollIndex(int count) => random.Next(count);

    public static int RollBetween(int minimum, int maximum) => Roll(Math.Min(minimum, maximum), Math.Max(minimum, maximum));

    private static int Roll(int minimum, int maximum) => random.Next(minimum, maximum + 1);
}
