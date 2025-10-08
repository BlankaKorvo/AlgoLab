namespace AlgoLab.Core.Config;

public sealed class ThrottleOptions
{
    public bool Enabled { get; set; } = true;
    /// <summary>Запросов в секунду (общий лимит RPS)</summary>
    public int Rps { get; set; } = 3;
    /// <summary>Максимум одновременных запросов</summary>
    public int Concurrency { get; set; } = 2;
    /// <summary>Очередь ожидания лимитера</summary>
    public int QueueLimit { get; set; } = 200;
    /// <summary>Размер окна в мс для RPS (обычно 1000)</summary>
    public int WindowMs { get; set; } = 1000;
}
