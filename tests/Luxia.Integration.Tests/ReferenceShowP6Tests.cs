using System.Security.Cryptography;
using Luxia.Hosting.Tools;
using Luxia.Output.Recording;

namespace Luxia.Integration.Tests;

/// <summary>
/// Non-régression du contenu P6 du show de référence (doc 41 §11, DEMO-3, DEMO-4) : scènes « Phase P6 » (effets)
/// rejouées et comparées à <c>tests/assets/golden/P6-scenes.txt</c> (régénérer avec <c>LUXIA_GOLDEN_UPDATE=1</c> après
/// un changement voulu et vérifié), et contrôles ciblés au plan d'adresses du parc réel.
/// </summary>
public sealed class ReferenceShowP6Tests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");
    private static readonly string GoldenFile = Path.Combine(AppContext.BaseDirectory, "golden", "P6-scenes.txt");

    [Fact]
    [Trait("Exigence", "GEN-131")]
    [Trait("Exigence", "PAL-010")]
    public void ReferenceShow_P6_ContentIsThere_AndValid()
    {
        var content = ProjectFiles.Load(Folder);
        content.Scenes.Scenes.Count(s => s.Category == "Phase P6").ShouldBe(10);
        content.Scenes.Scenes.Where(s => s.Category == "Phase P6").ShouldAllBe(s => s.Steps.All(step => step.Effects.Count > 0));
        content.Palettes.Palettes.Count(p => p.Kind == Scenes.Model.PaletteKind.Theme).ShouldBe(6);
        ProjectValidator.Validate(Folder).Where(i => i.Item.Contains("P6", StringComparison.Ordinal) || i.Field.StartsWith("effects", StringComparison.Ordinal)).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "EFF-002")]
    [Trait("Exigence", "EFF-005")]
    public void Wave_OnThePars_LeftToRight_QuarterCycleApart()
    {
        var frames = Run("0 lancer \"Vague sur les PAR (gauche → droite)\"", 0.5);

        // PAR 1 à 4 : gradateurs aux canaux 1, 8, 15, 22 ; vague de 10 à 100 % (les PAR ne s'éteignent pas, essai P6) ;
        // départ : bas, mi-course, haut, mi-course.
        ((double)frames[0][0]).ShouldBe(26, 1);
        ((double)frames[0][7]).ShouldBe(140, 1);
        frames[0][14].ShouldBe((byte)255);
        ((double)frames[0][21]).ShouldBe(140, 1);

        // Un demi-cycle plus tard (1 s), c'est l'inverse.
        var later = Run("0 lancer \"Vague sur les PAR (gauche → droite)\"", 1);
        later[^1][0].ShouldBe((byte)255);
        ((double)later[^1][14]).ShouldBe(26, 1);
        frames.Concat(later).ShouldAllBe(f => f[0] >= 25, "jamais éteint");
    }

    [Fact]
    [Trait("Exigence", "EFF-008")]
    public void SegmentChase_OneSectionOfTheBarsAtATime()
    {
        var frames = Run("0 lancer \"Chenillard des segments (on/off)\"", 1.6);

        // Barre 1 (51) : gradateurs de section aux canaux 51, 57, 63, 69 ; barre 2 (81) : 81, 87, 93, 99.
        int[] dimmers = [51, 57, 63, 69, 81, 87, 93, 99];
        for (var step = 0; step < 8; step++)
        {
            // Au milieu de chaque huitième de cycle (0,2 s), seule la section « step » est allumée.
            var frame = frames[(int)Math.Round(((step * 0.2) + 0.1) * 40)];
            dimmers.Select(d => frame[d - 1] > 0).ShouldBe(dimmers.Select((_, i) => i == step), $"huitième {step + 1}");
        }

        frames[0][52].ShouldBe((byte)255, "rouge de la section 1 (canal 53)");
    }

    [Fact]
    [Trait("Exigence", "EFF-008")]
    [Trait("Exigence", "EFF-004")]
    public void MultiHead_64Channels_HeadsShowDifferentColors()
    {
        var frame = Run("0 lancer \"Têtes décalées (effet multi-têtes)\"", 0.1)[0];

        // Effet multi-têtes en 64 canaux à l'adresse 181 : gradateur 188, têtes RVBW à partir de 193.
        frame[187].ShouldBe((byte)255);
        var heads = Enumerable.Range(0, 12).Select(h => (frame[192 + (h * 4)], frame[193 + (h * 4)], frame[194 + (h * 4)])).ToList();
        heads.Distinct().Count().ShouldBe(4, "thème Disco : 4 couleurs réparties sur les 12 têtes");
        heads[0].ShouldNotBe(heads[1]);
    }

    [Fact]
    [Trait("Exigence", "EFF-003")]
    public void Circle_AroundTrackCenter_SpansSixtyDegreesOfPan()
    {
        var frames = Run("0 lancer \"Cercle des lyres (autour de Piste centre)\"", 6);

        // Lyre 1 (111) : Pan 16 bits aux canaux 111-112 ; course Pan de la lyre dans sa définition.
        var pans = frames.Select(f => ((f[110] << 8) | f[111]) / 65535.0).ToList();
        var span = pans.Max() - pans.Min();
        var content = ProjectFiles.Load(Folder);
        var lyre = content.Installation.Fixtures.Single(f => f.Name == "Lyre 1");
        var range = content.TypeOf(lyre.FixtureTypeId)!.Physical.PanRange ?? Scenes.Compilation.EffectCompiler.DefaultPanRange;
        span.ShouldBe(60 / range, 0.01);
    }

    [Fact]
    [Trait("Exigence", "MOT-082")]
    [Trait("Exigence", "DEMO-4")]
    public void Trap_BigCircle_NeverCrossesTheForbiddenZone()
    {
        var frames = Run("0 lancer \"Piège : grand cercle de la lyre 1 (zone interdite)\"", 6);

        // Lyre 1 (111) : Pan 111-112, Tilt 113-114 en 16 bits. Zone interdite « Public » : Pan 30-70 %, Tilt 85-100 %.
        // Le cercle (Tilt jusqu'à 96 %) y entrerait ; la lyre est ramenée au bord le plus proche, jamais dedans.
        var points = frames.Select(f => (Pan: ((f[110] << 8) | f[111]) / 65535.0, Tilt: ((f[112] << 8) | f[113]) / 65535.0)).ToList();
        points.ShouldNotContain(p => p.Pan > 0.3 + 1e-3 && p.Pan < 0.7 - 1e-3 && p.Tilt > 0.85 + 1e-3);
        points.ShouldContain(p => Math.Abs(p.Tilt - 0.85) < 2e-3 && p.Pan > 0.35 && p.Pan < 0.65, "ramenée au bord bas de la zone quand elle y arrive par le milieu");
    }

    [Fact]
    [Trait("Exigence", "DEMO-3")]
    public void EveryP6Scene_ReplaysExactlyAsReference()
    {
        var content = ProjectFiles.Load(Folder);
        var actual = content.Scenes.Scenes
            .Where(s => s.Category == "Phase P6")
            .Select(s => $"{s.Name};{Hash(Run($"0 lancer \"{s.Name}\"", 6))}")
            .ToList();

        if (Environment.GetEnvironmentVariable("LUXIA_GOLDEN_UPDATE") == "1")
        {
            var source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "golden", "P6-scenes.txt"));
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
        var file = Path.Combine(Path.GetTempPath(), $"luxia-p6-{Guid.NewGuid():N}.dmxrec");
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
