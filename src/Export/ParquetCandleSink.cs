using AlgoLab.Core;
using AlgoLab.Core.Models.Enums;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
// алиас, чтобы точно взять тип из Parquet.Net
using PqSchema = Parquet.Schema.ParquetSchema;

namespace AlgoLab.Export;

/// <summary>
/// Сохраняет свечи в суточные parquet-файлы:
/// {OutputDir}/{instrument}/{timeframe}/{yyyy}/{MM}/{dd}/{yyyyMMdd}.parquet
/// </summary>
public sealed class ParquetCandleSink : FileCandleSinkBase
{
    // актуальный конструктор
    public ParquetCandleSink(string rootDir, string folderTemplate, string nameTemplate, bool deduplicate)
        : base(rootDir, folderTemplate, nameTemplate, deduplicate) { }

    // чтобы не падали старые вызовы (опционально)
    public ParquetCandleSink(string rootDir, string template, bool deduplicate)
        : this(rootDir, template, "{instrument}_{timeframe}_{from:yyyyMMdd}_{to:yyyyMMdd}", deduplicate) { }

    protected override string GetFilePath(string folder, InstrumentKey key, TimeframeCore tf,
        DateTime fromUtc, DateTime toUtc)
    {
        var name = PathTemplate.BuildFileName(NameTemplate, key, tf, fromUtc, toUtc);
        if (string.IsNullOrWhiteSpace(name))
            name = $"{fromUtc:yyyyMMdd}_{toUtc:yyyyMMdd}";
        return Path.Combine(folder, $"{name}.parquet");
    }

    /// <summary>
    /// Parquet.Net 4.x не поддерживает append in-place:
    /// читаем существующее (если есть), объединяем с новой партией,
    /// делаем дедуп по time и перезаписываем одним row group.
    /// </summary>
    protected override async Task WriteCoreAsync(string file, IReadOnlyList<CandleCore> batch, CancellationToken ct)
    {
        // 1) Собираем новые данные в словарь по ключу time (UTC)
        var map = new Dictionary<DateTime, (decimal O, decimal H, decimal L, decimal C, long V, bool IComplete)>();
        foreach (var c in batch)
        {
            var t = c.Time.ToUniversalTime();
            map[t] = (c.Open, c.High, c.Low, c.Close, c.Volume, c.IsComplete);
        }

        // 2) Если файл есть — подмешиваем существующие строки
        if (File.Exists(file))
        {
            using var fr = File.OpenRead(file);
            using var reader = await ParquetReader.CreateAsync(fr, cancellationToken: ct);
            using var rg = reader.OpenRowGroupReader(0);

            var s = reader.Schema;
            var t = (DateTime[])(await rg.ReadColumnAsync(s.DataFields[0], ct)).Data;
            var o = (decimal[])(await rg.ReadColumnAsync(s.DataFields[1], ct)).Data;
            var h = (decimal[])(await rg.ReadColumnAsync(s.DataFields[2], ct)).Data;
            var l = (decimal[])(await rg.ReadColumnAsync(s.DataFields[3], ct)).Data;
            var c = (decimal[])(await rg.ReadColumnAsync(s.DataFields[4], ct)).Data;
            var v = (long[])(await rg.ReadColumnAsync(s.DataFields[5], ct)).Data;
            var ic = (bool[])(await rg.ReadColumnAsync(s.DataFields[6], ct)).Data;

            for (int i = 0; i < t.Length; i++)
                map[t[i]] = (o[i], h[i], l[i], c[i], v[i], ic[i]); // новые перекрывают старые
        }

        // 3) Сортируем по времени и формируем колонки
        var ordered = map.OrderBy(kv => kv.Key).ToArray();

        var times = ordered.Select(kv => kv.Key).ToArray();
        var opens = ordered.Select(kv => kv.Value.O).ToArray();
        var highs = ordered.Select(kv => kv.Value.H).ToArray();
        var lows = ordered.Select(kv => kv.Value.L).ToArray();
        var closes = ordered.Select(kv => kv.Value.C).ToArray();
        var vols = ordered.Select(kv => kv.Value.V).ToArray();
        var comps = ordered.Select(kv => kv.Value.IComplete).ToArray();

        // 4) Схема и запись одним row group
        var schema = new PqSchema(
            new DataField<DateTime>("time"),
            new DecimalDataField("open", 18, 9),
            new DecimalDataField("high", 18, 9),
            new DecimalDataField("low", 18, 9),
            new DecimalDataField("close", 18, 9),
            new DataField<long>("volume"),
            new DataField<bool>("isComplete")
        );

        using var fs = File.Create(file);
        using var writer = await ParquetWriter.CreateAsync(schema, fs, cancellationToken: ct);
        using var rgw = writer.CreateRowGroup();

        await rgw.WriteColumnAsync(new DataColumn(schema.DataFields[0], times), ct);
        await rgw.WriteColumnAsync(new DataColumn(schema.DataFields[1], opens), ct);
        await rgw.WriteColumnAsync(new DataColumn(schema.DataFields[2], highs), ct);
        await rgw.WriteColumnAsync(new DataColumn(schema.DataFields[3], lows), ct);
        await rgw.WriteColumnAsync(new DataColumn(schema.DataFields[4], closes), ct);
        await rgw.WriteColumnAsync(new DataColumn(schema.DataFields[5], vols), ct);
        await rgw.WriteColumnAsync(new DataColumn(schema.DataFields[6], comps), ct);
    }
}
