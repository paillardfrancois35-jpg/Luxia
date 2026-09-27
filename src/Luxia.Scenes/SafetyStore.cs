using Luxia.Persistence;
using Luxia.Persistence.Json;
using Luxia.Scenes.Model;

namespace Luxia.Scenes;

/// <summary>Lecture / écriture de <c>sûreté.json</c> (doc 50).</summary>
public static class SafetyStore
{
    /// <summary>Nom du fichier.</summary>
    public const string FileName = "sûreté.json";

    /// <summary>Type de document « sûreté ».</summary>
    public static readonly DocumentType<SafetySettings> DocumentType = new("sûreté", SafetySettings.CurrentFormatVersion, []);

    /// <summary>Charge les réglages de sûreté ; absent = valeurs par défaut (10 s de strobe, 10 s de fumée, 30 s de repos).</summary>
    public static (SafetySettings Value, string? Message) Load(string projectFolder) =>
        ProjectPartStore.Load(projectFolder, FileName, DocumentType, () => new SafetySettings());

    /// <summary>Enregistre les réglages de sûreté.</summary>
    public static void Save(string projectFolder, SafetySettings settings) =>
        ProjectPartStore.Save(projectFolder, FileName, settings, DocumentType);
}
