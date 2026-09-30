namespace Luxia.Audio;

/// <summary>
/// État de l'analyse à un instant (doc 19 §1) : les trois signaux réunis. Publié à ~40 Hz, lisible depuis n'importe quel fil.
/// </summary>
/// <param name="Bpm">Tempo estimé (0 tant qu'il n'y en a pas).</param>
/// <param name="Confidence">Confiance du tempo (0 à 1, AUD-022).</param>
/// <param name="BeatPhase">Phase dans le temps en cours (0 à 1) à l'instant de la dernière trame.</param>
/// <param name="BarBeat">Temps dans la mesure (1 à 4), 0 si le premier temps n'est pas connu (AUD-024).</param>
/// <param name="HasGrid">La grille de temps est établie (le tempo est verrouillé).</param>
/// <param name="Silent">Silence détecté (AUD-005).</param>
/// <param name="Level">Niveau du son avant normalisation (0 à 1).</param>
/// <param name="Bass">Énergie des basses, normalisée.</param>
/// <param name="Mid">Énergie des médiums, normalisée.</param>
/// <param name="Treble">Énergie des aigus, normalisée.</param>
/// <param name="FrameIndex">Numéro de la dernière trame analysée.</param>
/// <param name="FrameRate">Trames par seconde.</param>
public sealed record AnalysisState(
    double Bpm,
    double Confidence,
    double BeatPhase,
    int BarBeat,
    bool HasGrid,
    bool Silent,
    double Level,
    double Bass,
    double Mid,
    double Treble,
    long FrameIndex,
    double FrameRate)
{
    /// <summary>Énergie perçue, lissée (0 à 1), AUD-060.</summary>
    public double Energy { get; init; }

    /// <summary>Niveau d'énergie discret, AUD-061.</summary>
    public EnergyLevel EnergyLevel { get; init; }

    /// <summary>Tendance de l'énergie, AUD-064.</summary>
    public EnergyTrend Trend { get; init; }

    /// <summary>Un break est en cours, AUD-062.</summary>
    public bool InBreak { get; init; }

    /// <summary>Force de la dernière impulsion des basses (0 à 1), AUD-041.</summary>
    public double BassPulseStrength { get; init; }

    /// <summary>Force de la dernière impulsion des aigus (0 à 1), AUD-041.</summary>
    public double TreblePulseStrength { get; init; }

    /// <summary>État initial : rien entendu.</summary>
    public static AnalysisState None { get; } = new(0, 0, 0, 0, false, true, 0, 0, 0, 0, 0, 172);
}
