using AlgoLab.Brokers.Tinkoff;
using AlgoLab.CLI;
using AlgoLab.Core.Config;
using AlgoLab.Export;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi;

var builder = Host.CreateDefaultBuilder(args)

    .ConfigureAppConfiguration(cfg =>
    {
        cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        cfg.AddEnvironmentVariables(prefix: "ALGOLAB_");
    })
.ConfigureServices((ctx, services) =>
{
    services.AddLogging(b => b.AddConsole());

    var token = Environment.GetEnvironmentVariable("TINKOFF_TOKEN")
                ?? ctx.Configuration["Tinkoff:Token"]
                ?? throw new InvalidOperationException("Set TINKOFF_TOKEN or Tinkoff:Token");

    var api = InvestApiClientFactory.Create(token);

    services.AddTinkoffBroker(
        instrumentsFactory: _ => api.Instruments,
        marketDataFactory: _ => api.MarketData);

    // троттлинг (секция "Throttle")
    services.AddOptions<ThrottleOptions>()
            .BindConfiguration("Throttle");

    // экспорт как раньше
    services.AddCandleExport(ctx.Configuration);

    services.AddHostedService<CliRunner>();
})
    .UseConsoleLifetime();

await builder.Build().RunAsync();



