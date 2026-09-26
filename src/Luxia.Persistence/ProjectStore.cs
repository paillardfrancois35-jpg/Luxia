using Luxia.Core.Projects;
using Luxia.Persistence.Json;

namespace Luxia.Persistence;

/// <summary>Résultat de l'ouverture d'un projet : les problèmes sont listés, jamais bloquants pour le reste (GEN-056).</summary>
/// <param name="Folder">Dossier du projet.</param>
/// <param name="Info">Fiche du projet (null si illisible).</param>
/// <param name="Messages">Messages à montrer à l'utilisateur (migrations, fichiers mis de côté).</param>
public sealed record ProjectLoadReport(string Folder, ProjectInfo? Info, IReadOnlyList<string> Messages)
{
    /// <summary>Le projet est utilisable.</summary>
    public bool Succeeded => Info is not null;
}

/// <summary>Création et ouverture d'un projet (un dossier de fichiers JSON, D12).</summary>
public static class ProjectStore
{
    /// <summary>Nom du fichier d'identité du projet.</summary>
    public const string ProjectFileName = "projet.json";

    /// <summary>Type de document « projet ».</summary>
    public static readonly DocumentType<ProjectInfo> DocumentType = new("projet", ProjectInfo.CurrentFormatVersion, []);

    /// <summary>Crée un projet vide dans <paramref name="folder"/> (créé si besoin).</summary>
    public static ProjectInfo Create(string folder, string name, string? description = null, TimeProvider? time = null)
    {
        var now = (time ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        var info = new ProjectInfo { Name = name, Description = description, CreatedUtc = now, ModifiedUtc = now };
        Directory.CreateDirectory(folder);
        VersionedJsonFile.Save(Path.Combine(folder, ProjectFileName), info, DocumentType);
        return info;
    }

    /// <summary>Ouvre un projet.</summary>
    public static ProjectLoadReport Open(string folder)
    {
        var result = VersionedJsonFile.Load(Path.Combine(folder, ProjectFileName), DocumentType);
        var messages = result.Message is null ? Array.Empty<string>() : [result.Message];
        return new ProjectLoadReport(folder, result.Value, messages);
    }

    /// <summary>Enregistre la fiche du projet (date de modification mise à jour).</summary>
    public static ProjectInfo Save(string folder, ProjectInfo info, TimeProvider? time = null)
    {
        ArgumentNullException.ThrowIfNull(info);
        var updated = info with { ModifiedUtc = (time ?? TimeProvider.System).GetUtcNow().UtcDateTime };
        VersionedJsonFile.Save(Path.Combine(folder, ProjectFileName), updated, DocumentType);
        return updated;
    }
}
