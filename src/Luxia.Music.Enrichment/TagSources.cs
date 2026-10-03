using System.Globalization;
using System.Text.Json;
using Luxia.Music.Classification;

namespace Luxia.Music.Enrichment;

/// <summary>Source en ligne d'étiquettes de genre d'un artiste (MUS-040).</summary>
public interface ITagSource
{
    /// <summary>Nom de la source (« MusicBrainz », « Last.fm »).</summary>
    string Name { get; }

    /// <summary>Étiquettes d'un artiste.</summary>
    /// <param name="artist">Nom de l'artiste.</param>
    /// <param name="cancellation">Annulation.</param>
    /// <returns>Les étiquettes ; liste vide si l'artiste est inconnu de la source ; <c>null</c> si la source est en erreur (réseau, limite).</returns>
    Task<IReadOnlyList<TagCount>?> GetTagsAsync(string artist, CancellationToken cancellation);
}

/// <summary>
/// MusicBrainz (base ouverte, sans clé) : recherche de l'artiste puis ses étiquettes et genres. Respecte la limite du service
/// (une requête par seconde) et se présente avec un nom d'application (MUS-042).
/// </summary>
public sealed class MusicBrainzSource : ITagSource
{
    private static readonly TimeSpan MinimumInterval = TimeSpan.FromMilliseconds(1100);

    private readonly HttpClient _http;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly TimeProvider _time;
    private DateTimeOffset _lastRequest = DateTimeOffset.MinValue;

    /// <summary>Crée la source.</summary>
    /// <param name="http">Client HTTP (le nom d'application est ajouté s'il manque).</param>
    /// <param name="time">Temps (réel par défaut).</param>
    /// <param name="delay">Attente entre deux requêtes (réelle par défaut ; simulée en test).</param>
    public MusicBrainzSource(HttpClient http, TimeProvider? time = null, Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
        _time = time ?? TimeProvider.System;
        _delay = delay ?? Task.Delay;
        if (http.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            http.DefaultRequestHeaders.UserAgent.ParseAdd("LuXia-enrichissement/1.011 (https://github.com/paillardfrancois35-jpg/Luxia)");
        }
    }

    /// <inheritdoc />
    public string Name => "MusicBrainz";

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagCount>?> GetTagsAsync(string artist, CancellationToken cancellation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artist);
        var wait = _lastRequest + MinimumInterval - _time.GetUtcNow();
        if (wait > TimeSpan.Zero)
        {
            await _delay(wait, cancellation).ConfigureAwait(false);
        }

        _lastRequest = _time.GetUtcNow();
        var query = Uri.EscapeDataString($"artist:\"{artist.Replace("\"", string.Empty, StringComparison.Ordinal)}\"");
        try
        {
            using var response = await _http.GetAsync(new Uri($"https://musicbrainz.org/ws/2/artist/?query={query}&fmt=json&limit=3", UriKind.Absolute), cancellation).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return Parse(await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false), artist);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException && !cancellation.IsCancellationRequested)
        {
            return null;
        }
    }

    /// <summary>Lit la réponse de la recherche d'artistes : étiquettes et genres du premier résultat dont le nom correspond.</summary>
    /// <param name="json">Réponse JSON.</param>
    /// <param name="artist">Artiste cherché.</param>
    /// <returns>Les étiquettes (vide si aucun artiste ne correspond).</returns>
    internal static IReadOnlyList<TagCount> Parse(string json, string artist)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("artists", out var artists) || artists.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var wanted = Normalization.TextKey.Of(artist);
        foreach (var candidate in artists.EnumerateArray())
        {
            var name = candidate.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
            var score = candidate.TryGetProperty("score", out var s) && s.TryGetInt32(out var value) ? value : 0;
            if (score < 90 || Identification.FuzzyMatch.Score(wanted, Normalization.TextKey.Of(name)) < 0.85)
            {
                continue;
            }

            var tags = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in new[] { "tags", "genres" })
            {
                if (candidate.TryGetProperty(property, out var list) && list.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tag in list.EnumerateArray())
                    {
                        if (tag.TryGetProperty("name", out var tagName) && tagName.GetString() is { Length: > 0 } text)
                        {
                            var count = tag.TryGetProperty("count", out var c) && c.TryGetInt32(out var v) ? v : 1;
                            tags[text] = tags.GetValueOrDefault(text) + count;
                        }
                    }
                }
            }

            return [.. tags.Select(t => new TagCount(t.Key, t.Value)).OrderByDescending(t => t.Count)];
        }

        return [];
    }
}

/// <summary>Last.fm (clé personnelle de l'utilisateur) : étiquettes les plus votées d'un artiste (MUS-042 : clé personnelle).</summary>
public sealed class LastFmSource : ITagSource
{
    private readonly HttpClient _http;
    private readonly string _key;

    /// <summary>Crée la source.</summary>
    /// <param name="http">Client HTTP.</param>
    /// <param name="apiKey">Clé d'API Last.fm de l'utilisateur.</param>
    public LastFmSource(HttpClient http, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        _http = http;
        _key = apiKey;
    }

    /// <inheritdoc />
    public string Name => "Last.fm";

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagCount>?> GetTagsAsync(string artist, CancellationToken cancellation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artist);
        var uri = new Uri(
            string.Create(CultureInfo.InvariantCulture, $"https://ws.audioscrobbler.com/2.0/?method=artist.gettoptags&artist={Uri.EscapeDataString(artist)}&autocorrect=1&api_key={Uri.EscapeDataString(_key)}&format=json"),
            UriKind.Absolute);
        try
        {
            using var response = await _http.GetAsync(uri, cancellation).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return Parse(await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException && !cancellation.IsCancellationRequested)
        {
            return null;
        }
    }

    /// <summary>Lit la réponse <c>artist.gettoptags</c> : les étiquettes avec leur poids (0 à 100).</summary>
    /// <param name="json">Réponse JSON.</param>
    /// <returns>Les étiquettes (vide si l'artiste est inconnu ou si la réponse est une erreur).</returns>
    internal static IReadOnlyList<TagCount> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("toptags", out var top) || !top.TryGetProperty("tag", out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return
        [
            .. list.EnumerateArray()
                .Where(t => t.TryGetProperty("name", out var name) && name.GetString() is { Length: > 0 })
                .Select(t => new TagCount(t.GetProperty("name").GetString()!, t.TryGetProperty("count", out var c) && c.TryGetInt32(out var v) ? v : 1))
                .OrderByDescending(t => t.Count)
                .Take(15),
        ];
    }
}
