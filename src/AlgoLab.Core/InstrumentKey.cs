using AlgoLab.Core.Models.Enums;

namespace AlgoLab.Core;

public readonly record struct InstrumentKey
{
    public string Value { get; }
    public InstrumentIdTypeCore Type { get; }
    public string? ClassCode { get; }
    private InstrumentKey(string value, InstrumentIdTypeCore type, string? classCode)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new System.ArgumentException("Empty value", nameof(value));
        if (type == InstrumentIdTypeCore.Ticker && string.IsNullOrWhiteSpace(classCode))
            throw new System.ArgumentException("ClassCode required for Ticker", nameof(classCode));
        Value = value; Type = type; ClassCode = classCode;
    }
    public static InstrumentKey Uid(string uid) => new(uid, InstrumentIdTypeCore.Uid, null);
    public static InstrumentKey Ticker(string t, string c) => new(t, InstrumentIdTypeCore.Ticker, c);
    public override string ToString() => Type == InstrumentIdTypeCore.Ticker ? $"{Value}@{ClassCode}" : $"uid:{Value}";
}
