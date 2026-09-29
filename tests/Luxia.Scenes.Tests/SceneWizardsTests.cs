using Luxia.Engine.Model;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;

namespace Luxia.Scenes.Tests;

/// <summary>Assistants de création de scènes (SCN-014).</summary>
public sealed class SceneWizardsTests
{
    private static readonly string[] ParNames = ["PAR 1", "PAR 2", "PAR 3", "PAR 4"];
    private static readonly string[] ColorNames = ["Rouge", "Vert", "Bleu", "Jaune"];
    private static readonly string[] LyreNames = ["Lyre 1", "Lyre 2"];
    private readonly ReferenceProject _project = new();

    private List<ValueTarget> Pars() => [.. ParNames.Select(n => ValueTarget.Fixture(_project.Fixture(n).Id))];

    [Fact]
    [Trait("Exigence", "SCN-014")]
    public void ColorChase_FourParsFourColors_FourSteps_ColorsShiftByOne()
    {
        var colors = ColorNames.Select(n => DefaultPalettes.Colors.Single(p => p.Name == n).Id).ToList();
        var steps = SceneWizards.ColorChase(Pars(), colors, Duration.FromBeats(1), Duration.Zero);

        steps.Count.ShouldBe(4);
        steps[0].Values.Where(v => v.PaletteId is not null).Select(v => v.PaletteId!.Value).ShouldBe(colors);
        steps[1].Values.Where(v => v.PaletteId is not null).Select(v => v.PaletteId!.Value).ShouldBe([colors[1], colors[2], colors[3], colors[0]]);
        steps.ShouldAllBe(s => s.Values.Count(v => v.Level == 1) == 4, "chaque étape allume les 4 PAR (MOT-041)");
        steps.ShouldAllBe(s => s.Hold == Duration.FromBeats(1));

        // Une scène faite des étapes générées se compile sans problème sur le parc.
        var scene = new Scene { Name = "Chenillard généré", LayerId = LayerSet.ColorsLayerId, Steps = steps };
        ShowCompiler.Compile(_project.Content(new SceneSet { Scenes = [scene] })).Issues.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "SCN-014")]
    public void Alternate_TwoSteps_Swapped()
    {
        var (a, b) = (Guid.NewGuid(), Guid.NewGuid());
        var steps = SceneWizards.Alternate(Pars(), a, b, Duration.FromSeconds(1), Duration.FromSeconds(0.2));
        steps.Count.ShouldBe(2);
        steps[0].Values.Where(v => v.PaletteId is not null).Select(v => v.PaletteId).ShouldBe([a, b, a, b]);
        steps[1].Values.Where(v => v.PaletteId is not null).Select(v => v.PaletteId).ShouldBe([b, a, b, a]);
        steps[0].Fade.ShouldBe(Duration.FromSeconds(0.2));
    }

    [Fact]
    [Trait("Exigence", "SCN-014")]
    public void PositionSweep_OneStepPerPalette_AllMembers()
    {
        var positions = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var lyres = LyreNames.Select(n => ValueTarget.Fixture(_project.Fixture(n).Id)).ToList();
        var steps = SceneWizards.PositionSweep(lyres, positions, Duration.FromSeconds(2), Duration.FromSeconds(1));
        steps.Count.ShouldBe(3);
        steps[2].Values.Where(v => v.PaletteId is not null).ShouldAllBe(v => v.PaletteId == positions[2]);
        SceneWizards.PositionSweep([], positions, Duration.Zero, Duration.Zero).ShouldBeEmpty();
    }
}
