namespace Luxia.Messaging.Events;

/// <summary>EVT-010 <c>ÉtapeChangée</c> (MOT-101).</summary>
/// <param name="SceneId">Scène.</param>
/// <param name="StepIndex">Nouvelle étape (0 = première).</param>
/// <param name="At">Instant (horloge du moteur).</param>
public sealed record StepChanged(Guid SceneId, int StepIndex, TimeSpan At);
