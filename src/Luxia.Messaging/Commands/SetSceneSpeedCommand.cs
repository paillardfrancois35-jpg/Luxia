namespace Luxia.Messaging.Commands;

/// <summary>CMD-016 <c>RéglerVitesseScène</c> : multiplicateur de vitesse d'une lecture, en direct (MOT-015).</summary>
/// <param name="Origin">Origine.</param>
/// <param name="SceneId">Scène en cours de lecture.</param>
/// <param name="Speed">Multiplicateur (borné de 0,1 à 10).</param>
public sealed record SetSceneSpeedCommand(CommandOrigin Origin, Guid SceneId, double Speed) : Command(Origin);
