namespace Luxia.Patch.Model;

/// <summary>
/// Installation d'un projet (doc 13 §1) : univers, appareils patchés, sélections manuelles.
/// Décrit le kit ; ne dépend pas de la salle du soir (voir <see cref="Venue"/>).
/// Enregistrée dans <c>installation.json</c> (doc 50).
/// </summary>
public sealed record Installation
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Univers (au moins un).</summary>
    public IReadOnlyList<PatchUniverse> Universes { get; init; } = [new PatchUniverse { Number = 1 }];

    /// <summary>Appareils patchés.</summary>
    public IReadOnlyList<PatchedFixture> Fixtures { get; init; } = [];

    /// <summary>Sélections manuelles.</summary>
    public IReadOnlyList<Selection> Selections { get; init; } = [];
}
