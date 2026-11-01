namespace AutoFateGrind.Core.Trading;

public enum TradeEntryState : byte
{
    Needed,
    Unknown,
    Learned,
    Held,
    Stocked,
    NoTrader
}
