// src/AlgoLab.Export/CsvCandleSink.cs
using AlgoLab.Core;
using AlgoLab.Core.Models.Enums;

namespace AlgoLab.Export;

public sealed class CsvCandleSink : FileCandleSinkBase
{
    public CsvCandleSink(string rootDir, string folderTemplate, string nameTemplate, bool deduplicate)
        : base(rootDir, folderTemplate, nameTemplate, deduplicate) { }

    protected override string GetFilePath(string folder, InstrumentKey key, TimeframeCore tf,
        DateTime fromUtc, DateTime toUtc)
    {
        var name = PathTemplate.BuildFileName(NameTemplate, key, tf, fromUtc, toUtc);
        if (string.IsNullOrWhiteSpace(name))
            name = $"{fromUtc:yyyyMMdd}_{toUtc:yyyyMMdd}";
        return Path.Combine(folder, $"{name}.csv");
    }

    protected override async Task WriteCoreAsync(string file, IReadOnlyList<CandleCore> batch, CancellationToken ct)
    {
        var newFile = !File.Exists(file);
        using var w = new StreamWriter(file, append: true, System.Text.Encoding.UTF8);
        if (newFile) await w.WriteLineAsync("time,open,high,low,close,volume,isComplete");
        foreach (var c in batch)
        {
            await w.WriteLineAsync(string.Join(",",
                c.Time.ToUniversalTime().ToString("O"),
                Invariant(c.Open), Invariant(c.High), Invariant(c.Low), Invariant(c.Close),
                c.Volume, c.IsComplete ? 1 : 0));
        }
    }

    protected override async Task AfterWriteAsync(string file, CancellationToken ct)
    {
        if (!Deduplicate) return;
        var lines = await File.ReadAllLinesAsync(file, ct);
        if (lines.Length <= 2) return;

        var header = lines[0];
        var seen = new HashSet<string>();
        var outLines = new List<string>(lines.Length) { header };

        for (int i = 1; i < lines.Length; i++)
        {
            var l = lines[i];
            var comma = l.IndexOf(',');
            if (comma <= 0) continue;
            var ts = l[..comma];
            if (seen.Add(ts)) outLines.Add(l);
        }
        if (outLines.Count != lines.Length)
            await File.WriteAllLinesAsync(file, outLines, ct);
    }
}
