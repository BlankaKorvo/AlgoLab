using AlgoLab.Core;
using AlgoLab.Core.Abstractions;
using AlgoLab.Core.Models.Enums;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Tinkoff.InvestApi.V1;
using MarketDataClient = Tinkoff.InvestApi.V1.MarketDataService.MarketDataServiceClient;

namespace AlgoLab.Brokers.Tinkoff;

public sealed class TinkoffMarketData : IMarketData
{
    private readonly MarketDataClient _market;
    private readonly IInstruments _instruments;
    private readonly ILogger<TinkoffMarketData> _log;
    public TinkoffMarketData(MarketDataClient market, IInstruments instruments, ILogger<TinkoffMarketData> log) => (_market,_instruments,_log)=(market,instruments,log);
    public async Task<IReadOnlyList<CandleCore>> GetCandlesAsync(InstrumentKey key, DateTime fromUtc, DateTime toUtc, TimeframeCore timeframe, bool onlyCompleted, CancellationToken ct=default)
    {
        if (fromUtc >= toUtc) throw new System.ArgumentOutOfRangeException(nameof(fromUtc), "from >= to");
        var uid = await _instruments.GetUidAsync(key, ct);
        var interval = timeframe switch 
        { 
            TimeframeCore.Min1=>CandleInterval._1Min, 
            TimeframeCore.Min5=>CandleInterval._5Min, 
            TimeframeCore.Min15=>CandleInterval._15Min, 
            TimeframeCore.Hour1=>CandleInterval.Hour, 
            TimeframeCore.Day1=>CandleInterval.Day, 
            _=>throw new NotSupportedException($"TF {timeframe} not supported") 
        };
        var acc = new List<CandleCore>(2048);
        var windows=0;
        foreach (var (a,b) in SplitIntoWindows(fromUtc,toUtc,timeframe))
        {
            windows++;
            _log.LogDebug("Candles window {Win}: {From:o}..{To:o} for {Key} ({Uid})", windows, a, b, key, uid);
            var req=new GetCandlesRequest
            { 
                InstrumentId=uid, 
                Interval=interval, 
                From=Timestamp.FromDateTime(System.DateTime.SpecifyKind(a,System.DateTimeKind.Utc)), 
                To=Timestamp.FromDateTime(System.DateTime.SpecifyKind(b,System.DateTimeKind.Utc)) 
            };
            var res = await _market.GetCandlesAsync(req, cancellationToken: ct);
            foreach(var c in res.Candles)
            { 
                if(onlyCompleted && !c.IsComplete) continue; 
                acc.Add(new CandleCore(c.Time.ToDateTime(), ToDecimal(c.Open), ToDecimal(c.High), ToDecimal(c.Low), ToDecimal(c.Close), c.Volume, c.IsComplete)); 
            }
        }
        acc.Sort((x,y)=>x.Time.CompareTo(y.Time));
        _log.LogInformation("Fetched {Count} candles for {Key} ({Uid}) in {Win} window(s)", acc.Count, key, uid, windows); 
        return acc;
    }
    private static decimal ToDecimal(Quotation q)=> q.Units + q.Nano/1_000_000_000m;
    private static IEnumerable<(System.DateTime from, System.DateTime to)> SplitIntoWindows(DateTime from, DateTime to, TimeframeCore tf)
    {
        var step = tf switch 
        { 
            TimeframeCore.Min1=>System.TimeSpan.FromDays(30), 
            TimeframeCore.Min5=>System.TimeSpan.FromDays(90), 
            TimeframeCore.Min15=>System.TimeSpan.FromDays(120), 
            TimeframeCore.Hour1=>System.TimeSpan.FromDays(180), 
            _=>System.TimeSpan.FromDays(365) 
        };
        for(var a=from; a<to; )
        {
            var b=a+step; 
            if(b>to) b=to; 
            yield return (a,b);
            a=b; 
        }
    }
}
