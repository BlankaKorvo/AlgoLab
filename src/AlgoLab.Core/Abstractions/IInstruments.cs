namespace AlgoLab.Core.Abstractions;

public interface IInstruments
{
    Task<string> GetUidAsync(InstrumentKey key, CancellationToken ct = default);
    Task<InstrumentCore> GetByAsync(InstrumentKey key, CancellationToken ct = default);
}
