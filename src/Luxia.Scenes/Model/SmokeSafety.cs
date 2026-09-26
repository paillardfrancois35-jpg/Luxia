namespace Luxia.Scenes.Model;

/// <summary>Limiteur de fumée (GEN-084, MOT-081).</summary>
public sealed record SmokeSafety
{
    /// <summary>Durée d'émission continue maximale, en secondes (défaut 10).</summary>
    public double MaxEmissionSeconds { get; init; } = 10;

    /// <summary>Repos minimal entre deux émissions, en secondes (défaut 30).</summary>
    public double MinRestSeconds { get; init; } = 30;
}
