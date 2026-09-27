using Luxia.Engine.Model;
using Luxia.Fixtures.Model;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;

namespace Luxia.Scenes.Tests;

/// <summary>T-PAL-03, SCN-013, SC-03 et fichiers <c>scènes.json</c>, <c>palettes.json</c>, <c>couches.json</c>.</summary>
public sealed class SceneUsageAndStoreTests : IDisposable
{
    private readonly ReferenceProject _project = new();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "luxia-scenes-" + Guid.NewGuid().ToString("N"));

    public SceneUsageAndStoreTests() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    [Trait("Exigence", "PAL-006")]
    public void PaletteUsage_ListsSteps_AndFreezeReplacesReferenceByValue()
    {
        var red = DefaultPalettes.Colors.Single(p => p.Name == "Rouge");
        var target = ValueTarget.Fixture(_project.Fixture("PAR 1").Id);
        var scenes = new SceneSet
        {
            Scenes =
            [
                new Scene
                {
                    Name = "Chase",
                    Steps =
                    [
                        new SceneStep { Values = [new SceneValue { Target = target, PaletteId = red.Id }] },
                        new SceneStep { Values = [ReferenceProject.Value(target.FixtureId!.Value, AttributeKind.Blue, 1)] },
                        new SceneStep { Values = [new SceneValue { Target = target, PaletteId = red.Id, Fade = Duration.FromSeconds(2) }] },
                    ],
                },
            ],
        };

        SceneUsage.PaletteUsages(scenes, red.Id).ShouldBe(["Scène « Chase », étape 1", "Scène « Chase », étape 3"]);

        var frozen = SceneUsage.FreezePalette(scenes, red, _project.Patch);

        SceneUsage.PaletteUsages(frozen, red.Id).ShouldBeEmpty();
        var value = frozen.Scenes[0].Steps[2].Values.Single();
        value.Color.ShouldBe(red.Light);
        value.Fade.ShouldBe(Duration.FromSeconds(2));
    }

    [Fact]
    [Trait("Exigence", "SCN-013")]
    public void SceneUsage_ListsScenesThatChainToIt()
    {
        var target = new Scene { Name = "Final" };
        var chaining = new Scene { Name = "Montée", Loop = LoopMode.Once, End = EndMode.Chain, ChainSceneId = target.Id };

        SceneUsage.SceneUsages(new SceneSet { Scenes = [target, chaining] }, target.Id).ShouldBe(["Scène « Montée » (enchaînement en fin de scène)"]);
    }

    [Fact]
    [Trait("Exigence", "SC-03")]
    public void ModeChange_ReportsLostAttributes_AndKeepsTheRest()
    {
        var par = _project.Fixture("PAR 1");
        var scenes = Enumerable.Range(1, 3).Select(i => new Scene
        {
            Name = $"Scène {i}",
            Steps = [new SceneStep { Values = [ReferenceProject.Value(par.Id, AttributeKind.Red, 1), ReferenceProject.Value(par.Id, AttributeKind.Shutter, 0.5)] }],
        }).ToList();
        var info = _project.Patch.Find(par.Id)!;
        var threeChannels = info.Type.Modes.Single(m => m.Name == "3 canaux");

        var impact = SceneUsage.ModeChangeImpact(new SceneSet { Scenes = scenes }, info, threeChannels);

        impact.Count.ShouldBe(3);
        impact[0].ShouldBe("Scène « Scène 1 », étape 1 : « Strobe / Obturateur » n'existe pas dans le mode « 3 canaux »");
    }

    [Fact]
    [Trait("Exigence", "INST-016")]
    public void ModeChange_CountsValuesOnASelectionContainingTheFixture()
    {
        var par = _project.Fixture("PAR 1");
        var auto = new ValueTarget { Auto = new AutoSelectionTarget(Patch.Rules.AutoSelectionKind.ByModel, Model: "Betopper LPC008S") };
        var scenes = new SceneSet { Scenes = [new Scene { Name = "Strobe", Steps = [new SceneStep { Values = [new SceneValue { Target = auto, Attribute = AttributeKind.Shutter, Level = 0.5 }] }] }] };
        var patch = _project.Patch;
        var info = patch.Find(par.Id)!;

        SceneUsage.ModeChangeImpact(scenes, info, info.Type.Modes.Single(m => m.Name == "3 canaux")).ShouldBeEmpty();
        SceneUsage.ModeChangeImpact(scenes, info, info.Type.Modes.Single(m => m.Name == "3 canaux"), patch).ShouldHaveSingleItem();
    }

    [Fact]
    [Trait("Exigence", "GEN-050")]
    [Trait("Exigence", "SCN-001")]
    public void Stores_RoundTrip_AllValueForms()
    {
        var scene = new Scene
        {
            Name = "Tout",
            Color = "#FF0000",
            Icon = "★",
            Category = "Phase P4",
            LayerId = LayerSet.ColorsLayerId,
            Loop = LoopMode.Count,
            LoopCount = 3,
            FadeOut = Duration.FromBeats(2),
            Steps =
            [
                new SceneStep
                {
                    Name = "Une",
                    Fade = Duration.FromSeconds(1.5),
                    Hold = new Duration(1, DurationUnit.Bars),
                    Curve = FadeCurve.SCurve,
                    Values =
                    [
                        new SceneValue { Target = ValueTarget.Fixture(Guid.NewGuid(), 2), Attribute = AttributeKind.Red, Level = 0.5 },
                        new SceneValue { Target = new ValueTarget { Auto = new AutoSelectionTarget(Patch.Rules.AutoSelectionKind.ByCategory, FixtureCategory.MovingHead) }, Channel = "gobo", Range = new RangeValue(8, 15) },
                        new SceneValue { Target = ValueTarget.Selection(Guid.NewGuid()), Color = new LogicalColor { R = 1, Uv = 0.5 }, Spread = Duration.FromSeconds(2) },
                        new SceneValue { Target = ValueTarget.Fixture(Guid.NewGuid()), PaletteId = Guid.NewGuid() },
                    ],
                },
            ],
        };

        SceneStore.Save(_folder, new SceneSet { Scenes = [scene] });
        var (loaded, message) = SceneStore.Load(_folder);

        message.ShouldBeNull();
        var back = loaded.Scenes.Single();
        back.ShouldBe(scene with { Steps = back.Steps });
        back.Steps[0].Values.ShouldBe(scene.Steps[0].Values);
        back.Steps[0].Hold.ShouldBe(scene.Steps[0].Hold);
        var text = File.ReadAllText(Path.Combine(_folder, SceneStore.FileName));
        text.ShouldContain("\"loop\": \"count\"");
        text.ShouldNotContain("\"hex\"");
        text.ShouldNotContain("\"dmx\"");
    }

    [Fact]
    [Trait("Exigence", "PAL-009")]
    [Trait("Exigence", "COU-006")]
    public void MissingFiles_GiveDefaultPalettesAndLayers()
    {
        var (palettes, _) = PaletteStore.Load(_folder);
        var (layers, _) = LayerStore.Load(_folder);

        palettes.Palettes.Where(p => p.Kind == PaletteKind.Color).Select(p => p.Name).ShouldBe(
            ["Blanc", "Blanc chaud", "Rouge", "Orange", "Ambre", "Jaune", "Vert", "Cyan", "Bleu", "Lavande", "Magenta", "Rose", "UV"]);
        layers.Layers.Select(l => (l.Name, l.Priority)).ShouldBe(
            [("Intensité", 1), ("Couleurs", 2), ("Mouvements", 3), ("Faisceau", 4), ("Effets", 5), ("Ambiance", 6), ("Libre", 7), ("Flashs", 99)]);
        layers.Layers.Single(l => l.Name == "Flashs").IntensityMode.ShouldBe(IntensityMode.Priority);
    }

    [Fact]
    [Trait("Exigence", "ERG-008")]
    public void DefaultLayers_HaveEightNonFlashAndFlash_WithFreeLayerWithoutFamilies()
    {
        var layers = LayerSet.Default().Layers;

        // Huit colonnes : une par fader de couche de l'APC mini (faders 1 à 8).
        layers.Count.ShouldBe(8);
        var free = layers.Single(l => l.Id == LayerSet.FreeLayerId);
        free.Name.ShouldBe("Libre");
        free.Families.ShouldBeEmpty("aucune famille attendue : rien n'y est signalé « hors famille »");
        layers.OrderBy(l => l.Priority).Last().Id.ShouldBe(LayerSet.FlashLayerId);
    }
}
