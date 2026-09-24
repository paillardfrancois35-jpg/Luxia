namespace Dmx.Core.Projects;

/// <summary>
/// Fiche d'identité d'un projet (fichier <c>projet.json</c> à la racine du dossier du projet, doc 02 §10.1).
/// Le contenu (installation, scènes…) viendra dans d'autres fichiers du même dossier, phase par phase.
/// </summary>
public sealed record ProjectInfo
{
    /// <summary>Version courante du format de <c>projet.json</c>.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Identifiant stable (GEN-052).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom affiché.</summary>
    public required string Name { get; init; }

    /// <summary>Description libre.</summary>
    public string? Description { get; init; }

    /// <summary>Date de création (UTC).</summary>
    public DateTime CreatedUtc { get; init; }

    /// <summary>Date de dernière modification (UTC).</summary>
    public DateTime ModifiedUtc { get; init; }
}
