using AlgoLab.Brokers.Tinkoff;
using AlgoLab.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace AlgoLab.Tests;

public class DiValidation
{
    [Test]
    public void ServiceProvider_Builds_With_Fakes()
    {
        var services=new ServiceCollection(); services.AddLogging();
        Tinkoff.InvestApi.V1.InstrumentsService.InstrumentsServiceClient i=null!;
        Tinkoff.InvestApi.V1.MarketDataService.MarketDataServiceClient m=null!;
        services.AddTinkoffBroker(_=>i,_=>m);
        var sp=services.BuildServiceProvider();
        Assert.IsNotNull(sp.GetRequiredService<IBrokerApi>());
        Assert.IsNotNull(sp.GetRequiredService<IInstruments>());
        Assert.IsNotNull(sp.GetRequiredService<IMarketData>());
    }
}
