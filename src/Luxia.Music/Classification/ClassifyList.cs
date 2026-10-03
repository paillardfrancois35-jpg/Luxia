using Luxia.Music.Base;
using Luxia.Music.Identification;
using Luxia.Music.Normalization;

namespace Luxia.Music.Classification;

/// <summary>Un titre à classer (écran « À classer », MUS-028) : venu d'un journal de soirée ou d'une playlist importée.</summary>
/// <param name="Artist">Artiste brut ; vide si inconnu.</param>
/// <param name="Title">Titre brut.</param>
/// <param name="App">Application source ; vide pour une playlist importée.</param>
/// <param name="Source"><c>journal</c> ou <c>playlist</c>.</param>
/// <param name="Plays">Nombre de passages en soirée.</param>
public sealed record PendingItem(string Artist, string Title, string App, string Source, int Plays);

/// <summary>Titres à classer venus de playlists importées, enregistrés dans <c>aclasser.json</c> (MUS-029).</summary>
public sealed record PendingSet
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Titres en attente.</summary>
    public IReadOnlyList<PendingItem> Items { get; init; } = [];
}

/// <summary>Un titre d'un artiste à classer, avec son nombre de passages.</summary>
/// <param name="Title">Titre nettoyé.</param>
/// <param name="TitleKey">Clé du titre.</param>
/// <param name="Plays">Passages en soirée.</param>
public sealed record ClassifyTitle(string Title, string TitleKey, int Plays);

/// <summary>Un artiste (ou un titre sans artiste) à classer, avec ses titres et la meilleure supposition.</summary>
/// <param name="ArtistKey">Clé de l'artiste ; vide si inconnu (le titre est alors classé seul).</param>
/// <param name="Artist">Artiste nettoyé, casse d'origine.</param>
/// <param name="Titles">Titres rencontrés, les plus joués d'abord.</param>
/// <param name="Plays">Total des passages.</param>
/// <param name="GuessFamilyId">Famille supposée (confiance faible) ; <c>null</c> si aucune.</param>
/// <param name="GuessConfidence">Confiance de la supposition.</param>
/// <param name="Source"><c>journal</c>, <c>playlist</c> ou <c>journal, playlist</c>.</param>
public sealed record ClassifyItem(string ArtistKey, string Artist, IReadOnlyList<ClassifyTitle> Titles, int Plays, string? GuessFamilyId, double GuessConfidence, string Source)
{
    /// <summary>Le classement concerne l'artiste (sinon seulement le titre, faute d'artiste).</summary>
    public bool HasArtist => ArtistKey.Length > 0;
}

/// <summary>
/// La liste « À classer » (MUS-028) : les titres joués en soirée (journaux) ou importés (playlists) que la base ne sait pas classer
/// (« Inconnu ») ou classe avec peu de confiance, regroupés par artiste pour les classer d'un geste. Un titre classé depuis disparaît :
/// la liste est recalculée avec la base à jour.
/// </summary>
public static class ClassifyList
{
    /// <summary>En dessous de cette confiance, un titre est proposé au classement.</summary>
    public const double LowConfidence = 0.6;

    /// <summary>Calcule la liste.</summary>
    /// <param name="entries">Lignes des journaux de soirée.</param>
    /// <param name="pending">Titres de playlists importées.</param>
    /// <param name="musicBase">Base musicale (à jour).</param>
    /// <param name="normalizer">Normaliseur de titres.</param>
    /// <param name="identifier">Identification.</param>
    /// <returns>Les artistes à classer, les plus joués d'abord.</returns>
    public static IReadOnlyList<ClassifyItem> Build(IEnumerable<EveningEntry> entries, IEnumerable<PendingItem> pending, MusicBase musicBase, TrackNormalizer normalizer, StyleIdentifier identifier)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(pending);
        ArgumentNullException.ThrowIfNull(musicBase);
        ArgumentNullException.ThrowIfNull(normalizer);
        ArgumentNullException.ThrowIfNull(identifier);

        // Un même morceau brut revient souvent : on ne l'identifie qu'une fois, avec son nombre de passages.
        var raw = new Dictionary<(string Artist, string Title, string App), (int Plays, string Source)>();
        foreach (var entry in entries)
        {
            Count(raw, entry.Artist, entry.Title, entry.App, 1, "journal");
        }

        foreach (var item in pending)
        {
            Count(raw, item.Artist, item.Title, item.App, item.Plays, item.Source);
        }

        var groups = new Dictionary<string, Group>(StringComparer.Ordinal);
        foreach (var ((artist, title, app), (plays, source)) in raw)
        {
            var normalized = normalizer.Normalize(title, artist, app);
            var result = identifier.Identify(normalized);
            if (result.IsKnown && result.Confidence >= LowConfidence)
            {
                continue;
            }

            var hypothesis = normalized.Hypotheses[Math.Min(result.Hypothesis, normalized.Hypotheses.Count - 1)];
            if (hypothesis.Title.Length == 0)
            {
                continue;
            }

            var key = hypothesis.Artist.Length > 0 ? hypothesis.Artist : "\u0001" + hypothesis.Title;
            if (!groups.TryGetValue(key, out var group))
            {
                group = new Group(hypothesis.Artist, hypothesis.ArtistDisplay);
                groups[key] = group;
            }

            group.Add(hypothesis.TitleDisplay, hypothesis.Title, plays, source, result);
        }

        return
        [
            .. groups.Values
                .Select(g => g.ToItem())
                .OrderByDescending(i => i.Plays)
                .ThenBy(i => i.Artist, StringComparer.OrdinalIgnoreCase),
        ];
    }

    private static void Count(Dictionary<(string, string, string), (int, string)> raw, string artist, string title, string app, int plays, string source)
    {
        var key = (artist.Trim(), title.Trim(), app.Trim());
        if (key.Item2.Length == 0)
        {
            return;
        }

        raw[key] = raw.TryGetValue(key, out var existing)
            ? (existing.Item1 + plays, existing.Item2.Contains(source, StringComparison.Ordinal) ? existing.Item2 : existing.Item2 + ", " + source)
            : (plays, source);
    }

    private sealed class Group(string artistKey, string artist)
    {
        private readonly Dictionary<string, (string Title, int Plays)> _titles = new(StringComparer.Ordinal);
        private readonly HashSet<string> _sources = new(StringComparer.Ordinal);
        private StyleResult? _guess;

        public void Add(string title, string titleKey, int plays, string source, StyleResult result)
        {
            _titles[titleKey] = _titles.TryGetValue(titleKey, out var old) ? (old.Title, old.Plays + plays) : (title, plays);
            foreach (var part in source.Split(", "))
            {
                _sources.Add(part);
            }

            if (result.IsKnown && (_guess is null || result.Confidence > _guess.Confidence))
            {
                _guess = result;
            }
        }

        public ClassifyItem ToItem() => new(
            artistKey,
            artist,
            [.. _titles.Select(t => new ClassifyTitle(t.Value.Title, t.Key, t.Value.Plays)).OrderByDescending(t => t.Plays).ThenBy(t => t.Title, StringComparer.OrdinalIgnoreCase)],
            _titles.Values.Sum(t => t.Plays),
            _guess?.FamilyId,
            _guess?.Confidence ?? 0,
            string.Join(", ", _sources.Order(StringComparer.Ordinal)));
    }
}
