namespace Luxia.Messaging.Events;

/// <summary>EVT-030 <c>ÉtapeShowActivée</c> : une étape d'un show devient active (supervision, journal).</summary>
/// <param name="ShowId">Show.</param>
/// <param name="ShowName">Nom du show.</param>
/// <param name="StepId">Identifiant de l'étape (« 2a »).</param>
/// <param name="StepName">Nom de l'étape.</param>
/// <param name="Reason">Ce qui l'a activée (« drop », « lancement », « forcée »…).</param>
/// <param name="At">Instant (horloge du moteur).</param>
public sealed record ShowStepActivated(Guid ShowId, string ShowName, string StepId, string StepName, string Reason, TimeSpan At);

/// <summary>Un show démarre ou s'arrête (journal, colonne « Shows »).</summary>
/// <param name="ShowId">Show.</param>
/// <param name="ShowName">Nom du show.</param>
/// <param name="Running">Le show joue (<c>true</c>) ou vient de s'arrêter.</param>
/// <param name="At">Instant (horloge du moteur).</param>
public sealed record ShowStateChanged(Guid ShowId, string ShowName, bool Running, TimeSpan At);

/// <summary>EVT-024 <c>TempoChangé</c> : le tempo bouge d'au moins 1 BPM ou change de source (D38).</summary>
/// <param name="Bpm">Tempo.</param>
/// <param name="Confidence">Confiance (0 à 1).</param>
/// <param name="Source">Source de l'horloge.</param>
/// <param name="At">Instant (horloge du moteur).</param>
public sealed record TempoChanged(double Bpm, double Confidence, Commands.TempoSourceKind Source, TimeSpan At);
