using Luxia.Patch.Model;
using Luxia.Persistence;
using Luxia.Persistence.Json;

namespace Luxia.Patch;

/// <summary>Lecture / écriture de <c>groupes.json</c> (doc 50, ERG-036).</summary>
public static class GroupStore
{
    /// <summary>Nom du fichier.</summary>
    public const string FileName = "groupes.json";

    /// <summary>Type de document « groupes ».</summary>
    public static readonly DocumentType<FixtureGroupSet> DocumentType = new("groupes", FixtureGroupSet.CurrentFormatVersion, []);

    /// <summary>Charge les groupes ; absent = aucun (tous les appareils sont « non assignés »).</summary>
    public static (FixtureGroupSet Value, string? Message) Load(string projectFolder) =>
        ProjectPartStore.Load(projectFolder, FileName, DocumentType, () => new FixtureGroupSet());

    /// <summary>Enregistre les groupes.</summary>
    public static void Save(string projectFolder, FixtureGroupSet groups) =>
        ProjectPartStore.Save(projectFolder, FileName, groups, DocumentType);
}
