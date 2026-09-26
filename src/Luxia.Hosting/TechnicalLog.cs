using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Luxia.Hosting;

/// <summary>
/// Journal technique (GEN-110) : fichiers tournants <c>Documents\LuXia\Journaux\technique-AAAAMMJJ.log</c>,
/// 5 Mo maximum par fichier, 20 fichiers conservés.
/// </summary>
public static class TechnicalLog
{
    /// <summary>Taille maximale d'un fichier.</summary>
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    /// <summary>Nombre de fichiers conservés.</summary>
    public const int RetainedFiles = 20;

    /// <summary>Crée la fabrique de journaux de l'application.</summary>
    /// <param name="logsFolder">Dossier des journaux.</param>
    /// <param name="console">Recopier aussi sur la console (outil sans interface).</param>
    /// <param name="minimumLevel">Niveau minimal.</param>
    public static ILoggerFactory Create(string logsFolder, bool console = false, LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        Directory.CreateDirectory(logsFolder);
        var configuration = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(logsFolder, "technique-.log"),
                formatProvider: System.Globalization.CultureInfo.GetCultureInfo("fr-FR"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: MaxFileSizeBytes,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: RetainedFiles,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");

        if (console)
        {
            configuration = configuration.WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                formatProvider: System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));
        }

        var logger = configuration.CreateLogger();
        return LoggerFactory.Create(builder => builder.AddSerilog(logger, dispose: true));
    }
}
