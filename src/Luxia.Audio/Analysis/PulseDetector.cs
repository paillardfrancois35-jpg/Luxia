namespace Luxia.Audio.Analysis;

/// <summary>
/// Détection des attaques d'une bande (AUD-040 à 043) à partir de **l'amplitude de la bande** (après normalisation du
/// volume) : une impulsion est un pic de l'amplitude qui (1) s'élève nettement au-dessus du **creux** qui le précède
/// (≈ 0,4 s) — c'est une attaque, pas une note tenue —, (2) n'est pas négligeable devant le pic de la bande sur une
/// trentaine de secondes — la bande est réellement présente, un passage sans basses ne déclenche rien —, et (3) respecte
/// un temps mort. La sensibilité règle les deux seuils (elle agit donc à tous les niveaux). La force (0 à 1) est relative
/// au pic récent (AUD-041). Le pic est reconnu une trame après son sommet : moins de 10 ms de retard (AUD-043).
/// </summary>
internal sealed class PulseDetector
{
    private readonly double _fps;
    private readonly double _valleyRise;
    private readonly double _peakRelease;
    private readonly double _longPeakRelease;
    private double _valley;
    private double _peak;
    private double _longPeak;
    private double _older;
    private double _previous;
    private double _previousValley;
    private long _frame;
    private long _lastPulse = long.MinValue / 2;

    public PulseDetector(double frameRate, double deadSeconds)
    {
        _fps = frameRate;
        DeadSeconds = deadSeconds;
        _valleyRise = 1 - Math.Exp(-1 / (0.4 * frameRate));
        _peakRelease = Math.Exp(-1 / (5.0 * frameRate));
        _longPeakRelease = Math.Exp(-1 / (30.0 * frameRate));
    }

    /// <summary>Temps mort minimal entre deux impulsions, en secondes (AUD-042).</summary>
    public double DeadSeconds { get; set; }

    /// <summary>Sensibilité de 0 (exigeant) à 1 (sensible), AUD-042.</summary>
    public double Sensitivity { get; set; } = 0.6;

    /// <summary>Rapport minimal du pic au creux qui le précède : de 4 (sensibilité 0) à 1,3 (sensibilité 1).</summary>
    public double JumpRatio => 4.0 - (2.7 * Math.Clamp(Sensitivity, 0, 1));

    /// <summary>Part minimale du pic de la bande sur trente secondes : de 55 % (sensibilité 0) à 8 % (sensibilité 1).</summary>
    public double PresenceFraction => 0.55 - (0.47 * Math.Clamp(Sensitivity, 0, 1));

    /// <summary>Traite l'amplitude de la bande pour une trame ; renvoie vrai si une impulsion vient d'être reconnue.</summary>
    public bool Process(double magnitude, out double strength)
    {
        strength = 0;
        _frame++;
        var pulse = false;
        var candidate = _previous;
        if (candidate > _older && candidate >= magnitude && candidate > JumpRatio * Math.Max(_previousValley, 1e-9)
            && candidate > PresenceFraction * _longPeak && candidate > 1e-6 && _frame - _lastPulse >= DeadSeconds * _fps)
        {
            strength = Math.Clamp(candidate / Math.Max(_peak, 1e-9), 0, 1);
            _lastPulse = _frame;
            pulse = true;
        }

        // Le creux descend tout de suite et remonte lentement : il garde la trace du dernier « vide » avant l'attaque.
        _previousValley = _valley;
        _valley = magnitude < _valley ? magnitude : _valley + (_valleyRise * (magnitude - _valley));
        _peak = Math.Max(magnitude, _peak * _peakRelease);
        _longPeak = Math.Max(magnitude, _longPeak * _longPeakRelease);
        _older = _previous;
        _previous = magnitude;
        return pulse;
    }

    /// <summary>Remet les statistiques à zéro (changement de morceau).</summary>
    public void Reset()
    {
        _valley = 0;
        _peak = 0;
        _longPeak = 0;
        _older = 0;
        _previous = 0;
        _previousValley = 0;
    }
}
