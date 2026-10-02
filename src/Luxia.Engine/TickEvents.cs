namespace Luxia.Engine;

/// <summary>
/// Impulsions audio du tick courant (doc 19 §4) et disponibilité du signal : les scènes à avance « impulsion » les
/// comptent ; sans signal audio elles avancent au temps de l'horloge (SCN-052). Renseigné par le moteur.
/// </summary>
internal sealed class TickEvents
{
    /// <summary>Impulsions des basses depuis le tick précédent.</summary>
    public int BassPulses { get; set; }

    /// <summary>Impulsions des aigus depuis le tick précédent.</summary>
    public int TreblePulses { get; set; }

    /// <summary>Énergie perçue (0 à 1), SCN-051.</summary>
    public double Energy { get; set; }

    /// <summary>Un signal audio exploitable est présent (pas de silence, capture active).</summary>
    public bool AudioLive { get; set; }

    /// <summary>Niveau d'énergie (0 Calme à 3 Explosif).</summary>
    public int EnergyLevel { get; set; }

    /// <summary>Événements musicaux du tick (D38).</summary>
    public Timing.MusicCues Cues { get; set; }
}
