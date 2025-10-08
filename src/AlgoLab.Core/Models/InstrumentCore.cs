namespace AlgoLab.Core;

public readonly record struct InstrumentCore(
    string Uid, string? Ticker, string? ClassCode, string? Name, string? Type);
