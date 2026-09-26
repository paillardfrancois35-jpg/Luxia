using Luxia.Engine.Model;
using Luxia.Messaging.Commands;
using Luxia.Messaging.Events;
using static Luxia.Engine.Tests.ShowBuilder;

namespace Luxia.Engine.Tests;

/// <summary>T-MOT-02 : fondus croisés, fusion entre couches (HTP, LTP, 4 modes d'intensité), masters, solo.</summary>
public sealed class LayerMergeTests
{
    private readonly ShowBuilder _show = new();
    private readonly TestFixture _par;

    public LayerMergeTests()
    {
        _par = _show.Par7(1);
    }

    [Fact]
    [Trait("Exigence", "MOT-030")]
    public void CrossFade_AttributeInBothScenes_InterpolatesDirectly()
    {
        // Sous-jacent (couche basse) : rouge à 0 ; A : rouge à 1 ; B : rouge à 0,5. Au milieu du fondu croisé : 0,75, sans passer par 0.
        var layer = _show.Layer("Couleurs", 2, crossFade: 2);
        var a = _show.Scene("A", layer, Step(0, 60, V(_par["r"], 1)));
        var b = _show.Scene("B", layer, Step(0, 60, V(_par["r"], 0.5)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(a);
        engine.Tick();

        engine.Launch(b);
        engine.Tick();
        var values = new List<double>();
        for (var i = 0; i < 80; i++)
        {
            engine.Tick();
            values.Add(engine.Value(_par["r"]));
        }

        values[39].ShouldBe(0.75, 1e-9);
        values.ShouldAllBe(v => v >= 0.5 - 1e-9);
        values[^1].ShouldBe(0.5, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MOT-030")]
    public void CrossFade_AttributeOnlyInNewScene_FadesFromUnderlying()
    {
        var low = _show.Layer("Base", 1);
        var layer = _show.Layer("Couleurs", 2, crossFade: 2);
        var baseScene = _show.Scene("Vert de base", low, Step(0, 60, V(_par["g"], 0.2)));
        var a = _show.Scene("A", layer, Step(0, 60, V(_par["r"], 1)));
        var b = _show.Scene("B", layer, Step(0, 60, V(_par["g"], 1)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(baseScene);
        engine.Launch(a);
        engine.Tick();

        engine.Launch(b);
        engine.Tick();
        engine.Run(1);

        // Vert : de 0,2 (sous-jacent) vers 1 → 0,6 à mi-fondu. Rouge (seulement dans A) : de 1 vers le sous-jacent 0 → 0,5.
        engine.Value(_par["g"]).ShouldBe(0.6, 1e-9);
        engine.Value(_par["r"]).ShouldBe(0.5, 1e-9);

        engine.Run(1);
        engine.Value(_par["g"]).ShouldBe(1, 1e-9);
        engine.Value(_par["r"]).ShouldBe(0, 1e-9);
        engine.Playback(a).ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "GEN-011")]
    [Trait("Exigence", "COU-003")]
    public void TwoLaunches_InSameTick_SameExclusiveLayer_LastOneWins()
    {
        var layer = _show.Layer("Couleurs", 2);
        var a = _show.Scene("A", layer, Step(0, 60, V(_par["r"], 1)));
        var b = _show.Scene("B", layer, Step(0, 60, V(_par["b"], 1)));
        var engine = new EngineHarness(_show.Build());

        engine.Launch(a);
        engine.Launch(b);
        engine.Tick();

        engine.Playback(a).ShouldBeNull();
        engine.Playback(b).ShouldNotBeNull();
        engine.Value(_par["b"]).ShouldBe(1);
        engine.Value(_par["r"]).ShouldBe(0);
    }

    [Fact]
    [Trait("Exigence", "MOT-031")]
    public void NonIntensity_LtpByPriority_HighestLayerWins_EvenIfLaunchedFirst()
    {
        var low = _show.Layer("Bas", 1);
        var high = _show.Layer("Haut", 5);
        var red = _show.Scene("Haut rouge", high, Step(0, 60, V(_par["r"], 1)));
        var dim = _show.Scene("Bas rouge", low, Step(0, 60, V(_par["r"], 0.3)));
        var engine = new EngineHarness(_show.Build());

        engine.Launch(red);
        engine.Tick();
        engine.Launch(dim);
        engine.Tick();

        engine.Value(_par["r"]).ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "MOT-031")]
    public void NonIntensity_SamePriority_MostRecentWins()
    {
        var one = _show.Layer("Un", 2, exclusive: false);
        var a = _show.Scene("A", one, Step(0, 60, V(_par["r"], 1)));
        var b = _show.Scene("B", one, Step(0, 60, V(_par["r"], 0.4)));
        var engine = new EngineHarness(_show.Build());

        engine.Launch(a);
        engine.Tick();
        engine.Launch(b);
        engine.Tick();

        engine.Value(_par["r"]).ShouldBe(0.4);
        engine.Engine.Snapshot.Playbacks.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Exigence", "MOT-031")]
    public void Intensity_Htp_HighestContributionWins()
    {
        var low = _show.Layer("Intensité", 1);
        var high = _show.Layer("Couleurs", 2);
        var bright = _show.Scene("Plein", low, Step(0, 60, V(_par["dim"], 0.9)));
        var dim = _show.Scene("Faible", high, Step(0, 60, V(_par["dim"], 0.3)));
        var engine = new EngineHarness(_show.Build());

        engine.Launch(bright);
        engine.Launch(dim);
        engine.Tick();

        engine.Value(_par["dim"]).ShouldBe(0.9);
    }

    [Theory]
    [InlineData(IntensityMode.Priority, 0.3)]
    [InlineData(IntensityMode.Additive, 1.0)]
    [InlineData(IntensityMode.Multiplicative, 0.9 * 0.3)]
    [InlineData(IntensityMode.Htp, 0.9)]
    [Trait("Exigence", "MOT-031")]
    public void Intensity_FourModes(IntensityMode mode, double expected)
    {
        var low = _show.Layer("Intensité", 1);
        var high = _show.Layer("Modulation", 2, mode: mode);
        var bright = _show.Scene("Plein", low, Step(0, 60, V(_par["dim"], 0.9)));
        var modulation = _show.Scene("Modulation", high, Step(0, 60, V(_par["dim"], 0.3)));
        var engine = new EngineHarness(_show.Build());

        engine.Launch(bright);
        engine.Launch(modulation);
        engine.Tick();

        engine.Value(_par["dim"]).ShouldBe(expected, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MOT-033")]
    [Trait("Exigence", "CMD-013")]
    public void LayerMaster_ScalesIntensity_NotColors_UnlessOptionSet()
    {
        var layer = _show.Layer("Couleurs", 2);
        var all = _show.Layer("Tout", 3, masterOnAll: true);
        var scene = _show.Scene("Rouge", layer, Step(0, 60, V(_par["dim"], 1), V(_par["r"], 1)));
        var other = _show.Scene("Vert", all, Step(0, 60, V(_par["g"], 1)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Launch(other);

        engine.Send(new SetLayerMasterCommand(CommandOrigin.User, layer.Id, 0.5));
        engine.Send(new SetLayerMasterCommand(CommandOrigin.User, all.Id, 0.5));
        engine.Tick();

        engine.Value(_par["dim"]).ShouldBe(0.5);
        engine.Value(_par["r"]).ShouldBe(1);
        engine.Value(_par["g"]).ShouldBe(0.5);
    }

    [Fact]
    [Trait("Exigence", "SCN-034")]
    public void Solo_MasksOtherPlaybacks_UntilStopped()
    {
        var colors = _show.Layer("Couleurs", 2);
        var moves = _show.Layer("Intensité", 1);
        var red = _show.Scene("Rouge", colors, Step(0, 60, V(_par["r"], 1)));
        var dim = _show.Scene("Plein", moves, Step(0, 60, V(_par["dim"], 1)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(dim);
        engine.Tick();

        engine.Launch(red, solo: true);
        engine.Tick();
        engine.Value(_par["dim"]).ShouldBe(0);
        engine.Value(_par["r"]).ShouldBe(1);

        engine.Stop(red);
        engine.Tick();
        engine.Value(_par["dim"]).ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "PAL-005")]
    public void LoadShow_WhilePlaying_UpdatesRunningSceneValues()
    {
        var layer = _show.Layer("Couleurs", 2);
        var scene = _show.Scene("Palette", layer, Step(0, 60, V(_par["r"], 1)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();

        _show.Scene(scene with { Steps = [Step(0, 60, V(_par["r"], 0.2))] });
        engine.Engine.LoadShow(_show.Build());
        engine.Tick();
        engine.Tick();

        engine.Playback(scene).ShouldNotBeNull();
        engine.Value(_par["r"]).ShouldBe(0.2, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "GEN-012")]
    [Trait("Exigence", "MOT-101")]
    public async Task UnknownScene_IsRejected_WithEventAndLogEntry()
    {
        await using var bus = new EventBus();
        var rejected = new List<CommandRejected>();
        var started = new List<SceneStarted>();
        bus.Subscribe<CommandRejected>(rejected.Add);
        bus.Subscribe<SceneStarted>(started.Add);
        var layer = _show.Layer("Couleurs", 2);
        var scene = _show.Scene("Rouge", layer, Step(0, 60, V(_par["r"], 1)));
        var engine = new EngineHarness(_show.Build(), bus: bus);

        engine.Send(new LaunchSceneCommand(CommandOrigin.Midi, Guid.NewGuid()));
        engine.Launch(scene);
        engine.Tick();
        await bus.FlushAsync();

        rejected.Single().Reason.ShouldBe("scène inconnue");
        started.Single().SceneId.ShouldBe(scene.Id);
        var log = engine.Engine.CommandLog();
        log.ShouldContain(e => e.Rejection == "scène inconnue" && e.Command.Origin == CommandOrigin.Midi);
    }

    [Fact]
    [Trait("Exigence", "GEN-010")]
    [Trait("Exigence", "GEN-112")]
    public void CommandLog_KeepsReceptionTime_Origin_AndGroupsFaderMoves()
    {
        var engine = new EngineHarness(_show.Build());
        engine.Clock.Advance(TimeSpan.FromMilliseconds(10));
        for (var i = 0; i < 5; i++)
        {
            engine.Send(new OverrideAttributesCommand(CommandOrigin.User, [new AttributeValue(_par.Id, "dim", i / 10.0)]));
        }

        engine.Send(new BlackoutCommand(CommandOrigin.Midi, true));
        engine.Tick();

        var log = engine.Engine.CommandLog().TakeLast(2).ToList();
        log[0].Repeat.ShouldBe(5);
        log[0].ReceivedAt.ShouldBe(TimeSpan.FromMilliseconds(10));
        log[0].AppliedAt.ShouldBe(TimeSpan.FromMilliseconds(35));
        log[1].Command.Origin.ShouldBe(CommandOrigin.Midi);
    }
}
