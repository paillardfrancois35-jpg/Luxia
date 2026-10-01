namespace Luxia.Audio.Analysis;

/// <summary>
/// Première étape de l'analyse (doc 19) : découpe le son mono en trames (fenêtre de 1 024 échantillons, saut d'environ 5,8 ms),
/// normalise le volume (AUD-004) et produit, par trame, le flux spectral large bande, celui des basses et des aigus,
/// les énergies par bande et le niveau. Sans allocation après construction.
/// </summary>
internal sealed class FrameAnalyzer
{
    /// <summary>Taille de la fenêtre d'analyse.</summary>
    public const int WindowSize = 1024;

    /// <summary>Cadence visée des trames (par seconde).</summary>
    public const double TargetFrameRate = 172;

    /// <summary>Niveau efficace sous lequel le son est considéré comme un silence (AUD-005).</summary>
    public const double SilenceLevel = 0.0005;

    private readonly Fft _fft = new(WindowSize);
    private readonly double[] _window = new double[WindowSize];
    private readonly double[] _re = new double[WindowSize];
    private readonly double[] _im = new double[WindowSize];
    private readonly double[] _previous = new double[(WindowSize / 2) + 1];
    private readonly float[] _ring = new float[WindowSize];
    private readonly int _hop;
    private readonly int _fullFrom;
    private readonly int _fullTo;
    private readonly int _bassFrom;
    private readonly int _bassTo;
    private readonly int _midTo;
    private readonly int _trebleTo;
    private int _ringPosition;
    private int _sinceFrame;
    private long _samples;
    private double _slowLevel = 0.05;
    private bool _hasPrevious;

    public FrameAnalyzer(int sampleRate)
    {
        SampleRate = sampleRate;
        _hop = Math.Max(1, (int)Math.Round(sampleRate / TargetFrameRate));
        for (var i = 0; i < WindowSize; i++)
        {
            _window[i] = 0.5 - (0.5 * Math.Cos(2 * Math.PI * i / (WindowSize - 1)));
        }

        var binWidth = (double)sampleRate / WindowSize;
        int Bin(double hz) => Math.Clamp((int)Math.Round(hz / binWidth), 1, WindowSize / 2);
        _fullFrom = Bin(30);
        _fullTo = Bin(10000);
        _bassFrom = Bin(40);
        _bassTo = Bin(150);
        _midTo = Bin(4000);
        _trebleTo = Bin(10000);
    }

    /// <summary>Fréquence d'échantillonnage du son entrant.</summary>
    public int SampleRate { get; }

    /// <summary>Échantillons entre deux trames.</summary>
    public int Hop => _hop;

    /// <summary>Trames produites par seconde.</summary>
    public double FrameRate => (double)SampleRate / _hop;

    /// <summary>Nombre d'échantillons reçus depuis le début.</summary>
    public long Samples => _samples;

    /// <summary>Envoie des échantillons mono ; <paramref name="onFrame"/> est appelée à chaque trame complète.</summary>
    public void Push(ReadOnlySpan<float> mono, Action<FrameFeatures> onFrame)
    {
        foreach (var sample in mono)
        {
            _ring[_ringPosition] = sample;
            _ringPosition = (_ringPosition + 1) % WindowSize;
            _samples++;
            if (++_sinceFrame >= _hop)
            {
                _sinceFrame = 0;
                onFrame(Analyze());
            }
        }
    }

    private FrameFeatures Analyze()
    {
        // Fenêtre la plus récente, dans l'ordre du temps.
        double sum = 0;
        for (var i = 0; i < WindowSize; i++)
        {
            var value = _ring[(_ringPosition + i) % WindowSize];
            sum += value * value;
            _re[i] = value;
        }

        var level = Math.Sqrt(sum / WindowSize);
        var silent = level < SilenceLevel;

        // AUD-004 : le niveau lent sert à ramener tout morceau à un niveau de travail comparable ; il monte vite, descend lentement.
        _slowLevel = level > _slowLevel ? _slowLevel + (0.05 * (level - _slowLevel)) : _slowLevel + (0.0005 * (level - _slowLevel));
        _slowLevel = Math.Max(_slowLevel, SilenceLevel * 4);
        var gain = 0.1 / _slowLevel;

        for (var i = 0; i < WindowSize; i++)
        {
            _re[i] *= _window[i] * gain;
            _im[i] = 0;
        }

        _fft.Transform(_re, _im);

        double flux = 0, bassFlux = 0, trebleFlux = 0, upperFlux = 0, bass = 0, mid = 0, treble = 0;
        for (var k = 1; k <= WindowSize / 2; k++)
        {
            var magnitude = Math.Sqrt((_re[k] * _re[k]) + (_im[k] * _im[k])) * 2 / WindowSize * 4;
            var compressed = Math.Log(1 + (200 * magnitude));
            var rise = _hasPrevious ? Math.Max(0, compressed - _previous[k]) : 0;
            _previous[k] = compressed;
            if (k >= _fullFrom && k <= _fullTo)
            {
                flux += rise;
            }

            if (k >= _bassFrom && k <= _bassTo)
            {
                bassFlux += rise;
                bass += magnitude * magnitude;
            }
            else if (k > _bassTo && k <= _midTo)
            {
                mid += magnitude * magnitude;
                upperFlux += rise;
            }
            else if (k > _midTo && k <= _trebleTo)
            {
                trebleFlux += rise;
                upperFlux += rise;
                treble += magnitude * magnitude;
            }
        }

        _hasPrevious = true;
        return new FrameFeatures(
            flux / Math.Max(1, _fullTo - _fullFrom + 1),
            bassFlux / Math.Max(1, _bassTo - _bassFrom + 1),
            trebleFlux / Math.Max(1, _trebleTo - _midTo),
            level,
            Math.Sqrt(bass),
            Math.Sqrt(mid),
            Math.Sqrt(treble),
            silent,
            gain,
            upperFlux / Math.Max(1, _trebleTo - _bassTo));
    }
}
