using AlgoLab.Brokers.Tinkoff;
using AlgoLab.CLI;
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

        services.AddInvestApiClient((_, settings) => settings.AccessToken = token);

        // ѕробрасываем клиентов в наш провайдер
        services.AddTinkoffBroker(
            instrumentsFactory: sp => sp.GetRequiredService<InvestApiClient>().Instruments,
            marketDataFactory: sp => sp.GetRequiredService<InvestApiClient>().MarketData
        );

        services.AddHostedService<CliRunner>();
    })
    .UseConsoleLifetime();

await builder.Build().RunAsync();



