namespace Luxia.Messaging.Events;

/// <summary>Nature d'une limite de sûreté (doc 15 §9).</summary>
public enum SafetyLimitKind
{
    /// <summary>Strobe : durée continue dépassée, pause forcée, vitesse plafonnée ou strobe interdit (MOT-080).</summary>
    Strobe,

    /// <summary>Fumée : durée d'émission dépassée ou repos minimal en cours (MOT-081).</summary>
    Smoke,

    /// <summary>Zone interdite Pan/Tilt d'une lyre (MOT-082).</summary>
    Zone,
}

/// <summary>EVT-012 <c>LimiteSécuritéAtteinte</c> (MOT-083) : publié une seule fois par épisode.</summary>
/// <param name="Kind">Limite.</param>
/// <param name="FixtureId">Appareil concerné.</param>
/// <param name="Label">Appareil (ou canal) concerné, lisible.</param>
/// <param name="Detail">Ce que le limiteur a fait (« strobe coupé après 10 s, pause de 10 s »).</param>
/// <param name="At">Instant (horloge du moteur).</param>
public sealed record SafetyLimitReached(SafetyLimitKind Kind, Guid FixtureId, string Label, string Detail, TimeSpan At);
