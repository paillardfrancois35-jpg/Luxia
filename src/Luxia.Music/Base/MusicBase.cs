using Luxia.Music.Identification;
using Luxia.Music.Normalization;

namespace Luxia.Music.Base;

/// <summary>
/// La base musicale locale en mémoire (doc 21 §3.2) : taxonomie, artistes (un style chacun) et titres, avec des index pour que
/// l'identification reste sous les 200 ms même avec 50 000 titres et 10 000 artistes (MUS-022). Sûre pour plusieurs fils : une
/// correction faite à l'écran n'attend jamais plus que la durée d'une recherche.
/// </summary>
public sealed partial class MusicBase
{
    private const int MaxFuzzyCandidates = 60;

    private readonly object _gate = new();
    private readonly List<ArtistRecord?> _artists = [];
    private readonly Dictionary<string, int> _artistByKey = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<int>> _grams = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<TitleRecord>> _titlesByArtist = new(StringComparer.Ordinal);
    private int _lastCode;
    private readonly Dictionary<string, MusicFamily> _familyByKey = new(StringComparer.Ordinal);
    private Taxonomy _taxonomy;

    /// <summary>Crée la base à partir des fichiers (ou de la base livrée).</summary>
    /// <param name="taxonomy">Familles (la famille « Inconnu » est ajoutée si elle manque).</param>
    /// <param name="artists">Artistes.</param>
    /// <param name="titles">Titres.</param>
    public MusicBase(Taxonomy taxonomy, ArtistSet artists, TitleSet titles)
    {
        ArgumentNullException.ThrowIfNull(taxonomy);
        ArgumentNullException.ThrowIfNull(artists);
        ArgumentNullException.ThrowIfNull(titles);
        _taxonomy = taxonomy.Families.Any(f => f.Id == Taxonomy.UnknownId)
            ? taxonomy
            : taxonomy with { Families = [.. taxonomy.Families, new MusicFamily { Id = Taxonomy.UnknownId, Name = Taxonomy.UnknownName }] };
        IndexFamilies();
        _lastCode = artists.Artists.Select(a => CodeNumber(a.Code)).DefaultIfEmpty(0).Max();
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
                return _artists.Count(a => a is not null);
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

    /// <summary>Ajoute une famille ou remplace celle de même identifiant (nom, étiquettes) ; l'ordre d'affichage est celui de la liste.</summary>
    /// <param name="family">Famille.</param>
    public void UpsertFamily(MusicFamily family)
    {
        ArgumentNullException.ThrowIfNull(family);
        if (family.Id.Length == 0 || TextKey.Of(family.Name).Length == 0)
        {
            return;
        }

        lock (_gate)
        {
            var families = _taxonomy.Families.ToList();
            var index = families.FindIndex(f => string.Equals(f.Id, family.Id, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                families[index] = family with { Id = families[index].Id };
            }
            else
            {
                families.Add(family);
            }

            _taxonomy = _taxonomy with { Families = families };
            IndexFamilies();
        }

        Changed?.Invoke(this, EventArgs.Empty);
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
            return _artistByKey.TryGetValue(key, out var index) ? _artists[index]!.Entry : null;
        }
    }

    /// <summary>Artiste d'après son code stable.</summary>
    /// <param name="code">Code (« A00012 »).</param>
    /// <returns>L'artiste, ou <c>null</c>.</returns>
    public ArtistEntry? FindArtistByCode(string? code)
    {
        lock (_gate)
        {
            return string.IsNullOrEmpty(code) ? null : _artists.OfType<ArtistRecord>().FirstOrDefault(a => a.Entry.Code == code)?.Entry;
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
                if (_artists[index] is not { } record)
                {
                    continue;
                }

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

    /// <summary>Photographie des artistes pour l'enregistrement.</summary>
    /// <returns>Les artistes.</returns>
    public ArtistSet ToArtistSet()
    {
        lock (_gate)
        {
            return new ArtistSet { Artists = [.. _artists.OfType<ArtistRecord>().Select(a => a.Entry)] };
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

    private static IEnumerable<string> Grams(string key)
    {
        var padded = "  " + key + " ";
        for (var i = 0; i + 3 <= padded.Length; i++)
        {
            yield return padded.Substring(i, 3);
        }
    }

    private string Canonical(string artistKey) =>
        _artistByKey.TryGetValue(artistKey, out var index) ? _artists[index]!.Key : artistKey;

    private string CanonicalName(string artist) =>
        _artistByKey.TryGetValue(TextKey.Of(artist), out var index) ? _artists[index]!.Entry.Name : artist;

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

    private static int CodeNumber(string code) =>
        code.Length > 1 && int.TryParse(code.AsSpan(1), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : 0;

    /// <summary>Nettoie une fiche : code attribué s'il manque, alias sans doublon, sans le nom lui-même ni le nom ou l'alias d'un autre artiste (unicité globale).</summary>
    private ArtistEntry Sanitize(ArtistEntry entry, int ownIndex)
    {
        var name = entry.Name.Trim();
        var nameKey = TextKey.Of(name);
        var seen = new HashSet<string>(StringComparer.Ordinal) { nameKey };
        var aliases = new List<string>();
        foreach (var alias in entry.Aliases)
        {
            var key = TextKey.Of(alias);
            if (key.Length == 0 || !seen.Add(key) || (_artistByKey.TryGetValue(key, out var owner) && owner != ownIndex))
            {
                continue;
            }

            aliases.Add(alias.Trim());
        }

        _lastCode = Math.Max(_lastCode, CodeNumber(entry.Code));
        var code = entry.Code.Length > 0 ? entry.Code : "A" + (++_lastCode).ToString("D5", System.Globalization.CultureInfo.InvariantCulture);
        return entry with { Code = code, Name = name, Aliases = aliases };
    }

    private void AddArtist(ArtistEntry entry)
    {
        if (TextKey.Of(entry.Name).Length == 0)
        {
            return;
        }

        var index = _artists.Count;
        var clean = Sanitize(entry, index);
        var keys = new[] { TextKey.Of(clean.Name) }.Concat(clean.Aliases.Select(TextKey.Of)).ToArray();
        _artists.Add(new ArtistRecord(clean, keys[0], keys));
        IndexArtist(index, keys);
    }

    private void ReplaceArtist(int index, ArtistEntry entry)
    {
        var old = _artists[index]!;
        var clean = Sanitize(entry with { Code = old.Entry.Code }, index);
        var keys = new[] { TextKey.Of(clean.Name) }.Concat(clean.Aliases.Select(TextKey.Of)).ToArray();
        foreach (var key in old.Keys.Where(k => _artistByKey.TryGetValue(k, out var i) && i == index))
        {
            _artistByKey.Remove(key);
        }

        // Le nom a changé : les titres suivent la nouvelle clé de l'artiste.
        if (keys[0] != old.Key && _titlesByArtist.Remove(old.Key, out var moved))
        {
            _titlesByArtist[keys[0]] = [.. moved.Select(t => t with { Entry = t.Entry with { Artist = clean.Name } })];
        }

        _artists[index] = new ArtistRecord(clean, keys[0], keys);
        IndexArtist(index, keys);
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
        var artistKey = _artistByKey.TryGetValue(TextKey.Of(entry.Artist), out var index) ? _artists[index]!.Key : TextKey.Of(entry.Artist);
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
