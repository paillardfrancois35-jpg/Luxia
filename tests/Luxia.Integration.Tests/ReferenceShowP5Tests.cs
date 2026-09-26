using System.Security.Cryptography;
using Luxia.Hosting.Tools;
using Luxia.Output.Recording;

namespace Luxia.Integration.Tests;

/// <summary>
/// Non-régression du contenu P5 du show de référence (doc 41 §11, DEMO-3, DEMO-4) : scènes « Phase P5 » rejouées et
/// comparées à <c>tests/assets/golden/P5-scenes.txt</c> (régénérer avec <c>LUXIA_GOLDEN_UPDATE=1</c> après un changement
/// voulu et vérifié), et contrôles ciblés des couches, flashs et limites de sûreté sur le parc réel.
/// </summary>
public sealed class ReferenceShowP5Tests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");
    private static readonly string GoldenFile = Path.Combine(AppContext.BaseDirectory, "golden", "P5-scenes.txt");

    [Fact]
    [Trait("Exigence", "GEN-131")]
    public void ReferenceShow_P5_ContentIsThere()
    {
        var content = ProjectFiles.Load(Folder);
        content.Scenes.Scenes.Count(s => s.Category == "Phase P5").ShouldBe(14);
        content.Venues.Active.ForbiddenZones.Count.ShouldBe(2);
        content.Safety.ShouldNotBeNull().Strobe.MaxContinuousSeconds.ShouldBe(10);
    }

    [Fact]
    [Trait("Exigence", "MOT-042")]
    [Trait("Exigence", "COU-003")]
    public void Layers_IntensityTimesColor_LightThePars_ColorAloneDoesNot()
    {
        var alone = Run("0 lancer \"Rouge – couleur seule\"", 1);
        alone[^1][0].ShouldBe((byte)0, "PAR 1 : gradateur à 0 sans la couche Intensité");
        alone[^1][1].ShouldBe((byte)255);

        var both = Run("0 lancer \"Rouge – couleur seule\"\n0 lancer \"Plein feu\"", 1);
        both[^1][0].ShouldBe((byte)255);
        both[^1][1].ShouldBe((byte)255);
        both[^1][3].ShouldBe((byte)0);
    }

    [Fact]
    [Trait("Exigence", "MOT-082")]
    [Trait("Exigence", "DEMO-4")]
    public void Trap_LyreTowardsThePublic_StopsAtTheZoneEdge()
    {
        var frames = Run("0 lancer \"Piège : lyre 1 vers le public\"", 1);

        // Lyre 1 (111) : Tilt 16 bits aux canaux 113-114 ; 95 % demandé, 85 % émis (bord de la zone).
        var tilt = ((frames[^1][112] << 8) | frames[^1][113]) / 65535.0;
        tilt.ShouldBe(0.85, 0.001);
    }

    [Fact]
    [Trait("Exigence", "MOT-080")]
    [Trait("Exigence", "DEMO-4")]
    public void StrobeScene_IsCutAfterTenSeconds_ThenResumesAfterThePause()
    {
        var frames = Run("0 lancer \"Strobe PAR (plafonné à 10 s)\"", 21);

        // PAR 1 : canal 5 (strobe). 40 trames par seconde.
        frames[(int)(9.5 * 40)][4].ShouldBeGreaterThan((byte)4);
        frames[(int)(10.5 * 40)][4].ShouldBe((byte)0, "au-delà de 10 s : pas de strobe");
        frames[(int)(19.5 * 40)][4].ShouldBe((byte)0, "pause de 10 s");
        frames[(int)(20.5 * 40)][4].ShouldBeGreaterThan((byte)4, "reprise après la pause");
    }

    [Fact]
    [Trait("Exigence", "MOT-081")]
    [Trait("Exigence", "DEMO-4")]
    public void LongSmoke_IsCutAtTenSeconds()
    {
        var frames = Run("0 lancer \"Fumée longue (plafonnée à 10 s)\"", 12);

        frames[(int)(9.5 * 40)][179].ShouldBe((byte)255);
        frames[(int)(10.5 * 40)][179].ShouldBe((byte)0);
    }

    [Fact]
    [Trait("Exigence", "MOT-072")]
    [Trait("Exigence", "COU-005")]
    public void PartialBlackoutFlash_KeepsTheUv_ThenGivesBack()
    {
        var frames = Run(
            "0 lancer \"Plein feu\"\n0 lancer \"Rouge – couleur seule\"\n1 flash \"Blackout partiel (sauf UV)\" appui\n2 flash \"Blackout partiel (sauf UV)\" relache",
            3);

        var during = frames[60];
        during[0].ShouldBe((byte)0, "PAR 1 éteint pendant le flash");
        during[160].ShouldBe((byte)255, "UV 1 (161) : gradateur toujours à 100 %");
        frames[^1][0].ShouldBe((byte)255, "au relâchement, retour instantané");
    }

    [Fact]
    [Trait("Exigence", "DEMO-3")]
    public void EveryP5Scene_ReplaysExactlyAsReference()
    {
        var content = ProjectFiles.Load(Folder);
        var actual = content.Scenes.Scenes
            .Where(s => s.Category == "Phase P5")
            .Select(s => $"{s.Name};{Hash(Run($"0 lancer \"{s.Name}\"", 6))}")
            .ToList();

        if (Environment.GetEnvironmentVariable("LUXIA_GOLDEN_UPDATE") == "1")
        {
            var source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "golden", "P5-scenes.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(source)!);
            File.WriteAllLines(source, actual);
            return;
        }

        File.ReadAllLines(GoldenFile).Where(l => l.Length > 0).ShouldBe(actual);
    }

    private static List<byte[]> Run(string script, double seconds)
    {
        var content = ProjectFiles.Load(Folder);
        var scenario = Scenario.Parse(script, content.Scenes, content.Layers, out var errors);
        errors.ShouldBeEmpty();
        var file = Path.Combine(Path.GetTempPath(), $"luxia-p5-{Guid.NewGuid():N}.dmxrec");
        try
        {
            ScenarioRunner.Run(content, scenario, TimeSpan.FromSeconds(seconds), file).Rejections.ShouldBeEmpty();
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
