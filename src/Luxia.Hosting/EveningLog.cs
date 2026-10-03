using System.Globalization;
using System.Text;
using Luxia.Messaging.Events;
using Microsoft.Extensions.Logging;

namespace Luxia.Hosting;

/// <summary>
/// Journal de soirée (GEN-111, MUS-025) : un fichier par jour, <c>Documents\LuXia\Journaux\soiree-AAAAMMJJ.csv</c>, avec une ligne par
/// morceau (heure, titre, artiste, style, confiance, méthode, style imposé, correction, show en cours). Une ligne de plus quand le style
/// est imposé ou corrigé. Les morceaux non identifiés ou peu sûrs alimentent l'écran « À classer » (MUS-028) : le journal sert à
/// enrichir la base musicale à la maison. Séparateur « ; », UTF-8 avec marque d'ordre des octets (ouvre dans Excel).
/// </summary>
public sealed class EveningLog : IDisposable
{
    /// <summary>En-tête du fichier.</summary>
    public const string Header = "heure;titre;artiste;style;confiance;méthode;imposé;show";

    private readonly string _folder;
    private readonly ILogger _logger;
    private readonly TimeProvider _time;
    private readonly object _gate = new();
    private readonly List<IDisposable> _subscriptions = [];
    private string? _show;

    /// <summary>Crée le journal et l'abonne au bus.</summary>
    /// <param name="bus">Bus d'événements.</param>
    /// <param name="folder">Dossier des journaux.</param>
    /// <param name="logger">Journal technique.</param>
    /// <param name="time">Temps (réel par défaut).</param>
    public EveningLog(IEventBus bus, string folder, ILogger logger, TimeProvider? time = null)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentNullException.ThrowIfNull(logger);
        _folder = folder;
        _logger = logger;
        _time = time ?? TimeProvider.System;
        _subscriptions.Add(bus.Subscribe<StyleDetected>(OnStyle));
        _subscriptions.Add(bus.Subscribe<ShowStateChanged>(OnShow));
    }

    /// <summary>Fichier du jour donné.</summary>
    /// <param name="day">Jour.</param>
    /// <returns>Chemin du fichier.</returns>
    public string FileFor(DateTimeOffset day) => Path.Combine(_folder, $"soiree-{day.LocalDateTime:yyyyMMdd}.csv");

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
    }

    private static string Cell(string value)
    {
        var text = value.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
        return text.Contains(';', StringComparison.Ordinal) || text.Contains('"', StringComparison.Ordinal) ? $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : text;
    }

    private void OnShow(ShowStateChanged e)
    {
        lock (_gate)
        {
            if (e.Running)
            {
                _show = e.ShowName;
            }
            else if (_show == e.ShowName)
            {
                _show = null;
            }
        }
    }

    private void OnStyle(StyleDetected e)
    {
        if (e.FamilyName.Length == 0 && e.Title.Length == 0)
        {
            return;
        }

        try
        {
            var now = _time.GetLocalNow();
            string show;
            lock (_gate)
            {
                show = _show ?? string.Empty;
            }

            var line = string.Join(
                ';',
                now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                Cell(e.Title),
                Cell(e.Artist),
                Cell(e.FamilyName),
                (e.Confidence * 100).ToString("0", CultureInfo.InvariantCulture),
                Cell(e.Method),
                e.Forced ? "oui" : string.Empty,
                Cell(show));
            var path = FileFor(now);
            lock (_gate)
            {
                Directory.CreateDirectory(_folder);
                var isNew = !File.Exists(path);
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream, new UTF8Encoding(isNew));
                if (isNew)
                {
                    writer.Write(Header + "\r\n");
                }

                writer.Write(line + "\r\n");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Journal de soirée non écrit");
        }
    }
}
