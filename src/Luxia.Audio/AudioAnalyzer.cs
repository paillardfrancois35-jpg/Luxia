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
    private readonly Action<FrameFeatures> _onFrame;
    private readonly int _publishEvery;
    private AnalysisState _state = AnalysisState.None;
    private long _frameIndex;
    private int _silentFrames;
    private int _sincePublish;
    private FrameFeatures _last;

    /// <summary>Crée l'analyseur pour une fréquence d'échantillonnage donnée.</summary>
    /// <param name="sampleRate">Fréquence du son mono entrant (44 100 ou 48 000 Hz).</param>
    /// <param name="minBpm">Tempo minimal exploré (AUD-020, défaut 70).</param>
    /// <param name="maxBpm">Tempo maximal exploré (AUD-020, défaut 180).</param>
    public AudioAnalyzer(int sampleRate, double minBpm = 70, double maxBpm = 180)
    {
        _frames = new FrameAnalyzer(sampleRate);
        _rhythm = new RhythmTracker(_frames.FrameRate, minBpm, maxBpm);
        _publishEvery = Math.Max(1, (int)(_frames.FrameRate / 40));
        _onFrame = OnFrame;
    }

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

    /// <summary>Envoie des échantillons mono (valeurs de −1 à 1).</summary>
    public void Push(ReadOnlySpan<float> mono) => _frames.Push(mono, _onFrame);

    /// <summary>Remet le suivi à zéro (changement de morceau connu, AUD-026).</summary>
    public void Reset()
    {
        _rhythm.Reset();
        Publish();
    }

    private void OnFrame(FrameFeatures features)
    {
        _last = features;
        _frameIndex++;
        if (features.Silent)
        {
            if (++_silentFrames == (int)(SilenceResetSeconds * _frames.FrameRate))
            {
                _rhythm.Reset();
            }
        }
        else
        {
            _silentFrames = 0;
        }

        _rhythm.Add(features.Flux, features.BassFlux);
        if (++_sincePublish >= _publishEvery)
        {
            _sincePublish = 0;
            Publish();
        }
    }

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
            _frames.FrameRate));
    }
}
