namespace Luxia.Engine.Timing;

/// <summary>
/// Ce que l'écoute de la musique (doc 19) fournit au moteur à chaque tick : tempo et position dans le temps (signal A),
/// impulsions depuis la lecture précédente (signal B). Implémenté hors du moteur (projet d'analyse audio).
/// </summary>
/// <param name="Live">Un son exploitable est entendu (ni silence, ni capture arrêtée).</param>
/// <param name="Bpm">Tempo estimé (0 = inconnu).</param>
/// <param name="Confidence">Confiance du tempo (0 à 1).</param>
/// <param name="HasGrid">Les temps sont verrouillés (la phase est fiable).</param>
/// <param name="BeatPhase">Phase dans le temps en cours à cet instant (0 à 1).</param>
/// <param name="BarBeat">Temps dans la mesure (1 à 4), 0 s'il est inconnu.</param>
/// <param name="BassPulses">Impulsions des basses depuis la lecture précédente.</param>
/// <param name="TreblePulses">Impulsions des aigus depuis la lecture précédente.</param>
public readonly record struct AudioReading(bool Live, double Bpm, double Confidence, bool HasGrid, double BeatPhase, int BarBeat, int BassPulses, int TreblePulses);

/// <summary>Source de lectures audio pour le moteur ; appelée par le fil du moteur à chaque tick, sans jamais bloquer.</summary>
public interface IAudioFeed
{
    /// <summary>Lecture courante ; les compteurs d'impulsions repartent de zéro à chaque appel.</summary>
    AudioReading Read();
}
