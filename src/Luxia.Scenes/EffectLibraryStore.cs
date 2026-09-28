using Luxia.Persistence;
using Luxia.Persistence.Json;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;

namespace Luxia.Scenes;

/// <summary>Lecture / écriture de <c>effets.json</c> (doc 50, EFF-007).</summary>
public static class EffectLibraryStore
{
    /// <summary>Nom du fichier.</summary>
    public const string FileName = "effets.json";

    /// <summary>Type de document « effets ».</summary>
    public static readonly DocumentType<EffectLibrary> DocumentType = new("effets", EffectLibrary.CurrentFormatVersion, []);

    /// <summary>Charge la bibliothèque d'effets ; absent = modèles livrés (<see cref="DefaultEffects"/>).</summary>
    public static (EffectLibrary Value, string? Message) Load(string projectFolder) =>
        ProjectPartStore.Load(projectFolder, FileName, DocumentType, DefaultEffects.Create);

    /// <summary>Enregistre la bibliothèque.</summary>
    public static void Save(string projectFolder, EffectLibrary library) =>
        ProjectPartStore.Save(projectFolder, FileName, library, DocumentType);
}
