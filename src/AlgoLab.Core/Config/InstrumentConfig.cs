namespace AlgoLab.Core.Config;

public sealed class InstrumentConfig
{
    public string? Uid { get; set; }
    public string? Ticker { get; set; }
    public string? ClassCode { get; set; }
    public string? Note { get; set; }
    public AlgoLab.Core.InstrumentKey ToKey()
    {
        if (!string.IsNullOrWhiteSpace(Uid)) return AlgoLab.Core.InstrumentKey.Uid(Uid!);
        if (!string.IsNullOrWhiteSpace(Ticker) && !string.IsNullOrWhiteSpace(ClassCode))
            return InstrumentKey.Ticker(Ticker!, ClassCode!);
        throw new InvalidOperationException("Specify Uid or Ticker+ClassCode.");
    }
}
