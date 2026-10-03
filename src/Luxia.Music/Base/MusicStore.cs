using Luxia.Persistence;
using Luxia.Persistence.Json;

namespace Luxia.Music.Base;

/// <summary>
/// Lecture / écriture de la base musicale d'un projet (doc 50 §12j) : <c>taxonomie.json</c>, <c>artistes.json</c>, <c>titres.json</c>,
/// <c>corrections.json</c>. Facultatifs : sans fichier, la taxonomie et la base de départ livrées s'appliquent (Q48).
/// </summary>
public static class MusicStore
{
    /// <summary>Fichier de la taxonomie.</summary>
    public const string TaxonomyFile = "taxonomie.json";

    /// <summary>Fichier des artistes.</summary>
    public const string ArtistsFile = "artistes.json";

    /// <summary>Fichier des titres.</summary>
    public const string TitlesFile = "titres.json";

    /// <summary>Fichier des corrections.</summary>
    public const string CorrectionsFile = "corrections.json";

    private static readonly DocumentType<Taxonomy> TaxonomyType = new("taxonomie", Taxonomy.CurrentFormatVersion, []);
    private static readonly DocumentType<ArtistSet> ArtistsType = new("artistes", ArtistSet.CurrentFormatVersion, []);
    private static readonly DocumentType<TitleSet> TitlesType = new("titres", TitleSet.CurrentFormatVersion, []);
    private static readonly DocumentType<CorrectionSet> CorrectionsType = new("corrections", CorrectionSet.CurrentFormatVersion, []);

    /// <summary>La base livrée avec l'application, sans aucun fichier de projet.</summary>
    /// <returns>Taxonomie par défaut et artistes de départ.</returns>
    public static MusicBase Default() => new(DefaultTaxonomy.Value, SeedData.Artists(), new TitleSet(), new CorrectionSet());

    /// <summary>Charge la base d'un projet ; un fichier absent prend la valeur livrée, un fichier défectueux est mis de côté (message).</summary>
    /// <param name="projectFolder">Dossier du projet.</param>
    /// <returns>La base et les messages éventuels.</returns>
    public static (MusicBase Base, IReadOnlyList<string> Messages) Load(string projectFolder)
    {
        var messages = new List<string>();
        var taxonomy = ProjectPartStore.Load(projectFolder, TaxonomyFile, TaxonomyType, () => DefaultTaxonomy.Value);
        var artists = ProjectPartStore.Load(projectFolder, ArtistsFile, ArtistsType, SeedData.Artists);
        var titles = ProjectPartStore.Load(projectFolder, TitlesFile, TitlesType, () => new TitleSet());
        var corrections = ProjectPartStore.Load(projectFolder, CorrectionsFile, CorrectionsType, () => new CorrectionSet());
        foreach (var message in new[] { taxonomy.Message, artists.Message, titles.Message, corrections.Message })
        {
            if (message is not null)
            {
                messages.Add(message);
            }
        }

        return (new MusicBase(taxonomy.Value.Families.Count == 0 ? DefaultTaxonomy.Value : taxonomy.Value, artists.Value, titles.Value, corrections.Value), messages);
    }

    /// <summary>Enregistre la base d'un projet (artistes, titres, corrections ; la taxonomie seulement si elle diffère de celle livrée).</summary>
    /// <param name="projectFolder">Dossier du projet.</param>
    /// <param name="musicBase">Base à enregistrer.</param>
    public static void Save(string projectFolder, MusicBase musicBase)
    {
        ArgumentNullException.ThrowIfNull(musicBase);
        ProjectPartStore.Save(projectFolder, ArtistsFile, musicBase.ToArtistSet(), ArtistsType);
        ProjectPartStore.Save(projectFolder, TitlesFile, musicBase.ToTitleSet(), TitlesType);
        ProjectPartStore.Save(projectFolder, CorrectionsFile, musicBase.ToCorrectionSet(), CorrectionsType);
        if (!ReferenceEquals(musicBase.Taxonomy, DefaultTaxonomy.Value) || File.Exists(Path.Combine(projectFolder, TaxonomyFile)))
        {
            ProjectPartStore.Save(projectFolder, TaxonomyFile, musicBase.Taxonomy, TaxonomyType);
        }
    }
}
