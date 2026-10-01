namespace Luxia.Audio.Analysis;

/// <summary>
/// Tempo, phase et premier temps (doc 19 §3) à partir des trames : estimation du tempo par autocorrélation de l'enveloppe des
/// attaques (somme des multiples de la période, plage réglable, préférence d'octave), puis suivi de la phase des temps par une
/// grille de temps lissée (le tempo et la phase sont estimés toutes les ~0,25 s et la grille est recalée en douceur : c'est
/// l'horloge prédictive de l'analyse). Le premier temps de la mesure est déduit de l'énergie des basses sur quatre positions.
/// </summary>
internal sealed class RhythmTracker
{
    private const double WindowSeconds = 8;
    private const double MinimumSeconds = 3;
    private const int VoteCapacity = 16;
    private const double UpdateSeconds = 0.25;
    private const double LocalMeanSeconds = 0.4;

    private readonly double _fps;
    private readonly int _capacity;
    private readonly double[] _flux;
    private readonly double[] _bass;
    private readonly double[] _processed;
    private readonly double[] _processedBass;
    private readonly double[] _scratch;
    private readonly double[] _correlation;
    private readonly int _minLag;
    private readonly int _maxLag;
    private readonly int _updateEvery;
    private readonly double[] _classScore = new double[4];
    private long _frames;
    private long _sinceUpdate;
    private bool _hasGrid;
    private double _origin;
    private double _period;
    private long _gridIndex;
    private long _accumulatedIndex = long.MinValue;
    private int _misses;
    private readonly (double Bpm, double Confidence)[] _votes = new (double, double)[VoteCapacity];
    private int _voteCount;
    private int _voteNext;
    private double _heldBpm;
    private double _bpm;
    private double _confidence;

    public RhythmTracker(double frameRate, double minBpm, double maxBpm)
    {
        _fps = frameRate;
        MinBpm = minBpm;
        MaxBpm = maxBpm;
        _capacity = (int)(frameRate * WindowSeconds * 1.5);
        _flux = new double[_capacity];
        _bass = new double[_capacity];
        var window = (int)(frameRate * WindowSeconds);
        _processed = new double[window];
        _processedBass = new double[window];
        _scratch = new double[window];
        _minLag = (int)Math.Floor(frameRate * 60 / maxBpm);
        _maxLag = (int)Math.Ceiling(frameRate * 60 / minBpm * 4);
        _correlation = new double[_maxLag + 2];
        _updateEvery = Math.Max(1, (int)(frameRate * UpdateSeconds));
    }

    /// <summary>Plage de tempo explorée.</summary>
    public double MinBpm { get; }

    /// <summary>Plage de tempo explorée.</summary>
    public double MaxBpm { get; }

    /// <summary>Centre de la préférence d'octave (AUD-023), en BPM.</summary>
    public double PreferredBpm { get; set; } = 118;

    /// <summary>Largeur de la préférence d'octave, en octaves (plus grand = moins de préférence).</summary>
    public double PreferenceWidth { get; set; } = 0.75;

    /// <summary>Tempo estimé (0 tant qu'il n'y en a pas).</summary>
    public double Bpm => _bpm;

    /// <summary>Confiance (0 à 1) du tempo estimé.</summary>
    public double Confidence => _confidence;

    /// <summary>La grille de temps est établie.</summary>
    public bool HasGrid => _hasGrid;

    /// <summary>Trames reçues.</summary>
    public long Frames => _frames;

    /// <summary>Oublie tout (changement de morceau, silence prolongé, AUD-026).</summary>
    public void Reset()
    {
        Array.Clear(_flux);
        Array.Clear(_bass);
        Array.Clear(_classScore);
        // Le tempo tenu survit à la remise à zéro : une reprise après une pause garde son tempo tant que l'écoute n'est pas sûre d'un autre.
        _voteCount = 0;
        _voteNext = 0;
        _frames = 0;
        _sinceUpdate = 0;
        _hasGrid = false;
        _misses = 0;
        _accumulatedIndex = long.MinValue;
        _confidence = 0;
    }

    /// <summary>Ajoute une trame ; renvoie vrai si le tempo ou la grille viennent d'être mis à jour.</summary>
    public bool Add(double flux, double bassFlux)
    {
        var slot = (int)(_frames % _capacity);
        _flux[slot] = flux;
        _bass[slot] = bassFlux;
        _frames++;
        if (++_sinceUpdate < _updateEvery)
        {
            return false;
        }

        _sinceUpdate = 0;
        return Update();
    }

    /// <summary>
    /// Position dans la grille de temps à l'instant donné (en trames, éventuellement fractionnaire) : phase dans le temps
    /// (0 à 1) et temps dans la mesure (1 à 4, 0 si le premier temps n'est pas connu).
    /// </summary>
    public (double Phase, int BarBeat) Query(double frame)
    {
        if (!_hasGrid || _period <= 0)
        {
            return (0, 0);
        }

        var beats = (frame - _origin) / _period;
        var whole = Math.Floor(beats);
        var phase = beats - whole;
        var downbeat = Downbeat();
        if (downbeat < 0)
        {
            return (phase, 0);
        }

        var index = _gridIndex + (long)whole;
        var inBar = (int)((((index - downbeat) % 4) + 4) % 4) + 1;
        return (phase, inBar);
    }

    private bool Update()
    {
        var available = (int)Math.Min(_frames, _processed.Length);
        if (available < _fps * MinimumSeconds)
        {
            return false;
        }

        Preprocess(_flux, _processed, available);
        Preprocess(_bass, _processedBass, available);
        if (!EstimateTempo(available, out var bpm, out var confidence))
        {
            return false;
        }

        Stabilize(bpm, confidence, out bpm, out confidence);
        _confidence = confidence;
        if (_bpm <= 0 || Math.Abs(bpm - _bpm) / _bpm > 0.06)
        {
            _hasGrid = false;
            _classScore.AsSpan().Clear();
            _accumulatedIndex = long.MinValue;
        }

        // Le tempo est lissé d'une estimation à l'autre (l'horloge ne doit pas trembler).
        _bpm = _bpm <= 0 || !_hasGrid ? bpm : _bpm + (0.3 * (bpm - _bpm));
        TrackBeats(available);
        return true;
    }

    /// <summary>
    /// Stabilise l'estimation : les dernières estimations (4 s) votent par grappes de ± 3 % pondérées par leur confiance ;
    /// la grappe la plus lourde donne le tempo (moyenne pondérée) et sa part du total donne la solidité. Un tempo déjà tenu ne
    /// cède la place à un autre que devant au moins six estimations concordantes et 70 % des voix : une reprise après une
    /// pause, une introduction sans rythme ou un passage ambigu ne font plus « chuter » le BPM.
    /// </summary>
    private void Stabilize(double raw, double confidence, out double bpm, out double stableConfidence)
    {
        _votes[_voteNext] = (raw, confidence);
        _voteNext = (_voteNext + 1) % VoteCapacity;
        _voteCount = Math.Min(VoteCapacity, _voteCount + 1);

        var bestWeight = -1.0;
        var bestSum = 0.0;
        var bestCount = 0;
        double total = 0;
        for (var i = 0; i < _voteCount; i++)
        {
            total += _votes[i].Confidence;
        }

        for (var i = 0; i < _voteCount; i++)
        {
            double weight = 0;
            double sum = 0;
            var count = 0;
            for (var j = 0; j < _voteCount; j++)
            {
                if (Math.Abs((_votes[j].Bpm / _votes[i].Bpm) - 1) <= 0.03)
                {
                    weight += _votes[j].Confidence;
                    sum += _votes[j].Bpm * _votes[j].Confidence;
                    count++;
                }
            }

            if (weight > bestWeight)
            {
                bestWeight = weight;
                bestSum = sum;
                bestCount = count;
            }
        }

        var cluster = bestWeight > 1e-9 ? bestSum / bestWeight : raw;
        var support = total > 1e-9 ? bestWeight / total : 0;
        var clusterConfidence = bestCount == 0 ? 0 : bestWeight / bestCount;
        var solidity = clusterConfidence * (0.5 + (0.5 * support));

        if (_heldBpm > 0 && Math.Abs((cluster / _heldBpm) - 1) > 0.04 && (bestCount < 6 || support < 0.7))
        {
            // Pas assez sûr pour quitter le tempo tenu : on le garde, avec une confiance qui ne dépasse pas celle de l'estimation.
            bpm = _heldBpm;
            stableConfidence = Math.Min(_confidence, Math.Max(solidity, 0.2));
            return;
        }

        bpm = cluster;
        stableConfidence = Math.Clamp(solidity, 0, 1);
        if (bestCount >= 3 && support >= 0.5)
        {
            _heldBpm = cluster;
        }
    }

    /// <summary>Copie les <paramref name="count"/> dernières trames de l'anneau, retire leur moyenne locale et ne garde que les hausses.</summary>
    private void Preprocess(double[] ring, double[] target, int count)
    {
        var start = _frames - count;
        for (var i = 0; i < count; i++)
        {
            _scratch[i] = ring[(int)((start + i) % _capacity)];
        }

        var half = Math.Max(1, (int)(_fps * LocalMeanSeconds / 2));
        double sum = 0;
        var lo = 0;
        var hi = -1;
        double energy = 0;
        for (var i = 0; i < count; i++)
        {
            var wantHi = Math.Min(count - 1, i + half);
            while (hi < wantHi)
            {
                hi++;
                sum += _scratch[hi];
            }

            var wantLo = Math.Max(0, i - half);
            while (lo < wantLo)
            {
                sum -= _scratch[lo];
                lo++;
            }

            var value = Math.Max(0, _scratch[i] - (sum / (hi - lo + 1)));
            target[i] = value;
            energy += value * value;
        }

        var rms = Math.Sqrt(energy / count);
        if (rms > 1e-12)
        {
            for (var i = 0; i < count; i++)
            {
                target[i] /= rms;
            }
        }
    }

    /// <summary>Enveloppe servant à placer les temps : les basses (le kick) d'abord, l'ensemble du spectre en appoint.</summary>
    private double Beat(int index) => _processedBass[index] + (0.5 * _processed[index]);

    private bool EstimateTempo(int count, out double bpm, out double confidence)
    {
        bpm = 0;
        confidence = 0;
        var maxLag = Math.Min(_maxLag, (int)(count * 0.75));
        double zero = 0;
        for (var i = 0; i < count; i++)
        {
            zero += _processed[i] * _processed[i];
        }

        if (zero < 1e-9)
        {
            return false;
        }

        _correlation[0] = 1;
        for (var lag = 1; lag <= maxLag; lag++)
        {
            double sum = 0;
            var n = count - lag;
            for (var i = 0; i < n; i++)
            {
                sum += _processed[i] * _processed[i + lag];
            }

            _correlation[lag] = sum / n / (zero / count);
        }

        double Correlation(double lag)
        {
            if (lag >= maxLag)
            {
                return 0;
            }

            var low = (int)Math.Floor(lag);
            var fraction = lag - low;
            return (_correlation[low] * (1 - fraction)) + (_correlation[low + 1] * fraction);
        }

        ReadOnlySpan<double> weights = [1.0, 0.7, 0.5, 0.4];
        double best = double.NegativeInfinity;
        double bestBpm = 0;
        double total = 0;
        var candidates = 0;
        for (var candidate = MinBpm; candidate <= MaxBpm + 1e-9; candidate += 0.25)
        {
            var period = _fps * 60 / candidate;
            double score = 0;
            double used = 0;
            for (var m = 1; m <= weights.Length; m++)
            {
                var lag = m * period;
                if (lag >= maxLag)
                {
                    break;
                }

                score += weights[m - 1] * Correlation(lag);
                used += weights[m - 1];
            }

            if (used <= 0)
            {
                continue;
            }

            score /= used;
            var octaves = Math.Log2(candidate / PreferredBpm);
            score *= Math.Exp(-0.5 * octaves * octaves / (PreferenceWidth * PreferenceWidth));
            total += score;
            candidates++;
            if (score > best)
            {
                best = score;
                bestBpm = candidate;
            }

        }

        if (candidates == 0 || bestBpm <= 0)
        {
            return false;
        }

        var mean = total / candidates;
        bpm = bestBpm;
        confidence = Math.Clamp((best - mean) / 0.25, 0, 1);
        return true;
    }

    private void TrackBeats(int count)
    {
        var period = _fps * 60 / _bpm;
        _period = period;

        // Dernier temps : le décalage (en trames avant la fin) qui donne la plus forte somme de l'enveloppe sur les temps passés.
        var beats = Math.Clamp((int)((count - 1) / period), 1, 16);
        var bestOffset = 0;
        var bestScore = double.NegativeInfinity;
        var offsets = (int)Math.Ceiling(period);
        for (var offset = 0; offset < offsets; offset++)
        {
            double score = 0;
            for (var k = 0; k < beats; k++)
            {
                var position = count - 1 - offset - (k * period);
                if (position < 1)
                {
                    break;
                }

                var low = (int)position;
                var fraction = position - low;
                var next = Math.Min(count - 1, low + 1);
                score += (Beat(low) * (1 - fraction)) + (Beat(next) * fraction);
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestOffset = offset;
            }
        }

        var measured = _frames - 1 - bestOffset;
        if (!_hasGrid)
        {
            _origin = measured;
            _gridIndex = 0;
            _hasGrid = true;
            _misses = 0;
            return;
        }

        // Ramène l'origine au dernier temps de la grille, puis la recale vers la mesure (horloge prédictive lissée).
        var advance = Math.Floor((measured - _origin) / period + 0.5);
        _origin += advance * period;
        _gridIndex += (long)advance;
        var error = measured - _origin;
        if (Math.Abs(error) < 0.25 * period)
        {
            _origin += 0.35 * error;
            _misses = 0;
        }
        else if (++_misses >= 3)
        {
            _origin = measured;
            _misses = 0;
        }

        AccumulateBars(count);
    }

    /// <summary>Énergie des basses sur les quatre positions de la mesure : la plus forte est le premier temps (AUD-024).</summary>
    private void AccumulateBars(int count)
    {
        if (_accumulatedIndex == long.MinValue)
        {
            _accumulatedIndex = _gridIndex - 1;
        }

        var oldestFrame = _frames - count;
        while (_accumulatedIndex < _gridIndex)
        {
            _accumulatedIndex++;
            var frame = _origin + ((_accumulatedIndex - _gridIndex) * _period);
            if (frame < oldestFrame + 2 || frame > _frames - 3)
            {
                continue;
            }

            var local = (int)Math.Round(frame - oldestFrame);
            double peak = 0;
            for (var d = -2; d <= 2; d++)
            {
                var i = Math.Clamp(local + d, 0, count - 1);
                peak = Math.Max(peak, _processedBass[i] + (0.5 * _processed[i]));
            }

            for (var c = 0; c < 4; c++)
            {
                _classScore[c] *= 0.97;
            }

            _classScore[(int)(((_accumulatedIndex % 4) + 4) % 4)] += peak;
        }
    }

    private int Downbeat()
    {
        var best = 0;
        for (var c = 1; c < 4; c++)
        {
            if (_classScore[c] > _classScore[best])
            {
                best = c;
            }
        }

        var second = double.NegativeInfinity;
        for (var c = 0; c < 4; c++)
        {
            if (c != best)
            {
                second = Math.Max(second, _classScore[c]);
            }
        }

        return _classScore[best] > 0 && _classScore[best] > second * 1.1 ? best : -1;
    }
}
