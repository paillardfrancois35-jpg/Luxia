namespace Luxia.Audio.Analysis;

/// <summary>
/// Signal C (doc 19 §5) : énergie perçue (volume, basses, densité des attaques) normalisée par l'historique récent
/// (AUD-004, AUD-061 : les seuils suivent la soirée, un volume fort ne rend pas tout « explosif »), niveaux discrets avec
/// hystérésis, tendance, et événements Break, Drop et Montée (AUD-062 à 064).
/// </summary>
internal sealed class EnergyTracker
{
    private const int HistorySteps = 24;
    private static readonly double[] Rise = [0.30, 0.60, 0.85];

    private readonly double _dt;
    private readonly double[] _energyHistory = new double[HistorySteps];
    private readonly double[] _densityHistory = new double[HistorySteps];
    private readonly int _stepFrames;
    private double _loud;
    private double _bassSmooth;
    private double _loudHigh;
    private double _loudLow;
    private double _bassHigh;
    private double _bassLow;
    private double _rate;
    private double _fast;
    private double _slow;
    private double _bassNorm;
    private double _minFast = 1;
    private double _breakHold;
    private double _sinceDrop = 99;
    private double _sinceBuild = 99;
    private double _levelHold;
    private EnergyLevel _candidate;
    private int _stepCounter;
    private int _historyIndex;
    private bool _buildActive;
    private double _warmup;
    private double _energyHigh;
    private double _energyLow;
    private long _frame;

    public EnergyTracker(double frameRate)
    {
        _dt = 1 / frameRate;
        _stepFrames = Math.Max(1, (int)(frameRate * 0.25));
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
        _warmup = 0;
        _energyHigh = 0;
        _energyLow = 0;
        Energy = 0;
        _fast = 0;
        _slow = 0;
        _rate = 0;
        InBreak = false;
        _buildActive = false;
        _breakHold = 0;
        Level = EnergyLevel.Calm;
        _candidate = EnergyLevel.Calm;
        Array.Clear(_energyHistory);
        Array.Clear(_densityHistory);
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
        var alpha = 1 - Math.Exp(-_dt / 0.4);
        _loud += alpha * (features.Level - _loud);
        _bassSmooth += alpha * (features.Bass - _bassSmooth);
        var loudLog = Math.Log(_loud + 1e-4);
        var bassLog = Math.Log(_bassSmooth + 1e-4);
        // Les bornes de l'historique ne comptent ni le silence ni la première seconde et demie (le lissage part de zéro).
        if (features.Silent || _warmup < 1.5)
        {
            if (!features.Silent)
            {
                _warmup += _dt;
            }

            if (_warmup < 1.5 && !features.Silent)
            {
                _loudHigh = _loudLow = loudLog;
                _bassHigh = _bassLow = bassLog;
            }
        }
        else
        {
            Track(ref _loudHigh, ref _loudLow, loudLog);
            Track(ref _bassHigh, ref _bassLow, bassLog);
        }
        var loudNorm = Norm(loudLog, _loudLow, _loudHigh);
        _bassNorm = Norm(bassLog, _bassLow, _bassHigh);

        _rate += (((pulses / _dt) - _rate) * _dt) / 2;
        var density = Math.Clamp(_rate / 6, 0, 1);
        var raw = features.Silent ? 0 : (0.55 * loudNorm) + (0.25 * _bassNorm) + (0.20 * density);
        Energy += (raw - Energy) * Math.Min(1, _dt / Math.Max(0.05, SmoothingSeconds));
        _fast += (raw - _fast) * Math.Min(1, _dt / 0.5);
        _slow += (raw - _slow) * Math.Min(1, _dt / 10);

        if (++_stepCounter >= _stepFrames)
        {
            _stepCounter = 0;
            Step(seconds, density, bpm, emit);
        }

        // AUD-061 : les seuils suivent l'historique de la soirée (environ deux minutes) : le niveau est relatif à l'énergie récente.
        if (_warmup >= 1.5 && !features.Silent)
        {
            if (_energyHigh == 0 && _energyLow == 0)
            {
                _energyHigh = _energyLow = Energy;
            }

            _energyHigh += Energy > _energyHigh ? 0.05 * (Energy - _energyHigh) : (Energy - _energyHigh) * _dt / 120;
            _energyLow += Energy < _energyLow ? 0.05 * (Energy - _energyLow) : (Energy - _energyLow) * _dt / 120;
        }

        _sinceDrop += _dt;
        _sinceBuild += _dt;
        UpdateLevel(seconds, emit);
    }

    private static void Track(ref double high, ref double low, double value)
    {
        // Fenêtre d'environ une minute : attaque rapide, relâchement lent.
        high += value > high ? 0.3 * (value - high) : (value - high) / (60 * 172);
        low += value < low ? 0.3 * (value - low) : (value - low) / (60 * 172);
    }

    /// <summary>Position dans la plage récente ; quand elle est étroite (son constant), on reste au milieu plutôt qu'à zéro.</summary>
    private static double Norm(double value, double low, double high) => Math.Clamp(0.5 + ((value - ((low + high) / 2)) / Math.Max(high - low, 0.7)), 0, 1);

    private void Step(double seconds, double density, double bpm, Action<AudioEvent> emit)
    {
        // Tendance (AUD-064) et historique de la montée (AUD-063).
        var oldest = _energyHistory[_historyIndex];
        var oldestDensity = _densityHistory[_historyIndex];
        _energyHistory[_historyIndex] = Energy;
        _densityHistory[_historyIndex] = density;
        _historyIndex = (_historyIndex + 1) % HistorySteps;
        var fourSecondsAgo = _energyHistory[(_historyIndex + HistorySteps - 16) % HistorySteps];
        var change = Energy - fourSecondsAgo;
        Trend = change > 0.06 ? EnergyTrend.Rising : change < -0.06 ? EnergyTrend.Falling : EnergyTrend.Steady;

        // Break : les basses disparaissent et l'énergie chute, pendant au moins deux mesures (AUD-062).
        var minimum = bpm > 0 ? Math.Clamp(4 * 60 / bpm, 1.5, 4) : 2.5;
        var breakCondition = _fast < 0.6 * _slow && _slow > 0.3;
        if (!InBreak)
        {
            _breakHold = breakCondition ? _breakHold + 0.25 : 0;
            if (_breakHold >= minimum)
            {
                InBreak = true;
                _minFast = _fast;
                emit(new AudioEvent(AudioEventKind.Break, seconds, Level, Energy));
            }
        }
        else
        {
            _minFast = Math.Min(_minFast, _fast);
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

        // Drop : retour brutal des basses et de l'énergie après un break ou une montée (AUD-062).
        if ((InBreak || _buildActive) && _sinceDrop > 8 && _bassNorm > 0.4 && _fast - Math.Min(_minFast, _fast) > 0.3 && _fast > 0.5)
        {
            InBreak = false;
            _buildActive = false;
            _sinceDrop = 0;
            _breakHold = 0;
            _minFast = 1;
            emit(new AudioEvent(AudioEventKind.Drop, seconds, Level, Energy));
        }
    }

    private void UpdateLevel(double seconds, Action<AudioEvent> emit)
    {
        var target = EnergyLevel.Calm;
        var relative = Math.Clamp(0.5 + ((Energy - ((_energyLow + _energyHigh) / 2)) / Math.Max(_energyHigh - _energyLow, 0.3)), 0, 1);
        for (var i = 0; i < Rise.Length; i++)
        {
            // Hystérésis : on monte au seuil, on redescend 0,05 plus bas.
            var threshold = (int)Level > i ? Rise[i] - 0.05 : Rise[i];
            if (relative >= threshold)
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

        _levelHold = target == _candidate ? _levelHold + _dt : 0;
        _candidate = target;
        if (_levelHold >= 1.5)
        {
            Level = target;
            _levelHold = 0;
            emit(new AudioEvent(AudioEventKind.EnergyChanged, seconds, Level, Energy));
        }
    }
}
