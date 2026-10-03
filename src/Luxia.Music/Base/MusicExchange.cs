using Luxia.Music.Normalization;
using Luxia.Persistence.Json;

namespace Luxia.Music.Base;

/// <summary>Un style (ligne de la table STYLE) du fichier d'échange.</summary>
public sealed record ExchangeStyle
{
    /// <summary>Code stable (« rock »).</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Libellé affiché, modifiable (« Rock »).</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Libellés de genre qui renvoient à ce style.</summary>
    public IReadOnlyList<string> Genres { get; init; } = [];

    /// <summary>Rang d'affichage (à partir de 1).</summary>
    public int Order { get; init; }
}

/// <summary>Un artiste (ligne de la table ARTISTE) du fichier d'échange.</summary>
public sealed record ExchangeArtist
{
    /// <summary>Code stable ; vide pour un nouvel artiste (le code est alors attribué à l'import).</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Nom.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Code du style ; vide ou inconnu = « Inconnu ».</summary>
    public string Style { get; init; } = string.Empty;
}

/// <summary>Un alias (ligne de la table ALIAS) du fichier d'échange.</summary>
public sealed record ExchangeAlias
{
    /// <summary>L'alias, unique dans toute la base.</summary>
    public string Alias { get; init; } = string.Empty;

    /// <summary>Code de l'artiste (ou, à défaut, son nom).</summary>
    public string Artist { get; init; } = string.Empty;
}

/// <summary>Fichier d'échange de la base musicale : les trois tables STYLE, ARTISTE et ALIAS (les titres ne sont pas livrés).</summary>
public sealed record ExchangeFile
{
    /// <summary>Version courante du format.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Styles.</summary>
    public IReadOnlyList<ExchangeStyle> Styles { get; init; } = [];

    /// <summary>Artistes.</summary>
    public IReadOnlyList<ExchangeArtist> Artists { get; init; } = [];

    /// <summary>Alias.</summary>
    public IReadOnlyList<ExchangeAlias> Aliases { get; init; } = [];
}

/// <summary>Bilan d'un import.</summary>
/// <param name="StylesChanged">Styles ajoutés ou mis à jour.</param>
/// <param name="ArtistsAdded">Artistes ajoutés.</param>
/// <param name="ArtistsUpdated">Artistes dont le nom ou le style a été mis à jour.</param>
/// <param name="AliasesAdded">Alias ajoutés.</param>
/// <param name="Problems">Lignes refusées, avec la raison.</param>
public sealed record ImportReport(int StylesChanged, int ArtistsAdded, int ArtistsUpdated, int AliasesAdded, IReadOnlyList<string> Problems)
{
    /// <summary>Résumé en une phrase.</summary>
    public string Summary =>
        $"{ArtistsAdded} artiste(s) ajouté(s), {ArtistsUpdated} mis à jour, {AliasesAdded} alias, {StylesChanged} style(s) ; {Problems.Count} ligne(s) refusée(s).";
}

/// <summary>
/// Export et import JSON des trois tables de la base (styles, artistes, alias) : sauvegarde, partage, et chargement d'une grande base
/// préparée ailleurs. Même format dans les deux sens ; un import ajoute et met à jour, il ne supprime rien.
/// </summary>
public static class MusicExchange
{
    private static readonly DocumentType<ExchangeFile> Type = new("échange de la base musicale", ExchangeFile.CurrentFormatVersion, []);

    /// <summary>Photographie la base au format d'échange.</summary>
    /// <param name="musicBase">Base.</param>
    /// <returns>Les trois tables.</returns>
    public static ExchangeFile Export(MusicBase musicBase)
    {
        ArgumentNullException.ThrowIfNull(musicBase);
        var artists = musicBase.ToArtistSet().Artists;
        return new ExchangeFile
        {
            Styles = [.. musicBase.Taxonomy.Families.Select((f, i) => new ExchangeStyle { Code = f.Id, Label = f.Name, Genres = f.Labels, Order = i + 1 })],
            Artists = [.. artists.Select(a => new ExchangeArtist { Code = a.Code, Name = a.Name, Style = a.Style })],
            Aliases = [.. artists.SelectMany(a => a.Aliases.Select(alias => new ExchangeAlias { Alias = alias, Artist = a.Code }))],
        };
    }

    /// <summary>Écrit l'export dans un fichier JSON versionné.</summary>
    /// <param name="musicBase">Base.</param>
    /// <param name="path">Fichier.</param>
    public static void ExportToFile(MusicBase musicBase, string path) => VersionedJsonFile.Save(path, Export(musicBase), Type);

    /// <summary>Lit un fichier d'échange et l'applique à la base.</summary>
    /// <param name="musicBase">Base.</param>
    /// <param name="path">Fichier.</param>
    /// <returns>Le bilan ; un fichier illisible donne un bilan vide avec la raison.</returns>
    public static ImportReport ImportFromFile(MusicBase musicBase, string path)
    {
        var loaded = VersionedJsonFile.Load(path, Type);
        return loaded.Succeeded
            ? Import(musicBase, loaded.Value!)
            : new ImportReport(0, 0, 0, 0, [loaded.Message ?? "fichier illisible"]);
    }

    /// <summary>Applique des tables à la base : styles (ajout ou mise à jour), artistes (par code, sinon par nom), alias.</summary>
    /// <param name="musicBase">Base.</param>
    /// <param name="file">Tables à importer.</param>
    /// <returns>Le bilan.</returns>
    public static ImportReport Import(MusicBase musicBase, ExchangeFile file)
    {
        ArgumentNullException.ThrowIfNull(musicBase);
        ArgumentNullException.ThrowIfNull(file);
        var problems = new List<string>();
        var (styles, added, updated, aliases) = (0, 0, 0, 0);

        foreach (var style in file.Styles.OrderBy(s => s.Order))
        {
            if (style.Code.Length == 0 || TextKey.Of(style.Label).Length == 0)
            {
                problems.Add($"style « {style.Code} » : code ou libellé vide");
                continue;
            }

            musicBase.UpsertFamily(new MusicFamily { Id = style.Code, Name = style.Label.Trim(), Labels = style.Genres });
            styles++;
        }

        foreach (var row in file.Artists)
        {
            var styleId = musicBase.FamilyById(row.Style)?.Id ?? Taxonomy.UnknownId;
            if (row.Style.Length > 0 && musicBase.FamilyById(row.Style) is null)
            {
                problems.Add($"artiste « {row.Name} » : style « {row.Style} » inconnu, classé « Inconnu »");
            }

            var existing = musicBase.FindArtistByCode(row.Code) ?? musicBase.OwnerOf(row.Name);
            var error = existing is null
                ? musicBase.SaveArtist(null, new ArtistEntry { Code = row.Code, Name = row.Name, Style = styleId }, [])
                : musicBase.SaveArtist(existing.Code, existing with { Name = row.Name, Style = styleId }, musicBase.TitlesOf(existing.Name));
            if (error is not null)
            {
                problems.Add($"artiste « {row.Name} » : {error}");
            }
            else if (existing is null)
            {
                added++;
            }
            else
            {
                updated++;
            }
        }

        foreach (var row in file.Aliases)
        {
            var owner = musicBase.FindArtistByCode(row.Artist) ?? musicBase.OwnerOf(row.Artist);
            if (owner is null)
            {
                problems.Add($"alias « {row.Alias} » : artiste « {row.Artist} » introuvable");
            }
            else if (musicBase.OwnerOf(row.Alias) is { } taken && taken.Code != owner.Code)
            {
                problems.Add($"alias « {row.Alias} » : appartient déjà à « {taken.Name} »");
            }
            else if (musicBase.OwnerOf(row.Alias) is null && musicBase.AddAlias(owner.Name, row.Alias))
            {
                aliases++;
            }
        }

        return new ImportReport(styles, added, updated, aliases, problems);
    }
}
