using AlgoLab.Core.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AlgoLab.Export;

public static class ExportRegistration
{
    public static IServiceCollection AddCandleExport(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddOptions<DownloadOptions>()
                .BindConfiguration("Download"); // берёт секцию "Download" из IConfiguration

        // нужно, чтобы потом удобно получать готовый объект
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<DownloadOptions>>().Value);

        services.AddSingleton<ICandleSink>(sp =>
        {
            var o = sp.GetRequiredService<DownloadOptions>();
            return o.Format switch
            {
                ExportFormat.Csv => new CsvCandleSink(o.OutputDir, o.PathTemplate, o.FileNameTemplate, o.Deduplicate),
                ExportFormat.Parquet => new ParquetCandleSink(o.OutputDir, o.PathTemplate, o.FileNameTemplate, o.Deduplicate),
                _ => throw new NotSupportedException($"Export format {o.Format} not supported")
            };
        });

        return services;
    }
}
