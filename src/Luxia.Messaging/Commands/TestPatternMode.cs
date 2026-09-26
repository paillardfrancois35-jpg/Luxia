namespace Luxia.Messaging.Commands;

/// <summary>Forme du test de sortie.</summary>
public enum TestPatternMode
{
    /// <summary>Un seul canal allumé à la fois, à la valeur de test (SORT-007).</summary>
    Chase,

    /// <summary>
    /// Tous les canaux varient en permanence entre 0 et la valeur de test, décalés d'un canal à l'autre ;
    /// <see cref="TestOutputCommand.StepDuration"/> est la période d'un aller-retour (endurance T-SORT-07).
    /// </summary>
    Ramp,
}
