using System.Security.Cryptography;
using Luxia.Hosting.Tools;
using Luxia.Output.Recording;

namespace Luxia.Integration.Tests;

/// <summary>
/// Non-régression des scènes P4 du show de référence (doc 41 §11, DEMO-3, T-MOT-07) : chaque scène est jouée 6 s en
/// temps virtuel et ses trames sont comparées octet par octet (empreinte SHA-256) à la référence enregistrée dans
/// <c>tests/assets/golden/P4-scenes.txt</c>. Pour la régénérer après un changement voulu et vérifié :
/// <c>LUXIA_GOLDEN_UPDATE=1</c> puis relancer ce test.
/// </summary>
public sealed class ReferenceShowP4Tests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");
    private static readonly string GoldenFile = Path.Combine(AppContext.BaseDirectory, "golden", "P4-scenes.txt");

    [Fact]
    [Trait("Exigence", "GEN-131")]
    [Trait("Exigence", "GEN-130")]
    public void ReferenceShow_P4_IsValid_WithoutAnyProblem()
    {
        ProjectValidator.Validate(Folder).ShouldBeEmpty();
        var content = ProjectFiles.Load(Folder);
        content.Scenes.Scenes.Count(s => s.Category == "Phase P4").ShouldBe(10);
    }

    [Fact]
    [Trait("Exigence", "MOT-040")]
    public void WarmWhite_OnFourPars_HasDimmerAndColor()
    {
        var frames = Play("Blanc chaud sur les 4 PAR", 1);
        var last = frames[^1];
        foreach (var address in new[] { 1, 8, 15, 22 })
        {
            last[address - 1].ShouldBe((byte)255);
            last[address].ShouldBe((byte)255);
            last[address + 1].ShouldBe((byte)199);
            last[address + 2].ShouldBe((byte)115);
        }
    }

    [Fact]
    [Trait("Exigence", "MOT-041")]
    public void Trap_ColorWithoutIntensity_LeavesSevenChannelParsDark()
    {
        var frames = Play("Piège : couleur sans intensité", 2);

        frames.ShouldAllBe(f => f[0] == 0 && f[7] == 0 && f[14] == 0 && f[21] == 0);
        frames[^1][1].ShouldBe((byte)255);
    }

    [Fact]
    [Trait("Exigence", "MOT-012")]
    public void ColorWheel_OnlyEverShowsSlotMedians()
    {
        var frames = Play("Roue de couleur qui bascule franchement", 10);

        // Lyre 1 (11 canaux à 111), roue de couleur au canal 118 : rouge 12, vert 20, bleu 28, jaune 36 — rien entre deux.
        frames.Select(f => f[117]).Distinct().Order().ShouldBe([(byte)12, (byte)20, (byte)28, (byte)36]);
    }

    [Fact]
    [Trait("Exigence", "SCN-010")]
    public void Wave_StartsEachParHalfASecondAfterItsNeighbour()
    {
        var frames = Play("Vague gauche → droite", 3);

        // Au bout de 1 s (tick 40) : PAR 1 a fini son fondu (1 s), PAR 2 est à mi-chemin, PAR 3 et 4 n'ont pas commencé.
        var at1s = frames[40];
        at1s[0].ShouldBe((byte)255);
        at1s[7].ShouldBe((byte)128);
        at1s[14].ShouldBe((byte)0);
        at1s[21].ShouldBe((byte)0);
    }

    [Fact]
    [Trait("Exigence", "DEMO-3")]
    [Trait("Exigence", "T-MOT-07")]
    public void EveryP4Scene_ReplaysExactlyAsReference()
    {
        var content = ProjectFiles.Load(Folder);
        var actual = content.Scenes.Scenes
            .Where(s => s.Category == "Phase P4")
            .Select(s => $"{s.Name};{Hash(Play(s.Name, 6))}")
            .ToList();

        if (Environment.GetEnvironmentVariable("LUXIA_GOLDEN_UPDATE") == "1")
        {
            var source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "golden", "P4-scenes.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(source)!);
            File.WriteAllLines(source, actual);
            return;
        }

        File.ReadAllLines(GoldenFile).Where(l => l.Length > 0).ShouldBe(actual);
    }

    private static List<byte[]> Play(string sceneName, double seconds)
    {
        var content = ProjectFiles.Load(Folder);
        var scene = content.Scenes.Scenes.Single(s => s.Name == sceneName);
        var file = Path.Combine(Path.GetTempPath(), $"luxia-p4-{Guid.NewGuid():N}.dmxrec");
        try
        {
            ScenarioRunner.Run(content, Scenario.ForScene(scene.Id), TimeSpan.FromSeconds(seconds), file).Rejections.ShouldBeEmpty();
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
