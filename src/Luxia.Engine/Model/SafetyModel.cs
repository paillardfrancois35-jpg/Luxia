namespace Luxia.Engine.Model;

/// <summary>
/// Limites de sûreté compilées (doc 15 §9, étape 9 de la chaîne de rendu) : réglages du projet, canaux de strobe et de
/// fumée du patch, zones interdites des lyres dans le lieu actif. Le moteur ne connaît ni la bibliothèque ni les lieux (D26).
/// </summary>
public sealed record SafetyModel
{
    /// <summary>Aucune limite (projet non ouvert, tests).</summary>
    public static SafetyModel None { get; } = new();

    /// <summary>Réglages du limiteur de strobe (GEN-083).</summary>
    public StrobeLimits Strobe { get; init; } = new();

    /// <summary>Réglages du limiteur de fumée (GEN-084).</summary>
    public SmokeLimits Smoke { get; init; } = new();

    /// <summary>Canaux de strobe (étiquette de sûreté « strobe », BIB-007), un par canal DMX émis.</summary>
    public IReadOnlyList<GuardedChannel> StrobeChannels { get; init; } = [];

    /// <summary>Canaux de fumée (étiquette « fumée »), un par canal DMX émis.</summary>
    public IReadOnlyList<GuardedChannel> SmokeChannels { get; init; } = [];

    /// <summary>Zones interdites Pan/Tilt du lieu actif, par appareil (GEN-085).</summary>
    public IReadOnlyList<MovementGuard> Zones { get; init; } = [];
}
