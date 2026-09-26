namespace Luxia.Engine.Model;

/// <summary>Réglages du limiteur de fumée (GEN-084, MOT-081).</summary>
public sealed record SmokeLimits
{
    /// <summary>Durée d'émission continue maximale, en secondes.</summary>
    public double MaxEmissionSeconds { get; init; } = 10;

    /// <summary>Repos minimal entre deux émissions, en secondes.</summary>
    public double MinRestSeconds { get; init; } = 30;
}
