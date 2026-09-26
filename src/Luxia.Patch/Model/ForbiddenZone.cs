namespace Luxia.Patch.Model;

/// <summary>
/// Zone interdite d'une lyre dans un lieu (INST-053, GEN-085) : rectangle de valeurs Pan/Tilt que le faisceau ne doit
/// pas viser (yeux du public, miroir…). Valeurs logiques normalisées 0-1, telles que lues dans le programmeur (avant
/// inversion de montage). Le moteur ramène toute cible qui y tombe au bord le plus proche (MOT-082).
/// </summary>
public sealed record ForbiddenZone
{
    /// <summary>Appareil concerné.</summary>
    public required Guid FixtureId { get; init; }

    /// <summary>Nom facultatif (« Public », « Miroir »).</summary>
    public string? Name { get; init; }

    /// <summary>Pan minimal (0-1).</summary>
    public double PanMin { get; init; }

    /// <summary>Pan maximal (0-1).</summary>
    public double PanMax { get; init; } = 1;

    /// <summary>Tilt minimal (0-1).</summary>
    public double TiltMin { get; init; }

    /// <summary>Tilt maximal (0-1).</summary>
    public double TiltMax { get; init; } = 1;
}
