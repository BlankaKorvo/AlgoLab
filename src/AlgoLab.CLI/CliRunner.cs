using System.Globalization;
using AlgoLab.Core;
using AlgoLab.Core.Abstractions;
using AlgoLab.Core.Config;
using AlgoLab.Core.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AlgoLab.CLI;

public sealed class CliRunner : BackgroundService
{
    private readonly ILogger<CliRunner> _log;
    private readonly IConfiguration _cfg;
    private readonly IBrokerApi _api;
    private readonly ICandleSink _sink;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly DownloadOptions _opt;

    public CliRunner(
        ILogger<CliRunner> log,
        IConfiguration cfg,
        IBrokerApi api,
        ICandleSink sink,
        DownloadOptions opt,
        IHostApplicationLifetime lifetime)
        => (_log, _cfg, _api, _sink, _opt, _lifetime) = (log, cfg, api, sink, opt, lifetime);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var resolve = _cfg.GetValue("CLI:ResolveOnStart", true);

            var instSection = _cfg.GetSection("Instruments");
            var list = new List<InstrumentConfig>();
            instSection.Bind(list);

            if (resolve && list.Count > 0)
            {
                _log.LogInformation("Resolving {Count} instruments...", list.Count);
                foreach (var ic in list)
                {
                    var key = ic.ToKey();
                    var uid = await _api.Instruments.GetUidAsync(key, stoppingToken);
                    _log.LogInformation("{Key} -> {Uid}", key, uid);
                }
            }

            if (_opt.Enabled && list.Count > 0)
            {
                _log.LogInformation("Download: {From:o}..{To:o}, TF={TF}, CompletedOnly={C}, Format={Fmt}, DOP={Dop}",
                    _opt.From, _opt.To, _opt.Timeframe, _opt.OnlyCompleted, _opt.Format, _opt.DegreeOfParallelism);

                await Parallel.ForEachAsync(list, new ParallelOptions
                {
                    CancellationToken = stoppingToken,
                    MaxDegreeOfParallelism = Math.Max(1, _opt.DegreeOfParallelism)
                }, async (ic, ct) =>
                {
                    var key = ic.ToKey();
                    try
                    {
                        var candles = await _api.MarketData.GetCandlesAsync(
                            key, _opt.From, _opt.To, _opt.Timeframe, _opt.OnlyCompleted, ct);

                        _log.LogInformation("{Key}: {Count} candles", key, candles.Count);
                        await _sink.WriteAsync(key, _opt.Timeframe, candles, ct);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Failed on {Key}", key);
                    }
                });

                _log.LogInformation("Download completed.");
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "CLI run failed");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
