namespace Luxia.Engine.Model;

/// <summary>Zones interdites et zone permise d'un appareil à Pan/Tilt dans le lieu actif (MOT-082, F7).</summary>
public sealed record MovementGuard
{
    /// <summary>Appareil (appareil de référence pour des jumeaux, qui partagent Pan et Tilt).</summary>
    public required Guid FixtureId { get; init; }

    /// <summary>Libellé lisible.</summary>
    public required string Label { get; init; }

    /// <summary>Indice du paramètre Pan.</summary>
    public required int Pan { get; init; }

    /// <summary>Indice du paramètre Tilt.</summary>
    public required int Tilt { get; init; }

    /// <summary>Rectangles interdits.</summary>
    public IReadOnlyList<PanTiltZone> Zones { get; init; } = [];

    /// <summary>
    /// Zone permise (F7, ERG-017) : les limites de l'appareil ; une cible en dehors est ramenée sur son bord. Nulle :
    /// aucune limite.
    /// </summary>
    public PanTiltZone? Limits { get; init; }
}
