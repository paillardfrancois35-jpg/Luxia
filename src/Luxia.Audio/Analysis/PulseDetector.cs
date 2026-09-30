namespace Luxia.Audio.Analysis;

/// <summary>
/// Détection des attaques d'une bande (AUD-040 à 043) : un pic du flux spectral qui dépasse la moyenne récente de plusieurs
/// écarts moyens (seuil réglable) et qui respecte un temps mort minimal. La force (0 à 1) est relative au contexte récent
/// (AUD-041). Le pic est reconnu une trame après son sommet : moins de 10 ms de retard.
/// </summary>
internal sealed class PulseDetector
{
    /// <summary>Flux minimal d'une attaque (en dessous, c'est du bruit de calcul).</summary>
    private const double MinimumFlux = 0.1;

    private readonly double _fps;
    private readonly double _meanRate;
    private readonly double _peakRelease;
    private double _mean;
    private double _deviation = 1e-6;
    private double _peak = 1e-6;
    private double _older;
    private double _previous;
    private long _frame;
    private long _lastPulse = long.MinValue / 2;

    public PulseDetector(double frameRate, double deadSeconds)
    {
        _fps = frameRate;
        DeadSeconds = deadSeconds;
        _meanRate = 1 - Math.Exp(-1 / (1.5 * frameRate));
        _peakRelease = Math.Exp(-1 / (4 * frameRate));
    }

    /// <summary>Temps mort minimal entre deux impulsions, en secondes (AUD-042).</summary>
    public double DeadSeconds { get; set; }

    /// <summary>Sensibilité de 0 (exigeant) à 1 (sensible), AUD-042.</summary>
    public double Sensitivity { get; set; } = 0.5;

    /// <summary>Traite une trame ; renvoie vrai si une impulsion vient d'être reconnue (sa force dans <paramref name="strength"/>).</summary>
    public bool Process(double flux, out double strength)
    {
        strength = 0;
        _frame++;
        // Un plancher absolu évite de « détecter » le bruit numérique d'un son parfaitement constant.
        var threshold = Math.Max(MinimumFlux, _mean + (Threshold() * Math.Max(_deviation, 1e-6)));
        var candidate = _previous;
        var pulse = false;
        if (candidate > _older && candidate >= flux && candidate > threshold && _frame - _lastPulse >= DeadSeconds * _fps)
        {
            var scale = Math.Max(_peak - _mean, 1e-6);
            strength = Math.Clamp((candidate - _mean) / scale, 0, 1);
            _lastPulse = _frame;
            pulse = true;
        }

        _mean += _meanRate * (flux - _mean);
        _deviation += _meanRate * (Math.Abs(flux - _mean) - _deviation);
        _peak = Math.Max(flux, _peak * _peakRelease);
        _older = _previous;
        _previous = flux;
        return pulse;
    }

    /// <summary>Remet les statistiques à zéro (changement de morceau).</summary>
    public void Reset()
    {
        _mean = 0;
        _deviation = 1e-6;
        _peak = 1e-6;
        _older = 0;
        _previous = 0;
    }

    /// <summary>Nombre d'écarts moyens au-dessus de la moyenne : 4 (sensibilité 0) à 1,2 (sensibilité 1).</summary>
    private double Threshold() => 4 - (2.8 * Math.Clamp(Sensitivity, 0, 1));
}
