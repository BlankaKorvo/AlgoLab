using AlgoLab.Core.Abstractions;
using AlgoLab.Core.Config;
using AlgoLab.Core.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace AlgoLab.CLI
{
    public sealed class CliRunner : BackgroundService
    {
        private readonly ILogger<CliRunner> _log;
        private readonly IConfiguration _cfg;
        private readonly IBrokerApi _api;
        private readonly IHostApplicationLifetime _lifetime;

        public CliRunner(ILogger<CliRunner> log, IConfiguration cfg, IBrokerApi api, IHostApplicationLifetime lifetime)
            => (_log, _cfg, _api, _lifetime) = (log, cfg, api, lifetime);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var resolve = _cfg.GetValue("CLI:ResolveOnStart", true);
                var enableDownload = _cfg.GetValue("Download:Enabled", false);

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

                if (enableDownload && list.Count > 0)
                {
                    var from = DateTime.Parse(_cfg["Download:From"]!, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                    var to = DateTime.Parse(_cfg["Download:To"]!, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                    var tf = Enum.Parse<TimeframeCore>(_cfg["Download:Timeframe"]!);
                    var onlyCompleted = _cfg.GetValue("Download:OnlyCompleted", true);

                    foreach (var ic in list)
                    {
                        var key = ic.ToKey();
                        var candles = await _api.MarketData.GetCandlesAsync(key, from, to, tf, onlyCompleted, stoppingToken);
                        _log.LogInformation("{Key}: {Count} candles", key, candles.Count);
                    }
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
}
