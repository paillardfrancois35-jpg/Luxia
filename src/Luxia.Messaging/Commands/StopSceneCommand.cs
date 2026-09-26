namespace Luxia.Messaging.Commands;

/// <summary>CMD-011 <c>ArrêterScène</c> : fondu de sortie puis arrêt.</summary>
/// <param name="Origin">Origine.</param>
/// <param name="SceneId">Scène.</param>
/// <param name="Fade">Fondu de sortie imposé ; <c>null</c> = fondu de sortie de la scène.</param>
public sealed record StopSceneCommand(CommandOrigin Origin, Guid SceneId, TimeSpan? Fade = null) : Command(Origin);
