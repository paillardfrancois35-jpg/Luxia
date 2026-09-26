using System.Text;
using Luxia.Core.Settings;
using Luxia.Engine.Timing;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Output.Arduino;
using Luxia.Output.Recording;
using Luxia.Persistence;
using Serilog.Events;

namespace Luxia.Tools.Headless;

/// <summary>Commandes de l'outil.</summary>
internal static class Commands
{
    public static int Help()
    {
        Console.WriteLine("""
            luxia-headless – outil sans interface (phase P0)

              ports
                  Liste les ports série et signale les cartes Arduino.

              lancer [--duree s] [--nul] [--test 1-16] [--exclus 180] [--valeur 50] [--pas ms] [--une-fois]
                     [--enregistrer fichier.dmxrec] [--frequence 40]
                  Démarre le moteur et les sorties des préférences (ou la sortie Nulle avec --nul).
                  --test lance le chenillard de test (CMD-024) ; Ctrl+C arrête proprement (blackout).

              endurance [--duree 3600] [--nul] [--canaux 1-512] [--exclus 180] [--valeur 50] [--periode ms]
                  T-SORT-07 : tous les canaux varient en continu (rampe) ; rapport de cadence, trames/s et erreurs.
                  Prudence avec les appareils branchés : la fumée (180) est exclue, valeur plafonnée à 50 % par défaut.

              gigue [--duree s (900 = 15 min par défaut)] [--frequence 40]
                  Mesure la cadence et la gigue du moteur (GEN-030, GEN-031), sortie Nulle.

              projet dossier [--nom "Nom"] [--description "…"]
                  Crée le projet s'il n'existe pas, sinon l'ouvre et affiche sa fiche (et les éventuels problèmes).

              relire fichier.dmxrec [--canaux 1-16]
                  Résume un enregistrement de trames : durée, trames, instants d'allumage de chaque canal.
            """);
        return 0;
    }

    public static int Ports()
    {
        var ports = new SystemSerialPortProvider().GetPorts();
        if (ports.Count == 0)
        {
            Console.WriteLine("Aucun port série.");
            return 0;
        }

        foreach (var p in ports)
        {
            var ids = p.VendorId is null ? "USB inconnu" : $"VID {p.VendorId:X4} PID {p.ProductId:X4}";
            var tag = p.IsBootloader ? " – Arduino (chargeur de démarrage)" : p.IsArduino ? " – Arduino" : string.Empty;
            Console.WriteLine($"{p.PortName,-8} {ids}{tag}");
        }

        return 0;
    }

    public static async Task<int> RunAsync(Arguments args)
    {
        using var loggers = TechnicalLog.Create(DataPaths.Current.Logs, console: true);
        await using var runtime = new LuxiaRuntime(DataPaths.Current, loggers);
        if (args.Has("frequence"))
        {
            runtime.Loop.RateHz = args.GetDouble("frequence", 40);
        }

        runtime.Start(forceNullOutput: args.Has("nul"));

        if (args.Get("enregistrer") is { } file)
        {
            Console.WriteLine($"Enregistrement : {Path.GetFullPath(runtime.StartRecording(Path.GetFullPath(file)))}");
        }

        TimeSpan? testDuration = null;
        if (args.Has("test"))
        {
            var settings = runtime.Preferences.Current.TestOutput with
            {
                Range = args.Get("test") ?? runtime.Preferences.Current.TestOutput.Range,
                ExcludedChannels = args.Get("exclus") ?? runtime.Preferences.Current.TestOutput.ExcludedChannels,
                ValuePercent = args.GetInt("valeur", runtime.Preferences.Current.TestOutput.ValuePercent),
                StepMilliseconds = args.GetInt("pas", runtime.Preferences.Current.TestOutput.StepMilliseconds),
            };
            var error = runtime.StartTest(settings, CommandOrigin.Tool, loop: !args.Has("une-fois"), remember: false);
            if (error is not null)
            {
                Console.Error.WriteLine(error);
                return 2;
            }

            if (args.Has("une-fois") && Core.Dmx.ChannelRange.TryParse(settings.Range, out var range)
                && Core.Dmx.ChannelList.TryParse(settings.ExcludedChannels, out var excluded))
            {
                var count = Enumerable.Range(range.First, range.Count).Except(excluded).Count();
                testDuration = TimeSpan.FromMilliseconds(count * settings.StepMilliseconds) + TimeSpan.FromMilliseconds(500);
            }
        }

        var duration = args.Has("duree") ? TimeSpan.FromSeconds(args.GetDouble("duree", 10)) : testDuration;
        using var cancel = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancel.Cancel();
        };

        Console.WriteLine(duration is null ? "En marche – Ctrl+C pour arrêter." : $"En marche pendant {duration.Value.TotalSeconds:F1} s.");
        var started = DateTime.UtcNow;
        while (!cancel.IsCancellationRequested && (duration is null || DateTime.UtcNow - started < duration))
        {
            try
            {
                await Task.Delay(1000, cancel.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            PrintStatus(runtime);
        }

        var recorded = runtime.StopRecording();
        if (recorded is not null)
        {
            Console.WriteLine($"Enregistrement fermé : {recorded}");
        }

        return 0;
    }

    public static async Task<int> EnduranceAsync(Arguments args)
    {
        using var loggers = TechnicalLog.Create(DataPaths.Current.Logs, console: false);
        await using var runtime = new LuxiaRuntime(DataPaths.Current, loggers);
        var duration = TimeSpan.FromSeconds(args.GetDouble("duree", 3600));
        runtime.Start(forceNullOutput: args.Has("nul"));

        var settings = new TestOutputPreferences
        {
            Range = args.Get("canaux") ?? "1-512",
            ExcludedChannels = args.Get("exclus") ?? "180",
            ValuePercent = args.GetInt("valeur", 50),
            StepMilliseconds = args.GetInt("periode", 4000),
        };
        var error = runtime.StartTest(settings, CommandOrigin.Tool, remember: false, mode: TestPatternMode.Ramp);
        if (error is not null)
        {
            Console.Error.WriteLine(error);
            return 2;
        }

        Console.WriteLine($"Endurance pendant {duration.TotalMinutes:F0} min : canaux {settings.Range}, exclus {settings.ExcludedChannels}, max {settings.ValuePercent} %");
        var started = DateTime.UtcNow;
        var minFps = double.MaxValue;
        while (DateTime.UtcNow - started < duration)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Min(60, duration.TotalSeconds))).ConfigureAwait(false);
            PrintStatus(runtime);
            foreach (var route in runtime.Router.Routes.Where(r => r.Driver.Status.State == Messaging.Events.OutputConnectionState.Connected))
            {
                minFps = Math.Min(minFps, route.Driver.Status.FramesPerSecond);
            }
        }

        runtime.StopTest(CommandOrigin.Tool);
        var stats = runtime.Loop.Statistics;
        PrintJitterReport(stats);
        foreach (var (universe, driver) in runtime.Router.Routes)
        {
            Console.WriteLine($"  {driver.Name} (univers {universe}) : état {driver.Status.State}, erreurs {driver.Status.ErrorCount}");
        }

        Console.WriteLine($"  Trames/s minimales observées (pilotes connectés) : {(minFps == double.MaxValue ? 0 : minFps):F1}");
        return 0;
    }

    public static async Task<int> JitterAsync(Arguments args)
    {
        using var loggers = TechnicalLog.Create(DataPaths.Current.Logs, console: false, LogEventLevel.Warning);
        await using var runtime = new LuxiaRuntime(DataPaths.Current, loggers);
        runtime.Loop.RateHz = args.GetDouble("frequence", 40);
        var duration = TimeSpan.FromSeconds(args.GetDouble("duree", 900));

        runtime.Start(forceNullOutput: true);
        Console.WriteLine($"Mesure de la cadence pendant {duration.TotalSeconds:F0} s à {runtime.Loop.RateHz:F1} Hz…");
        Console.WriteLine(runtime.IsSleepBlocked ? "Mise en veille du PC bloquée pendant la mesure." : "Attention : la mise en veille du PC n'a pas pu être bloquée.");
        var started = DateTime.UtcNow;
        while (DateTime.UtcNow - started < duration)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Min(10, duration.TotalSeconds))).ConfigureAwait(false);
            var s = runtime.Loop.Statistics;
            Console.WriteLine($"  {s.Elapsed.TotalSeconds,6:F0} s : {s.MeasuredRateHz:F2} Hz, gigue p99 {s.P99Lateness.TotalMilliseconds:F1} ms, max {s.MaxLateness.TotalMilliseconds:F1} ms");
        }

        var stats = runtime.Loop.Statistics;
        PrintJitterReport(stats);
        var ok = Math.Abs(stats.MeasuredRateHz - stats.TargetRateHz) <= 0.5 && stats.P99Lateness < TimeSpan.FromMilliseconds(5);
        Console.WriteLine(ok ? "Résultat : CONFORME (GEN-030, GEN-031)" : "Résultat : NON CONFORME");
        return ok ? 0 : 1;
    }

    public static int Project(Arguments args)
    {
        if (args.Positional.Count == 0)
        {
            Console.Error.WriteLine("Dossier du projet manquant.");
            return 2;
        }

        var folder = Path.GetFullPath(args.Positional[0]);
        if (!File.Exists(Path.Combine(folder, ProjectStore.ProjectFileName)))
        {
            var name = args.Get("nom") ?? Path.GetFileName(folder);
            ProjectStore.Create(folder, name, args.Get("description"));
            Console.WriteLine($"Projet créé : {folder}");
        }

        var report = ProjectStore.Open(folder);
        foreach (var message in report.Messages)
        {
            Console.WriteLine(message);
        }

        if (report.Info is { } info)
        {
            Console.WriteLine($"{info.Name} ({info.Id}) – créé le {info.CreatedUtc.ToLocalTime():dd/MM/yyyy HH:mm}");
        }

        return report.Succeeded ? 0 : 1;
    }

    public static int Replay(Arguments args)
    {
        if (args.Positional.Count == 0)
        {
            Console.Error.WriteLine("Fichier à relire manquant.");
            return 2;
        }

        var (header, frames) = RecordingReader.ReadAll(args.Positional[0]);
        var range = Core.Dmx.ChannelRange.TryParse(args.Get("canaux"), out var r) ? r : new Core.Dmx.ChannelRange(1, 512);
        var duration = frames.Count > 0 ? frames[^1].Elapsed : TimeSpan.Zero;

        Console.WriteLine($"Enregistrement du {header.StartedUtc.ToLocalTime():dd/MM/yyyy HH:mm:ss}, univers {header.Universe}, {header.RateHz:F0} Hz");
        Console.WriteLine($"{frames.Count} trames sur {duration.TotalSeconds:F2} s (moyenne {(duration.TotalSeconds > 0 ? (frames.Count - 1) / duration.TotalSeconds : 0):F1} trames/s)");

        var report = new StringBuilder();
        var lit = 0;
        for (var c = range.First; c <= range.Last; c++)
        {
            var first = frames.FirstOrDefault(f => f.Values[c - 1] != 0);
            if (first is null)
            {
                continue;
            }

            lit++;
            var max = frames.Max(f => f.Values[c - 1]);
            var litFrames = frames.Count(f => f.Values[c - 1] != 0);
            report.AppendLine(System.Globalization.CultureInfo.CurrentCulture, $"  canal {c,3} : allumé à {first.Elapsed.TotalSeconds,7:F2} s, valeur max {max,3}, {litFrames} trames");
        }

        Console.WriteLine($"Canaux allumés dans {range} : {lit}");
        Console.Write(report);
        return 0;
    }

    private static void PrintStatus(LuxiaRuntime runtime)
    {
        var parts = runtime.Router.Routes.Select(r =>
            $"{r.Driver.Name} [{r.Driver.Status.State}] {r.Driver.Status.FramesPerSecond:F1} tr/s{(r.Driver.Status.Message is { } m ? $" – {m}" : string.Empty)}");
        var test = runtime.Engine.TestState;
        var testText = test.Active ? $" | test : canal {test.CurrentChannel}" : string.Empty;
        Console.WriteLine($"{runtime.Loop.Statistics.MeasuredRateHz:F1} Hz | {string.Join(" | ", parts)}{testText}");
    }

    private static void PrintJitterReport(TickStatistics s)
    {
        Console.WriteLine();
        Console.WriteLine("Rapport de cadence");
        Console.WriteLine($"  Durée            : {s.Elapsed.TotalSeconds:F1} s");
        Console.WriteLine($"  Ticks            : {s.TickCount}");
        Console.WriteLine($"  Fréquence visée  : {s.TargetRateHz:F2} Hz");
        Console.WriteLine($"  Fréquence mesurée: {s.MeasuredRateHz:F3} Hz (exigence : ± 0,5 Hz)");
        Console.WriteLine($"  Retard moyen     : {s.MeanLateness.TotalMilliseconds:F2} ms");
        Console.WriteLine($"  Gigue (99e cent.): {s.P99Lateness.TotalMilliseconds:F2} ms (exigence : < 5 ms)");
        Console.WriteLine($"  Retard maximal   : {s.MaxLateness.TotalMilliseconds:F2} ms");
        Console.WriteLine($"  Ticks sautés     : {s.SkippedTicks}");
    }
}
