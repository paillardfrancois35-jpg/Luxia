namespace Luxia.Audio.Analysis;

/// <summary>
/// Signal C (doc 19 §5) : énergie perçue, niveaux discrets, tendance, et événements Break, Drop et Montée (AUD-060 à 064).
/// <list type="bullet">
/// <item>L'énergie combine trois mesures lissées (volume sur ~1 s, basses, densité des attaques), chacune remplacée par son
/// **rang** dans l'histoire récente du morceau (histogrammes à décroissance, demi-vie 90 s) : « plus fort que 80 % de ce
/// qu'on vient d'entendre ». Un passage calme est donc bas, un refrain haut, quel que soit le volume (AUD-004) ; un morceau
/// uniformément fort est ramené vers le milieu (plage d'histoire étroite) ; les seuils suivent la soirée (AUD-061).</item>
/// <item>Les niveaux ont une hystérésis et une durée minimale (montée 2 s, descente 3 s) : un passage stable reste stable.</item>
/// <item>Break et Drop se jugent sur le volume (rapide 0,5 s contre référence 6 s **figée pendant le break**) : le creux se
/// voit en moins de deux secondes, le retour se compare au niveau d'avant la pause.</item>
/// </list>
/// </summary>
internal sealed class EnergyTracker
{
    private const int HistorySteps = 24;
    private const double SampleSeconds = 0.25;
    private const double WarmupSeconds = 6;
    private const double LoudFloorDb = -70;
    private const double LoudStepDb = 0.5;
    private const int LoudBins = 141;
    private const double DensityStep = 0.1;
    private const int DensityBins = 101;
    private static readonly double[] Rise = [0.30, 0.52, 0.72];

    private readonly double _dt;
    private readonly double _sampleDecay;
    private readonly int _stepFrames;
    private readonly double[] _loudHistogram = new double[LoudBins];
    private readonly double[] _bassHistogram = new double[LoudBins];
    private readonly double[] _densityHistogram = new double[DensityBins];
    private readonly double[] _energyHistory = new double[HistorySteps];
    private readonly double[] _densityHistory = new double[HistorySteps];
    private double _loud;
    private double _bass;
    private double _rate;
    private double _fast;
    private double _trough;
    private double _slow;
    private double _bassFast;
    private double _bassSlow;
    private double _preSlow;
    private double _preBassSlow;
    private double _breakHold;
    private double _bassOutHold;
    private double _breakSeconds;
    private double _sinceDrop = 99;
    private double _sinceBuild = 99;
    private double _levelHold;
    private double _awake;
    private EnergyLevel _candidate;
    private int _stepCounter;
    private int _historyIndex;
    private bool _buildActive;
    private long _frame;

    public EnergyTracker(double frameRate)
    {
        _dt = 1 / frameRate;
        _stepFrames = Math.Max(1, (int)(frameRate * SampleSeconds));
        _sampleDecay = Math.Pow(0.5, SampleSeconds / 90);
    }

    /// <summary>Constante de temps du lissage de l'énergie, en secondes (AUD-060, défaut 2 s).</summary>
    public double SmoothingSeconds { get; set; } = 2;

    /// <summary>Énergie lissée (0 à 1).</summary>
    public double Energy { get; private set; }

    /// <summary>Niveau discret.</summary>
    public EnergyLevel Level { get; private set; }

    /// <summary>Tendance sur quatre secondes.</summary>
    public EnergyTrend Trend { get; private set; }

    /// <summary>Un break est en cours.</summary>
    public bool InBreak { get; private set; }

    /// <summary>Remet l'historique à zéro (silence prolongé, changement de morceau).</summary>
    public void Reset()
    {
        Array.Clear(_loudHistogram);
        Array.Clear(_bassHistogram);
        Array.Clear(_densityHistogram);
        Array.Clear(_energyHistory);
        Array.Clear(_densityHistory);
        _awake = 0;
        _loud = 0;
        _bass = 0;
        _rate = 0;
        _fast = 0;
        _slow = 0;
        _bassFast = 0;
        _bassSlow = 0;
        Energy = 0;
        InBreak = false;
        _buildActive = false;
        _breakHold = 0;
        _bassOutHold = 0;
        Level = EnergyLevel.Calm;
        _candidate = EnergyLevel.Calm;
    }

    /// <summary>Traite une trame.</summary>
    /// <param name="features">Mesures de la trame.</param>
    /// <param name="pulses">Nombre d'impulsions reconnues pendant la trame (basses + aigus).</param>
    /// <param name="bpm">Tempo connu (0 sinon), pour la durée minimale d'un break.</param>
    /// <param name="emit">Reçoit les événements.</param>
    public void Process(in FrameFeatures features, int pulses, double bpm, Action<AudioEvent> emit)
    {
        _frame++;
        var seconds = _frame * _dt;
        var silent = features.Silent;
        var power = features.Level * features.Level;
        var gain = Math.Max(features.Gain, 1e-6);
        var bassPower = features.Bass * features.Bass / (gain * gain);

        Ema(ref _loud, power, 1.2);
        Ema(ref _bass, bassPower, 1.2);
        Ema(ref _rate, pulses / _dt, 3);
        if (!silent)
        {
            _awake += _dt;
        }

        // Break et Drop : volume rapide contre référence lente, figée pendant le break pour juger le retour contre l'avant.
        Ema(ref _fast, power, 0.5);
        Ema(ref _bassFast, bassPower, 0.5);
        if (!InBreak && _awake > 1)
        {
            Ema(ref _slow, power, 6);
            Ema(ref _bassSlow, bassPower, 6);
        }

        if (++_stepCounter >= _stepFrames)
        {
            _stepCounter = 0;
            Step(seconds, silent, bpm, emit);
        }

        _sinceDrop += _dt;
        _sinceBuild += _dt;
        UpdateLevel(seconds, emit);
    }

    private static int LoudBin(double power) => Math.Clamp((int)(((10 * Math.Log10(power + 1e-12)) - LoudFloorDb) / LoudStepDb), 0, LoudBins - 1);

    private static double Rank(double[] histogram, int bin)
    {
        double below = 0;
        double total = 0;
        for (var i = 0; i < histogram.Length; i++)
        {
            total += histogram[i];
            if (i < bin)
            {
                below += histogram[i];
            }
            else if (i == bin)
            {
                below += histogram[i] / 2;
            }
        }

        return total <= 1e-9 ? 0.5 : below / total;
    }

    private static double Percentile(double[] histogram, double fraction)
    {
        double total = 0;
        foreach (var weight in histogram)
        {
            total += weight;
        }

        if (total <= 1e-9)
        {
            return 0;
        }

        var target = total * fraction;
        double running = 0;
        for (var i = 0; i < histogram.Length; i++)
        {
            running += histogram[i];
            if (running >= target)
            {
                return i;
            }
        }

        return histogram.Length - 1;
    }

    private void Ema(ref double value, double sample, double seconds) => value += (sample - value) * Math.Min(1, _dt / seconds);

    private void Step(double seconds, bool silent, double bpm, Action<AudioEvent> emit)
    {
        var density = Math.Clamp(_rate, 0, 10);
        var warm = _awake >= WarmupSeconds;
        double raw;
        if (silent)
        {
            raw = 0;
        }
        else
        {
            // Histoire récente du morceau : décroissance de 1/2 en 90 s, mise à jour seulement quand on entend quelque chose.
            for (var i = 0; i < LoudBins; i++)
            {
                _loudHistogram[i] *= _sampleDecay;
                _bassHistogram[i] *= _sampleDecay;
            }

            for (var i = 0; i < DensityBins; i++)
            {
                _densityHistogram[i] *= _sampleDecay;
            }

            var loudBin = LoudBin(_loud);
            var bassBin = LoudBin(_bass);
            var densityBin = Math.Clamp((int)(density / DensityStep), 0, DensityBins - 1);
            _loudHistogram[loudBin] += 1;
            _bassHistogram[bassBin] += 1;
            _densityHistogram[densityBin] += 1;

            if (warm)
            {
                raw = (0.55 * Rank(_loudHistogram, loudBin)) + (0.25 * Rank(_bassHistogram, bassBin)) + (0.20 * Rank(_densityHistogram, densityBin));

                // Un morceau dont le volume varie peu (plage de 10 % à 90 % étroite) est ramené vers le milieu : pas de faux contrastes.
                var spread = (Percentile(_loudHistogram, 0.9) - Percentile(_loudHistogram, 0.1)) * LoudStepDb;
                raw = 0.5 + ((raw - 0.5) * Math.Clamp(spread / 8, 0.3, 1));
            }
            else
            {
                raw = 0.5;
            }
        }

        Energy += (raw - Energy) * Math.Min(1, SampleSeconds / Math.Max(0.05, SmoothingSeconds));

        // Tendance (AUD-064) et historique de la montée (AUD-063).
        var oldest = _energyHistory[_historyIndex];
        var oldestDensity = _densityHistory[_historyIndex];
        _energyHistory[_historyIndex] = Energy;
        _densityHistory[_historyIndex] = density;
        _historyIndex = (_historyIndex + 1) % HistorySteps;
        var fourSecondsAgo = _energyHistory[(_historyIndex + HistorySteps - 16) % HistorySteps];
        var change = Energy - fourSecondsAgo;
        Trend = change > 0.12 ? EnergyTrend.Rising : change < -0.12 ? EnergyTrend.Falling : EnergyTrend.Steady;

        // Break : le volume tombe sous 30 % de la référence (≈ −5 dB) pendant une demi-mesure à une mesure (AUD-062).
        var minimum = bpm > 0 ? Math.Clamp(2 * 60 / bpm, 0.8, 2.0) : 1.2;
        if (!InBreak)
        {
            // Deux façons d'entrer en break : le volume s'effondre (< 30 % de la référence), ou — pause moins profonde mais plus
            // longue, comme le retrait des basses de *Summer* — les basses disparaissent (< 8 %) et le volume baisse (< 60 %).
            var deep = warm && !silent && _slow > 1e-6 && _fast < 0.30 * _slow;
            var bassOut = warm && !silent && _slow > 1e-6 && _fast < 0.60 * _slow && _bassFast < 0.08 * _bassSlow;
            _breakHold = deep ? _breakHold + SampleSeconds : 0;
            _bassOutHold = bassOut ? _bassOutHold + SampleSeconds : 0;
            if (_breakHold >= minimum || _bassOutHold >= 2.5 * minimum)
            {
                InBreak = true;
                _breakSeconds = 0;
                _preSlow = _slow;
                _trough = _fast;
                _preBassSlow = _bassSlow;
                emit(new AudioEvent(AudioEventKind.Break, seconds, Level, Energy));
            }
        }
        else
        {
            _breakSeconds += SampleSeconds;
            _trough = Math.Min(_trough, _fast);

            // Drop : le volume remonte d'au moins 8 dB au-dessus du creux de la pause et retrouve le quart du niveau d'avant, avec
            // le retour des basses (AUD-062). Mesuré sur *Animals* : le retour d'un drop n'atteint souvent que 30 % de la puissance
            // d'avant la pause et 10 % de ses basses moyennes. Trois quarts de seconde de pause au moins : pas de faux drop.
            var loudReturn = _fast > 0.25 * _preSlow && _fast > 6 * _trough && _bassFast > 0.05 * _preBassSlow;
            var bassReturn = _bassFast > 0.5 * _preBassSlow && _fast > 0.5 * _preSlow;
            if (_sinceDrop > 6 && _breakSeconds >= 0.75 && (loudReturn || bassReturn))
            {
                InBreak = false;
                _buildActive = false;
                _sinceDrop = 0;
                _breakHold = 0;
                _bassOutHold = 0;
                emit(new AudioEvent(AudioEventKind.Drop, seconds, Level, Energy));
            }
            else if (_breakSeconds > 45 || silent)
            {
                // Pas de retour : ce n'était pas une pause avant un drop (passage calme du morceau).
                InBreak = false;
                _breakHold = 0;
            }
        }

        // Montée : énergie et densité rythmique croissantes sur six secondes (AUD-063).
        if (!_buildActive && seconds > 20 && _sinceBuild > 20 && oldest > 0 && Energy - oldest > 0.25 && density > oldestDensity * 1.2 && Trend == EnergyTrend.Rising)
        {
            _buildActive = true;
            _sinceBuild = 0;
            emit(new AudioEvent(AudioEventKind.BuildUp, seconds, Level, Energy));
        }
        else if (_buildActive && Trend == EnergyTrend.Falling && !InBreak)
        {
            _buildActive = false;
        }
    }

    private void UpdateLevel(double seconds, Action<AudioEvent> emit)
    {
        var target = EnergyLevel.Calm;
        for (var i = 0; i < Rise.Length; i++)
        {
            // Hystérésis : on monte au seuil, on redescend 0,04 plus bas.
            var threshold = (int)Level > i ? Rise[i] - 0.04 : Rise[i];
            if (Energy >= threshold)
            {
                target = (EnergyLevel)(i + 1);
            }
        }

        if (target == Level)
        {
            _levelHold = 0;
            _candidate = Level;
            return;
        }

        // Durée minimale avant de changer : 2 s pour monter, 3 s pour descendre (un passage stable reste stable).
        _levelHold = target == _candidate ? _levelHold + _dt : 0;
        _candidate = target;
        var required = target > Level ? 2.0 : 3.0;
        if (_levelHold >= required)
        {
            Level = target;
            _levelHold = 0;
            emit(new AudioEvent(AudioEventKind.EnergyChanged, seconds, Level, Energy));
        }
    }
}
