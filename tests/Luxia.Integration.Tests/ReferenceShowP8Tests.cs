using System.Security.Cryptography;
using Luxia.Fixtures.Rules;
using Luxia.Hosting.Tools;
using Luxia.Output.Recording;
using Luxia.Show;
using Luxia.Show.Model;

namespace Luxia.Integration.Tests;

/// <summary>
/// Non-régression du contenu P8 du show de référence (doc 41 §8 et §9, DEMO-3, DEMO-4) : séquences et shows « Phase P8 »
/// rejoués au métronome (120 BPM) avec des événements musicaux simulés, et comparés à <c>tests/assets/golden/P8-shows.txt</c>
/// (régénérer avec <c>LUXIA_GOLDEN_UPDATE=1</c> après un changement voulu et vérifié).
/// </summary>
public sealed class ReferenceShowP8Tests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");
    private static readonly string GoldenFile = Path.Combine(AppContext.BaseDirectory, "golden", "P8-shows.txt");

    // Simulation commune aux shows : énergie Groove, drop, break, montée, drop… (doc 20 §3.4).
    private const string Music = "3 energie 40\n9 simuler drop\n15 simuler break\n19 simuler montee\n27 simuler drop\n33 simuler break\n35 simuler drop\n";

    [Fact]
    [Trait("Exigence", "GEN-131")]
    [Trait("Exigence", "SHOW-024")]
    public void ReferenceShow_P8_ContentIsThere_AndOnlyThePitfallIsAnError()
    {
        var (sequences, _) = SequenceStore.Load(Folder);
        var (shows, _) = ShowStore.Load(Folder);
        sequences.Sequences.Count(s => s.Category == "Phase P8").ShouldBe(6);
        shows.Shows.Count(s => s.Category == "Phase P8").ShouldBe(7);
        shows.Shows.Single(s => s.Name == "Ambiance UV et fumée (secondaire)").Secondary.ShouldBeTrue();

        // DEMO-4 : le piège est la seule erreur des fichiers de P8.
        ProjectValidator.Validate(Folder)
            .Where(i => i.File is SequenceStore.FileName or ShowStore.FileName)
            .ShouldAllBe(i => i.Severity == IssueSeverity.Error && i.Item == "show « Piège : boucle sans condition »");
    }

    [Fact]
    [Trait("Exigence", "SHOW-004")]
    public void Rise_RampsTheColorsLayerOverEightBars()
    {
        var frames = Run("0 sequence \"Montée 16 mesures\"", 9);

        // PAR 1 : gradateur au canal 1 ; à 120 BPM, 8 mesures = 16 s : 30 % au départ, ≈ 56 % à 6 s.
        ((double)frames[(int)(0.2 * 40)][0]).ShouldBe(77, 3);
        ((double)frames[6 * 40][0]).ShouldBe(143, 4);
    }

    [Fact]
    [Trait("Exigence", "SHOW-022")]
    [Trait("Exigence", "SHOW-029")]
    public void CoupletRefrainDrop_EndOfTheSong_LeadsToTheFinal_AndTheNextSongToTheIntro()
    {
        // Essai P8, ex. 13 : 3e refrain coupé par un break (fin du morceau), puis silence, puis morceau suivant.
        const string script = """
            0 show "Couplet / Refrain / Drop"
            1 energie 40
            3 simuler drop
            7 simuler break
            9 simuler drop
            13 simuler break
            15 simuler drop
            17 simuler break
            21 simuler silence
            25 simuler reprise
            """;
        var lines = Report(script, 28).Lines.Where(l => l.Contains('◆', StringComparison.Ordinal)).ToList();

        var final = lines.FindIndex(l => l.Contains("étape 5 « Final »", StringComparison.Ordinal));
        final.ShouldBeGreaterThan(0, "le silence mène au final, même quand le 3e refrain a été coupé par un break");
        lines[final - 1].ShouldContain("étape 1 « Couplet »");
        lines.Skip(final + 1).ShouldContain(l => l.Contains("étape 0 « Intro »", StringComparison.Ordinal), "le son reprend : retour à l'intro");
        lines.Last().ShouldContain("étape 1 « Couplet »", Case.Sensitive, "compteur remis à zéro : l'intro repart vers le couplet");
    }

    [Fact]
    [Trait("Exigence", "SHOW-024")]
    public void Pitfall_IsRefused_AndNothingLights()
    {
        var report = Report("0 show \"Piège : boucle sans condition\"", 2);

        report.Rejections.ShouldHaveSingleItem().ShouldContain("boucle sans condition (a → b → a)");
        report.Lines.ShouldNotContain(l => l.Contains('◆'));
    }

    [Fact]
    [Trait("Exigence", "DEMO-3")]
    [Trait("Exigence", "SHOW-023")]
    public void EveryP8SequenceAndShow_ReplaysExactlyAsReference()
    {
        var (sequences, _) = SequenceStore.Load(Folder);
        var (shows, _) = ShowStore.Load(Folder);
        var actual = sequences.Sequences.Where(s => s.Category == "Phase P8")
            .Select(s => $"séquence {s.Name};{Hash(Run($"0 sequence \"{s.Name}\"", (s.Bars * 2) + 2))}")
            .Concat(shows.Shows.Where(s => s.Category == "Phase P8" && !s.Name.StartsWith("Piège", StringComparison.Ordinal))
                .Select(s => $"show {s.Name};{Hash(Run($"0 show \"{s.Name}\"\n{Music}", 50))}"))
            .ToList();

        if (Environment.GetEnvironmentVariable("LUXIA_GOLDEN_UPDATE") == "1")
        {
            var source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "golden", "P8-shows.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(source)!);
            File.WriteAllLines(source, actual);
            return;
        }

        File.ReadAllLines(GoldenFile).Where(l => l.Length > 0).ShouldBe(actual);
    }

    private static ScenarioReport Report(string script, double seconds, string? file = null)
    {
        var content = ProjectFiles.Load(Folder);
        var (sequences, _) = SequenceStore.Load(Folder);
        var (shows, _) = ShowStore.Load(Folder);
        var scenario = Scenario.Parse("0 tempo 120\n" + script, content.Scenes, content.Layers, out var errors, sequences, shows);
        errors.ShouldBeEmpty();
        return ScenarioRunner.Run(content, scenario, TimeSpan.FromSeconds(seconds), file, sequences: sequences, shows: shows);
    }

    private static List<byte[]> Run(string script, double seconds)
    {
        var file = Path.Combine(Path.GetTempPath(), $"luxia-p8-{Guid.NewGuid():N}.dmxrec");
        try
        {
            Report(script, seconds, file).Rejections.ShouldBeEmpty();
            using var stream = File.OpenRead(file);
            return [.. RecordingReader.ReadAll(stream).Frames.Select(f => f.Values)];
        }
        finally
        {
            File.Delete(file);
        }
    }

    private static string Hash(List<byte[]> frames) =>
        Convert.ToHexString(SHA256.HashData(frames.SelectMany(f => f).ToArray()));
}
