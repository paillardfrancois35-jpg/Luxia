using Luxia.Engine.Model;
using Luxia.Fixtures.Model;
using Luxia.Patch.Model;
using Luxia.Patch.Rules;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;

namespace Luxia.Scenes.Tests;

/// <summary>Compilation d'un projet vers le moteur (D26) : paramètres du parc réel, cibles, palettes, problèmes signalés.</summary>
public sealed class ShowCompilerTests
{
    private readonly ReferenceProject _project = new();

    [Fact]
    [Trait("Exigence", "MOT-090")]
    public void Parameters_OfReferenceRig_HaveRolesAddressesAnd16Bits()
    {
        var model = ShowCompiler.Compile(_project.Content()).Model;
        var par = _project.Fixture("PAR 1").Id;
        var lyre = _project.Fixture("Lyre 1").Id;

        var dim = model.Parameters[model.IndexOf(par, "dim")];
        dim.Role.ShouldBe(ParameterRole.Intensity);
        dim.Outputs.ShouldBe([new ChannelAddress(1, 1)]);

        var red = model.Parameters[model.IndexOf(par, "r")];
        red.Role.ShouldBe(ParameterRole.Emitter);
        red.IntensitySource.ShouldBe(-1);

        // Lyre 11 canaux : Pan grossier à 111, fin à 112 ; défaut au centre ; roue de couleur discrète.
        var pan = model.Parameters[model.IndexOf(lyre, "pan")];
        pan.Outputs.ShouldBe([new ChannelAddress(1, 111, 112)]);
        pan.Default.ShouldBe(128 / 255.0, 1e-9);
        model.Parameters[model.IndexOf(lyre, "color")].Discrete.ShouldBeTrue();
        // Q28 : le Strobe du LPC008S a une plage fixe « Pas de strobe » (0-4) : il devient discret, comme celui de l'UV.
        model.Parameters[model.IndexOf(par, "strobe")].Discrete.ShouldBeTrue();
        model.Parameters[model.IndexOf(par, "fn")].Discrete.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "MOT-040")]
    [Trait("Exigence", "BIB-006")]
    public void Par3Channels_GetsVirtualIntensity_ThatItsEmittersFollow()
    {
        var par = _project.Fixture("PAR 1");
        _project.Installation = _project.Installation with
        {
            Fixtures = [.. _project.Installation.Fixtures.Select(f => f.Id == par.Id ? f with { ModeName = "3 canaux" } : f)],
        };

        var model = ShowCompiler.Compile(_project.Content()).Model;

        var dimmer = model.IndexOf(par.Id, RigParameter.VirtualIntensityKey);
        dimmer.ShouldBeGreaterThanOrEqualTo(0);
        model.Parameters[dimmer].IsVirtual.ShouldBeTrue();
        model.Parameters[model.IndexOf(par.Id, "r")].IntensitySource.ShouldBe(dimmer);
    }

    [Fact]
    [Trait("Exigence", "MOT-092")]
    public void Twins_ShareParametersOfFirstFixture()
    {
        var group = Guid.NewGuid();
        var twin = _project.Fixture("PAR 1") with { Id = Guid.NewGuid(), Name = "PAR 1 bis", TwinGroupId = group };
        _project.Installation = _project.Installation with
        {
            Fixtures = [.. _project.Installation.Fixtures.Select(f => f.Name == "PAR 1" ? f with { TwinGroupId = group } : f), twin],
        };

        var model = ShowCompiler.Compile(_project.Content()).Model;

        model.IndexOf(twin.Id, "dim").ShouldBe(model.IndexOf(_project.Fixture("PAR 1").Id, "dim"));
        model.Aliases[twin.Id].ShouldBe(_project.Fixture("PAR 1").Id);
    }

    [Fact]
    [Trait("Exigence", "MOT-091")]
    public void AbsentFixture_InActiveVenue_IsMarkedAbsent()
    {
        var lyre = _project.Fixture("Lyre 2");
        var venue = _project.Venues.Active;
        _project.Venues = _project.Venues with
        {
            Venues = [.. _project.Venues.Venues.Select(v => v.Id != venue.Id ? v : v with
            {
                Placements = [.. v.Placements.Select(p => p.FixtureId == lyre.Id ? p with { Absent = true } : p)],
            })],
        };

        var model = ShowCompiler.Compile(_project.Content()).Model;

        model.Parameters.Where(p => p.FixtureId == lyre.Id).ShouldAllBe(p => p.Absent);
        model.Parameters.Where(p => p.FixtureId == _project.Fixture("Lyre 1").Id).ShouldAllBe(p => !p.Absent);
    }

    [Fact]
    [Trait("Exigence", "SCN-007")]
    public void AutoSelection_ByCategory_IncludesFixturePatchedLater()
    {
        var value = new SceneValue
        {
            Target = new ValueTarget { Auto = new AutoSelectionTarget(AutoSelectionKind.ByCategory, FixtureCategory.Par) },
            Attribute = AttributeKind.Intensity,
            Level = 1,
        };
        var scene = new Scene { Name = "Tous les PAR", LayerId = LayerSet.IntensityLayerId, Steps = [new SceneStep { Values = [value] }] };
        var scenes = new SceneSet { Scenes = [scene] };

        var before = ShowCompiler.Compile(_project.Content(scenes)).Model.Scenes[0].Steps[0].Values.Count;

        var extra = _project.Fixture("PAR 4") with { Id = Guid.NewGuid(), Name = "PAR 5", Address = 200 };
        _project.Installation = _project.Installation with { Fixtures = [.. _project.Installation.Fixtures, extra] };
        var after = ShowCompiler.Compile(_project.Content(scenes)).Model;

        // 4 LPC008S + 2 gros PAR, puis le PAR ajouté : sa valeur apparaît sans toucher à la scène.
        before.ShouldBe(6);
        after.Scenes[0].Steps[0].Values.Count.ShouldBe(7);
        after.Scenes[0].Steps[0].Values.ShouldContain(v => v.Parameter == after.IndexOf(extra.Id, "dim"));
    }

    [Fact]
    [Trait("Exigence", "SCN-007")]
    [Trait("Exigence", "SCN-010")]
    public void ManualSelection_KeepsItsOrder_ForTheFan()
    {
        var names = new[] { "PAR 4", "PAR 3", "PAR 2", "PAR 1" };
        var selection = new Selection { Name = "Droite → gauche", Items = [.. names.Select(n => new SelectionItem(_project.Fixture(n).Id))] };
        _project.Installation = _project.Installation with { Selections = [selection] };
        var value = new SceneValue
        {
            Target = ValueTarget.Selection(selection.Id),
            Attribute = AttributeKind.Red,
            Level = 1,
            Spread = Duration.FromSeconds(3),
        };
        var scene = new Scene { Name = "Vague", LayerId = LayerSet.ColorsLayerId, Steps = [new SceneStep { Values = [value] }] };

        var model = ShowCompiler.Compile(_project.Content(new SceneSet { Scenes = [scene] })).Model;

        var delays = names.Select(n => model.Scenes[0].Steps[0].Values.Single(v => v.Parameter == model.IndexOf(_project.Fixture(n).Id, "r")).Delay.Value);
        delays.ShouldBe([0.0, 1.0, 2.0, 3.0]);
    }

    [Fact]
    [Trait("Exigence", "SCN-008")]
    [Trait("Exigence", "PAL-002")]
    public void PaletteReference_IsTranslatedPerFixture_AndModelSpecificValueWins()
    {
        var amber = DefaultPalettes.Colors.Single(p => p.Name == "Ambre");
        var bigPar = _project.Fixture("Gros PAR 1");
        var refined = amber with
        {
            Values =
            [
                new PaletteValue { FixtureTypeId = bigPar.FixtureTypeId, Attribute = AttributeKind.Red, Level = 1 },
                new PaletteValue { FixtureTypeId = bigPar.FixtureTypeId, Attribute = AttributeKind.Green, Level = 0.4 },
                new PaletteValue { FixtureTypeId = bigPar.FixtureTypeId, Attribute = AttributeKind.White, Level = 0.2 },
            ],
        };
        var palettes = new PaletteSet { Palettes = [refined] };
        var value = new SceneValue { Target = new ValueTarget { Auto = new AutoSelectionTarget(AutoSelectionKind.AllFixtures) }, PaletteId = amber.Id };
        var scene = new Scene { Name = "Ambre", LayerId = LayerSet.ColorsLayerId, Steps = [new SceneStep { Values = [value] }] };

        var model = ShowCompiler.Compile(_project.Content(new SceneSet { Scenes = [scene] }, palettes)).Model;
        var values = model.Scenes[0].Steps[0].Values.ToDictionary(v => v.Parameter, v => v.Value);
        double Level(string fixture, string key) => values[model.IndexOf(_project.Fixture(fixture).Id, key)];

        Level("PAR 1", "r").ShouldBe(1);
        Level("PAR 1", "g").ShouldBe(0.5);
        Level("Gros PAR 1", "g").ShouldBe(0.4);
        Level("Gros PAR 1", "w").ShouldBe(0.2);
        (Level("Lyre 1", "color") * 255).ShouldBeInRange(32, 47);
        values.ShouldNotContainKey(model.IndexOf(_project.Fixture("UV 1").Id, "uv1"));
    }

    [Fact]
    [Trait("Exigence", "SCN-007")]
    public void FixtureValue_WinsOverSelectionValue_WhateverTheOrder()
    {
        var par = _project.Fixture("PAR 2").Id;
        var scene = new Scene
        {
            Name = "Précis",
            LayerId = LayerSet.ColorsLayerId,
            Steps =
            [
                new SceneStep
                {
                    Values =
                    [
                        ReferenceProject.Value(par, AttributeKind.Red, 0.2),
                        new SceneValue { Target = new ValueTarget { Auto = new AutoSelectionTarget(AutoSelectionKind.AllFixtures) }, Attribute = AttributeKind.Red, Level = 1 },
                    ],
                },
            ],
        };

        var model = ShowCompiler.Compile(_project.Content(new SceneSet { Scenes = [scene] })).Model;

        model.Scenes[0].Steps[0].Values.Single(v => v.Parameter == model.IndexOf(par, "r")).Value.ShouldBe(0.2);
        model.Scenes[0].Steps[0].Values.Single(v => v.Parameter == model.IndexOf(_project.Fixture("PAR 1").Id, "r")).Value.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "GEN-131")]
    public void MissingPalette_IsReported_WithFileObjectAndField()
    {
        var scene = new Scene
        {
            Name = "Cassée",
            LayerId = LayerSet.ColorsLayerId,
            Steps = [new SceneStep { Values = [new SceneValue { Target = ValueTarget.Fixture(_project.Fixture("PAR 1").Id), PaletteId = Guid.NewGuid() }] }],
        };

        var issues = ShowCompiler.Compile(_project.Content(new SceneSet { Scenes = [scene] })).Issues;

        var issue = issues.Single();
        issue.File.ShouldBe("scènes.json");
        issue.Item.ShouldBe("scène « Cassée », étape 1");
        issue.Field.ShouldBe("values[0]");
        issue.Message.ShouldBe("palette introuvable");
    }

    [Fact]
    [Trait("Exigence", "INST-021")]
    public void SwapPanTilt_SendsPanValueToTiltChannel()
    {
        var lyre = _project.Fixture("Lyre 1");
        _project.Installation = _project.Installation with
        {
            Fixtures = [.. _project.Installation.Fixtures.Select(f => f.Id == lyre.Id ? f with { Options = new FixtureOptions { SwapPanTilt = true } } : f)],
        };
        var scene = new Scene { Name = "Pan", LayerId = LayerSet.MovementsLayerId, Steps = [new SceneStep { Values = [ReferenceProject.Value(lyre.Id, AttributeKind.Pan, 0.3)] }] };

        var model = ShowCompiler.Compile(_project.Content(new SceneSet { Scenes = [scene] })).Model;

        model.Scenes[0].Steps[0].Values.Single().Parameter.ShouldBe(model.IndexOf(lyre.Id, "tilt"));
    }
}
