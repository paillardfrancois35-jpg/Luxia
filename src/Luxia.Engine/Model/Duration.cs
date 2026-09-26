namespace Luxia.Engine.Model;

/// <summary>
/// Durée d'une étape, d'un fondu ou d'un retard : en secondes ou en temps musicaux (GEN-023).
/// Une durée musicale est convertie avec le tempo courant du moteur (<see cref="RenderEngine.Bpm"/>).
/// </summary>
/// <param name="Value">Quantité (≥ 0).</param>
/// <param name="Unit">Unité.</param>
public readonly record struct Duration(double Value, DurationUnit Unit = DurationUnit.Seconds)
{
    /// <summary>Temps par mesure (GEN-024).</summary>
    public const int BeatsPerBar = 4;

    /// <summary>Durée nulle.</summary>
    public static Duration Zero => default;

    /// <summary>Durée en secondes.</summary>
    public static Duration FromSeconds(double seconds) => new(seconds);

    /// <summary>Durée en temps musicaux.</summary>
    public static Duration FromBeats(double beats) => new(beats, DurationUnit.Beats);

    /// <summary>Convertit en secondes au tempo donné (2 temps à 120 BPM = 1 s ; à 90 BPM = 1,333 s).</summary>
    public double ToSeconds(double bpm)
    {
        var value = Math.Max(0, Value);
        return Unit switch
        {
            DurationUnit.Beats => value * 60.0 / bpm,
            DurationUnit.Bars => value * BeatsPerBar * 60.0 / bpm,
            _ => value,
        };
    }
}
