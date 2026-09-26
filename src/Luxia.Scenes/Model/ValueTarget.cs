namespace Luxia.Scenes.Model;

/// <summary>
/// Cible d'une valeur de scène (SCN-007) : un appareil (ou une de ses cellules), une sélection manuelle,
/// ou une sélection automatique. Une seule des trois formes est renseignée.
/// </summary>
public sealed record ValueTarget
{
    /// <summary>Appareil patché visé.</summary>
    public Guid? FixtureId { get; init; }

    /// <summary>Cellule de l'appareil (0 = appareil entier, doc 12 §2.6).</summary>
    public int Cell { get; init; }

    /// <summary>Sélection manuelle visée (INST-030), dans son ordre.</summary>
    public Guid? SelectionId { get; init; }

    /// <summary>Sélection automatique visée (INST-031), dans l'ordre du patch.</summary>
    public AutoSelectionTarget? Auto { get; init; }

    /// <summary>Un appareil entier, ou une de ses cellules.</summary>
    public static ValueTarget Fixture(Guid fixtureId, int cell = 0) => new() { FixtureId = fixtureId, Cell = cell };

    /// <summary>Une sélection manuelle.</summary>
    public static ValueTarget Selection(Guid selectionId) => new() { SelectionId = selectionId };
}
