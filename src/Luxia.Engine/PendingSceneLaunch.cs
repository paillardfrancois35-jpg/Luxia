namespace Luxia.Engine;

/// <summary>Scène qui attend son instant musical pour démarrer (MOT-018), publiée avec l'instantané.</summary>
/// <param name="SceneId">Scène.</param>
/// <param name="BeatsRemaining">Temps restant à attendre.</param>
public readonly record struct PendingSceneLaunch(Guid SceneId, double BeatsRemaining);
