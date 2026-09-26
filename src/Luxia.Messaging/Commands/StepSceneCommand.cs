namespace Luxia.Messaging.Commands;

/// <summary>CMD-015 <c>ÉtapeSuivante</c> / <c>ÉtapePrécédente</c> : pas à pas manuel sur une lecture (MOT-019).</summary>
/// <param name="Origin">Origine.</param>
/// <param name="SceneId">Scène en cours de lecture.</param>
/// <param name="Direction">Sens.</param>
public sealed record StepSceneCommand(CommandOrigin Origin, Guid SceneId, StepDirection Direction) : Command(Origin);
