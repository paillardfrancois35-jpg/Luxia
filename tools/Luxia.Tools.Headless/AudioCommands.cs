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
        report.AppendLine("| Morceau | BPM mesuré | BPM attendu | Écart | Convergence | Confiance | 1er temps connu |");
        report.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            try
            {
                var result = AudioFileAnalysis.Analyze(file, args.GetDouble("duree", 0), args.GetDouble("debut", 0));
                var confidence = result.Timeline.Count == 0 ? 0 : result.Timeline.Skip(result.Timeline.Count / 2).Average(p => p.Confidence);
                var expected = annotations.GetValueOrDefault(name);
                var gap = expected is { } e && e > 0 ? Gap(result.Bpm, e) : "—";
                var line = string.Create(
                    CultureInfo.InvariantCulture,
                    $"| {name} | {result.Bpm:0.0} | {(expected is { } x ? x.ToString("0.#", CultureInfo.InvariantCulture) : "?")} | {gap} | {(result.ConvergenceSeconds < 0 ? "non" : result.ConvergenceSeconds.ToString("0", CultureInfo.InvariantCulture) + " s")} | {confidence:0.00} | {result.DownbeatKnown * 100:0} % |");
                Console.WriteLine(line);
                report.AppendLine(line);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{name} : erreur {ex.Message}");
                report.AppendLine(string.Create(CultureInfo.InvariantCulture, $"| {name} | erreur | | | | | |"));
            }
        }

        if (args.Get("rapport") is { } path)
        {
            File.WriteAllText(path, report.ToString(), Encoding.UTF8);
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
