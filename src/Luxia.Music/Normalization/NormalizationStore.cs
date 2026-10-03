using Luxia.Persistence;
using Luxia.Persistence.Json;

namespace Luxia.Music.Normalization;

/// <summary>Lecture / écriture de <c>normalisation.json</c> (doc 50, MUS-020) : règles de normalisation modifiables du projet.</summary>
public static class NormalizationStore
{
    /// <summary>Nom du fichier.</summary>
    public const string FileName = "normalisation.json";

    /// <summary>Type de document « normalisation ».</summary>
    public static readonly DocumentType<NormalizationRules> DocumentType = new("normalisation", NormalizationRules.CurrentFormatVersion, []);

    /// <summary>Charge les règles ; absent = règles livrées avec l'application.</summary>
    public static (NormalizationRules Value, string? Message) Load(string projectFolder) =>
        ProjectPartStore.Load(projectFolder, FileName, DocumentType, () => NormalizationRules.Default);

    /// <summary>Enregistre les règles.</summary>
    public static void Save(string projectFolder, NormalizationRules rules) =>
        ProjectPartStore.Save(projectFolder, FileName, rules, DocumentType);
}
