namespace Luxia.Messaging.Commands;

/// <summary>
/// CMD-017 <c>MontrerÉtape</c> : l'étape d'une scène est jouée, effets compris, au-dessus de toutes les couches et figée
/// sur cette étape, sans apparaître comme une scène lancée (aperçu de l'édition, EFF-006). Une seule étape montrée à la
/// fois ; <paramref name="SceneId"/> nul : plus rien n'est montré.
/// </summary>
/// <param name="Origin">Origine.</param>
/// <param name="SceneId">Scène, ou nul pour arrêter l'aperçu.</param>
/// <param name="StepIndex">Étape (0 = première).</param>
public sealed record ShowStepCommand(CommandOrigin Origin, Guid? SceneId, int StepIndex = 0) : Command(Origin);
