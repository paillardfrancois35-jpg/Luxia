using Dmx.Patch.Model;
using Dmx.Persistence.Json;

namespace Dmx.Patch;

/// <summary>Lecture / écriture de <c>installation.json</c> (doc 50 §? , univers, patch, sélections manuelles).</summary>
public static class InstallationStore
{
    /// <summary>Nom du fichier.</summary>
    public const string FileName = "installation.json";

    /// <summary>Type de document « installation ».</summary>
    public static readonly DocumentType<Installation> DocumentType = new("installation", Installation.CurrentFormatVersion, []);

    /// <summary>Charge l'installation d'un projet ; absente = installation vide (un univers, aucun appareil).</summary>
    public static (Installation Value, string? Message) Load(string projectFolder)
    {
        var result = VersionedJsonFile.Load(Path.Combine(projectFolder, FileName), DocumentType);
        return result.Succeeded
            ? (result.Value!, result.Message)
            : (new Installation(), result.Status == LoadStatus.Missing ? null : result.Message);
    }

    /// <summary>Enregistre l'installation.</summary>
    public static void Save(string projectFolder, Installation installation) =>
        VersionedJsonFile.Save(Path.Combine(projectFolder, FileName), installation, DocumentType);
}
