namespace Luxia.Engine.Model;

/// <summary>Ce qui fait passer une scène à l'étape suivante (MOT-017, doc 16 §5).</summary>
public enum StepAdvanceMode
{
    /// <summary>La durée de l'étape (fondu + maintien), en secondes ou en temps musicaux.</summary>
    Duration,

    /// <summary>Chaque temps de l'horloge (ou tous les N temps).</summary>
    Beat,

    /// <summary>Chaque début de mesure (ou toutes les N mesures).</summary>
    Bar,

    /// <summary>Chaque impulsion des basses (le kick) ; au temps de l'horloge sans signal audio (SCN-052).</summary>
    BassPulse,

    /// <summary>Chaque impulsion des aigus (caisse claire, charleston) ; au temps de l'horloge sans signal audio (SCN-052).</summary>
    TreblePulse,
}
