// src/AlgoLab.Core/Config/DownloadOptions.cs
using System;
using AlgoLab.Core.Models.Enums;

namespace AlgoLab.Core.Config;

/// <summary>
/// Настройки выгрузки свечей.
/// Биндинг идёт из секции "Download" в appsettings.
/// </summary>
public sealed class DownloadOptions
{
    /// <summary>Включить выгрузку при старте CLI.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Начало интервала (желательно в UTC; допускаются локальные/unspecified — нормализуем).</summary>
    public DateTime From { get; set; }

    /// <summary>Конец интервала (будет обрезан по UtcNow).</summary>
    public DateTime To { get; set; }

    /// <summary>Таймфрейм свечей.</summary>
    public TimeframeCore Timeframe { get; set; } = TimeframeCore.Min5;

    /// <summary>Сохранять только завершённые свечи.</summary>
    public bool OnlyCompleted { get; set; } = true;

    /// <summary>Формат экспорта (Csv или Parquet).</summary>
    public ExportFormat Format { get; set; } = ExportFormat.Csv;

    /// <summary>Корневая папка для выгрузки.</summary>
    public string OutputDir { get; set; } = "data";

    /// <summary>
    /// Шаблон подпапок относительно <see cref="OutputDir"/>.
    /// Токены: {instrument}, {timeframe}, {yyyy}, {MM}, {dd}, {from:fmt}, {to:fmt}.
    /// По умолчанию: "{instrument}/{timeframe}".
    /// </summary>
    public string PathTemplate { get; set; } = "{instrument}/{timeframe}";

    /// <summary>
    /// Шаблон имени файла (без расширения).
    /// Токены: {instrument}, {timeframe}, {yyyy}, {MM}, {dd}, а также форматные
    /// {from:fmt} и {to:fmt} (любой .NET формат даты/времени).
    /// По умолчанию: "{instrument}_{timeframe}_{from:yyyyMMdd}_{to:yyyyMMdd}".
    /// </summary>
    public string FileNameTemplate { get; set; } = "{instrument}_{timeframe}_{from:yyyyMMdd}_{to:yyyyMMdd}";

    /// <summary>Дедупликация по timestamp внутри файла после записи.</summary>
    public bool Deduplicate { get; set; } = true;

    /// <summary>Параллелизм по инструментам/окнам на уровне CLI.</summary>
    public int DegreeOfParallelism { get; set; } = 2;
}
