using System;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using clib.Utils;

namespace AutoFateGrind.Core.Game.Fates;

internal static class FateClock
{
    private const float UnknownRemainingSeconds = -1f;

    public static long LocalClockOffsetSeconds => DateTimeOffset.UtcNow.ToUnixTimeSeconds() - Framework.GetServerTime();

    public static float Remaining(PublicEvent fate)
    {
        var endTimeEpoch = fate.EndTimeEpoch;
        if (endTimeEpoch == 0)
        {
            return UnknownRemainingSeconds;
        }
        return endTimeEpoch - Framework.GetServerTime();
    }

    public static bool IsOnMap(PublicEvent fate) => (fate.State == FateState.Running && Remaining(fate) > 0f) || fate.IsOnMap;
}
