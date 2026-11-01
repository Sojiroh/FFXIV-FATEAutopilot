namespace AutoFateGrind.Core.Game.Watchers;

internal sealed record TeleportRefusal(uint LogMessageId, string Text, long Tick)
{
    public bool IsAlreadyUnderway => LogMessageId == TeleportRefusals.AlreadyUnderwayId;

    public bool IsPermanent => LogMessageId is TeleportRefusals.NotAttunedId or TeleportRefusals.InsufficientGilId;

    public bool IsInsufficientGil => LogMessageId == TeleportRefusals.InsufficientGilId;
}
