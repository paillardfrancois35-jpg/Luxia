using Luxia.Persistence;
using Luxia.Persistence.Json;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;

namespace Luxia.Scenes;

/// <summary>Lecture / écriture de <c>palettes.json</c> (doc 50).</summary>
public static class PaletteStore
{
    /// <summary>Nom du fichier.</summary>
    public const string FileName = "palettes.json";

    /// <summary>Type de document « palettes ».</summary>
    public static readonly DocumentType<PaletteSet> DocumentType = new("palettes", PaletteSet.CurrentFormatVersion, []);

    /// <summary>Charge les palettes d'un projet ; absent = jeu de palettes couleur par défaut (PAL-009).</summary>
    public static (PaletteSet Value, string? Message) Load(string projectFolder) =>
        ProjectPartStore.Load(projectFolder, FileName, DocumentType, DefaultPalettes.Create);

    /// <summary>Enregistre les palettes.</summary>
    public static void Save(string projectFolder, PaletteSet palettes) =>
        ProjectPartStore.Save(projectFolder, FileName, palettes, DocumentType);
}
