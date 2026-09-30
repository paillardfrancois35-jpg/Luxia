namespace Luxia.Messaging.Commands;

/// <summary>CMD-010 <c>LancerScène</c> : démarre (ou redémarre) une scène dans sa couche.</summary>
/// <param name="Origin">Origine.</param>
/// <param name="SceneId">Scène.</param>
/// <param name="LayerId">Couche ; <c>null</c> = couche d'appartenance de la scène.</param>
/// <param name="Fade">Fondu d'entrée imposé ; <c>null</c> = fondu de la scène, ou fondu croisé de la couche si elle remplace une autre scène.</param>
/// <param name="Solo">
/// Jouer la scène seule (SCN-034) : les autres lectures sont masquées (pas arrêtées) tant qu'elle joue.
/// </param>
/// <param name="Immediate">
/// Démarrer tout de suite, sans attendre l'instant musical choisi par la scène (MOT-018) : utilisé par le moteur quand l'instant arrive.
/// </param>
/// <param name="StopIfPlaying">
/// Bascule (LIVE-003) : si la scène joue déjà, l'arrêter au lieu de la relancer. C'est le moteur qui tranche, au moment
/// où il traite la commande : un écran ou un contrôleur ne décide jamais d'après un état qui peut avoir un tick de retard.
/// </param>
public sealed record LaunchSceneCommand(
    CommandOrigin Origin,
    Guid SceneId,
    Guid? LayerId = null,
    TimeSpan? Fade = null,
    bool Solo = false,
    bool StopIfPlaying = false,
    bool Immediate = false) : Command(Origin);
