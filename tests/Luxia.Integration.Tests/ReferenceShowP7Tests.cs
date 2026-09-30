using System.Security.Cryptography;
using Luxia.Hosting.Tools;
using Luxia.Output.Recording;

namespace Luxia.Integration.Tests;

/// <summary>
/// Non-régression du contenu P7 du show de référence (doc 41 §11, DEMO-3, DEMO-4) : scènes « Phase P7 » (au rythme)
/// rejouées à 120 BPM fixes et comparées à <c>tests/assets/golden/P7-scenes.txt</c> (régénérer avec
/// <c>LUXIA_GOLDEN_UPDATE=1</c> après un changement voulu et vérifié), et contrôles ciblés au plan d'adresses du parc réel.
/// </summary>
public sealed class ReferenceShowP7Tests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");
    private static readonly string GoldenFile = Path.Combine(AppContext.BaseDirectory, "golden", "P7-scenes.txt");

    [Fact]
    [Trait("Exigence", "GEN-131")]
    [Trait("Exigence", "MOT-017")]
    public void ReferenceShow_P7_ContentIsThere_AndValid()
    {
        var content = ProjectFiles.Load(Folder);
        content.Scenes.Scenes.Count(s => s.Category == "Phase P7").ShouldBe(7);
        content.Scenes.Scenes.Single(s => s.Name == "Calibration de latence").Advance.ShouldBe(Engine.Model.StepAdvanceMode.Beat);
        content.Scenes.Scenes.Single(s => s.Name == "Départ à la mesure (blanc chaud)").Quantize.ShouldBe(Engine.Model.LaunchQuantize.Bar);
        content.Scenes.Scenes.Single(s => s.Name == "Mouvement lent à 30 BPM (horloge propre)").OwnBpm.ShouldBe(30);
        ProjectValidator.Validate(Folder).Where(i => i.Item.Contains("P7", StringComparison.Ordinal)).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "MOT-017")]
    public void OnePerBeat_LightsOneParPerBeat_AtTheClockTempo()
    {
        var frames = Run("0 lancer \"Un PAR par temps (chenillard au tempo)\"", 2.2);

        // PAR 1 à 4 : gradateurs aux canaux 1, 8, 15, 22. À 120 BPM, un temps = 0,5 s.
        int[] dimmers = [1, 8, 15, 22];
        for (var beat = 0; beat < 4; beat++)
        {
            var frame = frames[(int)Math.Round(((beat * 0.5) + 0.25) * 40)];
            dimmers.Select(d => frame[d - 1] > 0).ShouldBe(dimmers.Select((_, i) => i == beat), $"temps {beat + 1}");
        }
    }

    [Fact]
    [Trait("Exigence", "MOT-016")]
    [Trait("Exigence", "CMD-041")]
    public void OnePerBeat_FollowsAScenarioTempoChange()
    {
        // À 60 BPM un temps dure 1 s : le second PAR s'allume à 1 s, pas à 0,5 s.
        var frames = Run("0 tempo 60\n0 lancer \"Un PAR par temps (chenillard au tempo)\"", 1.6);

        frames[(int)(0.75 * 40)][7].ShouldBe((byte)0, "PAR 2 pas encore allumé à 0,75 s");
        frames[(int)(1.25 * 40)][7].ShouldBeGreaterThan((byte)0, "PAR 2 allumé à 1,25 s");
    }

    [Fact]
    [Trait("Exigence", "MOT-018")]
    public void QuantizedStart_WaitsForTheNextBar()
    {
        // Lancée à 0,7 s (pendant la première mesure) : elle ne démarre qu'à 2,0 s.
        var frames = Run("0.7 lancer \"Départ à la mesure (blanc chaud)\"", 2.6);

        frames[(int)(1.9 * 40)][0].ShouldBe((byte)0, "rien avant la mesure suivante");
        frames[(int)(2.3 * 40)][0].ShouldBeGreaterThan((byte)0, "allumée après le début de la mesure");
    }

    [Fact]
    [Trait("Exigence", "MOT-020")]
    public void SlowMovement_FollowsItsOwnTempo_WhateverTheMainClock()
    {
        // Un cercle en 4 temps à 30 BPM = 8 s ; la même scène avec le tempo principal à 240 BPM ne change pas.
        var slow = Run("0 lancer \"Mouvement lent à 30 BPM (horloge propre)\"", 4);
        var fastMain = Run("0 tempo 240\n0 lancer \"Mouvement lent à 30 BPM (horloge propre)\"", 4);

        Hash(slow).ShouldBe(Hash(fastMain));
    }

    [Fact]
    [Trait("Exigence", "DEMO-3")]
    public void EveryP7Scene_ReplaysExactlyAsReference()
    {
        var content = ProjectFiles.Load(Folder);
        var actual = content.Scenes.Scenes
            .Where(s => s.Category == "Phase P7")
            .Select(s => $"{s.Name};{Hash(Run($"0 lancer \"{s.Name}\"", 6))}")
            .ToList();

        if (Environment.GetEnvironmentVariable("LUXIA_GOLDEN_UPDATE") == "1")
        {
            var source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "golden", "P7-scenes.txt"));
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
        var file = Path.Combine(Path.GetTempPath(), $"luxia-p7-{Guid.NewGuid():N}.dmxrec");
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
