using AlgoLab.Core.Models.Enums;

namespace AlgoLab.Core.Abstractions;

public interface IBrokerApi
{
    IMarketData MarketData { get; }
    IInstruments Instruments { get; }
    ITrading? Trading { get; }
    IAccounts? Accounts { get; }
    BrokerCapabilities Capabilities { get; }
}
