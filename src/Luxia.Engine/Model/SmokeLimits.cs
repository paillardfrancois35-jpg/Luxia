namespace Luxia.Engine.Model;

/// <summary>Réglages du limiteur de fumée (GEN-084, MOT-081).</summary>
public sealed record SmokeLimits
{
    /// <summary>Durée d'émission continue maximale, en secondes.</summary>
    public double MaxEmissionSeconds { get; init; } = 10;

    /// <summary>Repos après une émission coupée par la limite, et plafond de tout repos, en secondes.</summary>
    public double MinRestSeconds { get; init; } = 30;

    /// <summary>
    /// Repos après une émission plus courte que la limite = durée émise × ce facteur, plafonné à
    /// <see cref="MinRestSeconds"/> (essai P5 : 30 s après une bouffée de 2 s était excessif). Avec 3, la machine ne
    /// fume jamais plus d'un quart du temps. 0 = aucun repos après une émission courte.
    /// </summary>
    public double RestFactor { get; init; } = 3;
}
