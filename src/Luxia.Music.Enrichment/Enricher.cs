using System.Text.Json;
using Luxia.Music.Base;
using Luxia.Music.Classification;
using Luxia.Music.Normalization;

namespace Luxia.Music.Enrichment;

/// <summary>
/// Cache des réponses des sources en ligne (MUS-042) : un fichier par artiste et par source, valable 90 jours. On ne redemande jamais deux
/// fois la même chose au service, et l'outil refait un passage sans réseau (<c>--hors-ligne</c>) à partir du cache.
/// </summary>
public sealed class CachedTagSource : ITagSource
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(90);

    private readonly ITagSource _inner;
    private readonly string _folder;
    private readonly TimeProvider _time;
    private readonly bool _offline;

    /// <summary>Crée le cache autour d'une source.</summary>
    /// <param name="inner">Source en ligne.</param>
    /// <param name="folder">Dossier du cache.</param>
    /// <param name="offline">Ne jamais interroger la source : le cache seul répond.</param>
    /// <param name="time">Temps (réel par défaut).</param>
    public CachedTagSource(ITagSource inner, string folder, bool offline = false, TimeProvider? time = null)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        _inner = inner;
        _folder = folder;
        _offline = offline;
        _time = time ?? TimeProvider.System;
    }

    /// <summary>Nombre de réponses prises dans le cache depuis la création.</summary>
    public int Hits { get; private set; }

    /// <summary>Nombre de requêtes faites à la source en ligne depuis la création.</summary>
    public int Requests { get; private set; }

    /// <inheritdoc />
    public string Name => _inner.Name;

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagCount>?> GetTagsAsync(string artist, CancellationToken cancellation)
    {
        var path = Path.Combine(_folder, $"{TextKey.Of(_inner.Name).Replace(' ', '-')}-{TextKey.Of(artist).Replace(' ', '-')}.json");
        if (File.Exists(path) && ReadCache(path) is { } cached)
        {
            Hits++;
            return cached;
        }

        if (_offline)
        {
            return null;
        }

        Requests++;
        var tags = await _inner.GetTagsAsync(artist, cancellation).ConfigureAwait(false);
        if (tags is not null)
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(path, JsonSerializer.Serialize(new Entry(_time.GetUtcNow(), [.. tags])));
        }

        return tags;
    }

    private List<TagCount>? ReadCache(string path)
    {
        try
        {
            var entry = JsonSerializer.Deserialize<Entry>(File.ReadAllText(path));
            return entry is not null && _time.GetUtcNow() - entry.FetchedAt < Lifetime ? entry.Tags : null;
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return null;
        }
    }

    private sealed record Entry(DateTimeOffset FetchedAt, List<TagCount> Tags);
}

/// <summary>
/// L'outil d'enrichissement (MUS-040) : pour des artistes que la base ne sait pas classer, interroge les sources en ligne, convertit les
/// étiquettes en une famille de la taxonomie et **propose** (rien n'entre dans la base sans validation, MUS-041).
/// </summary>
public sealed class Enricher
{
    private readonly IReadOnlyList<ITagSource> _sources;
    private readonly MusicBase _base;

    /// <summary>Crée l'outil.</summary>
    /// <param name="sources">Sources, dans l'ordre d'essai.</param>
    /// <param name="musicBase">Base musicale (taxonomie et étiquettes de genre).</param>
    public Enricher(IReadOnlyList<ITagSource> sources, MusicBase musicBase)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(musicBase);
        _sources = sources;
        _base = musicBase;
    }

    /// <summary>
    /// Artistes dont **aucune** source n'a répondu au dernier passage (réseau, limite de débit, réponse inattendue, ou absent du cache hors ligne) :
    /// ce n'est pas « aucune proposition » (la source a répondu sans étiquette reconnue) ; à relancer.
    /// </summary>
    public IReadOnlyList<string> Unanswered { get; private set; } = [];

    /// <summary>Propose une famille pour chaque artiste (au plus <paramref name="max"/>) ; chaque source est interrogée jusqu'à une réponse reconnue.</summary>
    /// <param name="artists">Artistes à classer.</param>
    /// <param name="max">Nombre maximal d'artistes traités.</param>
    /// <param name="progress">Progression (une ligne par artiste).</param>
    /// <param name="cancellation">Annulation.</param>
    /// <returns>Les propositions (un artiste sans réponse reconnue n'en a pas).</returns>
    public async Task<IReadOnlyList<Proposal>> ProposeAsync(IEnumerable<string> artists, int max, IProgress<string>? progress, CancellationToken cancellation)
    {
        ArgumentNullException.ThrowIfNull(artists);
        var proposals = new List<Proposal>();
        var unanswered = new List<string>();
        foreach (var artist in artists.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.OrdinalIgnoreCase).Take(max))
        {
            cancellation.ThrowIfCancellationRequested();
            Proposal? found = null;
            var answered = false;
            foreach (var source in _sources)
            {
                var tags = await source.GetTagsAsync(artist, cancellation).ConfigureAwait(false);
                answered |= tags is not null;
                if (tags is { Count: > 0 } && TagMapper.Map(tags, _base) is { } mapped)
                {
                    found = new Proposal { Artist = artist, Style = mapped.Family.Id, Confidence = mapped.Confidence, Source = source.Name, Tags = [.. tags.Take(8).Select(t => t.Name)] };
                    break;
                }
            }

            if (found is null && !answered)
            {
                unanswered.Add(artist);
            }

            progress?.Report(found is not null
                ? $"{artist} : {found.Style} ({found.Confidence:P0}, {found.Source})"
                : answered ? $"{artist} : aucune proposition" : $"{artist} : requête sans réponse (réseau, limite de débit ou absent du cache) : à relancer");
            if (found is not null)
            {
                proposals.Add(found);
            }
        }

        Unanswered = unanswered;
        return proposals;
    }
}
