using AlgoLab.Core;
using AlgoLab.Core.Abstractions;
using AlgoLab.Core.Config;
using AlgoLab.Core.Models.Enums;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Tinkoff.InvestApi.V1;
using MarketDataClient = Tinkoff.InvestApi.V1.MarketDataService.MarketDataServiceClient;

namespace AlgoLab.Brokers.Tinkoff;

public sealed class TinkoffMarketData : IMarketData
{
    private readonly MarketDataClient _market;
    private readonly IInstruments _instruments;
    private readonly ILogger<TinkoffMarketData> _log;

    private readonly RateLimiter? _rpsLimiter;
    private readonly RateLimiter? _concLimiter;

    // Polly v8: экспоненциальные ретраи для временных gRPC-ошибок
    private static readonly ResiliencePipeline _rpcRetry =
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(250),
                UseJitter = true,
                ShouldHandle = static args =>
                {
                    var ex = args.Outcome.Exception;
                    if (ex is RpcException rex)
                    {
                        return new ValueTask<bool>(
                            rex.StatusCode is StatusCode.Unavailable
                                           or StatusCode.ResourceExhausted
                                           or StatusCode.DeadlineExceeded
                                           or StatusCode.Internal);
                    }
                    return new ValueTask<bool>(false);
                }
            })
            .Build();

    public TinkoffMarketData(
        MarketDataClient market,
        IInstruments instruments,
        ILogger<TinkoffMarketData> log,
        IOptions<ThrottleOptions>? throttle = null)
    {
        (_market, _instruments, _log) = (market, instruments, log);

        var o = throttle?.Value;
        if (o is { Enabled: true })
        {
            // RPS limiter (FixedWindow)
            if (o.Rps > 0)
            {
                var rpsOptions = new FixedWindowRateLimiterOptions
                {
                    PermitLimit = o.Rps,
                    Window = TimeSpan.FromMilliseconds(Math.Max(100, o.WindowMs)),
                    QueueLimit = Math.Max(0, o.QueueLimit),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                };
                _rpsLimiter = new FixedWindowRateLimiter(rpsOptions);
            }

            // Concurrency limiter
            if (o.Concurrency > 0)
            {
                var concOptions = new ConcurrencyLimiterOptions
                {
                    PermitLimit = o.Concurrency,
                    QueueLimit = Math.Max(0, o.QueueLimit),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                };
                _concLimiter = new ConcurrencyLimiter(concOptions);
            }
        }
    }
    public async Task<IReadOnlyList<CandleCore>> GetCandlesAsync(
        InstrumentKey key,
        DateTime fromUtc,
        DateTime toUtc,
        TimeframeCore timeframe,
        bool onlyCompleted,
        CancellationToken ct = default)
    {
        // 1) UTC-нормализация и защита "в будущее"
        var from = AsUtc(fromUtc);
        var to = AsUtc(toUtc);
        if (to > DateTime.UtcNow) to = DateTime.UtcNow;
        if (from >= to) throw new ArgumentOutOfRangeException(nameof(fromUtc), "from >= to");

        // 2) UID + интервалы (как ты указал)
        var uid = await _instruments.GetUidAsync(key, ct);
        var interval = timeframe switch
        {
            TimeframeCore.Min1 => CandleInterval._1Min,
            TimeframeCore.Min5 => CandleInterval._5Min,
            TimeframeCore.Min15 => CandleInterval._15Min,
            TimeframeCore.Hour1 => CandleInterval.Hour,
            TimeframeCore.Day1 => CandleInterval.Day,
            _ => throw new NotSupportedException($"TF {timeframe} not supported")
        };

        // 3) Выгрузка по окнам (лимиты API), троттлинг + ретраи
        var acc = new List<CandleCore>(2048);
        var windows = 0;

        foreach (var (a, b) in SplitIntoWindows(from, to, timeframe))
        {
            windows++;
            _log.LogDebug("Candles window {Win}: {From:o}..{To:o} for {Key} ({Uid})", windows, a, b, key, uid);

            var req = new GetCandlesRequest
            {
                InstrumentId = uid,
                Interval = interval,
                From = Timestamp.FromDateTime(a), // уже UTC
                To = Timestamp.FromDateTime(b)
            };

            RateLimitLease? concLease = null, rpsLease = null;
            try
            {
                if (_concLimiter != null) concLease = await _concLimiter.AcquireAsync(1, ct);
                if (_rpsLimiter != null) rpsLease = await _rpsLimiter.AcquireAsync(1, ct);

                var res = await _rpcRetry.ExecuteAsync(
                    static async (state, ct2) =>
                        await state.client.GetCandlesAsync(state.request, cancellationToken: ct2).ResponseAsync,
                    (client: _market, request: req),
                    ct);

                foreach (var c in res.Candles)
                {
                    if (onlyCompleted && !c.IsComplete) continue;
                    acc.Add(new CandleCore(
                        c.Time.ToDateTime(),
                        ToDecimal(c.Open),
                        ToDecimal(c.High),
                        ToDecimal(c.Low),
                        ToDecimal(c.Close),
                        c.Volume,
                        c.IsComplete));
                }
            }
            finally
            {
                rpsLease?.Dispose();
                concLease?.Dispose();
            }
        }

        acc.Sort((x, y) => x.Time.CompareTo(y.Time));
        _log.LogInformation("Fetched {Count} candles for {Key} ({Uid}) in {Win} window(s)", acc.Count, key, uid, windows);
        return acc;
    }

    private static DateTime AsUtc(DateTime t) =>
        t.Kind switch
        {
            DateTimeKind.Utc => t,
            DateTimeKind.Local => t.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(t, DateTimeKind.Local).ToUniversalTime()
        };

    private static decimal ToDecimal(Quotation q) => q.Units + q.Nano / 1_000_000_000m;

    // Лимиты окон под InvestAPI:
    // Min1/Min5/Min15 -> до 1 дня; Hour1 -> до 7 дней; Day1 -> до 365 дней
    private static IEnumerable<(DateTime from, DateTime to)> SplitIntoWindows(
        DateTime from, DateTime to, TimeframeCore tf)
    {
        var step = tf switch
        {
            TimeframeCore.Min1 => TimeSpan.FromDays(1),
            TimeframeCore.Min5 => TimeSpan.FromDays(1),
            TimeframeCore.Min15 => TimeSpan.FromDays(1),
            TimeframeCore.Hour1 => TimeSpan.FromDays(7),
            TimeframeCore.Day1 => TimeSpan.FromDays(365),
            _ => throw new NotSupportedException($"TF {tf} not supported")
        };

        for (var a = from; a < to;)
        {
            var b = a + step;
            if (b > to) b = to;
            yield return (a, b);
            a = b;
        }
    }
}
