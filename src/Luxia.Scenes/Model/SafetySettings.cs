namespace Luxia.Scenes.Model;

/// <summary>
/// Réglages des limites de sûreté du projet (doc 02 §13, GEN-083, GEN-084), enregistrés dans <c>sûreté.json</c> (doc 50).
/// Ils voyagent avec le show : une IA de conception ou l'utilisateur les règle une fois pour le parc (D29).
/// Les zones interdites des lyres sont propres à chaque lieu (<c>lieux.json</c>, INST-053).
/// </summary>
public sealed record SafetySettings
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Limiteur de strobe.</summary>
    public StrobeSafety Strobe { get; init; } = new();

    /// <summary>Limiteur de fumée.</summary>
    public SmokeSafety Smoke { get; init; } = new();
}

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

/// <summary>Limiteur de fumée (GEN-084, MOT-081).</summary>
public sealed record SmokeSafety
{
    /// <summary>Durée d'émission continue maximale, en secondes (défaut 10).</summary>
    public double MaxEmissionSeconds { get; init; } = 10;

    /// <summary>Repos minimal entre deux émissions, en secondes (défaut 30).</summary>
    public double MinRestSeconds { get; init; } = 30;
}
