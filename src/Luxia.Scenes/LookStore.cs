using Luxia.Persistence;
using Luxia.Persistence.Json;
using Luxia.Scenes.Model;

namespace Luxia.Scenes;

/// <summary>Lecture / écriture de <c>looks.json</c> (doc 50, ERG-023).</summary>
public static class LookStore
{
    /// <summary>Nom du fichier.</summary>
    public const string FileName = "looks.json";

    /// <summary>Type de document « looks ».</summary>
    public static readonly DocumentType<LookSet> DocumentType = new("looks", LookSet.CurrentFormatVersion, []);

    /// <summary>Charge les looks ; absent = aucun.</summary>
    public static (LookSet Value, string? Message) Load(string projectFolder) =>
        ProjectPartStore.Load(projectFolder, FileName, DocumentType, () => new LookSet());

    /// <summary>Enregistre les looks.</summary>
    public static void Save(string projectFolder, LookSet looks) =>
        ProjectPartStore.Save(projectFolder, FileName, looks, DocumentType);
}
