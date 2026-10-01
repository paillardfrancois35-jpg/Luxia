using System.Globalization;
using System.Text;
using Luxia.Audio;

namespace Luxia.Tools.Headless;

/// <summary>Commande <c>audio</c> : analyse de fichiers du jeu de test (doc 19 §7) et rapport chiffré (T-AUD-02).</summary>
internal static class AudioCommands
{
    public static int Analyse(Arguments args)
    {
        if (args.Positional.Count == 0)
        {
            Console.Error.WriteLine("Usage : luxia-headless audio <fichier ou dossier> [--rapport rapport.md] [--duree 60] [--debut 10]");
            return 1;
        }

        var target = Path.GetFullPath(args.Positional[0]);
        var files = Directory.Exists(target)
            ? [.. Directory.EnumerateFiles(target, "*.*", SearchOption.AllDirectories).Where(f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)).Order()]
            : new List<string> { target };
        var annotations = LoadAnnotations(Path.Combine(Directory.Exists(target) ? target : Path.GetDirectoryName(target)!, "annotations.csv"));
        var report = new StringBuilder();
        report.AppendLine("| Morceau | BPM mesuré | BPM attendu | Écart | Convergence | Confiance | 1er temps connu | Kicks/s | Aigus/s | Énergie | Breaks | Drops | Montées |");
        report.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            try
            {
                var result = AudioFileAnalysis.Analyze(
                    file,
                    args.GetDouble("duree", 0),
                    args.GetDouble("debut", 0),
                    args.Has("trace") ? (s, st) => Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  {s,6:0} s niveau {st.Level:0.000} bpm {st.Bpm:0.0} conf {st.Confidence:0.00} basses {st.Bass:0.0000} méd {st.Mid:0.0000} aig {st.Treble:0.0000} énergie {st.Energy:0.00} {st.EnergyLevel} {st.Trend}{(st.InBreak ? " BREAK" : string.Empty)}")) : null);
                var confidence = result.Timeline.Count == 0 ? 0 : result.Timeline.Skip(result.Timeline.Count / 2).Average(p => p.Confidence);
                var expected = annotations.GetValueOrDefault(name);
                var gap = expected is { } e && e > 0 ? Gap(result.Bpm, e) : "—";
                var line = string.Create(
                    CultureInfo.InvariantCulture,
                    $"| {name} | {result.Bpm:0.0} | {(expected is { } x ? x.ToString("0.#", CultureInfo.InvariantCulture) : "?")} | {gap} | {(result.ConvergenceSeconds < 0 ? "non" : result.ConvergenceSeconds.ToString("0", CultureInfo.InvariantCulture) + " s")} | {confidence:0.00} | {result.DownbeatKnown * 100:0} % | {result.BassPulsesPerSecond:0.0} | {result.TreblePulsesPerSecond:0.0} | {result.MeanEnergy:0.00} | {result.Events.Count(e => e.Kind == AudioEventKind.Break)} | {result.Events.Count(e => e.Kind == AudioEventKind.Drop)} | {result.Events.Count(e => e.Kind == AudioEventKind.BuildUp)} |");
                Console.WriteLine(line);
                report.AppendLine(line);
                if (args.Has("evenements"))
                {
                    foreach (var ev in result.Events.Where(x => x.Kind is AudioEventKind.Break or AudioEventKind.Drop or AudioEventKind.BuildUp))
                    {
                        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"      {ev.Seconds,6:0.0} s  {ev.Kind}"));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{name} : erreur {ex.Message}");
                report.AppendLine(string.Create(CultureInfo.InvariantCulture, $"| {name} | erreur | | | | | | | | | | | |"));
            }
        }

        if (args.Get("rapport") is { } path)
        {
            File.WriteAllText(path, report.ToString(), Encoding.UTF8);
        }

        return 0;
    }

    /// <summary>
    /// Essai de bout en bout sans interface : joue un fichier sur la sortie par défaut et l'écoute par la boucle WASAPI
    /// (<c>luxia-headless audio-ecoute fichier --duree 25</c>) ; affiche le tempo trouvé chaque seconde.
    /// </summary>
    public static int Listen(Arguments args)
    {
        if (args.Positional.Count == 0)
        {
            Console.Error.WriteLine("Usage : luxia-headless audio-ecoute <fichier> [--duree 25] [--debut 30]");
            return 1;
        }

        if (args.Has("brut"))
        {
            return ListenRaw(args);
        }

        using var listener = new AudioListener(new WasapiSourceFactory(), new ConsoleLogger());
        listener.EventRaised += (_, e) =>
        {
            if (e.Kind is AudioEventKind.Break or AudioEventKind.Drop or AudioEventKind.BuildUp)
            {
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"      ► {e.Kind} ({e.Level}, énergie {e.Energy:0.00})"));
            }
        };
        listener.Start();
        Console.WriteLine(listener.Status);
        using var reader = new NAudio.Wave.MediaFoundationReader(Path.GetFullPath(args.Positional[0]));
        reader.CurrentTime = TimeSpan.FromSeconds(args.GetDouble("debut", 0));
        using var output = new NAudio.Wave.WasapiOut();
        output.Init(reader);
        output.Volume = 0.6f;
        output.Play();
        var seconds = args.GetInt("duree", 25);
        for (var i = 0; i < seconds; i++)
        {
            Thread.Sleep(1000);
            var state = listener.State;
            var reading = listener.Read();
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{i + 1,3} s  niveau {state.Level:0.000}  tempo {state.Bpm:0.0}  confiance {state.Confidence:0.00}  temps {reading.BarBeat}  phase {reading.BeatPhase:0.00}  {(reading.Live ? "écoute" : "silence")}"));
        }

        output.Stop();
        return 0;
    }

    /// <summary>Écoute brute (<c>--brut</c>) : blocs reçus et crête par seconde, sans analyse, pour voir si la boucle WASAPI livre toujours.</summary>
    private static int ListenRaw(Arguments args)
    {
        using var source = new WasapiSourceFactory().Create(null);
        var blocks = 0;
        var peak = 0f;
        source.BlockAvailable += (_, b) =>
        {
            Interlocked.Increment(ref blocks);
            foreach (var v in b.Samples.Span)
            {
                peak = Math.Max(peak, Math.Abs(v));
            }
        };
        source.StartCapture();
        Console.WriteLine(source.Name);
        using var reader = new NAudio.Wave.MediaFoundationReader(Path.GetFullPath(args.Positional[0]));
        reader.CurrentTime = TimeSpan.FromSeconds(args.GetDouble("debut", 0));
        using var output = new NAudio.Wave.WasapiOut();
        output.Init(reader);
        output.Volume = 0.6f;
        output.Play();
        for (var i = 0; i < args.GetInt("duree", 25); i++)
        {
            Thread.Sleep(1000);
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{i + 1,3} s  blocs {Interlocked.Exchange(ref blocks, 0)}  crête {peak:0.000}  lecture {reader.CurrentTime.TotalSeconds:0.0} s"));
            peak = 0;
        }

        output.Stop();
        source.StopCapture();
        return 0;
    }

    /// <summary>
    /// Diagnostic de l'analyse sur des fichiers (<c>luxia-headless audio-diag dossier|fichier [--duree 90]</c>) : calage du tempo,
    /// impulsions des basses (cadence, part sur les temps, cadence dans les passages sans basses), niveaux d'énergie, drops.
    /// </summary>
    public static int Diagnose(Arguments args)
    {
        if (args.Positional.Count == 0)
        {
            Console.Error.WriteLine("Usage : luxia-headless audio-diag <fichier ou dossier> [--duree 90] [--filtre texte]");
            return 1;
        }

        var target = Path.GetFullPath(args.Positional[0]);
        var files = Directory.Exists(target)
            ? [.. Directory.EnumerateFiles(target, "*.mp3", SearchOption.AllDirectories).Order()]
            : new List<string> { target };
        if (args.Get("filtre") is { } filter)
        {
            files = [.. files.Where(f => f.Contains(filter, StringComparison.OrdinalIgnoreCase))];
        }

        var annotations = LoadAnnotations(Path.Combine(Directory.Exists(target) ? target : Path.GetDirectoryName(target)!, "annotations.csv"));
        Console.WriteLine("Morceau | BPM | réf | calage s | basses/s | sur les temps | basses/s calmes | aigus/s | % Calme Groove Énerg. Explosif | chgts niveau/min | énergie σ | Break Drop Montée");
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var seconds = new List<double>();
            var bassPerSecond = new List<double>();
            var energies = new List<double>();
            var levels = new int[4];
            var changes = 0;
            var previousLevel = -1;
            var aligned = 0;
            var gridPulses = 0;
            var bassPulses = new List<(double Time, bool Aligned)>();
            var phaseHistogram = new int[10];
            var treble = 0;
            double bassAccumulator = 0;
            var bassSamples = 0;
            var nextSecond = 1.0;
            var blocks = 0;
            var result = AudioFileAnalysis.Analyze(
                file,
                args.GetDouble("duree", 90),
                0,
                null,
                (time, state, bass, trebleCount) =>
                {
                    blocks++;
                    if (args.Has("serie") && time >= args.GetDouble("serie", 0) && time < args.GetDouble("serie", 0) + 4 && blocks % 2 == 0)
                    {
                        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  {time,7:0.000} basses {state.Bass:0.0000} aigus {state.Treble:0.0000} niveau {state.Level:0.000} {(bass > 0 ? "◆ IMPULSION" : string.Empty)}"));
                    }

                    treble += trebleCount;
                    bassAccumulator += state.Bass;
                    bassSamples++;
                    if (bass > 0)
                    {
                        var onBeat = state.HasGrid && Math.Min(state.BeatPhase, 1 - state.BeatPhase) <= 0.15;
                        if (state.HasGrid)
                        {
                            gridPulses++;
                            if (onBeat)
                            {
                                aligned++;
                            }
                        }

                        bassPulses.Add((time, onBeat));
                        if (state.HasGrid)
                        {
                            phaseHistogram[Math.Min(9, (int)(state.BeatPhase * 10))]++;
                        }
                    }

                    var level = (int)state.EnergyLevel;
                    if (time >= nextSecond)
                    {
                        seconds.Add(bassAccumulator / Math.Max(1, bassSamples));
                        bassAccumulator = 0;
                        bassSamples = 0;
                        nextSecond += 1;
                        energies.Add(state.Energy);
                        levels[level]++;
                        if (previousLevel >= 0 && previousLevel != level)
                        {
                            changes++;
                        }

                        previousLevel = level;
                    }
                });
            var sorted = seconds.Order().ToList();
            var p90 = sorted.Count == 0 ? 0 : sorted[(int)(sorted.Count * 0.9)];
            var calm = seconds.Select((b, i) => (b, i)).Where(x => x.b < 0.25 * p90).Select(x => x.i).ToHashSet();
            var calmPulses = bassPulses.Count(p => calm.Contains((int)p.Time));
            var duration = result.DurationSeconds;
            var expected = annotations.GetValueOrDefault(name);
            var settle = "—";
            if (expected > 0)
            {
                var found = -1.0;
                for (var i = 0; i < result.Timeline.Count; i++)
                {
                    var window = result.Timeline.Skip(i).Take(5).ToList();
                    if (window.Count == 5 && window.All(p => p.Bpm > 0 && Math.Abs(p.Bpm - expected) / expected <= 0.02))
                    {
                        found = result.Timeline[i].Seconds;
                        break;
                    }
                }

                settle = found < 0 ? "jamais" : found.ToString("0", CultureInfo.InvariantCulture);
            }

            var mean = energies.Count == 0 ? 0 : energies.Average();
            var sigma = energies.Count == 0 ? 0 : Math.Sqrt(energies.Average(e => (e - mean) * (e - mean)));
            var total = Math.Max(1, levels.Sum());
            Console.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{name} | {result.Bpm:0.0} | {(expected > 0 ? expected.ToString("0", CultureInfo.InvariantCulture) : "?")} | {settle} | {bassPulses.Count / duration:0.0} | {(gridPulses == 0 ? 0 : 100.0 * aligned / gridPulses):0} % | {(calm.Count == 0 ? 0 : calmPulses / (double)calm.Count):0.0} ({calm.Count}s) | {treble / duration:0.0} | {100.0 * levels[0] / total:0} {100.0 * levels[1] / total:0} {100.0 * levels[2] / total:0} {100.0 * levels[3] / total:0} | {changes / (duration / 60):0.0} | {sigma:0.00} | {result.Events.Count(e => e.Kind == AudioEventKind.Break)} {result.Events.Count(e => e.Kind == AudioEventKind.Drop)} {result.Events.Count(e => e.Kind == AudioEventKind.BuildUp)}"));
            if (args.Has("phases"))
            {
                Console.WriteLine("      phase des impulsions de basses (10 classes de 0 à 1) : " + string.Join(' ', phaseHistogram));
            }

            if (args.Has("evenements"))
            {
                foreach (var ev in result.Events.Where(x => x.Kind is AudioEventKind.Break or AudioEventKind.Drop))
                {
                    Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"      {ev.Seconds,6:0.0} s  {ev.Kind}"));
                }
            }
        }

        return 0;
    }

    private static string Gap(double measured, double expected)
    {
        var ratio = measured / expected;
        if (Math.Abs(ratio - 1) <= 0.02)
        {
            return "✓ ±2 %";
        }

        foreach (var (factor, label) in new[] { (2.0, "octave ×2"), (0.5, "octave ÷2"), (1.5, "×1,5"), (2.0 / 3, "×2/3") })
        {
            if (Math.Abs((ratio / factor) - 1) <= 0.03)
            {
                return label;
            }
        }

        return string.Create(CultureInfo.InvariantCulture, $"{(ratio - 1) * 100:+0.0;-0.0} %");
    }

    private static Dictionary<string, double> LoadAnnotations(string path)
    {
        var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path))
        {
            return map;
        }

        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var cells = line.Split(';');
            if (cells.Length >= 2 && double.TryParse(cells[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var bpm))
            {
                map[Path.GetFileNameWithoutExtension(cells[0])] = bpm;
            }
        }

        return map;
    }
}

/// <summary>Journal sur la console pour les essais d'écoute : on voit les erreurs d'analyse et les reconnexions.</summary>
internal sealed class ConsoleLogger : Microsoft.Extensions.Logging.ILogger
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => logLevel >= Microsoft.Extensions.Logging.LogLevel.Warning;

    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            Console.WriteLine($"      [{logLevel}] {formatter(state, exception)} {exception}");
        }
    }
}
