using Luxia.Persistence;
using Luxia.Persistence.Json;
using Luxia.Show.Model;

namespace Luxia.Show;

/// <summary>Lecture / écriture de <c>shows.json</c> (doc 50, SHOW-020).</summary>
public static class ShowStore
{
    /// <summary>Nom du fichier.</summary>
    public const string FileName = "shows.json";

    /// <summary>Type de document « shows ».</summary>
    public static readonly DocumentType<ShowSet> DocumentType = new("shows", ShowSet.CurrentFormatVersion, []);

    /// <summary>Charge les shows ; absent = aucun.</summary>
    public static (ShowSet Value, string? Message) Load(string projectFolder) =>
        ProjectPartStore.Load(projectFolder, FileName, DocumentType, () => new ShowSet());

    /// <summary>Enregistre les shows.</summary>
    public static void Save(string projectFolder, ShowSet shows) =>
        ProjectPartStore.Save(projectFolder, FileName, shows, DocumentType);
}
