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
/// <param name="Energy">Énergie perçue (0 à 1, signal C).</param>
/// <param name="EnergyLevel">Niveau d'énergie : 0 Calme, 1 Groove, 2 Énergique, 3 Explosif (AUD-061).</param>
/// <param name="Cues">Événements musicaux (drop, break, montée, silence, reprise) depuis la lecture précédente (D38).</param>
public readonly record struct AudioReading(bool Live, double Bpm, double Confidence, bool HasGrid, double BeatPhase, int BarBeat, int BassPulses, int TreblePulses, double Energy = 0, int EnergyLevel = 0, MusicCues Cues = MusicCues.None);

/// <summary>Source de lectures audio pour le moteur ; appelée par le fil du moteur à chaque tick, sans jamais bloquer.</summary>
public interface IAudioFeed
{
    /// <summary>Lecture courante ; les compteurs d'impulsions et les événements repartent de zéro à chaque appel.</summary>
    AudioReading Read();
}
