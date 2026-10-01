namespace Luxia.Audio;

/// <summary>Réglages de l'analyse (AUD-020, AUD-023, AUD-042, AUD-060, AUD-081).</summary>
public sealed record AudioTuning
{
    /// <summary>Tempo minimal exploré (défaut 70).</summary>
    public double MinBpm { get; init; } = 70;

    /// <summary>Tempo maximal exploré (défaut 180).</summary>
    public double MaxBpm { get; init; } = 180;

    /// <summary>Centre de la préférence d'octave, en BPM (défaut 118, plage préférée 90-150).</summary>
    public double PreferredBpm { get; init; } = 118;

    /// <summary>Sensibilité des impulsions de 0 (exigeant) à 1 (sensible).</summary>
    public double PulseSensitivity { get; init; } = 0.6;

    /// <summary>Temps mort des impulsions des basses, en secondes.</summary>
    public double BassDeadSeconds { get; init; } = 0.25;

    /// <summary>Temps mort des impulsions des aigus, en secondes.</summary>
    public double TrebleDeadSeconds { get; init; } = 0.10;

    /// <summary>Lissage de l'énergie, en secondes.</summary>
    public double EnergySmoothingSeconds { get; init; } = 2;
}
