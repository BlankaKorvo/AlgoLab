// src/AlgoLab.Export/PathTemplate.cs
using System.Globalization;
using System.Text.RegularExpressions;
using AlgoLab.Core;
using AlgoLab.Core.Models.Enums;

namespace AlgoLab.Export;

internal static class PathTemplate
{
    private static readonly Regex FromRx = new(@"\{from:(?<fmt>[^}]+)\}", RegexOptions.Compiled);
    private static readonly Regex ToRx = new(@"\{to:(?<fmt>[^}]+)\}", RegexOptions.Compiled);

    public static string BuildFolder(string rootDir, string template,
        InstrumentKey key, TimeframeCore tf, DateTime fromUtc, DateTime toUtc)
        => Path.Combine(rootDir, Replace(template, key, tf, fromUtc, toUtc));

    public static string BuildFileName(string template,
        InstrumentKey key, TimeframeCore tf, DateTime fromUtc, DateTime toUtc)
        => Replace(template, key, tf, fromUtc, toUtc);

    private static string Replace(string tpl, InstrumentKey key, TimeframeCore tf, DateTime fromUtc, DateTime toUtc)
    {
        tpl ??= string.Empty;

        var inst = key.Type == InstrumentIdTypeCore.Ticker
            ? $"{key.Value}@{key.ClassCode}"
            : $"uid-{key.Value}";

        // базовые токены
        var s = tpl
            .Replace("{instrument}", Sanitize(inst))
            .Replace("{timeframe}", tf.ToString())
            // историческая совместимость: {yyyy}/{MM}/{dd} = из fromUtc
            .Replace("{yyyy}", fromUtc.ToString("yyyy", CultureInfo.InvariantCulture))
            .Replace("{MM}", fromUtc.ToString("MM", CultureInfo.InvariantCulture))
            .Replace("{dd}", fromUtc.ToString("dd", CultureInfo.InvariantCulture));

        // форматные токены {from:fmt} и {to:fmt}
        s = FromRx.Replace(s, m => fromUtc.ToString(m.Groups["fmt"].Value, CultureInfo.InvariantCulture));
        s = ToRx.Replace(s, m => toUtc.ToString(m.Groups["fmt"].Value, CultureInfo.InvariantCulture));

        return s;
    }

    private static string Sanitize(string v) => v
        .Replace(':', '_').Replace('@', '_').Replace('/', '_').Replace('\\', '_');
}
