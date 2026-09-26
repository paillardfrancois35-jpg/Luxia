namespace Luxia.Messaging.Commands;

/// <summary>CMD-010 <c>LancerScène</c> : démarre (ou redémarre) une scène dans sa couche.</summary>
/// <param name="Origin">Origine.</param>
/// <param name="SceneId">Scène.</param>
/// <param name="LayerId">Couche ; <c>null</c> = couche d'appartenance de la scène.</param>
/// <param name="Fade">Fondu d'entrée imposé ; <c>null</c> = fondu de la scène, ou fondu croisé de la couche si elle remplace une autre scène.</param>
/// <param name="Solo">
/// Jouer la scène seule (SCN-034) : les autres lectures sont masquées (pas arrêtées) tant qu'elle joue.
/// </param>
public sealed record LaunchSceneCommand(
    CommandOrigin Origin,
    Guid SceneId,
    Guid? LayerId = null,
    TimeSpan? Fade = null,
    bool Solo = false) : Command(Origin);
