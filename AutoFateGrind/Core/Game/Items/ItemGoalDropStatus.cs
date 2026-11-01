using System;

namespace AutoFateGrind.Core.Game.Items;

internal readonly record struct ItemGoalDropStatus(uint ItemId, int Have, int Need)
{
    public bool Done => Have >= Need;

    public int Left => Math.Max(0, Need - Have);
}
