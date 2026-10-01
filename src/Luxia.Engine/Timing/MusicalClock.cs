using Luxia.Engine.Model;
using Luxia.Messaging.Commands;

namespace Luxia.Engine.Timing;

/// <summary>
/// Horloge musicale du moteur (doc 19 §3, GEN-023, GEN-034) : tempo, position en temps, compteur de mesures.
/// Avancée par le moteur du temps réellement écoulé ; les réglages (Fixe, Tap, ×2, ÷2, recalage) arrivent par
/// commandes. Le tempo et la source se lisent depuis n'importe quel fil ; le reste appartient au fil du moteur.
/// </summary>
public sealed class MusicalClock
{
    /// <summary>Tempo minimal accepté.</summary>
    public const double MinBpm = 20;

    /// <summary>Tempo maximal accepté.</summary>
    public const double MaxBpm = 400;

    /// <summary>Intervalle sans frappe après lequel le tap repart de zéro (AUD-025).</summary>
    public const double TapResetSeconds = 2;

    /// <summary>Nombre de frappes à partir duquel le tempo est calculé (AUD-025).</summary>
    public const int TapMinimum = 4;

    /// <summary>Nombre maximal de frappes retenues (AUD-025).</summary>
    public const int TapMaximum = 8;

    private readonly List<double> _taps = [];
    private double _bpm = 120;
    private int _source = (int)TempoSourceKind.Fixed;
    private double _confidence = 1;
    private double _position;
    private double _latency;
    private double _shift;
    private bool _audioLocked;
    private int _barMismatch;
    private bool _barManual;
    private double _audioScale = 1;
    private double _lastRawBpm;
    private int _silentTicks;

    /// <summary>Confiance minimale de l'analyse pour que l'horloge la suive ; en dessous, elle garde son tempo (AUD-022, GEN-034).</summary>
    public const double MinAudioConfidence = 0.3;

    /// <summary>Tempo courant en temps par minute.</summary>
    public double Bpm => Volatile.Read(ref _bpm);

    /// <summary>Source du tempo.</summary>
    public TempoSourceKind Source => (TempoSourceKind)Volatile.Read(ref _source);

    /// <summary>Indice de confiance du tempo (0 à 1) : 1 pour Fixe et Tap, celui de l'analyse pour Audio (AUD-022).</summary>
    public double Confidence => Volatile.Read(ref _confidence);

    /// <summary>Décalage de latence global en secondes (±0,5 s, GEN-035, AUD-027).</summary>
    public double LatencySeconds => Volatile.Read(ref _latency);

    /// <summary>Position depuis l'origine, en temps (fractions comprises), sans décalage de latence.</summary>
    public double BeatPosition => _position;

    /// <summary>Position vue par les scènes : avancée du décalage de latence (GEN-035).</summary>
    public double EffectivePosition => _position + (LatencySeconds * Bpm / 60.0);

    /// <summary>Numéro du temps en cours depuis l'origine (0, 1, 2…).</summary>
    public long BeatIndex => (long)Math.Floor(EffectivePosition + 1e-9);

    /// <summary>Phase dans le temps en cours (0 inclus à 1 exclu).</summary>
    public double Phase
    {
        get
        {
            var position = EffectivePosition + 1e-9;
            return Math.Clamp(position - Math.Floor(position), 0, 1);
        }
    }

    /// <summary>Temps dans la mesure, de 1 à 4 (AUD-029).</summary>
    public int BeatInBar => (int)(((BeatIndex % Duration.BeatsPerBar) + Duration.BeatsPerBar) % Duration.BeatsPerBar) + 1;

    /// <summary>Numéro de mesure depuis l'origine, à partir de 1.</summary>
    public long Bar => (long)Math.Floor((double)BeatIndex / Duration.BeatsPerBar) + 1;

    /// <summary>Temps franchis pendant le dernier <see cref="Advance"/> (0, 1 ou plus si le tick a duré).</summary>
    public int BeatsCrossed { get; private set; }

    /// <summary>Débuts de mesure franchis pendant le dernier <see cref="Advance"/>.</summary>
    public int BarsCrossed { get; private set; }

    /// <summary>
    /// Somme des sauts de phase (en temps) dus aux recalages : frappe de tap, « 1 ici », latence. Les effets calés sur
    /// l'horloge (MOT-062) suivent ces sauts pour rester sur le temps.
    /// </summary>
    public double ShiftTotal => _shift;

    /// <summary>Fait avancer l'horloge de <paramref name="elapsed"/> secondes réelles.</summary>
    public void Advance(double elapsed)
    {
        BeatsCrossed = 0;
        BarsCrossed = 0;
        if (elapsed <= 0)
        {
            return;
        }

        var before = BeatIndex;
        _position += elapsed * Bpm / 60.0;
        var after = BeatIndex;
        if (after > before)
        {
            BeatsCrossed = (int)Math.Min(after - before, int.MaxValue);
            BarsCrossed = (int)Math.Max(0, (after / Duration.BeatsPerBar) - (before / Duration.BeatsPerBar));
        }
    }

    /// <summary>Tempo fixe (CMD-041) : la source devient Fixe, la phase continue.</summary>
    public void SetFixed(double bpm)
    {
        Volatile.Write(ref _bpm, Math.Clamp(bpm, MinBpm, MaxBpm));
        Volatile.Write(ref _source, (int)TempoSourceKind.Fixed);
        Volatile.Write(ref _confidence, 1);
        _taps.Clear();
    }

    /// <summary>Choisit la source sans toucher au tempo courant (CMD-041).</summary>
    public void UseSource(TempoSourceKind source)
    {
        Volatile.Write(ref _source, (int)source);
        _taps.Clear();
        _audioLocked = false;
        _barMismatch = 0;
        _barManual = false;
        _audioScale = 1;
        _lastRawBpm = 0;
    }

    /// <summary>
    /// Suit l'écoute de la musique (source Audio, AUD-021, AUD-024, GEN-034) : tempo lissé (recalé d'un coup si le morceau
    /// change), phase ramenée en douceur sur les temps entendus, premier temps corrigé s'il est confirmé plus d'une seconde.
    /// Sans son fiable, l'horloge continue au dernier tempo connu.
    /// </summary>
    public void FollowAudio(in AudioReading reading)
    {
        Volatile.Write(ref _confidence, reading.Live ? reading.Confidence : 0);
        if (!reading.Live)
        {
            // Plus de son depuis une seconde et demie : le prochain morceau repart sans correction d'octave.
            if (++_silentTicks >= 60)
            {
                _audioScale = 1;
                _lastRawBpm = 0;
            }

            return;
        }

        _silentTicks = 0;
        if (reading.Bpm <= 0 || reading.Confidence < MinAudioConfidence || !reading.HasGrid)
        {
            return;
        }

        // Correction d'octave de l'utilisateur (×2, ÷2) : elle s'applique au tempo entendu et tient jusqu'au prochain morceau.
        // Si l'analyse change elle-même d'octave (rapport 2, 1/2, 3/2 ou 2/3), la correction s'ajuste pour garder le même tempo.
        if (_lastRawBpm > 0)
        {
            var ratio = reading.Bpm / _lastRawBpm;
            if (Math.Abs(ratio - 1) > 0.15)
            {
                _audioScale = IsOctaveRatio(ratio) ? _audioScale / ratio : 1;
            }
        }

        _lastRawBpm = reading.Bpm;
        var heard = reading.Bpm * _audioScale;
        var jump = Math.Abs(heard - Bpm) / Bpm > 0.06;
        if (jump)
        {
            // Nouveau morceau : le « 1 » posé à la main pour le précédent ne vaut plus.
            _barManual = false;
        }

        Volatile.Write(ref _bpm, Math.Clamp(jump || !_audioLocked ? heard : Bpm + (0.15 * (heard - Bpm)), MinBpm, MaxBpm));

        // Phase attendue : celle des temps du tempo corrigé. Avec ×2, deux temps de l'horloge par temps entendu ; avec ÷ 2, un temps sur
        // deux (on garde le candidat le plus proche de la phase courante, pour ne pas sauter d'un temps à l'autre). Sans cela, la phase du
        // tempo d'origine ramenait les voyants au rythme de la musique (essai P7, exemple 28 b).
        var scaled = Math.Abs(_audioScale - 1) > 0.01;
        var fraction = _position - Math.Floor(_position);
        var error = 0.0;
        if (!scaled)
        {
            error = reading.BeatPhase - fraction;
        }
        else if (_audioScale > 1)
        {
            var target = reading.BeatPhase * _audioScale;
            error = (target - Math.Floor(target)) - fraction;
        }
        else
        {
            var candidates = Math.Max(1, (int)Math.Round(1 / _audioScale));
            var best = double.MaxValue;
            for (var k = 0; k < candidates; k++)
            {
                var target = (reading.BeatPhase + k) * _audioScale;
                var candidate = (target - Math.Floor(target)) - fraction;
                candidate -= Math.Round(candidate);
                if (Math.Abs(candidate) < Math.Abs(best))
                {
                    best = candidate;
                }
            }

            error = best;
        }

        error -= Math.Round(error);
        var delta = !_audioLocked || jump || Math.Abs(error) > 0.35 ? error : 0.12 * error;
        _position += delta;
        _shift += delta;
        _audioLocked = true;

        if (!_barManual && !scaled && reading.BarBeat is >= 1 and <= Duration.BeatsPerBar)
        {
            var gap = (reading.BarBeat - BeatInBar + Duration.BeatsPerBar) % Duration.BeatsPerBar;
            if (gap == 0)
            {
                _barMismatch = 0;
            }
            else if (++_barMismatch >= 40)
            {
                _position += gap;
                _shift += gap;
                _barMismatch = 0;
            }
        }
    }

    /// <summary>
    /// Une frappe de tap (CMD-040, AUD-025) : la phase se cale sur la frappe ; à partir de quatre frappes, le tempo est la
    /// moyenne des intervalles des huit dernières, et la source devient Tap. Plus de deux secondes sans frappe : on repart de zéro.
    /// </summary>
    /// <param name="at">Instant de la frappe, en secondes sur l'horloge du moteur.</param>
    /// <param name="secondsAgo">Âge de la frappe à l'instant du tick (la commande attend le tick suivant).</param>
    public void Tap(double at, double secondsAgo)
    {
        if (_taps.Count > 0 && at - _taps[^1] > TapResetSeconds)
        {
            _taps.Clear();
        }

        _taps.Add(at);
        if (_taps.Count > TapMaximum)
        {
            _taps.RemoveAt(0);
        }

        if (_taps.Count >= TapMinimum)
        {
            var mean = (_taps[^1] - _taps[0]) / (_taps.Count - 1);
            if (mean > 0)
            {
                Volatile.Write(ref _bpm, Math.Clamp(60.0 / mean, MinBpm, MaxBpm));
                Volatile.Write(ref _source, (int)TempoSourceKind.Tap);
                Volatile.Write(ref _confidence, 1);
            }
        }

        // La frappe est un temps : on recale sur le temps entier le plus proche, tel qu'il était à l'instant de la frappe.
        var beatsAgo = secondsAgo * Bpm / 60.0;
        var atTap = _position - beatsAgo;
        var snapped = Math.Round(atTap) + beatsAgo;
        _shift += snapped - _position;
        _position = snapped;
    }

    /// <summary>×2 ou ÷2 (AUD-023, CMD-042).</summary>
    public void Scale(double factor)
    {
        Volatile.Write(ref _bpm, Math.Clamp(Bpm * factor, MinBpm, MaxBpm));
        _taps.Clear();
        RememberManualTempo();
    }

    /// <summary>Ajoute (ou retire) des BPM au tempo courant (CMD-042).</summary>
    public void Nudge(double deltaBpm)
    {
        Volatile.Write(ref _bpm, Math.Clamp(Bpm + deltaBpm, MinBpm, MaxBpm));
        _taps.Clear();
        RememberManualTempo();
    }

    /// <summary>En source Audio, un réglage manuel du tempo devient un facteur appliqué au tempo entendu (il ne dure pas qu'un tick).</summary>
    private void RememberManualTempo()
    {
        if (Source == TempoSourceKind.Audio && _lastRawBpm > 0)
        {
            _audioScale = Bpm / _lastRawBpm;
        }
    }

    private static readonly double[] OctaveRatios = [2.0, 0.5, 1.5, 2.0 / 3];

    private static bool IsOctaveRatio(double ratio)
    {
        foreach (var known in OctaveRatios)
        {
            if (Math.Abs((ratio / known) - 1) <= 0.05)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>« Le 1 est maintenant » (AUD-024) : le temps en cours devient le premier d'une mesure.</summary>
    public void ResyncBar()
    {
        var beats = Duration.BeatsPerBar;
        var snapped = Math.Round(_position / beats) * beats;
        _shift += snapped - _position;
        _position = snapped;

        // Le « 1 » posé à la main prime sur le premier temps deviné par l'écoute (fiable à environ 50 %), jusqu'au prochain morceau.
        _barManual = true;
    }

    /// <summary>Décalage de latence global (GEN-035), borné à ±500 ms (un micro USB de conférence, avec son traitement du signal, a montré 300 à 500 ms à l'essai P7).</summary>
    public void SetLatency(double seconds)
    {
        var before = EffectivePosition;
        Volatile.Write(ref _latency, Math.Clamp(seconds, -0.5, 0.5));
        _shift += EffectivePosition - before;
    }
}
