using Luxia.Persistence;
using Luxia.Persistence.Json;
using Luxia.Scenes.Model;

namespace Luxia.Scenes;

/// <summary>Lecture / écriture de <c>live.json</c> (doc 50).</summary>
public static class LiveStore
{
    /// <summary>Nom du fichier.</summary>
    public const string FileName = "live.json";

    /// <summary>Type de document « live ».</summary>
    public static readonly DocumentType<LiveSettings> DocumentType = new("live", LiveSettings.CurrentFormatVersion, []);

    /// <summary>Charge les réglages du Live ; absent = écran déduit des couches et des scènes.</summary>
    public static (LiveSettings Value, string? Message) Load(string projectFolder) =>
        ProjectPartStore.Load(projectFolder, FileName, DocumentType, () => new LiveSettings());

    /// <summary>Enregistre les réglages du Live.</summary>
    public static void Save(string projectFolder, LiveSettings settings) =>
        ProjectPartStore.Save(projectFolder, FileName, settings, DocumentType);
}
