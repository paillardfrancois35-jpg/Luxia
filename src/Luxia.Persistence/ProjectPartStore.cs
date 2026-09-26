using Luxia.Core.Snapshots;
using Luxia.Persistence.Json;

namespace Luxia.Persistence;

/// <summary>
/// Fichiers d'un projet autres que <c>projet.json</c> : un fichier par partie (D12), chacun versionné.
/// Un fichier défectueux est mis de côté sans empêcher le reste du projet de se charger (GEN-056).
/// </summary>
public static class ProjectPartStore
{
    /// <summary>Fichier des instantanés de console.</summary>
    public const string ConsoleFileName = "console.json";

    /// <summary>Type de document « console ».</summary>
    public static readonly DocumentType<ConsoleData> ConsoleType = new("console", ConsoleData.CurrentFormatVersion, []);

    /// <summary>Charge une partie ; absente = valeur par défaut.</summary>
    public static (T Value, string? Message) Load<T>(string projectFolder, string fileName, DocumentType<T> type, Func<T> empty)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(empty);
        var result = VersionedJsonFile.Load(Path.Combine(projectFolder, fileName), type);
        return result.Succeeded ? (result.Value!, result.Message) : (empty(), result.Status == LoadStatus.Missing ? null : result.Message);
    }

    /// <summary>Enregistre une partie.</summary>
    public static void Save<T>(string projectFolder, string fileName, T value, DocumentType<T> type)
        where T : class =>
        VersionedJsonFile.Save(Path.Combine(projectFolder, fileName), value, type);
}
