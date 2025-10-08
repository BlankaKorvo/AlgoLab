using AlgoLab.Core;
using AlgoLab.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Tinkoff.InvestApi.V1;
using InstrumentsClient = Tinkoff.InvestApi.V1.InstrumentsService.InstrumentsServiceClient;

public sealed class TinkoffInstruments : IInstruments
{
    private readonly InstrumentsClient _client;
    private readonly ILogger<TinkoffInstruments> _log;
    private readonly ConcurrentDictionary<InstrumentKey, string> _uidCache = new();

    public TinkoffInstruments(InstrumentsClient client, ILogger<TinkoffInstruments> log)
        => (_client, _log) = (client, log);

    public async Task<string> GetUidAsync(InstrumentKey key, CancellationToken ct = default)
    {
        if (key.Type == AlgoLab.Core.Models.Enums.InstrumentIdTypeCore.Uid)
            return key.Value;

        if (_uidCache.TryGetValue(key, out var cached))
        {
            _log.LogDebug("UID cache hit for {key}", key);
            return cached;
        }

        var req = new InstrumentRequest
        {
            IdType = InstrumentIdType.Ticker,
            ClassCode = key.ClassCode!,
            Id = key.Value
        };

        var resp = await _client.GetInstrumentByAsync(req, cancellationToken: ct);
        var uid = resp.Instrument?.Uid;
        if (string.IsNullOrWhiteSpace(uid))
            throw new InvalidOperationException($"UID not found for {key}");

        _uidCache[key] = uid;
        _log.LogDebug("UID cached for {key}: {uid}", key, uid);
        return uid;
    }

    public async Task<InstrumentCore> GetByAsync(InstrumentKey key, CancellationToken ct = default)
    {
        var req = new InstrumentRequest
        {
            IdType = key.Type == AlgoLab.Core.Models.Enums.InstrumentIdTypeCore.Uid
                        ? InstrumentIdType.Uid
                        : InstrumentIdType.Ticker,
            ClassCode = key.ClassCode ?? string.Empty,
            Id = key.Value
        };

        var resp = await _client.GetInstrumentByAsync(req, cancellationToken: ct);
        var i = resp.Instrument ?? throw new InvalidOperationException($"Instrument not found: {key}");

        return new InstrumentCore(
            Uid: i.Uid,
            Ticker: string.IsNullOrWhiteSpace(i.Ticker) ? null : i.Ticker,
            ClassCode: string.IsNullOrWhiteSpace(i.ClassCode) ? null : i.ClassCode,
            Name: string.IsNullOrWhiteSpace(i.Name) ? null : i.Name,
            Type: i.InstrumentType.ToString()
        );
    }
}