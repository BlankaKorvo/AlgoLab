using AlgoLab.Core.Models.Enums;

namespace AlgoLab.Core.Abstractions;

public interface IMarketData
{
        Task<IReadOnlyList<CandleCore>> GetCandlesAsync(
        InstrumentKey key, DateTime fromUtc, DateTime toUtc,
        TimeframeCore timeframe, bool onlyCompleted, CancellationToken ct = default);
}
