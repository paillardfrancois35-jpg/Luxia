using Luxia.Audio.Analysis;

namespace Luxia.Audio;

/// <summary>
/// Chaîne d'analyse unique du son (doc 19 §1) : du son mono aux trois signaux (tempo et temps, impulsions, énergie).
/// Ne dépend ni de la carte son ni d'un fil : elle reçoit des échantillons et se teste sur des signaux ou des fichiers
/// (T-AUD-01, T-AUD-02). Un seul fil doit appeler <see cref="Push"/>.
/// </summary>
public sealed class AudioAnalyzer
{
    /// <summary>Durée de silence continu qui remet le suivi à zéro (AUD-005, AUD-026), en secondes.</summary>
    public const double SilenceResetSeconds = 1.5;

    private readonly FrameAnalyzer _frames;
    private readonly RhythmTracker _rhythm;
    private readonly PulseDetector _bassPulses;
    private readonly PulseDetector _treblePulses;
    private readonly EnergyTracker _energy;
    private readonly Action<FrameFeatures> _onFrame;
    private readonly int _publishEvery;
    private AnalysisState _state = AnalysisState.None;
    private long _frameIndex;
    private int _silentFrames;
    private bool _silenceAnnounced;
    private int _sincePublish;
    private int _bassCount;
    private int _trebleCount;
    private long _bassTotal;
    private long _trebleTotal;
    private double _bassStrength;
    private double _trebleStrength;
    private FrameFeatures _last;

    /// <summary>Crée l'analyseur pour une fréquence d'échantillonnage donnée.</summary>
    /// <param name="sampleRate">Fréquence du son mono entrant (44 100 ou 48 000 Hz).</param>
    /// <param name="minBpm">Tempo minimal exploré (AUD-020, défaut 70).</param>
    /// <param name="maxBpm">Tempo maximal exploré (AUD-020, défaut 180).</param>
    public AudioAnalyzer(int sampleRate, double minBpm = 70, double maxBpm = 180)
    {
        _frames = new FrameAnalyzer(sampleRate);
        _rhythm = new RhythmTracker(_frames.FrameRate, minBpm, maxBpm);
        _bassPulses = new PulseDetector(_frames.FrameRate, 0.25);
        _treblePulses = new PulseDetector(_frames.FrameRate, 0.10);
        _energy = new EnergyTracker(_frames.FrameRate);
        _publishEvery = Math.Max(1, (int)(_frames.FrameRate / 40));
        _onFrame = OnFrame;
    }

    /// <summary>Levé (sur le fil qui appelle <see cref="Push"/>) à chaque événement musical : silence, break, drop, montée, niveau.</summary>
    public event Action<AudioEvent>? EventRaised;

    /// <summary>Dernier état publié.</summary>
    public AnalysisState State => Volatile.Read(ref _state);

    /// <summary>Fréquence d'échantillonnage du son entrant.</summary>
    public int SampleRate => _frames.SampleRate;

    /// <summary>Trames par seconde.</summary>
    public double FrameRate => _frames.FrameRate;

    /// <summary>Nombre de trames analysées.</summary>
    public long FrameCount => _frameIndex;

    /// <summary>Centre de la préférence d'octave (AUD-023), en BPM.</summary>
    public double PreferredBpm
    {
        get => _rhythm.PreferredBpm;
        set => _rhythm.PreferredBpm = value;
    }

    /// <summary>Sensibilité des impulsions de 0 à 1 (AUD-042).</summary>
    public double PulseSensitivity
    {
        get => _bassPulses.Sensitivity;
        set
        {
            _bassPulses.Sensitivity = value;
            _treblePulses.Sensitivity = value;
        }
    }

    /// <summary>Temps mort des impulsions des basses, en secondes (AUD-042).</summary>
    public double BassDeadSeconds
    {
        get => _bassPulses.DeadSeconds;
        set => _bassPulses.DeadSeconds = Math.Clamp(value, 0.03, 1);
    }

    /// <summary>Temps mort des impulsions des aigus, en secondes (AUD-042).</summary>
    public double TrebleDeadSeconds
    {
        get => _treblePulses.DeadSeconds;
        set => _treblePulses.DeadSeconds = Math.Clamp(value, 0.03, 1);
    }

    /// <summary>Lissage de l'énergie, en secondes (AUD-060).</summary>
    public double EnergySmoothingSeconds
    {
        get => _energy.SmoothingSeconds;
        set => _energy.SmoothingSeconds = Math.Clamp(value, 0.2, 10);
    }

    /// <summary>Envoie des échantillons mono (valeurs de −1 à 1).</summary>
    public void Push(ReadOnlySpan<float> mono) => _frames.Push(mono, _onFrame);

    /// <summary>Impulsions reconnues depuis le dernier appel (les compteurs repartent de zéro).</summary>
    public (int Bass, int Treble) TakePulses() => (Interlocked.Exchange(ref _bassCount, 0), Interlocked.Exchange(ref _trebleCount, 0));

    /// <summary>Remet le suivi à zéro (changement de morceau connu, AUD-026).</summary>
    public void Reset()
    {
        _rhythm.Reset();
        _energy.Reset();
        _bassPulses.Reset();
        _treblePulses.Reset();
        Publish();
    }

    private readonly double[] _upperRecent = new double[5];
    private int _upperIndex;
    private double _upperAverage;

    private void OnFrame(FrameFeatures features)
    {
        _last = features;
        _frameIndex++;
        if (features.Silent)
        {
            var limit = (int)(SilenceResetSeconds * _frames.FrameRate);
            if (++_silentFrames == limit)
            {
                _rhythm.Reset();
                _energy.Reset();
                _bassPulses.Reset();
                _treblePulses.Reset();
            }

            if (_silentFrames >= _frames.FrameRate * 0.5 && !_silenceAnnounced)
            {
                _silenceAnnounced = true;
                Raise(AudioEventKind.Silence);
            }
        }
        else
        {
            _silentFrames = 0;
            if (_silenceAnnounced)
            {
                _silenceAnnounced = false;
                Raise(AudioEventKind.Resumed);
            }
        }

        // Un kick est un coup de batterie : l'attaque des basses s'accompagne d'un claquement dans les médiums et les aigus. Une note de
        // basse (guitare, synthé) n'en a pas : elle ne compte pas (essai P7, exemple 19). La sensibilité règle l'exigence.
        _upperRecent[_upperIndex] = features.UpperFlux;
        _upperIndex = (_upperIndex + 1) % _upperRecent.Length;
        var upperPeak = _upperRecent.Max();
        var factor = 2.0 - (1.0 * Math.Clamp(PulseSensitivity, 0, 1));
        var punch = upperPeak > factor * _upperAverage && upperPeak > 0.05;
        _upperAverage += 0.006 * (features.UpperFlux - _upperAverage);

        var pulses = 0;
        if (_bassPulses.Process(features.Bass, out var bassStrength, punch))
        {
            Interlocked.Increment(ref _bassCount);
            _bassTotal++;
            _bassStrength = bassStrength;
            pulses++;
        }

        if (_treblePulses.Process(features.Treble, out var trebleStrength))
        {
            Interlocked.Increment(ref _trebleCount);
            _trebleTotal++;
            _trebleStrength = trebleStrength;
            pulses++;
        }

        _rhythm.Add(features.Flux, features.BassFlux);
        _energy.Process(features, pulses, _rhythm.Bpm, e => EventRaised?.Invoke(e));
        if (++_sincePublish >= _publishEvery)
        {
            _sincePublish = 0;
            Publish();
        }
    }

    private void Raise(AudioEventKind kind) =>
        EventRaised?.Invoke(new AudioEvent(kind, _frameIndex / _frames.FrameRate, _energy.Level, _energy.Energy));

    private void Publish()
    {
        var (phase, barBeat) = _rhythm.Query(_rhythm.Frames - 1);
        Volatile.Write(ref _state, new AnalysisState(
            _rhythm.Bpm,
            _rhythm.Confidence,
            phase,
            barBeat,
            _rhythm.HasGrid,
            _silentFrames >= _frames.FrameRate * 0.5,
            _last.Level,
            _last.Bass,
            _last.Mid,
            _last.Treble,
            _frameIndex,
            _frames.FrameRate)
        {
            Energy = _energy.Energy,
            EnergyLevel = _energy.Level,
            Trend = _energy.Trend,
            InBreak = _energy.InBreak,
            BassPulseCount = _bassTotal,
            TreblePulseCount = _trebleTotal,
            BassPulseStrength = _bassStrength,
            TreblePulseStrength = _trebleStrength,
        });
    }
}
