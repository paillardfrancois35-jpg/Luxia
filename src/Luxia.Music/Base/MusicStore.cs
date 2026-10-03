using System.Globalization;
using System.Text.Json.Nodes;
using Luxia.Persistence;
using Luxia.Persistence.Json;

namespace Luxia.Music.Base;

/// <summary>
/// Lecture / écriture de la base musicale d'un projet (doc 50 §12j) : <c>taxonomie.json</c>, <c>artistes.json</c>, <c>titres.json</c>.
/// Les fichiers de la version 1 (poids, origine, <c>corrections.json</c>, <c>aclasser.json</c>) sont migrés au premier chargement, l'original
/// gardé en <c>.v1.bak</c>. Facultatifs : sans fichier, la taxonomie et la base de départ livrées s'appliquent (Q48).
/// </summary>
public static class MusicStore
{
    /// <summary>Fichier de la taxonomie.</summary>
    public const string TaxonomyFile = "taxonomie.json";

    /// <summary>Fichier des artistes.</summary>
    public const string ArtistsFile = "artistes.json";

    /// <summary>Fichier des titres.</summary>
    public const string TitlesFile = "titres.json";

    /// <summary>Fichier des propositions de l'outil d'enrichissement, en attente de validation.</summary>
    public const string ProposalsFile = "propositions.json";

    private static readonly DocumentType<Classification.ProposalSet> ProposalsType = new("propositions", Classification.ProposalSet.CurrentFormatVersion, []);
    private static readonly DocumentType<Taxonomy> TaxonomyType = new("taxonomie", Taxonomy.CurrentFormatVersion, []);
    private static readonly DocumentType<ArtistSet> ArtistsType = new("artistes", ArtistSet.CurrentFormatVersion, [new JsonMigration(1, ArtistsV1ToV2)]);
    private static readonly DocumentType<TitleSet> TitlesType = new("titres", TitleSet.CurrentFormatVersion, [new JsonMigration(1, TitlesV1ToV2)]);

    /// <summary>La base livrée avec l'application, sans aucun fichier de projet.</summary>
    /// <returns>Taxonomie par défaut et artistes de départ.</returns>
    public static MusicBase Default() => new(DefaultTaxonomy.Value, SeedData.Artists(), new TitleSet());

    /// <summary>Charge la base d'un projet ; un fichier absent prend la valeur livrée, un fichier défectueux est mis de côté (message).</summary>
    /// <param name="projectFolder">Dossier du projet.</param>
    /// <returns>La base et les messages éventuels.</returns>
    public static (MusicBase Base, IReadOnlyList<string> Messages) Load(string projectFolder)
    {
        var messages = new List<string>();
        var taxonomy = ProjectPartStore.Load(projectFolder, TaxonomyFile, TaxonomyType, () => DefaultTaxonomy.Value);
        var artists = ProjectPartStore.Load(projectFolder, ArtistsFile, ArtistsType, SeedData.Artists);
        var titles = ProjectPartStore.Load(projectFolder, TitlesFile, TitlesType, () => new TitleSet());
        foreach (var message in new[] { taxonomy.Message, artists.Message, titles.Message, SetAsideObsolete(projectFolder) })
        {
            if (message is not null)
            {
                messages.Add(message);
            }
        }

        return (new MusicBase(taxonomy.Value.Families.Count == 0 ? DefaultTaxonomy.Value : taxonomy.Value, artists.Value, titles.Value), messages);
    }

    /// <summary>Charge seulement la taxonomie du projet (validation des shows) ; absente = celle livrée.</summary>
    /// <param name="projectFolder">Dossier du projet.</param>
    /// <returns>La taxonomie.</returns>
    public static Taxonomy LoadTaxonomy(string projectFolder)
    {
        var loaded = ProjectPartStore.Load(projectFolder, TaxonomyFile, TaxonomyType, () => DefaultTaxonomy.Value).Value;
        return loaded.Families.Count == 0 ? DefaultTaxonomy.Value : loaded;
    }

    /// <summary>Charge les propositions de l'outil d'enrichissement (MUS-040) ; absent = aucune.</summary>
    /// <param name="projectFolder">Dossier du projet.</param>
    /// <returns>Les propositions.</returns>
    public static Classification.ProposalSet LoadProposals(string projectFolder) =>
        ProjectPartStore.Load(projectFolder, ProposalsFile, ProposalsType, () => new Classification.ProposalSet()).Value;

    /// <summary>Enregistre les propositions en attente de validation.</summary>
    /// <param name="projectFolder">Dossier du projet.</param>
    /// <param name="proposals">Propositions.</param>
    public static void SaveProposals(string projectFolder, Classification.ProposalSet proposals) =>
        ProjectPartStore.Save(projectFolder, ProposalsFile, proposals, ProposalsType);

    /// <summary>Enregistre la base d'un projet (artistes, titres ; la taxonomie seulement si elle diffère de celle livrée).</summary>
    /// <param name="projectFolder">Dossier du projet.</param>
    /// <param name="musicBase">Base à enregistrer.</param>
    public static void Save(string projectFolder, MusicBase musicBase)
    {
        ArgumentNullException.ThrowIfNull(musicBase);
        ProjectPartStore.Save(projectFolder, ArtistsFile, musicBase.ToArtistSet(), ArtistsType);
        ProjectPartStore.Save(projectFolder, TitlesFile, musicBase.ToTitleSet(), TitlesType);
        if (!ReferenceEquals(musicBase.Taxonomy, DefaultTaxonomy.Value) || File.Exists(Path.Combine(projectFolder, TaxonomyFile)))
        {
            ProjectPartStore.Save(projectFolder, TaxonomyFile, musicBase.Taxonomy, TaxonomyType);
        }
    }

    // Version 1 -> 2 : un seul style par artiste (le plus pondéré), plus de poids ni d'origine, un code stable par artiste.
    private static void ArtistsV1ToV2(JsonObject document)
    {
        if (document["artists"] is not JsonArray artists)
        {
            return;
        }

        var n = 0;
        foreach (var node in artists.OfType<JsonObject>())
        {
            var style = node["styles"] is JsonObject styles
                ? styles.Where(kv => kv.Value is not null).OrderByDescending(kv => kv.Value!.GetValue<double>()).Select(kv => kv.Key).FirstOrDefault()
                : null;
            node.Remove("styles");
            node.Remove("source");
            node["code"] = "A" + (++n).ToString("D5", CultureInfo.InvariantCulture);
            node["style"] = style ?? Taxonomy.UnknownId;
        }
    }

    private static void TitlesV1ToV2(JsonObject document)
    {
        if (document["titles"] is not JsonArray titles)
        {
            return;
        }

        foreach (var node in titles.OfType<JsonObject>())
        {
            node.Remove("source");
        }
    }

    // Les corrections en Live et la liste « À classer » enregistrée n'existent plus : une correction est une modification de la base,
    // « À classer » est le filtre « Inconnu » de la liste des artistes. Les anciens fichiers sont gardés en .v1.bak.
    private static string? SetAsideObsolete(string projectFolder)
    {
        var moved = new List<string>();
        foreach (var name in new[] { "corrections.json", "aclasser.json" })
        {
            var path = Path.Combine(projectFolder, name);
            if (File.Exists(path))
            {
                File.Move(path, path + ".v1.bak", overwrite: true);
                moved.Add(name);
            }
        }

        return moved.Count == 0 ? null : $"Fichiers abandonnés mis de côté : {string.Join(", ", moved)} (copie .v1.bak)";
    }
}
