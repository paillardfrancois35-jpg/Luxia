namespace Luxia.Core.Snapshots;

/// <summary>
/// Instantané de console (CONS-010) : l'état de tous les faders pris, pour les tests répétitifs.
/// Rangé dans le projet (fichier <c>console.json</c>).
/// </summary>
public sealed record ConsoleSnapshot
{
    /// <summary>Identifiant stable (GEN-052).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom affiché (« PAR 1 en blanc »).</summary>
    public required string Name { get; init; }

    /// <summary>Catégorie (« Phase P1 » tant que l'utilisateur ne l'a pas validé, doc 41 REF-3).</summary>
    public string? Category { get; init; }

    /// <summary>Description (ce qu'on doit observer).</summary>
    public string? Description { get; init; }

    /// <summary>Univers.</summary>
    public int Universe { get; init; } = 1;

    /// <summary>Canaux pris et leurs valeurs.</summary>
    public IReadOnlyList<SnapshotChannel> Channels { get; init; } = [];
}
