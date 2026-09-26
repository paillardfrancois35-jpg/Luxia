using Luxia.Engine.Model;
using Luxia.Messaging.Commands;
using static Luxia.Engine.Tests.ShowBuilder;

namespace Luxia.Engine.Tests;

/// <summary>T-MOT-01 : étapes, fondus, courbes, boucles, fins, vitesse — en temps virtuel (GEN-033).</summary>
public sealed class ScenePlaybackTests
{
    private readonly ShowBuilder _show = new();
    private readonly TestFixture _par;
    private readonly TestFixture _lyre;
    private readonly EngineLayer _layer;

    public ScenePlaybackTests()
    {
        _par = _show.Par7(1);
        _lyre = _show.Lyre(111);
        _layer = _show.Layer("Tout", 1);
    }

    [Fact]
    [Trait("Exigence", "MOT-010")]
    public void Step_FadeOneSecond_HoldTwo_NextStepAtThreeSeconds()
    {
        var scene = _show.Scene("Chase", _layer, Step(1, 2, V(_par["r"], 1)), Step(1, 2, V(_par["g"], 1)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();

        engine.Run(3 - 0.025);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(0);

        engine.Tick();
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "MOT-011")]
    [Trait("Exigence", "GEN-032")]
    public void LinearFade_ZeroToFullInTwoSeconds_EightyRegularSteps()
    {
        var scene = _show.Scene("Montée", _layer, Step(2, 10, V(_par["dim"], 1)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();

        var values = new List<double> { engine.Value(_par["dim"]) };
        for (var i = 0; i < 80; i++)
        {
            engine.Tick();
            values.Add(engine.Value(_par["dim"]));
        }

        values[0].ShouldBe(0, 1e-9);
        for (var i = 1; i <= 80; i++)
        {
            (values[i] - values[i - 1]).ShouldBe(1.0 / 80, 1e-9);
        }

        values[80].ShouldBe(1, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "GEN-032")]
    public void IrregularTicks_FadeStillEndsOnTime()
    {
        var scene = _show.Scene("Montée", _layer, Step(2, 10, V(_par["dim"], 1)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();

        // Ticks irréguliers (10 ms à 90 ms) : le fondu suit le temps écoulé réel, pas le nombre de ticks.
        var random = new Random(3);
        var elapsed = 0.0;
        while (elapsed < 1.9)
        {
            var dt = 0.01 + (random.NextDouble() * 0.08);
            engine.Clock.Advance(TimeSpan.FromSeconds(dt));
            engine.Engine.Tick();
            elapsed += dt;
        }

        engine.Value(_par["dim"]).ShouldBe(elapsed / 2, 1e-5);
        engine.Clock.Advance(TimeSpan.FromSeconds(2 - elapsed));
        engine.Engine.Tick();
        engine.Value(_par["dim"]).ShouldBe(1, 1e-5);
    }

    [Fact]
    [Trait("Exigence", "MOT-011")]
    public void SCurve_IsSmoothAtBothEnds_AndCrossesHalfWayInTheMiddle()
    {
        var scene = _show.Scene("S", _layer, Step(2, 10, V(_par["dim"], 1)) with { Curve = FadeCurve.SCurve });
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();

        engine.Run(0.5);
        engine.Value(_par["dim"]).ShouldBeLessThan(0.25);
        engine.Run(0.5);
        engine.Value(_par["dim"]).ShouldBe(0.5, 1e-9);
    }

    [Theory]
    [InlineData(DiscreteSwitch.Start, 0.0)]
    [InlineData(DiscreteSwitch.Middle, 1.0)]
    [InlineData(DiscreteSwitch.End, 2.0)]
    [Trait("Exigence", "MOT-012")]
    public void DiscreteAttribute_SwitchesFrankly_AtChosenPoint(DiscreteSwitch point, double switchSeconds)
    {
        var red = 10 / 255.0;
        var blue = 30 / 255.0;
        var scene = _show.Scene(
            "Roue",
            _layer,
            Step(0, 1, V(_lyre["color"], red)),
            Step(2, 5, V(_lyre["color"], blue)) with { Switch = point });
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();

        var seen = new List<byte>();
        for (var i = 0; i < 40 * 4; i++)
        {
            engine.Tick();
            seen.Add(engine[115]);
        }

        // Aucune valeur intermédiaire : seulement l'emplacement rouge puis l'emplacement bleu.
        seen.Distinct().ShouldBe([(byte)10, (byte)30]);
        var firstBlue = seen.IndexOf(30);
        var expected = (int)Math.Round((1 + switchSeconds) / 0.025) - 1;
        Math.Abs(firstBlue - expected).ShouldBeLessThanOrEqualTo(1);
    }

    [Fact]
    [Trait("Exigence", "MOT-013")]
    public void LoopOnce_ThenStops()
    {
        var scene = _show.Scene(ThreeSteps() with { Loop = LoopMode.Once });
        var engine = new EngineHarness(_show.Build());

        var steps = RecordSteps(engine, scene, 4);

        steps.ShouldBe([0, 1, 2]);
        engine.Playback(scene).ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "MOT-013")]
    public void LoopCount_PlaysNPasses()
    {
        var scene = _show.Scene(ThreeSteps() with { Loop = LoopMode.Count, LoopCount = 2 });
        var engine = new EngineHarness(_show.Build());

        var steps = RecordSteps(engine, scene, 8);

        steps.ShouldBe([0, 1, 2, 0, 1, 2]);
    }

    [Fact]
    [Trait("Exigence", "MOT-013")]
    public void LoopInfinite_WrapsAround()
    {
        var scene = _show.Scene(ThreeSteps());
        var engine = new EngineHarness(_show.Build());

        var steps = RecordSteps(engine, scene, 6.5);

        steps.ShouldBe([0, 1, 2, 0, 1, 2, 0]);
    }

    [Fact]
    [Trait("Exigence", "MOT-013")]
    public void LoopPingPong_GoesBackAndForth()
    {
        var scene = _show.Scene(ThreeSteps() with { Loop = LoopMode.PingPong });
        var engine = new EngineHarness(_show.Build());

        var steps = RecordSteps(engine, scene, 6.5);

        steps.ShouldBe([0, 1, 2, 1, 0, 1, 2]);
    }

    [Fact]
    [Trait("Exigence", "MOT-013")]
    [Trait("Exigence", "MOT-004")]
    public void LoopRandom_NeverRepeatsCurrentStep_AndIsReproducibleWithSeed()
    {
        var scene = _show.Scene(ThreeSteps() with { Loop = LoopMode.Random });
        var model = _show.Build();

        var first = RecordSteps(new EngineHarness(model, seed: 42), scene, 30);
        var second = RecordSteps(new EngineHarness(model, seed: 42), scene, 30);

        first.Zip(first.Skip(1)).ShouldAllBe(pair => pair.First != pair.Second);
        first.Distinct().Count().ShouldBe(3);
        second.ShouldBe(first);
    }

    [Fact]
    [Trait("Exigence", "MOT-014")]
    public void EndHold_StaysOnLastStep()
    {
        var scene = _show.Scene(ThreeSteps() with { Loop = LoopMode.Once, End = EndMode.Hold });
        var engine = new EngineHarness(_show.Build());

        var steps = RecordSteps(engine, scene, 6);

        steps.ShouldBe([0, 1, 2]);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(2);
        engine.Value(_par["b"]).ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "MOT-014")]
    public void EndStop_FadesOutWithSceneFadeOut()
    {
        var scene = _show.Scene(ThreeSteps() with { Loop = LoopMode.Once, FadeOut = Duration.FromSeconds(1) });
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();

        engine.Run(3 + 0.5);
        engine.Playback(scene)!.Value.State.ShouldBe(PlaybackState.FadingOut);
        engine.Value(_par["b"]).ShouldBe(0.5, 0.03);

        engine.Run(0.6);
        engine.Playback(scene).ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "MOT-014")]
    public void EndChain_LaunchesNextSceneInSameLayer()
    {
        var next = _show.Scene("Suite", _layer, Step(0, 1, V(_par["dim"], 1)));
        var scene = _show.Scene(ThreeSteps() with { Loop = LoopMode.Once, End = EndMode.Chain, ChainSceneId = next.Id });
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();

        engine.Run(3.05);

        engine.Playback(scene).ShouldBeNull();
        engine.Playback(next)!.Value.LayerId.ShouldBe(_layer.Id);
        engine.Value(_par["dim"]).ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "MOT-015")]
    [Trait("Exigence", "CMD-016")]
    public void Speed_Doubled_MakesStepsTwiceShorter()
    {
        var scene = _show.Scene(ThreeSteps() with { Speed = 2 });
        var engine = new EngineHarness(_show.Build());

        var steps = RecordSteps(engine, scene, 1.75);
        steps.ShouldBe([0, 1, 2, 0]);

        // Mi-étape (0,25 s réelle à ×2) puis vitesse ×0,5 : la demi-étape restante dure 1 s réelle.
        engine.Send(new SetSceneSpeedCommand(CommandOrigin.User, scene.Id, 0.5));
        engine.Run(0.95);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(0);
        engine.Run(0.1);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "MOT-019")]
    [Trait("Exigence", "CMD-015")]
    public void ManualNextAndPrevious_ChangeStep()
    {
        var scene = _show.Scene(ThreeSteps() with { Steps = [.. ThreeSteps().Steps.Select(s => s with { Hold = Duration.FromSeconds(100) })] });
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();

        engine.Send(new StepSceneCommand(CommandOrigin.User, scene.Id, StepDirection.Next));
        engine.Tick();
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(1);

        engine.Send(new StepSceneCommand(CommandOrigin.User, scene.Id, StepDirection.Previous));
        engine.Send(new StepSceneCommand(CommandOrigin.User, scene.Id, StepDirection.Previous));
        engine.Tick();
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(2);
    }

    [Theory]
    [InlineData(120, 1.0)]
    [InlineData(90, 1.333)]
    [Trait("Exigence", "GEN-023")]
    public void MusicalDuration_TwoBeats_DependsOnTempo(double bpm, double seconds)
    {
        Duration.FromBeats(2).ToSeconds(bpm).ShouldBe(seconds, 0.001);
        new Duration(1, DurationUnit.Bars).ToSeconds(120).ShouldBe(2, 1e-9);

        var scene = _show.Scene("Temps", _layer, Step(0, 0, V(_par["r"], 1)) with { Hold = Duration.FromBeats(2) }, Step(0, 0, V(_par["g"], 1)) with { Hold = Duration.FromBeats(2) });
        var engine = new EngineHarness(_show.Build());
        engine.Engine.Bpm = bpm;
        engine.Launch(scene);
        engine.Tick();

        engine.Run(seconds - 0.05);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(0);
        engine.Run(0.075);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "SCN-010")]
    public void PerValueDelay_SpreadsTheFade_AcrossMembers()
    {
        var pars = new[] { _par, _show.Par7(8), _show.Par7(15) };
        var values = pars.Select((p, i) => new StepValue(p["r"], 1, Duration.FromSeconds(1), Duration.FromSeconds(i))).ToArray();
        var scene = _show.Scene("Vague", _layer, Step(1, 5, values));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();

        engine.Run(1);
        pars.Select(p => engine.Value(p["r"])).ToArray().ShouldBe([1, 0, 0], tolerance: 1e-9);
        engine.Run(0.5);
        pars.Select(p => engine.Value(p["r"])).ToArray().ShouldBe([1, 0.5, 0], tolerance: 1e-9);
        engine.Run(1.5);
        pars.Select(p => engine.Value(p["r"])).ToArray().ShouldBe([1, 1, 1], tolerance: 1e-9);
    }

    [Fact]
    [Trait("Exigence", "SCN-011")]
    public void PerAttributeFade_ColorsSlow_PositionInstant()
    {
        var scene = _show.Scene(
            "Mixte",
            _layer,
            Step(0, 1, V(_par["r"], 0), V(_lyre["pan"], 0)),
            Step(0, 10, new StepValue(_par["r"], 1, Duration.FromSeconds(2)), V(_lyre["pan"], 1)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();

        engine.Run(1 + 1);
        engine.Value(_lyre["pan"]).ShouldBe(1);
        engine.Value(_par["r"]).ShouldBe(0.5, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MOT-011")]
    public void StepChange_InterpolatesFromPreviousStepValue()
    {
        var scene = _show.Scene(
            "Rouge vers vert",
            _layer,
            Step(0, 1, V(_par["r"], 1), V(_par["g"], 0)),
            Step(2, 1, V(_par["r"], 0), V(_par["g"], 1)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();

        engine.Run(2);
        engine.Value(_par["r"]).ShouldBe(0.5, 1e-9);
        engine.Value(_par["g"]).ShouldBe(0.5, 1e-9);
    }

    private EngineScene ThreeSteps() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Trois",
        LayerId = _layer.Id,
        Steps =
        [
            Step(0, 1, V(_par["r"], 1)),
            Step(0, 1, V(_par["g"], 1)),
            Step(0, 1, V(_par["b"], 1)),
        ],
    };

    /// <summary>Lance la scène et relève l'étape courante à chaque changement, pendant la durée donnée.</summary>
    private static List<int> RecordSteps(EngineHarness engine, EngineScene scene, double seconds)
    {
        engine.Launch(scene);
        engine.Tick();
        var steps = new List<int> { engine.Playback(scene)!.Value.StepIndex };
        var ticks = (int)Math.Round(seconds / 0.025);
        for (var i = 0; i < ticks; i++)
        {
            engine.Tick();
            if (engine.Playback(scene) is { } playback && playback.StepIndex != steps[^1])
            {
                steps.Add(playback.StepIndex);
            }
        }

        return steps;
    }
}
