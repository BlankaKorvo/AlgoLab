
using AlgoLab.Core;


using AlgoLab.Core.Models.Enums;


/// <summary>Приёмник свечей (файл/БД/поток и т.п.).</summary>
public interface ICandleSink
{
    /// <summary>Сохранить партию свечей.</summary>
    Task WriteAsync(
        InstrumentKey key,
        TimeframeCore timeframe,
        IReadOnlyList<CandleCore> candles,
        CancellationToken ct = default);
}
