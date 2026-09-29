namespace Luxia.Patch.Model;

/// <summary>
/// Groupe d'appareils de l'arbre des dimmers (ERG-036, Q37) : un nom, un parent (arbre), les appareils qu'il contient
/// directement et, éventuellement, un dimmer (un niveau réglable en direct, multiplié le long de l'arbre).
/// Un appareil n'est que dans un seul groupe ; un appareil sans groupe est dans le groupe implicite « non assigné »
/// (<see cref="FixtureGroupSet.UnassignedName"/>), sans dimmer.
/// </summary>
public sealed record FixtureGroup
{
    /// <summary>Identifiant stable (GEN-052).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom affiché.</summary>
    public required string Name { get; init; }

    /// <summary>Groupe parent ; <c>null</c> = racine.</summary>
    public Guid? ParentId { get; init; }

    /// <summary>Le groupe a un dimmer (un fader dans « Groupes dimmer », affectable à la seconde platine MIDI).</summary>
    public bool HasDimmer { get; init; }

    /// <summary>Appareils rangés directement dans ce groupe (les sous-groupes ont les leurs).</summary>
    public IReadOnlyList<Guid> FixtureIds { get; init; } = [];
}

/// <summary>Arbre des groupes d'un projet, enregistré dans <c>groupes.json</c> (doc 50).</summary>
public sealed record FixtureGroupSet
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Nom par défaut du groupe implicite des appareils sans groupe.</summary>
    public const string DefaultUnassignedName = "Non assigné";

    /// <summary>Nom du groupe implicite des appareils sans groupe (renommable).</summary>
    public string UnassignedName { get; init; } = DefaultUnassignedName;

    /// <summary>Groupes, dans l'ordre d'affichage (les frères gardent cet ordre dans l'arbre).</summary>
    public IReadOnlyList<FixtureGroup> Groups { get; init; } = [];
}
