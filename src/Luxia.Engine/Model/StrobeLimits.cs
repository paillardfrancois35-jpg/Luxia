namespace Luxia.Engine.Model;

/// <summary>Réglages du limiteur de strobe (GEN-083, MOT-080).</summary>
public sealed record StrobeLimits
{
    /// <summary>Durée continue maximale de strobe par appareil, en secondes.</summary>
    public double MaxContinuousSeconds { get; init; } = 10;

    /// <summary>Pause forcée après un dépassement, en secondes.</summary>
    public double PauseSeconds { get; init; } = 10;

    /// <summary>Strobe interdit partout.</summary>
    public bool Forbidden { get; init; }

    /// <summary>Vitesse maximale (0-1 de chaque plage progressive de strobe ; 1 = pas de plafond).</summary>
    public double MaxSpeed { get; init; } = 1;
}
