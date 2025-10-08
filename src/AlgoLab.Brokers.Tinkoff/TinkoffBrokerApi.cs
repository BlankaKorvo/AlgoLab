using AlgoLab.Core.Abstractions;
using AlgoLab.Core.Models.Enums;

namespace AlgoLab.Brokers.Tinkoff;

public sealed class TinkoffBrokerApi : IBrokerApi
{
    public IMarketData MarketData { get; }
    public IInstruments Instruments { get; }
    public ITrading? Trading => null;
    public IAccounts? Accounts => null;
    public BrokerCapabilities Capabilities => BrokerCapabilities.MarketData | BrokerCapabilities.Instruments;
    public TinkoffBrokerApi(IMarketData marketData, IInstruments instruments) => (MarketData, Instruments) = (marketData, instruments);
}
