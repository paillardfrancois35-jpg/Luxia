using Luxia.Core.Dmx;
using Luxia.Core.Time;
using Luxia.Engine;
using Luxia.Fixtures.Model;
using Luxia.Fixtures.Rules;
using Luxia.Messaging.Commands;
using Luxia.Patch.Model;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;

namespace Luxia.Scenes.Tests;

/// <summary>ERG-036, ERG-037 : de <c>groupes.json</c> aux dimmers du moteur, sur le parc réel.</summary>
public sealed class DimmerCompilerTests
{
    private readonly ReferenceProject _project = new();

    private ProjectContent WithGroups(FixtureGroupSet groups) =>
        _project.Content() with { Groups = groups };

    private static readonly string[] ParNames = ["PAR 1", "PAR 2", "PAR 3", "PAR 4"];

    private FixtureGroup Pars(FixtureGroup? parent = null, bool dimmer = true) =>
        new()
        {
            Name = "PAR scène",
            ParentId = parent?.Id,
            HasDimmer = dimmer,
            FixtureIds = [.. ParNames.Select(n => _project.Fixture(n).Id)],
        };

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public void Compile_WithoutGroups_GivesTheEngineNoGroup()
    {
        var result = ShowCompiler.Compile(_project.Content());

        result.Model.DimmerGroups.ShouldBeEmpty();
        result.Model.ParameterGroups.ShouldAllBe(g => g == -1);
        result.Issues.ShouldNotContain(i => i.File == "groupes.json");
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public void Compile_GroupsTree_GivesParentFirstAndTheGroupOfEachFixtureParameter()
    {
        var parc = new FixtureGroup { Name = "Parc", HasDimmer = true };
        var pars = Pars(parc);
        var result = ShowCompiler.Compile(WithGroups(new FixtureGroupSet { Groups = [pars, parc] }));
        var model = result.Model;

        model.DimmerGroups.Select(g => g.Name).ShouldBe(["Parc", "PAR scène"]);
        model.DimmerGroups[1].Parent.ShouldBe(0);
        var par1 = model.IndexOf(_project.Fixture("PAR 1").Id, "dim");
        var lyre = model.IndexOf(_project.Fixture("Lyre 1").Id, "dim");
        model.ParameterGroups[par1].ShouldBe(1);
        model.ParameterGroups[lyre].ShouldBe(-1);
        result.Issues.ShouldNotContain(i => i.File == "groupes.json");
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public void Compile_FaultyGroups_AreReportedAsWarnings_AndNeverBlock()
    {
        var pars = Pars() with { ParentId = Guid.NewGuid(), FixtureIds = [Guid.NewGuid(), .. Pars().FixtureIds] };

        var result = ShowCompiler.Compile(WithGroups(new FixtureGroupSet { Groups = [pars] }));

        result.Issues.Where(i => i.File == "groupes.json").ShouldAllBe(i => i.Severity == IssueSeverity.Warning);
        result.Issues.ShouldContain(i => i.File == "groupes.json" && i.Message.Contains("parent introuvable"));
        result.Issues.ShouldContain(i => i.File == "groupes.json" && i.Message.Contains("introuvable dans le patch"));
        result.Model.DimmerGroups.ShouldHaveSingleItem().Parent.ShouldBe(-1);
    }

    [Fact]
    [Trait("Exigence", "ERG-037")]
    public void EndToEnd_GroupDimmer_HalvesTheFourParsOnTheWire_LeavingTheLyreAlone()
    {
        var pars = Pars();
        var scene = new Scene
        {
            Name = "Plein",
            LayerId = LayerSet.ColorsLayerId,
            Steps =
            [
                new SceneStep
                {
                    Values =
                    [
                        new SceneValue { Target = ValueTarget.Fixture(_project.Fixture("PAR 1").Id), Attribute = AttributeKind.Intensity, Level = 1 },
                        new SceneValue { Target = ValueTarget.Fixture(_project.Fixture("Lyre 1").Id), Attribute = AttributeKind.Intensity, Level = 1 },
                    ],
                },
            ],
        };
        var content = _project.Content(new SceneSet { Scenes = [scene] }) with { Groups = new FixtureGroupSet { Groups = [pars] } };
        var sink = new LastFrameSink();
        var engine = new RenderEngine(sink, new VirtualClock(), seed: 1);
        var model = ShowCompiler.Compile(content).Model;
        engine.LoadShow(model);
        engine.Send(new LaunchSceneCommand(CommandOrigin.Tool, scene.Id));
        engine.Send(new SetGroupDimmerCommand(CommandOrigin.Tool, pars.Id, 0.5));
        engine.Tick();

        var par1 = _project.Fixture("PAR 1");
        sink.Frame[par1.Address - 1].ShouldBe((byte)128);
        engine.Snapshot.Values[model.IndexOf(par1.Id, "dim")].ShouldBe(0.5, 1e-9);
        engine.Snapshot.Values[model.IndexOf(_project.Fixture("Lyre 1").Id, "dim")].ShouldBe(1, 1e-9);
        engine.Snapshot.DimmerEffective.ShouldBe([0.5], 1e-9);
    }

    private sealed class LastFrameSink : IFrameSink
    {
        public byte[] Frame { get; } = new byte[DmxConstants.ChannelCount];

        public void Submit(int universe, DmxFrame frame, TimeSpan timestamp) => frame.ReadOnlyValues.CopyTo(Frame);
    }
}
