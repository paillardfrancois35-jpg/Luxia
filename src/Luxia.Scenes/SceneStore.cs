using Luxia.Persistence;
using Luxia.Persistence.Json;
using Luxia.Scenes.Model;

namespace Luxia.Scenes;

/// <summary>Lecture / écriture de <c>scènes.json</c> (doc 50).</summary>
public static class SceneStore
{
    /// <summary>Nom du fichier.</summary>
    public const string FileName = "scènes.json";

    /// <summary>Type de document « scènes ».</summary>
    public static readonly DocumentType<SceneSet> DocumentType = new("scènes", SceneSet.CurrentFormatVersion, []);

    /// <summary>Charge les scènes d'un projet ; absent = contenu d'un nouveau projet (scène « Plein feu », MOT-042).</summary>
    public static (SceneSet Value, string? Message) Load(string projectFolder) =>
        ProjectPartStore.Load(projectFolder, FileName, DocumentType, SceneSet.Default);

    /// <summary>Enregistre les scènes.</summary>
    public static void Save(string projectFolder, SceneSet scenes) =>
        ProjectPartStore.Save(projectFolder, FileName, scenes, DocumentType);
}
