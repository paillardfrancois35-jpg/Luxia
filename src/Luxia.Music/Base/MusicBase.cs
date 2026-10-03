using Luxia.Music.Identification;
using Luxia.Music.Normalization;

namespace Luxia.Music.Base;

/// <summary>
/// La base musicale locale en mémoire (doc 21 §3.2) : taxonomie, artistes, titres et corrections, avec des index pour que
/// l'identification reste sous les 200 ms même avec 50 000 titres et 10 000 artistes (MUS-022). Sûre pour plusieurs fils : une
/// correction faite à l'écran n'attend jamais plus que la durée d'une recherche.
/// </summary>
public sealed class MusicBase
{
    private const int MaxFuzzyCandidates = 60;

    private readonly object _gate = new();
    private readonly List<ArtistRecord> _artists = [];
    private readonly Dictionary<string, int> _artistByKey = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<int>> _grams = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<TitleRecord>> _titlesByArtist = new(StringComparer.Ordinal);
    private readonly List<StyleCorrection> _corrections;
    private readonly Dictionary<string, MusicFamily> _familyByKey = new(StringComparer.Ordinal);
    private readonly Taxonomy _taxonomy;

    /// <summary>Crée la base à partir des fichiers (ou de la base livrée).</summary>
    /// <param name="taxonomy">Familles.</param>
    /// <param name="artists">Artistes.</param>
    /// <param name="titles">Titres.</param>
    /// <param name="corrections">Corrections faites en Live.</param>
    public MusicBase(Taxonomy taxonomy, ArtistSet artists, TitleSet titles, CorrectionSet corrections)
    {
        ArgumentNullException.ThrowIfNull(taxonomy);
        ArgumentNullException.ThrowIfNull(artists);
        ArgumentNullException.ThrowIfNull(titles);
        ArgumentNullException.ThrowIfNull(corrections);
        _taxonomy = taxonomy;
        _corrections = [.. corrections.Corrections];
        IndexFamilies();
        foreach (var artist in artists.Artists)
        {
            AddArtist(artist);
        }

        foreach (var title in titles.Titles)
        {
            AddTitle(title);
        }
    }

    /// <summary>Levé quand la base change (correction, ajout) ; le propriétaire enregistre alors les fichiers.</summary>
    public event EventHandler? Changed;

    /// <summary>La taxonomie.</summary>
    public Taxonomy Taxonomy
    {
        get
        {
            lock (_gate)
            {
                return _taxonomy;
            }
        }
    }

    /// <summary>Nombre d'artistes.</summary>
    public int ArtistCount
    {
        get
        {
            lock (_gate)
            {
                return _artists.Count;
            }
        }
    }

    /// <summary>Nombre de titres.</summary>
    public int TitleCount
    {
        get
        {
            lock (_gate)
            {
                return _titlesByArtist.Values.Sum(l => l.Count);
            }
        }
    }

    /// <summary>Famille d'après son identifiant, son nom, l'une des parties de son nom (« Électro » pour « Électro / Dance ») ou une étiquette de genre.</summary>
    /// <param name="text">Identifiant, nom ou étiquette.</param>
    /// <returns>La famille, ou <c>null</c>.</returns>
    public MusicFamily? FindFamily(string? text)
    {
        var key = TextKey.Of(text);
        if (key.Length == 0)
        {
            return null;
        }

        lock (_gate)
        {
            return _familyByKey.GetValueOrDefault(key);
        }
    }

    /// <summary>Famille d'après son identifiant exact.</summary>
    /// <param name="id">Identifiant.</param>
    /// <returns>La famille, ou <c>null</c>.</returns>
    public MusicFamily? FamilyById(string? id)
    {
        lock (_gate)
        {
            return _taxonomy.Families.FirstOrDefault(f => string.Equals(f.Id, id, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>Artiste dont le nom ou un alias a cette clé.</summary>
    /// <param name="key">Clé normalisée.</param>
    /// <returns>L'artiste, ou <c>null</c>.</returns>
    public ArtistEntry? FindArtist(string key)
    {
        lock (_gate)
        {
            return _artistByKey.TryGetValue(key, out var index) ? _artists[index].Entry : null;
        }
    }

    /// <summary>Artistes proches de cette clé, du plus proche au moins proche (rapprochement flou, au moins <paramref name="threshold"/>).</summary>
    /// <param name="key">Clé normalisée.</param>
    /// <param name="threshold">Score minimal (0 à 1).</param>
    /// <returns>Artistes et scores.</returns>
    public IReadOnlyList<(ArtistEntry Artist, double Score)> FuzzyArtists(string key, double threshold)
    {
        lock (_gate)
        {
            var counts = new Dictionary<int, int>();
            foreach (var gram in Grams(key))
            {
                if (_grams.TryGetValue(gram, out var indexes))
                {
                    foreach (var index in indexes)
                    {
                        counts[index] = counts.GetValueOrDefault(index) + 1;
                    }
                }
            }

            var result = new List<(ArtistEntry, double)>();
            foreach (var (index, _) in counts.OrderByDescending(c => c.Value).Take(MaxFuzzyCandidates))
            {
                var record = _artists[index];
                var best = record.Keys.Max(k => FuzzyMatch.Score(key, k));
                if (best >= threshold)
                {
                    result.Add((record.Entry, best));
                }
            }

            return [.. result.OrderByDescending(r => r.Item2)];
        }
    }

    /// <summary>Titre exact d'un artiste (nom, alias ou titre alias), pour une version donnée ou l'original.</summary>
    /// <param name="artistKey">Clé de l'artiste (nom canonique ou alias).</param>
    /// <param name="titleKey">Clé du titre.</param>
    /// <param name="versionKey">Clé de la version ; vide pour l'original.</param>
    /// <returns>Le titre, ou <c>null</c>.</returns>
    public TitleEntry? FindTitle(string artistKey, string titleKey, string versionKey)
    {
        lock (_gate)
        {
            return _titlesByArtist.TryGetValue(Canonical(artistKey), out var list)
                ? list.FirstOrDefault(t => t.VersionKey == versionKey && t.Keys.Contains(titleKey))?.Entry
                : null;
        }
    }

    /// <summary>Titres d'un artiste proches de ce titre (rapprochement flou).</summary>
    /// <param name="artistKey">Clé de l'artiste.</param>
    /// <param name="titleKey">Clé du titre.</param>
    /// <param name="versionKey">Clé de la version ; vide pour l'original.</param>
    /// <param name="threshold">Score minimal (0 à 1).</param>
    /// <returns>Titres et scores, du plus proche au moins proche.</returns>
    public IReadOnlyList<(TitleEntry Title, double Score)> FuzzyTitles(string artistKey, string titleKey, string versionKey, double threshold)
    {
        lock (_gate)
        {
            if (!_titlesByArtist.TryGetValue(Canonical(artistKey), out var list))
            {
                return [];
            }

            return
            [
                .. list.Where(t => t.VersionKey == versionKey)
                    .Select(t => (t.Entry, Score: t.Keys.Max(k => FuzzyMatch.Score(titleKey, k))))
                    .Where(r => r.Score >= threshold)
                    .OrderByDescending(r => r.Score),
            ];
        }
    }

    /// <summary>
    /// Enregistre une correction faite en Live (MUS-024) : crée ou met à jour l'artiste ou le titre avec cette famille (source
    /// <c>correction</c>, prioritaire) et l'ajoute à l'historique.
    /// </summary>
    /// <param name="scope"><c>title</c> ou <c>artist</c>.</param>
    /// <param name="artist">Artiste (nom affiché).</param>
    /// <param name="title">Titre (étendue <c>title</c>).</param>
    /// <param name="version">Clé de la version du titre, s'il y en a une.</param>
    /// <param name="familyId">Famille choisie.</param>
    /// <param name="oldFamilyId">Famille avant la correction.</param>
    /// <param name="at">Date.</param>
    public void Correct(string scope, string artist, string? title, string? version, string familyId, string? oldFamilyId, DateTimeOffset at)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(artist);
        ArgumentException.ThrowIfNullOrWhiteSpace(familyId);
        lock (_gate)
        {
            if (scope == "title" && !string.IsNullOrWhiteSpace(title))
            {
                var artistKey = Canonical(TextKey.Of(artist));
                var versionKey = version ?? string.Empty;
                var titleKey = TextKey.Of(title);
                var existing = _titlesByArtist.GetValueOrDefault(artistKey)?.FirstOrDefault(t => t.VersionKey == versionKey && t.Keys.Contains(titleKey));
                if (existing is not null)
                {
                    _titlesByArtist[artistKey].Remove(existing);
                }

                AddTitle(new TitleEntry { Artist = CanonicalName(artist), Title = title, Aliases = existing?.Entry.Aliases ?? [], Version = versionKey.Length == 0 ? null : versionKey, Style = familyId, Bpm = existing?.Entry.Bpm, Source = "correction" });
            }
            else
            {
                var key = TextKey.Of(artist);
                var existing = _artistByKey.TryGetValue(key, out var index) ? _artists[index] : null;
                var entry = new ArtistEntry { Name = existing?.Entry.Name ?? artist, Aliases = existing?.Entry.Aliases ?? [], Styles = new Dictionary<string, double> { [familyId] = 1.0 }, Source = "correction" };
                if (existing is not null)
                {
                    ReplaceArtist(index, entry);
                }
                else
                {
                    AddArtist(entry);
                }
            }

            _corrections.Add(new StyleCorrection { At = at, Scope = scope == "title" && !string.IsNullOrWhiteSpace(title) ? "title" : "artist", Artist = artist, Title = title, Version = version, OldStyle = oldFamilyId, NewStyle = familyId });
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Ajoute ou remplace un artiste (import, édition).</summary>
    /// <param name="artist">Artiste.</param>
    public void UpsertArtist(ArtistEntry artist)
    {
        ArgumentNullException.ThrowIfNull(artist);
        lock (_gate)
        {
            var key = TextKey.Of(artist.Name);
            if (_artistByKey.TryGetValue(key, out var index))
            {
                ReplaceArtist(index, artist);
            }
            else
            {
                AddArtist(artist);
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Photographie des artistes pour l'enregistrement.</summary>
    /// <returns>Les artistes.</returns>
    public ArtistSet ToArtistSet()
    {
        lock (_gate)
        {
            return new ArtistSet { Artists = [.. _artists.Select(a => a.Entry)] };
        }
    }

    /// <summary>Photographie des titres pour l'enregistrement.</summary>
    /// <returns>Les titres.</returns>
    public TitleSet ToTitleSet()
    {
        lock (_gate)
        {
            return new TitleSet { Titles = [.. _titlesByArtist.Values.SelectMany(l => l).Select(t => t.Entry)] };
        }
    }

    /// <summary>Photographie des corrections pour l'enregistrement.</summary>
    /// <returns>Les corrections.</returns>
    public CorrectionSet ToCorrectionSet()
    {
        lock (_gate)
        {
            return new CorrectionSet { Corrections = [.. _corrections] };
        }
    }

    private static IEnumerable<string> Grams(string key)
    {
        var padded = "  " + key + " ";
        for (var i = 0; i + 3 <= padded.Length; i++)
        {
            yield return padded.Substring(i, 3);
        }
    }

    private string Canonical(string artistKey) =>
        _artistByKey.TryGetValue(artistKey, out var index) ? _artists[index].Key : artistKey;

    private string CanonicalName(string artist) =>
        _artistByKey.TryGetValue(TextKey.Of(artist), out var index) ? _artists[index].Entry.Name : artist;

    private void IndexFamilies()
    {
        _familyByKey.Clear();
        foreach (var family in _taxonomy.Families)
        {
            foreach (var label in family.Labels)
            {
                _familyByKey.TryAdd(TextKey.Of(label), family);
            }
        }

        // L'identifiant, le nom complet et chaque partie du nom l'emportent sur les étiquettes d'une autre famille
        // (« House » est d'abord une famille, avant d'être un genre cité par une autre).
        var strong = new HashSet<string>(StringComparer.Ordinal);
        foreach (var family in _taxonomy.Families)
        {
            foreach (var key in new[] { TextKey.Of(family.Name), TextKey.Of(family.Id) })
            {
                _familyByKey[key] = family;
                strong.Add(key);
            }
        }

        foreach (var family in _taxonomy.Families)
        {
            foreach (var part in family.Name.Split('/'))
            {
                var key = TextKey.Of(part);
                if (key.Length > 0 && strong.Add(key))
                {
                    _familyByKey[key] = family;
                }
            }
        }
    }
    private void AddArtist(ArtistEntry entry)
    {
        var keys = new[] { TextKey.Of(entry.Name) }.Concat(entry.Aliases.Select(TextKey.Of)).Where(k => k.Length > 0).Distinct().ToArray();
        if (keys.Length == 0)
        {
            return;
        }

        var index = _artists.Count;
        _artists.Add(new ArtistRecord(entry, keys[0], keys));
        IndexArtist(index, keys);
    }

    private void ReplaceArtist(int index, ArtistEntry entry)
    {
        var old = _artists[index];
        var keys = new[] { TextKey.Of(entry.Name) }.Concat(entry.Aliases.Select(TextKey.Of)).Where(k => k.Length > 0).Distinct().ToArray();
        foreach (var key in old.Keys.Where(k => _artistByKey.TryGetValue(k, out var i) && i == index))
        {
            _artistByKey.Remove(key);
        }

        _artists[index] = new ArtistRecord(entry, old.Key, [.. keys.Union(old.Keys)]);
        IndexArtist(index, _artists[index].Keys);
    }

    private void IndexArtist(int index, string[] keys)
    {
        foreach (var key in keys)
        {
            _artistByKey.TryAdd(key, index);
            foreach (var gram in Grams(key).Distinct())
            {
                if (!_grams.TryGetValue(gram, out var list))
                {
                    list = [];
                    _grams[gram] = list;
                }

                if (list.Count == 0 || list[^1] != index)
                {
                    list.Add(index);
                }
            }
        }
    }

    private void AddTitle(TitleEntry entry)
    {
        var artistKey = _artistByKey.TryGetValue(TextKey.Of(entry.Artist), out var index) ? _artists[index].Key : TextKey.Of(entry.Artist);
        var keys = new[] { TextKey.Of(entry.Title) }.Concat(entry.Aliases.Select(TextKey.Of)).Where(k => k.Length > 0).Distinct().ToArray();
        if (keys.Length == 0)
        {
            return;
        }

        if (!_titlesByArtist.TryGetValue(artistKey, out var list))
        {
            list = [];
            _titlesByArtist[artistKey] = list;
        }

        list.Add(new TitleRecord(entry, keys, entry.Version ?? string.Empty));
    }

    private sealed record ArtistRecord(ArtistEntry Entry, string Key, string[] Keys);

    private sealed record TitleRecord(TitleEntry Entry, string[] Keys, string VersionKey);
}
