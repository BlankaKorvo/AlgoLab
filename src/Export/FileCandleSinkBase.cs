// src/AlgoLab.Export/FileCandleSinkBase.cs
using System.Collections.Concurrent;
using System.Globalization;
using AlgoLab.Core;
using AlgoLab.Core.Abstractions;
using AlgoLab.Core.Models.Enums;

namespace AlgoLab.Export;

public abstract class FileCandleSinkBase : ICandleSink
{
    protected readonly string RootDir;
    protected readonly string FolderTemplate;
    protected readonly string NameTemplate;
    protected readonly bool Deduplicate;

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    protected FileCandleSinkBase(string rootDir, string folderTemplate, string nameTemplate, bool deduplicate)
        => (RootDir, FolderTemplate, NameTemplate, Deduplicate) =
           (rootDir, folderTemplate ?? "", nameTemplate ?? "", deduplicate);

    public async Task WriteAsync(InstrumentKey key, TimeframeCore timeframe,
        IReadOnlyList<CandleCore> candles, CancellationToken ct = default)
    {
        if (candles.Count == 0) return;

        // вычислим окно по данным (UTC)
        var fromUtc = candles.Min(c => c.Time.ToUniversalTime());
        var toUtc = candles.Max(c => c.Time.ToUniversalTime());

        var folder = PathTemplate.BuildFolder(RootDir, FolderTemplate, key, timeframe, fromUtc, toUtc);
        Directory.CreateDirectory(folder);

        var path = GetFilePath(folder, key, timeframe, fromUtc, toUtc);

        var sem = Locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        try
        {
            await WriteCoreAsync(path, candles, ct);
            await AfterWriteAsync(path, ct);
        }
        finally { sem.Release(); }
    }

    protected virtual Task AfterWriteAsync(string file, CancellationToken ct) => Task.CompletedTask;

    protected abstract string GetFilePath(string folder, InstrumentKey key, TimeframeCore tf,
        DateTime fromUtc, DateTime toUtc);

    protected abstract Task WriteCoreAsync(string file, IReadOnlyList<CandleCore> batch, CancellationToken ct);

    protected static string Invariant(decimal d) => d.ToString(CultureInfo.InvariantCulture);
}
