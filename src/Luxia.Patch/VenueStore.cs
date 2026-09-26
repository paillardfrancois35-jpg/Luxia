using Luxia.Patch.Model;
using Luxia.Persistence.Json;

namespace Luxia.Patch;

/// <summary>Lecture / écriture de <c>lieux.json</c> (doc 50 §?, lieux du projet, lieu actif — doc 13 §5).</summary>
public static class VenueStore
{
    /// <summary>Nom du fichier.</summary>
    public const string FileName = "lieux.json";

    /// <summary>Type de document « lieux ».</summary>
    public static readonly DocumentType<VenueSet> DocumentType = new("lieux", VenueSet.CurrentFormatVersion, []);

    /// <summary>Charge les lieux d'un projet ; absent = un lieu « Générique » par défaut (INST-050).</summary>
    public static (VenueSet Value, string? Message) Load(string projectFolder)
    {
        var result = VersionedJsonFile.Load(Path.Combine(projectFolder, FileName), DocumentType);
        return result.Succeeded
            ? (result.Value!, result.Message)
            : (new VenueSet(), result.Status == LoadStatus.Missing ? null : result.Message);
    }

    /// <summary>Enregistre les lieux.</summary>
    public static void Save(string projectFolder, VenueSet venues) =>
        VersionedJsonFile.Save(Path.Combine(projectFolder, FileName), venues, DocumentType);
}
