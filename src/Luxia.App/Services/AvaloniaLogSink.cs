using Avalonia.Logging;
using Microsoft.Extensions.Logging;

namespace Luxia.App.Services;

/// <summary>
/// Journaux internes d'Avalonia (liaisons, propriétés, mise en page) envoyés au journal technique (GEN-110, GEN-117) :
/// une liaison cassée ou une valeur refusée par un champ doit laisser une trace, pas seulement un message à l'écran.
/// </summary>
internal sealed class AvaloniaLogSink(ILogger logger) : ILogSink
{
    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate) =>
        Log(level, area, source, messageTemplate, []);

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        if (!IsEnabled(level, area))
        {
            return;
        }

        var text = propertyValues.Length == 0 ? messageTemplate : $"{messageTemplate} — {string.Join(" ; ", propertyValues)}";

#pragma warning disable CA2254 // Le modèle vient d'Avalonia : on le journalise tel quel, sans le réinterpréter.
        logger.Log(level >= LogEventLevel.Error ? LogLevel.Error : LogLevel.Warning, "Avalonia [{Zone}] {Source} : {Message}", area, source?.GetType().Name, text);
#pragma warning restore CA2254
    }
}
