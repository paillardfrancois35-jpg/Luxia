namespace Luxia.Messaging.Events;

/// <summary>EVT-010 <c>ScèneArrêtée</c> (MOT-101) : la lecture est terminée (fondu de sortie achevé ou fin de scène).</summary>
/// <param name="SceneId">Scène.</param>
/// <param name="SceneName">Nom de la scène.</param>
/// <param name="At">Instant (horloge du moteur).</param>
public sealed record SceneStopped(Guid SceneId, string SceneName, TimeSpan At);
