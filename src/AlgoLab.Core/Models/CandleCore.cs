namespace AlgoLab.Core;

public readonly record struct CandleCore(
    DateTime Time, decimal Open, decimal High,
    decimal Low, decimal Close, long Volume, bool IsComplete);
