using Luxia.Engine.Model;
using Luxia.Fixtures.Model;
using Luxia.Hosting.Tools;
using Luxia.Messaging.Commands;
using Luxia.Output.Recording;
using Luxia.Scenes;
using Luxia.Scenes.Model;

namespace Luxia.Integration.Tests;

/// <summary>Outils sans interface : validation d'un projet (GEN-131), déroulé d'une scène ou d'un scénario (GEN-132, MOT-103).</summary>
public sealed class HeadlessToolsTests : IDisposable
{
    private static readonly string Reference = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "luxia-headless-" + Guid.NewGuid().ToString("N"));

    public HeadlessToolsTests()
    {
        foreach (var file in Directory.EnumerateFiles(Reference, "*.json", SearchOption.AllDirectories))
        {
            var target = Path.Combine(_folder, Path.GetRelativePath(Reference, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    [Trait("Exigence", "GEN-131")]
    public void Validate_MissingPalette_GivesFileObjectAndField()
    {
        var par = ParId();
        SceneStore.Save(_folder, new SceneSet
        {
            Scenes =
            [
                new Scene
                {
                    Name = "Cassée",
                    LayerId = LayerSet.ColorsLayerId,
                    Steps = [new SceneStep { Values = [new SceneValue { Target = ValueTarget.Fixture(par), PaletteId = Guid.NewGuid() }] }],
                },
                new Scene { Name = "Sans suite", End = EndMode.Chain, Steps = [new SceneStep()] },
                new Scene { Name = "Hors bornes", Steps = [new SceneStep { Values = [new SceneValue { Target = ValueTarget.Fixture(par), Attribute = AttributeKind.Red, Level = 2 }] }] },
            ],
        });

        var issues = ProjectValidator.Validate(_folder).Select(i => i.ToString()).ToList();

        issues.ShouldContain("Avertissement – scènes.json – scène « Cassée », étape 1 – values[0] : palette introuvable");
        issues.ShouldContain(i => i.Contains("scène « Sans suite »", StringComparison.Ordinal) && i.Contains("chainSceneId", StringComparison.Ordinal));
        issues.ShouldContain(i => i.Contains("level 2 hors de 0 à 1", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Exigence", "GEN-131")]
    public void Validate_UnreadableFile_IsReported()
    {
        File.WriteAllText(Path.Combine(_folder, SceneStore.FileName), "{ pas du json");

        var issues = ProjectValidator.Validate(_folder);

        issues.ShouldContain(i => i.File == SceneStore.FileName && i.Severity == Fixtures.Rules.IssueSeverity.Error);
    }

    [Fact]
    [Trait("Exigence", "MOT-103")]
    public void Scenario_Parse_ResolvesNames_AndReportsBadLines()
    {
        var scenes = new SceneSet { Scenes = [new Scene { Name = "Blanc chaud" }] };
        var text = """
            # démonstration
            0     lancer "Blanc chaud"
            1,5   blackout oui
            2     grand-master 50
            3     arreter "Inconnue"
            4     danser
            6     fin
            """;

        var scenario = Scenario.Parse(text, scenes, LayerSet.Default(), out var errors);

        scenario.Steps.Select(s => s.Command.GetType().Name).ShouldBe(["LaunchSceneCommand", "BlackoutCommand", "SetGrandMasterCommand"]);
        ((SetGrandMasterCommand)scenario.Steps[2].Command).Level.ShouldBe(0.5);
        scenario.End.ShouldBe(TimeSpan.FromSeconds(6));
        errors.Count.ShouldBe(2);
        errors[0].ShouldStartWith("ligne 5 : scène inconnue « Inconnue »");
    }

    [Fact]
    [Trait("Exigence", "GEN-132")]
    [Trait("Exigence", "MOT-103")]
    public void Run_Scene_SummarizesWhoLightsUp_AndWritesReplayableRecording()
    {
        var par = ParId();
        var scene = new Scene
        {
            Name = "Rouge",
            LayerId = LayerSet.ColorsLayerId,
            Steps =
            [
                new SceneStep
                {
                    Fade = Duration.FromSeconds(1),
                    Values =
                    [
                        new SceneValue { Target = ValueTarget.Fixture(par), Color = new LogicalColor { R = 1 } },
                        new SceneValue { Target = ValueTarget.Fixture(par), Attribute = AttributeKind.Intensity, Level = 1 },
                    ],
                },
            ],
        };
        var content = ProjectFiles.Load(_folder) with { Scenes = new SceneSet { Scenes = [scene] } };
        var recording = Path.Combine(_folder, "essai.dmxrec");

        var report = ScenarioRunner.Run(content, Scenario.ForScene(scene.Id), TimeSpan.FromSeconds(2), recording);

        report.Rejections.ShouldBeEmpty();
        report.Frames.ShouldBe(81);
        report.Lines.ShouldContain(l => l.Contains("PAR 1 : rouge #FF0000 à 100 %", StringComparison.Ordinal));
        using var stream = File.OpenRead(recording);
        var (_, frames) = RecordingReader.ReadAll(stream);
        frames.Count.ShouldBe(81);
        frames[^1].Values[0].ShouldBe((byte)255);
        frames[^1].Values[1].ShouldBe((byte)255);
    }

    private Guid ParId() => ProjectFiles.Load(_folder).Installation.Fixtures.Single(f => f.Name == "PAR 1").Id;
}
