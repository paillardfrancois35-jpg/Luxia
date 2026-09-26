using Luxia.Persistence;
using Luxia.Persistence.Json;
using Luxia.Scenes.Model;

namespace Luxia.Scenes;

/// <summary>Lecture / écriture de <c>couches.json</c> (doc 50).</summary>
public static class LayerStore
{
    /// <summary>Nom du fichier.</summary>
    public const string FileName = "couches.json";

    /// <summary>Type de document « couches ».</summary>
    public static readonly DocumentType<LayerSet> DocumentType = new("couches", LayerSet.CurrentFormatVersion, []);

    /// <summary>Charge les couches d'un projet ; absent = modèle de couches par défaut (doc 17 §1.3).</summary>
    public static (LayerSet Value, string? Message) Load(string projectFolder) =>
        ProjectPartStore.Load(projectFolder, FileName, DocumentType, LayerSet.Default);

    /// <summary>Enregistre les couches.</summary>
    public static void Save(string projectFolder, LayerSet layers) =>
        ProjectPartStore.Save(projectFolder, FileName, layers, DocumentType);
}
