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
