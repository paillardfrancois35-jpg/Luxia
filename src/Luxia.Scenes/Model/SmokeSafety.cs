namespace Luxia.Scenes.Model;

/// <summary>Limiteur de fumée (GEN-084, MOT-081).</summary>
public sealed record SmokeSafety
{
    /// <summary>Durée d'émission continue maximale, en secondes (défaut 10).</summary>
    public double MaxEmissionSeconds { get; init; } = 10;

    /// <summary>Repos après une émission coupée par la limite, et plafond de tout repos, en secondes (défaut 30).</summary>
    public double MinRestSeconds { get; init; } = 30;

    /// <summary>Repos après une émission courte = durée émise × ce facteur, plafonné à <see cref="MinRestSeconds"/> (défaut 3).</summary>
    public double RestFactor { get; init; } = 3;
}
