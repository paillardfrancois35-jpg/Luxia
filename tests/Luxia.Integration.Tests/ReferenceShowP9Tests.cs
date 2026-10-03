using System.Security.Cryptography;
using Luxia.Fixtures.Rules;
using Luxia.Hosting.Tools;
using Luxia.Output.Recording;
using Luxia.Show;

namespace Luxia.Integration.Tests;

/// <summary>
/// Non-régression du contenu P9 du show de référence (doc 41, DEMO-3, DEMO-4) : le show « Style du morceau (P9) » suit le style (simulé ici :
/// la lecture en cours de Windows n'existe pas dans les tests), revient à « Neutre » au morceau suivant ; le piège (un style qui n'est le
/// nom d'aucune famille) est signalé par un avertissement. Trames comparées à <c>tests/assets/golden/P9-shows.txt</c> (régénérer avec
/// <c>LUXIA_GOLDEN_UPDATE=1</c> après un changement voulu et vérifié).
/// </summary>
public sealed class ReferenceShowP9Tests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");
    private static readonly string GoldenFile = Path.Combine(AppContext.BaseDirectory, "golden", "P9-shows.txt");

    // Un morceau Rock, un morceau Électro, un morceau Latino, un Slow, puis un morceau non identifié : « morceau suivant » entre deux.
    private const string Script = """
        0 show "Style du morceau (P9)"
        3 style Rock
        9 simuler morceau
        10 style Électro
        17 simuler morceau
        18 style Latino
        25 simuler morceau
        26 style Slow
        33 simuler morceau
        34 style Inconnu
        """;

    [Fact]
    [Trait("Exigence", "GEN-131")]
    [Trait("Exigence", "SHOW-024")]
    public void ReferenceShow_P9_ContentIsThere_AndOnlyThePitfallWarns()
    {
        var (shows, _) = ShowStore.Load(Folder);
        shows.Shows.Count(s => s.Category == "Phase P9").ShouldBe(2);

        // DEMO-4 : le piège est le seul problème des shows de P9, et il dit quoi écrire à la place.
        var issues = ProjectValidator.Validate(Folder).Where(i => i.Item.Contains("(P9)", StringComparison.Ordinal)).ToList();
        var warning = issues.ShouldHaveSingleItem();
        warning.Severity.ShouldBe(IssueSeverity.Warning);
        warning.Item.ShouldContain("Piège : style qui n'existe pas");
        warning.Message.ShouldContain("Musette");
        warning.Message.ShouldContain("écrivez plutôt « Bal »");
    }

    [Fact]
    [Trait("Exigence", "MUS-021")]
    [Trait("Exigence", "SHOW-022")]
    public void StyleShow_FollowsTheStyle_AndGoesBackToNeutralAtTheNextSong()
    {
        var steps = Report(Script, 40).Lines.Where(l => l.Contains('◆', StringComparison.Ordinal)).ToList();
        string[] expected = ["Neutre", "Rock", "Neutre", "Électro", "Neutre", "Latino", "Neutre", "Slow", "Neutre", "Inconnu"];
        var found = steps.Select(l => expected.FirstOrDefault(e => l.Contains($"« {e}", StringComparison.Ordinal))).Where(e => e is not null).ToList();

        found.ShouldBe(expected);
    }

    [Fact]
    [Trait("Exigence", "SHOW-022")]
    public void StyleShow_FollowsAChangeOfStyleInTheMiddleOfATrack_FromAnyAmbiance()
    {
        // Essai P9 (ex. 10) : un style imposé ou corrigé pendant le titre fait passer d'une ambiance à l'autre, sans repasser par « Neutre ».
        var steps = Report("0 show \"Style du morceau (P9)\"\n3 style Rock\n9 style Latino\n15 style Électro\n21 style Inconnu", 28).Lines.Where(l => l.Contains('◆', StringComparison.Ordinal)).ToList();
        string[] expected = ["Neutre", "Rock", "Latino", "Électro", "Inconnu"];
        var found = steps.Select(l => expected.FirstOrDefault(e => l.Contains($"« {e}", StringComparison.Ordinal))).Where(e => e is not null).ToList();

        found.ShouldBe(expected);
    }

    [Fact]
    [Trait("Exigence", "SHOW-024")]
    public void Pitfall_StaysInItsWaitingStep()
    {
        var report = Report("0 show \"Piège : style qui n'existe pas (P9)\"\n3 style Bal\n5 style Rock", 12);

        report.Rejections.ShouldBeEmpty();
        report.Lines.Count(l => l.Contains('◆', StringComparison.Ordinal)).ShouldBe(1, "seule l'étape initiale s'active : aucune famille ne s'appelle « Musette »");
    }

    [Fact]
    [Trait("Exigence", "DEMO-3")]
    public void StyleShow_ReplaysExactlyAsReference()
    {
        var actual = $"show Style du morceau (P9);{Hash(Run(Script, 40))}";

        if (Environment.GetEnvironmentVariable("LUXIA_GOLDEN_UPDATE") == "1")
        {
            var source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "golden", "P9-shows.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(source)!);
            File.WriteAllLines(source, [actual]);
            return;
        }

        File.ReadAllLines(GoldenFile).Where(l => l.Length > 0).ShouldBe([actual]);
    }

    private static Luxia.Hosting.Tools.ScenarioReport Report(string script, double seconds, string? file = null)
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
        var file = Path.Combine(Path.GetTempPath(), $"luxia-p9-{Guid.NewGuid():N}.dmxrec");
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
