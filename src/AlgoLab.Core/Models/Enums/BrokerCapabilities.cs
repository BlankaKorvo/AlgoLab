namespace AlgoLab.Core.Models.Enums;

[System.Flags]

public enum BrokerCapabilities
{
    None        = 0,
    MarketData  = 1,
    Instruments = 2,
    Trading     = 4,
    Accounts    = 8,
    Streaming   = 16
}
