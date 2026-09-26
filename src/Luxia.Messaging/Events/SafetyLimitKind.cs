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
