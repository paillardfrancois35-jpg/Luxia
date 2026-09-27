namespace Luxia.Scenes.Model;

/// <summary>Limiteur de strobe (GEN-083, MOT-080).</summary>
public sealed record StrobeSafety
{
    /// <summary>Durée continue maximale de strobe par appareil, en secondes (défaut 10).</summary>
    public double MaxContinuousSeconds { get; init; } = 10;

    /// <summary>Pause forcée après un dépassement, en secondes (défaut 10).</summary>
    public double PauseSeconds { get; init; } = 10;

    /// <summary>Strobe interdit partout.</summary>
    public bool Forbidden { get; init; }

    /// <summary>Vitesse maximale, en % de chaque plage de strobe progressive (100 = pas de plafond).</summary>
    public double MaxSpeedPercent { get; init; } = 100;
}
