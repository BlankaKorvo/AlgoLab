using AlgoLab.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using InstrumentsClient = Tinkoff.InvestApi.V1.InstrumentsService.InstrumentsServiceClient;
using MarketDataClient = Tinkoff.InvestApi.V1.MarketDataService.MarketDataServiceClient;

namespace AlgoLab.Brokers.Tinkoff;

public static class TinkoffServiceCollectionExtensions
{
    public static IServiceCollection AddTinkoffBroker(this IServiceCollection services,
        Func<IServiceProvider, InstrumentsClient> instrumentsFactory,
        Func<IServiceProvider, MarketDataClient> marketDataFactory)
    {
        services.AddSingleton(instrumentsFactory);
        services.AddSingleton(marketDataFactory);
        services.AddSingleton<IInstruments, TinkoffInstruments>();
        services.AddSingleton<IMarketData, TinkoffMarketData>();
        services.AddSingleton<IBrokerApi, TinkoffBrokerApi>();
        return services;
    }
}
