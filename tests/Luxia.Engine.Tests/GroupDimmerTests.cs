using Luxia.Engine.Model;
using Luxia.Messaging.Commands;
using static Luxia.Engine.Tests.ShowBuilder;

namespace Luxia.Engine.Tests;

/// <summary>ERG-037, CMD-031 : dimmers de groupe, règle proportionnelle le long de l'arbre, après la fusion des couches.</summary>
public sealed class GroupDimmerTests
{
    private readonly ShowBuilder _show = new();

    private static SetGroupDimmerCommand Level(DimmerGroup group, double level) => new(CommandOrigin.User, group.Id, level);

    [Fact]
    [Trait("Exigence", "ERG-037")]
    [Trait("Exigence", "CMD-031")]
    public void GroupDimmer_HalvesIntensity_NotColors()
    {
        var par = _show.Par7(1);
        var group = _show.Group("PAR");
        _show.Assign(par, group);
        var scene = _show.Scene("Rouge", _show.Layer("Tout", 1), Step(0, 1, V(par["dim"], 1), V(par["r"], 1)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);

        engine.Send(Level(group, 0.5));
        engine.Tick();

        engine[1].ShouldBe((byte)128);
        engine[2].ShouldBe((byte)255);
    }

    [Fact]
    [Trait("Exigence", "ERG-037")]
    public void Tree_EachStageMultiplies_SiblingsOnlySeeTheirParents()
    {
        // Parc 80 % → Face 50 % → PAR ; Parc 80 % → Lyres (pas de dimmer propre) ; UV hors de l'arbre.
        var par = _show.Par7(1);
        var lyre = _show.Par7(10, "Lyre");
        var uv = _show.Par7(20, "UV");
        var park = _show.Group("Parc");
        var face = _show.Group("Face", park);
        var lyres = _show.Group("Lyres", park, dimmer: false);
        _show.Assign(par, face);
        _show.Assign(lyre, lyres);
        var scene = _show.Scene("Plein", _show.Layer("Tout", 1), Step(0, 1, V(par["dim"], 1), V(lyre["dim"], 1), V(uv["dim"], 1)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);

        engine.Send(Level(park, 0.8));
        engine.Send(Level(face, 0.5));
        engine.Tick();

        engine.Value(par["dim"]).ShouldBe(0.4, 1e-9);
        engine.Value(lyre["dim"]).ShouldBe(0.8, 1e-9);
        engine.Value(uv["dim"]).ShouldBe(1, 1e-9);
        engine.Engine.Snapshot.DimmerEffective.ShouldBe([0.8, 0.4, 0.8], 1e-9);
    }

    [Fact]
    [Trait("Exigence", "ERG-037")]
    public void Proportional_KeepsTheShapeOfAnEffect_NotACeiling()
    {
        // Deux instants d'un scintillement (0,4 puis 0,8) : à 50 %, ils deviennent 0,2 et 0,4 (une règle « plafond » les aurait aplatis).
        var par = _show.Par7(1);
        var group = _show.Group("PAR");
        _show.Assign(par, group);
        var layer = _show.Layer("Tout", 1);
        var low = _show.Scene("Bas", layer, Step(0, 1, V(par["dim"], 0.4)));
        var high = _show.Scene("Haut", layer, Step(0, 1, V(par["dim"], 0.8)));
        var engine = new EngineHarness(_show.Build());
        engine.Send(Level(group, 0.5));
        engine.Launch(low);
        engine.Tick();
        var first = engine.Value(par["dim"]);
        engine.Launch(high);
        engine.Tick();

        first.ShouldBe(0.2, 1e-9);
        engine.Value(par["dim"]).ShouldBe(0.4, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "ERG-037")]
    [Trait("Exigence", "MOT-040")]
    public void VirtualIntensity_Rgb3Channels_FollowsTheGroupDimmer()
    {
        var par = _show.Par3(1);
        var group = _show.Group("PAR");
        _show.Assign(par, group);
        var scene = _show.Scene("Blanc", _show.Layer("Tout", 1), Step(0, 1, V(par[RigParameter.VirtualIntensityKey], 1), V(par["r"], 1), V(par["g"], 1), V(par["b"], 1)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);

        engine.Send(Level(group, 0.5));
        engine.Tick();

        new[] { engine[1], engine[2], engine[3] }.ShouldAllBe(v => v == 128);
    }

    [Fact]
    [Trait("Exigence", "ERG-037")]
    [Trait("Exigence", "MOT-071")]
    public void GroupDimmer_AndGrandMaster_Multiply()
    {
        var par = _show.Par7(1);
        var group = _show.Group("PAR");
        _show.Assign(par, group);
        var scene = _show.Scene("Plein", _show.Layer("Tout", 1), Step(0, 1, V(par["dim"], 1)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);

        engine.Send(Level(group, 0.5));
        engine.Send(new SetGrandMasterCommand(CommandOrigin.User, 0.5));
        engine.Tick();

        engine.Value(par["dim"]).ShouldBe(0.25, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "CMD-031")]
    public void UnknownGroup_OrGroupWithoutDimmer_IsRejected()
    {
        var group = _show.Group("Organisation", dimmer: false);
        _show.Par7(1);
        var engine = new EngineHarness(_show.Build());

        engine.Send(Level(group, 0.5));
        engine.Send(new SetGroupDimmerCommand(CommandOrigin.User, Guid.NewGuid(), 0.5));
        engine.Tick();

        var log = engine.Engine.CommandLog();
        log[^2].Rejection.ShouldBe("ce groupe n'a pas de dimmer");
        log[^1].Rejection.ShouldBe("groupe inconnu");
    }

    [Fact]
    [Trait("Exigence", "CMD-031")]
    public void Level_IsClamped_AndKeptWhenTheShowIsReloaded()
    {
        var par = _show.Par7(1);
        var group = _show.Group("PAR");
        _show.Assign(par, group);
        var scene = _show.Scene("Plein", _show.Layer("Tout", 1), Step(0, 1, V(par["dim"], 1)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Send(Level(group, 7));
        engine.Tick();
        engine.Value(par["dim"]).ShouldBe(1, 1e-9);

        engine.Send(Level(group, 0.3));
        engine.Tick();
        engine.Engine.LoadShow(_show.Build());
        engine.Tick();

        engine.Engine.Snapshot.DimmerLevels.ShouldBe([0.3], 1e-9);
        engine.Value(par["dim"]).ShouldBe(0.3, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "ERG-037")]
    public void Blackout_StillWins_AndUngroupedFixturesAreUntouched()
    {
        var par = _show.Par7(1);
        var other = _show.Par7(10);
        var group = _show.Group("PAR");
        _show.Assign(par, group);
        var scene = _show.Scene("Plein", _show.Layer("Tout", 1), Step(0, 1, V(par["dim"], 1), V(other["dim"], 1)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Send(Level(group, 0.5));
        engine.Tick();
        engine.Value(other["dim"]).ShouldBe(1, 1e-9);

        engine.Send(new BlackoutCommand(CommandOrigin.User, true));
        engine.Tick();

        engine.Value(par["dim"]).ShouldBe(0, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public void ShowModel_ParentAfterChild_IsRefused()
    {
        var child = new DimmerGroup(Guid.NewGuid(), "Enfant", 1, true);
        var parent = new DimmerGroup(Guid.NewGuid(), "Parent", -1, true);

        Should.Throw<ArgumentException>(() => new ShowModel([], dimmerGroups: [child, parent]));
    }
}
