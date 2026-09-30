using Luxia.Messaging.Commands;

namespace Luxia.Engine;

/// <summary>État de l'horloge musicale publié avec l'instantané (doc 19 §3) : tempo, source, position, mesure.</summary>
/// <param name="Bpm">Tempo courant.</param>
/// <param name="Source">Source du tempo.</param>
/// <param name="Confidence">Confiance (0 à 1).</param>
/// <param name="BeatInBar">Temps dans la mesure (1 à 4).</param>
/// <param name="Bar">Numéro de mesure.</param>
/// <param name="Phase">Phase dans le temps (0 à 1).</param>
/// <param name="LatencySeconds">Décalage de latence global.</param>
public readonly record struct TempoInfo(double Bpm, TempoSourceKind Source, double Confidence, int BeatInBar, long Bar, double Phase, double LatencySeconds)
{
    /// <summary>Tempo fixe à 120 BPM, avant tout réglage.</summary>
    public static TempoInfo Default =>  new(120, TempoSourceKind.Fixed, 1, 1, 1, 0, 0);
}
