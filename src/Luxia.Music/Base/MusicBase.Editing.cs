using Luxia.Music.Identification;
using Luxia.Music.Normalization;

namespace Luxia.Music.Base;

// Édition de la base musicale à l'écran (MUS-027) : recherche, ajout, suppression, alias, fusion de doublons, titres.
public sealed partial class MusicBase
{
    /// <summary>Artistes dont le nom ou un alias contient ce texte (comparaison sur la forme normalisée), triés par nom.</summary>
    /// <param name="filter">Texte cherché ; vide = tous.</param>
    /// <param name="max">Nombre maximal d'artistes rendus.</param>
    /// <param name="styleId">Seulement les artistes de ce style (« inconnu » pour ceux à classer) ; <c>null</c> = tous.</param>
    /// <returns>Les artistes.</returns>
    public IReadOnlyList<ArtistEntry> SearchArtists(string? filter, int max = 500, string? styleId = null)
    {
        var key = TextKey.Of(filter);
        lock (_gate)
        {
            return
            [
                .. _artists.OfType<ArtistRecord>()
                    .Where(a => styleId is null || string.Equals(a.Entry.Style, styleId, StringComparison.OrdinalIgnoreCase))
                    .Where(a => key.Length == 0 || a.Keys.Any(k => k.Contains(key, StringComparison.Ordinal)))
                    .Select(a => a.Entry)
                    .OrderBy(a => TextKey.Of(a.Name), StringComparer.Ordinal)
                    .Take(max),
            ];
        }
    }

    /// <summary>Titres connus d'un artiste (nom ou alias).</summary>
    /// <param name="artist">Artiste.</param>
    /// <returns>Les titres, triés.</returns>
    public IReadOnlyList<TitleEntry> TitlesOf(string artist)
    {
        lock (_gate)
        {
            return _titlesByArtist.TryGetValue(Canonical(TextKey.Of(artist)), out var list)
                ? [.. list.Select(t => t.Entry).OrderBy(t => TextKey.Of(t.Title), StringComparer.Ordinal)]
                : [];
        }
    }

    /// <summary>Retire un artiste et ses titres de la base.</summary>
    /// <param name="name">Nom de l'artiste.</param>
    /// <returns><c>false</c> si l'artiste n'existe pas.</returns>
    public bool RemoveArtist(string name)
    {
        lock (_gate)
        {
            if (!_artistByKey.TryGetValue(TextKey.Of(name), out var index) || _artists[index] is not { } record)
            {
                return false;
            }

            Remove(index, record);
            _titlesByArtist.Remove(record.Key);
        }

        RaiseChanged();
        return true;
    }

    /// <summary>Donne son style à un artiste (le crée au besoin).</summary>
    /// <param name="name">Nom de l'artiste.</param>
    /// <param name="familyId">Famille.</param>
    public void SetArtistStyle(string name, string familyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(familyId);
        lock (_gate)
        {
            if (_artistByKey.TryGetValue(TextKey.Of(name), out var index) && _artists[index] is { } existing)
            {
                ReplaceArtist(index, existing.Entry with { Style = familyId });
            }
            else
            {
                AddArtist(new ArtistEntry { Name = name.Trim(), Style = familyId });
            }
        }

        RaiseChanged();
    }

    /// <summary>
    /// Un morceau d'un artiste absent de la base : l'artiste y est ajouté avec le style « Inconnu », pour être classé plus tard (filtre « Inconnu »
    /// de l'écran Base musicale).
    /// </summary>
    /// <param name="name">Nom de l'artiste (nettoyé).</param>
    /// <returns><c>true</c> si l'artiste a été ajouté, <c>false</c> s'il existait déjà ou si le nom est vide.</returns>
    public bool InjectUnknownArtist(string name)
    {
        if (TextKey.Of(name).Length == 0)
        {
            return false;
        }

        lock (_gate)
        {
            if (_artistByKey.ContainsKey(TextKey.Of(name)))
            {
                return false;
            }

            AddArtist(new ArtistEntry { Name = name.Trim(), Style = Taxonomy.UnknownId });
        }

        RaiseChanged();
        return true;
    }

    /// <summary>Donne un style propre à un titre (le crée au besoin) ; l'artiste est ajouté (« Inconnu ») s'il manque.</summary>
    /// <param name="artist">Artiste.</param>
    /// <param name="title">Titre.</param>
    /// <param name="version">Clé de la version ; vide pour l'original.</param>
    /// <param name="familyId">Famille.</param>
    public void SetTitleStyle(string artist, string title, string? version, string familyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artist);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(familyId);
        lock (_gate)
        {
            if (!_artistByKey.ContainsKey(TextKey.Of(artist)))
            {
                AddArtist(new ArtistEntry { Name = artist.Trim(), Style = Taxonomy.UnknownId });
            }

            var artistKey = Canonical(TextKey.Of(artist));
            var versionKey = version ?? string.Empty;
            var titleKey = TextKey.Of(title);
            var existing = _titlesByArtist.GetValueOrDefault(artistKey)?.FirstOrDefault(t => t.VersionKey == versionKey && t.Keys.Contains(titleKey));
            if (existing is not null)
            {
                _titlesByArtist[artistKey].Remove(existing);
            }

            AddTitle(new TitleEntry { Artist = CanonicalName(artist), Title = existing?.Entry.Title ?? title, Aliases = existing?.Entry.Aliases ?? [], Version = versionKey.Length == 0 ? null : versionKey, Style = familyId, Bpm = existing?.Entry.Bpm });
        }

        RaiseChanged();
    }

    /// <summary>Artiste à qui appartient ce nom ou cet alias (comparaison sur la forme normalisée).</summary>
    /// <param name="nameOrAlias">Nom ou alias.</param>
    /// <returns>L'artiste, ou <c>null</c>.</returns>
    public ArtistEntry? OwnerOf(string? nameOrAlias) => FindArtist(TextKey.Of(nameOrAlias));

    /// <summary>
    /// Enregistre la fiche d'un artiste d'un seul bloc (bouton Enregistrer de l'écran) : le crée (<paramref name="code"/> vide ; le code de <paramref name="entry"/> est gardé s'il est libre) ou le remplace,
    /// avec ses alias et la liste complète de ses titres. Rien n'est modifié si un contrôle échoue.
    /// </summary>
    /// <param name="code">Code de l'artiste modifié ; vide pour un nouvel artiste.</param>
    /// <param name="entry">Nom, alias et style.</param>
    /// <param name="titles">Titres de l'artiste (leur artiste est ignoré).</param>
    /// <returns>Un message d'erreur, ou <c>null</c> si la fiche est enregistrée.</returns>
    public string? SaveArtist(string? code, ArtistEntry entry, IReadOnlyList<TitleEntry> titles)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(titles);
        var name = entry.Name.Trim();
        var nameKey = TextKey.Of(name);
        if (nameKey.Length == 0)
        {
            return "le nom de l'artiste est vide";
        }

        lock (_gate)
        {
            var index = -1;
            if (!string.IsNullOrEmpty(code))
            {
                index = _artists.FindIndex(a => a?.Entry.Code == code);
                if (index < 0)
                {
                    return "artiste introuvable (supprimé ailleurs ?)";
                }
            }

            if (_artistByKey.TryGetValue(nameKey, out var nameOwner) && nameOwner != index)
            {
                return $"« {name} » est déjà le nom ou l'alias de « {_artists[nameOwner]!.Entry.Name} »";
            }

            foreach (var alias in entry.Aliases.Where(a => TextKey.Of(a).Length > 0))
            {
                if (_artistByKey.TryGetValue(TextKey.Of(alias), out var aliasOwner) && aliasOwner != index)
                {
                    return $"l'alias « {alias.Trim()} » appartient déjà à « {_artists[aliasOwner]!.Entry.Name} »";
                }
            }

            if (_taxonomy.Families.All(f => !string.Equals(f.Id, entry.Style, StringComparison.OrdinalIgnoreCase)))
            {
                return $"style inconnu de la taxonomie : « {entry.Style} »";
            }

            if (index >= 0)
            {
                ReplaceArtist(index, entry);
            }
            else
            {
                AddArtist(entry with { Code = _artists.Any(x => x?.Entry.Code == entry.Code) ? string.Empty : entry.Code });
            }

            var record = _artists[_artistByKey[nameKey]]!;
            _titlesByArtist.Remove(record.Key);
            foreach (var title in titles.Where(t => TextKey.Of(t.Title).Length > 0))
            {
                AddTitle(title with { Artist = record.Entry.Name, Title = title.Title.Trim() });
            }
        }

        RaiseChanged();
        return null;
    }

    /// <summary>Ajoute un alias (autre orthographe) à un artiste.</summary>
    /// <param name="name">Nom de l'artiste.</param>
    /// <param name="alias">Alias.</param>
    /// <returns><c>false</c> si l'artiste n'existe pas ou si l'alias désigne déjà un autre artiste.</returns>
    public bool AddAlias(string name, string alias)
    {
        lock (_gate)
        {
            var aliasKey = TextKey.Of(alias);
            if (aliasKey.Length == 0 || !_artistByKey.TryGetValue(TextKey.Of(name), out var index) || _artists[index] is not { } record)
            {
                return false;
            }

            if (_artistByKey.TryGetValue(aliasKey, out var other) && other != index)
            {
                return false;
            }

            if (record.Keys.Contains(aliasKey))
            {
                return true;
            }

            ReplaceArtist(index, record.Entry with { Aliases = [.. record.Entry.Aliases, alias.Trim()] });
        }

        RaiseChanged();
        return true;
    }

    /// <summary>
    /// Fusionne deux fiches du même artiste (doublon) : la fiche gardée reprend le nom retiré comme alias, ses alias, ses titres et,
    /// si elle était « Inconnu », le style de l'autre ; l'autre fiche disparaît.
    /// </summary>
    /// <param name="keepName">Artiste gardé.</param>
    /// <param name="removeName">Artiste retiré.</param>
    /// <returns><c>false</c> si l'un des deux n'existe pas ou s'il s'agit du même.</returns>
    public bool Merge(string keepName, string removeName)
    {
        lock (_gate)
        {
            if (!_artistByKey.TryGetValue(TextKey.Of(keepName), out var keepIndex) || _artists[keepIndex] is not { } keep
                || !_artistByKey.TryGetValue(TextKey.Of(removeName), out var removeIndex) || _artists[removeIndex] is not { } remove
                || keepIndex == removeIndex)
            {
                return false;
            }

            var aliases = keep.Entry.Aliases
                .Concat([remove.Entry.Name])
                .Concat(remove.Entry.Aliases)
                .Where(a => TextKey.Of(a) != keep.Key)
                .GroupBy(TextKey.Of)
                .Select(g => g.First())
                .ToList();
            var style = keep.Entry.Style != Taxonomy.UnknownId ? keep.Entry.Style : remove.Entry.Style;
            var movedTitles = _titlesByArtist.GetValueOrDefault(remove.Key) ?? [];
            _titlesByArtist.Remove(remove.Key);
            Remove(removeIndex, remove);
            ReplaceArtist(keepIndex, keep.Entry with { Aliases = aliases, Style = style });
            if (movedTitles.Count > 0)
            {
                if (!_titlesByArtist.TryGetValue(keep.Key, out var list))
                {
                    list = [];
                    _titlesByArtist[keep.Key] = list;
                }

                list.AddRange(movedTitles.Select(t => t with { Entry = t.Entry with { Artist = keep.Entry.Name } }));
            }
        }

        RaiseChanged();
        return true;
    }

    /// <summary>
    /// Doublons probables : artistes dont les noms ne diffèrent que par l'ordre des mots, un article (« The », « Les »), une faute de
    /// frappe ou la ponctuation. À confirmer par l'utilisateur (fusion).
    /// </summary>
    /// <param name="threshold">Score minimal du rapprochement flou (0 à 1).</param>
    /// <param name="max">Nombre maximal de paires.</param>
    /// <returns>Les paires et leur score, de la plus sûre à la moins sûre.</returns>
    public IReadOnlyList<(ArtistEntry First, ArtistEntry Second, double Score)> FindDuplicates(double threshold = 0.9, int max = 200)
    {
        List<ArtistRecord> records;
        lock (_gate)
        {
            records = [.. _artists.OfType<ArtistRecord>()];
        }

        var found = new Dictionary<(string, string), (ArtistEntry, ArtistEntry, double)>();
        void Add(ArtistRecord a, ArtistRecord b, double score)
        {
            var pair = string.CompareOrdinal(a.Key, b.Key) < 0 ? (a.Key, b.Key) : (b.Key, a.Key);
            if (a.Key != b.Key && !found.ContainsKey(pair))
            {
                found[pair] = (a.Entry, b.Entry, score);
            }
        }

        // Même nom une fois les articles retirés et les mots triés : doublon certain.
        var bySignature = records.GroupBy(r => Signature(r.Key)).Where(g => g.Count() > 1);
        foreach (var group in bySignature)
        {
            var list = group.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                for (var j = i + 1; j < list.Count; j++)
                {
                    Add(list[i], list[j], 1.0);
                }
            }
        }

        foreach (var record in records)
        {
            foreach (var (candidate, score) in FuzzyArtists(record.Key, threshold))
            {
                lock (_gate)
                {
                    if (_artistByKey.TryGetValue(TextKey.Of(candidate.Name), out var index) && _artists[index] is { } other)
                    {
                        Add(record, other, score);
                    }
                }
            }
        }

        return [.. found.Values.OrderByDescending(d => d.Item3).ThenBy(d => d.Item1.Name, StringComparer.Ordinal).Take(max)];
    }

    /// <summary>Ajoute ou remplace un titre (clé : artiste, titre, version).</summary>
    /// <param name="title">Titre.</param>
    public void UpsertTitle(TitleEntry title)
    {
        ArgumentNullException.ThrowIfNull(title);
        lock (_gate)
        {
            var artistKey = Canonical(TextKey.Of(title.Artist));
            var titleKey = TextKey.Of(title.Title);
            var versionKey = title.Version ?? string.Empty;
            if (_titlesByArtist.TryGetValue(artistKey, out var list))
            {
                list.RemoveAll(t => t.VersionKey == versionKey && t.Keys.Contains(titleKey));
            }

            AddTitle(title with { Artist = CanonicalName(title.Artist) });
        }

        RaiseChanged();
    }

    /// <summary>Retire un titre de la base.</summary>
    /// <param name="artist">Artiste.</param>
    /// <param name="title">Titre.</param>
    /// <param name="version">Clé de la version ; vide pour l'original.</param>
    /// <returns><c>false</c> si le titre n'existe pas.</returns>
    public bool RemoveTitle(string artist, string title, string? version = null)
    {
        lock (_gate)
        {
            var titleKey = TextKey.Of(title);
            var versionKey = version ?? string.Empty;
            if (!_titlesByArtist.TryGetValue(Canonical(TextKey.Of(artist)), out var list) || list.RemoveAll(t => t.VersionKey == versionKey && t.Keys.Contains(titleKey)) == 0)
            {
                return false;
            }
        }

        RaiseChanged();
        return true;
    }

    private static string Signature(string key)
    {
        var words = TextKey.Words(key).ToList();
        while (words.Count > 1 && words[0] is "the" or "les" or "le" or "la" or "l" or "un" or "une")
        {
            words.RemoveAt(0);
        }

        words.Sort(StringComparer.Ordinal);
        return string.Join(' ', words);
    }

    private void Remove(int index, ArtistRecord record)
    {
        foreach (var key in record.Keys.Where(k => _artistByKey.TryGetValue(k, out var i) && i == index))
        {
            _artistByKey.Remove(key);
        }

        _artists[index] = null;
    }

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
