using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Luxia.Audio;

/// <summary>Une estimation de tempo du fichier à un instant donné.</summary>
/// <param name="Seconds">Instant dans le morceau.</param>
/// <param name="Bpm">Tempo estimé à cet instant.</param>
/// <param name="Confidence">Confiance.</param>
public readonly record struct TempoPoint(double Seconds, double Bpm, double Confidence);

/// <summary>Résultat de l'analyse d'un fichier (jeu de test audio, doc 19 §7).</summary>
/// <param name="DurationSeconds">Durée analysée.</param>
/// <param name="Timeline">Tempo estimé toutes les secondes environ.</param>
/// <param name="Bpm">Tempo médian sur la seconde moitié du morceau (le plus stable).</param>
/// <param name="ConvergenceSeconds">Instant à partir duquel le tempo reste dans ± 2 % de <paramref name="Bpm"/> (−1 s'il ne converge pas).</param>
/// <param name="DownbeatKnown">Part du temps où le premier temps de la mesure était connu.</param>
/// <param name="Events">Événements musicaux relevés (break, drop, montée, silence, niveaux).</param>
/// <param name="BassPulsesPerSecond">Impulsions des basses par seconde.</param>
/// <param name="TreblePulsesPerSecond">Impulsions des aigus par seconde.</param>
/// <param name="MeanEnergy">Énergie moyenne (0 à 1).</param>
public sealed record FileAnalysis(
    double DurationSeconds,
    IReadOnlyList<TempoPoint> Timeline,
    double Bpm,
    double ConvergenceSeconds,
    double DownbeatKnown,
    IReadOnlyList<AudioEvent> Events,
    double BassPulsesPerSecond,
    double TreblePulsesPerSecond,
    double MeanEnergy);

/// <summary>Analyse d'un fichier audio (mp3, wav…) sans passer par la carte son : tests et rapport chiffré (T-AUD-02).</summary>
public static class AudioFileAnalysis
{
    /// <summary>Lit le fichier, le ramène en mono à 44,1 kHz et le fait analyser.</summary>
    /// <param name="path">Fichier audio.</param>
    /// <param name="maxSeconds">Durée maximale analysée (0 = tout le fichier).</param>
    /// <param name="skipSeconds">Début ignoré (introductions, silence).</param>
    public static FileAnalysis Analyze(string path, double maxSeconds = 0, double skipSeconds = 0)
    {
        using var reader = new MediaFoundationReader(path);
        ISampleProvider samples = reader.ToSampleProvider();
        if (samples.WaveFormat.Channels == 2)
        {
            samples = new StereoToMonoSampleProvider(samples) { LeftVolume = 0.5f, RightVolume = 0.5f };
        }
        else if (samples.WaveFormat.Channels > 2)
        {
            throw new NotSupportedException("Fichier à plus de deux canaux.");
        }

        if (samples.WaveFormat.SampleRate != 44100)
        {
            samples = new WdlResamplingSampleProvider(samples, 44100);
        }

        var analyzer = new AudioAnalyzer(44100);
        var events = new List<AudioEvent>();
        analyzer.EventRaised += events.Add;
        var bassTotal = 0;
        var trebleTotal = 0;
        double energySum = 0;
        var energyCount = 0;
        var block = new float[4096];
        var total = 0L;
        var skip = (long)(skipSeconds * 44100);
        var limit = maxSeconds > 0 ? (long)(maxSeconds * 44100) : long.MaxValue;
        var timeline = new List<TempoPoint>();
        var nextPoint = 1.0;
        var known = 0;
        var checks = 0;
        int read;
        while ((read = samples.Read(block, 0, block.Length)) > 0)
        {
            var start = 0;
            if (total < skip)
            {
                start = (int)Math.Min(read, skip - total);
            }

            total += read;
            if (start < read)
            {
                analyzer.Push(block.AsSpan(start, read - start));
                var (bass, treble) = analyzer.TakePulses();
                bassTotal += bass;
                trebleTotal += treble;
                energySum += analyzer.State.Energy;
                energyCount++;
            }

            var seconds = (total - skip) / 44100.0;
            if (seconds >= nextPoint)
            {
                var state = analyzer.State;
                timeline.Add(new TempoPoint(seconds, state.Bpm, state.Confidence));
                nextPoint += 1;
                checks++;
                if (state.BarBeat > 0)
                {
                    known++;
                }
            }

            if (total - skip >= limit)
            {
                break;
            }
        }

        var duration = Math.Max(0, (total - skip) / 44100.0);
        var second = timeline.Where(p => p.Seconds >= duration / 2 && p.Bpm > 0).Select(p => p.Bpm).Order().ToList();
        var bpm = second.Count > 0 ? second[second.Count / 2] : 0;
        var convergence = -1.0;
        if (bpm > 0)
        {
            for (var i = 0; i < timeline.Count; i++)
            {
                if (timeline.Skip(i).All(p => p.Bpm > 0 && Math.Abs(p.Bpm - bpm) / bpm <= 0.02))
                {
                    convergence = timeline[i].Seconds;
                    break;
                }
            }
        }

        return new FileAnalysis(
            duration,
            timeline,
            bpm,
            convergence,
            checks == 0 ? 0 : (double)known / checks,
            events,
            duration > 0 ? bassTotal / duration : 0,
            duration > 0 ? trebleTotal / duration : 0,
            energyCount == 0 ? 0 : energySum / energyCount);
    }
}
