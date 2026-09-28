using Luxia.Engine.Model;
using static Luxia.Engine.Tests.ShowBuilder;

namespace Luxia.Engine.Tests;

/// <summary>T-EFF-01 : effets générés exécutés par le moteur (doc 15 §7, doc 16 §6), en temps virtuel.</summary>
public sealed class EffectTests
{
    private readonly ShowBuilder _show = new();
    private readonly TestFixture[] _pars;
    private readonly TestFixture _lyre;
    private readonly EngineLayer _layer;

    public EffectTests()
    {
        _pars = [_show.Par7(1, "PAR 1"), _show.Par7(11, "PAR 2"), _show.Par7(21, "PAR 3"), _show.Par7(31, "PAR 4")];
        _lyre = _show.Lyre(111);
        _layer = _show.Layer("Tout", 1);
    }

    private static EngineStep StepWith(double fade, IReadOnlyList<StepValue> values, params EngineEffect[] effects) => new()
    {
        Fade = Duration.FromSeconds(fade),
        Hold = Duration.FromSeconds(60),
        Values = values,
        Effects = effects,
    };

    private EngineEffect IntensityWave(EffectShape shape = EffectShape.Sine, double period = 1, double spread = 1, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "Vague",
        Shape = shape,
        Period = Duration.FromSeconds(period),
        Channels = [.. _pars.Select((par, i) => new EffectChannel(par["dim"], i, spread * i / _pars.Length, 0.5, 1))],
    };

    [Fact]
    [Trait("Exigence", "MOT-060")]
    [Trait("Exigence", "EFF-002")]
    [Trait("Exigence", "EFF-005")]
    public void Sine_FourPars_NinetyDegreesApart_ExpectedValues()
    {
        var scene = _show.Scene("Vague", _layer, StepWith(0, [], IntensityWave()));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();

        // t = 0 : PAR 1 au plus bas, PAR 2 à mi-montée (en retard d'un quart), PAR 3 au plus haut, PAR 4 à mi-descente.
        engine.Value(_pars[0]["dim"]).ShouldBe(0, 1e-9);
        engine.Value(_pars[1]["dim"]).ShouldBe(0.5, 1e-9);
        engine.Value(_pars[2]["dim"]).ShouldBe(1, 1e-9);
        engine.Value(_pars[3]["dim"]).ShouldBe(0.5, 1e-9);

        // Un quart de cycle plus tard, chacun a pris la valeur de son voisin de gauche : la vague va de gauche à droite.
        engine.Run(0.25);
        engine.Value(_pars[0]["dim"]).ShouldBe(0.5, 1e-9);
        engine.Value(_pars[1]["dim"]).ShouldBe(0, 1e-9);
        engine.Value(_pars[2]["dim"]).ShouldBe(0.5, 1e-9);
        engine.Value(_pars[3]["dim"]).ShouldBe(1, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MOT-015")]
    [Trait("Exigence", "MOT-060")]
    public void SceneSpeed_DoublesEffectSpeed()
    {
        var scene = _show.Scene(new EngineScene { Id = Guid.NewGuid(), Name = "Vague", LayerId = _layer.Id, Speed = 2, Steps = [StepWith(0, [], IntensityWave())] });
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();
        engine.Run(0.25);

        // Vitesse 2 : un quart de seconde = un demi-cycle.
        engine.Value(_pars[0]["dim"]).ShouldBe(1, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MOT-061")]
    [Trait("Exigence", "EFF-003")]
    public void RelativeCircle_AroundStepPosition()
    {
        var circle = new EngineEffect
        {
            Id = Guid.NewGuid(),
            Shape = EffectShape.Circle,
            Period = Duration.FromSeconds(4),
            Relative = true,
            Channels = [new EffectChannel(_lyre["pan"], 0, 0, 0, 0.2, EffectAxis.X), new EffectChannel(_lyre["tilt"], 0, 0, 0, 0.2, EffectAxis.Y)],
        };
        var scene = _show.Scene("Cercle", _layer, StepWith(0, [V(_lyre["pan"], 0.3), V(_lyre["tilt"], 0.6)], circle));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();

        engine.Value(_lyre["pan"]).ShouldBe(0.3, 1e-9);
        engine.Value(_lyre["tilt"]).ShouldBe(0.7, 1e-9);

        engine.Run(1);
        engine.Value(_lyre["pan"]).ShouldBe(0.4, 1e-9);
        engine.Value(_lyre["tilt"]).ShouldBe(0.6, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MOT-061")]
    public void RelativeEffect_WithoutStepValue_AddsToUnderlyingLayer()
    {
        var below = _show.Layer("Positions", 1);
        var above = _show.Layer("Mouvements", 2);
        var position = _show.Scene("Position", below, Step(0, 60, V(_lyre["pan"], 0.25)));
        var sweep = new EngineEffect
        {
            Id = Guid.NewGuid(),
            Shape = EffectShape.SweepPan,
            Period = Duration.FromSeconds(4),
            Relative = true,
            Channels = [new EffectChannel(_lyre["pan"], 0, 0, 0, 0.1, EffectAxis.X)],
        };
        var moving = _show.Scene("Balayage", above, StepWith(0, [], sweep));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(position);
        engine.Launch(moving);
        engine.Tick();
        engine.Run(1);

        engine.Value(_lyre["pan"]).ShouldBe(0.3, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MOT-061")]
    public void AbsoluteEffect_ReplacesStepValue()
    {
        var effect = IntensityWave(EffectShape.Square);
        var scene = _show.Scene("Carré", _layer, StepWith(0, [.. _pars.Select(p => V(p["dim"], 0.4))], effect));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();

        engine.Value(_pars[0]["dim"]).ShouldBe(1, 1e-9);
        engine.Value(_pars[2]["dim"]).ShouldBe(0, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MOT-063")]
    public void EffectEntersWithSceneFade_NoJump()
    {
        var circle = new EngineEffect
        {
            Id = Guid.NewGuid(),
            Shape = EffectShape.Circle,
            Period = Duration.FromSeconds(1),
            Relative = true,
            Channels = [new EffectChannel(_lyre["pan"], 0, 0, 0, 0.4, EffectAxis.X), new EffectChannel(_lyre["tilt"], 0, 0, 0, 0.4, EffectAxis.Y)],
        };
        var scene = _show.Scene("Cercle", _layer, StepWith(2, [V(_lyre["pan"], 0.5), V(_lyre["tilt"], 0.5)], circle));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();

        // La lyre part du centre (valeur par défaut) : l'écart du cercle grandit avec le fondu, sans saut.
        var previousPan = engine.Value(_lyre["pan"]);
        var previousTilt = engine.Value(_lyre["tilt"]);
        previousTilt.ShouldBe(0.5, 1e-9);
        for (var i = 0; i < 80; i++)
        {
            engine.Tick();
            var pan = engine.Value(_lyre["pan"]);
            var tilt = engine.Value(_lyre["tilt"]);
            Math.Abs(pan - previousPan).ShouldBeLessThan(0.05);
            Math.Abs(tilt - previousTilt).ShouldBeLessThan(0.05);
            previousPan = pan;
            previousTilt = tilt;
        }
    }

    [Fact]
    [Trait("Exigence", "MOT-063")]
    public void EffectLeavesWithSceneFadeOut()
    {
        var scene = _show.Scene("Vague", _layer, StepWith(0, [], IntensityWave(EffectShape.Square, period: 10)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();
        engine.Value(_pars[0]["dim"]).ShouldBe(1, 1e-9);

        engine.Stop(scene, 1);
        engine.Tick();
        engine.Run(0.5);
        engine.Value(_pars[0]["dim"]).ShouldBe(0.5, 0.03);
        engine.Run(0.6);
        engine.Value(_pars[0]["dim"]).ShouldBe(0, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "EFF-009")]
    public void TwoRelativeEffects_SameAttribute_Add()
    {
        EngineEffect Sweep(double size) => new()
        {
            Id = Guid.NewGuid(),
            Shape = EffectShape.SweepPan,
            Period = Duration.FromSeconds(4),
            Relative = true,
            Channels = [new EffectChannel(_lyre["pan"], 0, 0, 0, size, EffectAxis.X)],
        };

        var scene = _show.Scene("Double", _layer, StepWith(0, [V(_lyre["pan"], 0.5)], Sweep(0.1), Sweep(0.2)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();
        engine.Run(1);

        engine.Value(_lyre["pan"]).ShouldBe(0.5 + 0.05 + 0.1, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "EFF-004")]
    public void Table_Stepped_Alternates_Interpolated_Blends()
    {
        EngineEffect Alternate(bool stepped) => new()
        {
            Id = Guid.NewGuid(),
            Shape = EffectShape.Table,
            Period = Duration.FromSeconds(1),
            Stepped = stepped,
            Channels = [new EffectChannel(_pars[0]["r"], 0, 0, 0, 0, Table: [1.0, 0.0])],
        };

        var stepped = _show.Scene("Alternance", _layer, StepWith(0, [], Alternate(true)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(stepped);
        engine.Tick();
        engine.Run(0.25);
        engine.Value(_pars[0]["r"]).ShouldBe(1, 1e-9);
        engine.Run(0.5);
        engine.Value(_pars[0]["r"]).ShouldBe(0, 1e-9);

        var blended = _show.Scene("Dégradé", _layer, StepWith(0, [], Alternate(false)));
        engine = new EngineHarness(_show.Build());
        engine.Launch(blended);
        engine.Tick();
        engine.Run(0.25);
        engine.Value(_pars[0]["r"]).ShouldBe(0.5, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "EFF-002")]
    [Trait("Exigence", "MOT-004")]
    public void RandomShape_SameSeed_SameValues_MembersDiffer()
    {
        var effect = IntensityWave(EffectShape.Random, spread: 0) with { Seed = 42 };
        var scene = _show.Scene("Scintillement", _layer, StepWith(0, [], effect));
        List<double> Record(int seed)
        {
            var engine = new EngineHarness(_show.Build(), seed);
            engine.Launch(scene);
            engine.Tick();
            var values = new List<double>();
            for (var i = 0; i < 20; i++)
            {
                engine.Run(0.5);
                values.AddRange(_pars.Select(p => engine.Value(p["dim"])));
            }

            return values;
        }

        var first = Record(7);
        Record(7).ShouldBe(first);
        Record(8).ShouldNotBe(first);
        first.Distinct().Count().ShouldBeGreaterThan(40);
    }

    [Fact]
    [Trait("Exigence", "EFF-001")]
    public void SameEffectInTwoSteps_ContinuesWithoutRestart()
    {
        var id = Guid.NewGuid();
        var effect = IntensityWave(EffectShape.SawUp, period: 4, id: id);
        var scene = _show.Scene("Deux étapes", _layer, Step(0, 1) with { Effects = [effect] }, Step(0, 1) with { Effects = [effect] });
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();
        engine.Run(1.5);

        // 1,5 s dans une dent de scie de 4 s : pas de retour au début au changement d'étape (1 s).
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(1);
        engine.Value(_pars[0]["dim"]).ShouldBe(1.5 / 4, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "EFF-001")]
    public void EffectOnlyInFirstStep_FadesOutWithSecondStepFade()
    {
        var effect = IntensityWave(EffectShape.Square);
        var scene = _show.Scene(
            "Puis fixe",
            _layer,
            StepWith(0, [], effect) with { Hold = Duration.FromSeconds(1) },
            Step(1, 10, V(_pars[0]["dim"], 0.2)));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();
        engine.Run(1);
        engine.Run(1.1);
        engine.Value(_pars[0]["dim"]).ShouldBe(0.2, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MOT-054")]
    public void HueFade_RedToGreen_PassesThroughYellow()
    {
        var par = _pars[0];
        _show.ColorGroups.Add(new ColorGroup(par["r"], par["g"], par["b"]));
        EngineStep Color(double r, double g, bool hue) => Step(1, 5, V(par["r"], r), V(par["g"], g), V(par["b"], 0)) with { HueFade = hue };

        var withHue = _show.Scene("Teinte", _layer, Color(1, 0, true), Color(0, 1, true));
        var engine = new EngineHarness(_show.Build());
        engine.Launch(withHue);
        engine.Tick();
        engine.Run(6.5);
        engine.Value(par["r"]).ShouldBe(1, 1e-6);
        engine.Value(par["g"]).ShouldBe(1, 1e-6);

        var direct = _show.Scene("Direct", _layer, Color(1, 0, false), Color(0, 1, false));
        engine = new EngineHarness(_show.Build());
        engine.Launch(direct);
        engine.Tick();
        engine.Run(6.5);
        engine.Value(par["r"]).ShouldBe(0.5, 1e-6);
        engine.Value(par["g"]).ShouldBe(0.5, 1e-6);
    }

    [Theory]
    [InlineData(EffectShape.Triangle, 0.25, 0.0)]
    [InlineData(EffectShape.Triangle, 0.5, 0.5)]
    [InlineData(EffectShape.SawUp, 0.75, 0.25)]
    [InlineData(EffectShape.SawDown, 0.75, -0.25)]
    [InlineData(EffectShape.Square, 0.49, 0.5)]
    [InlineData(EffectShape.Square, 0.51, -0.5)]
    [InlineData(EffectShape.Pulse, 0.0, 0.5)]
    [InlineData(EffectShape.Pulse, 0.25, 0.0)]
    [InlineData(EffectShape.Pulse, 0.6, -0.5)]
    [Trait("Exigence", "EFF-002")]
    public void Shapes_ExpectedOffsets(EffectShape shape, double cycles, double expected) =>
        EffectShapes.Offset(shape, cycles).ShouldBe(expected, 1e-9);

    [Fact]
    [Trait("Exigence", "EFF-003")]
    public void PositionShapes_CircleAndEight()
    {
        EffectShapes.Offset(EffectShape.Circle, 0.25, EffectAxis.X).ShouldBe(0.5, 1e-9);
        EffectShapes.Offset(EffectShape.Circle, 0.25, EffectAxis.Y).ShouldBe(0, 1e-9);
        EffectShapes.Offset(EffectShape.Eight, 0.125, EffectAxis.Y).ShouldBe(0.5, 1e-9);
        EffectShapes.Offset(EffectShape.Eight, 0.5, EffectAxis.X).ShouldBe(0, 1e-9);
        EffectShapes.Offset(EffectShape.SweepPan, 0.25, EffectAxis.Y).ShouldBe(0);

        // Aléatoire lent : continu d'un cycle à l'autre.
        var before = EffectShapes.Offset(EffectShape.RandomSlow, 2.9999, EffectAxis.X, seed: 5);
        var after = EffectShapes.Offset(EffectShape.RandomSlow, 3.0001, EffectAxis.X, seed: 5);
        Math.Abs(before - after).ShouldBeLessThan(1e-3);
    }

    [Fact]
    [Trait("Exigence", "EFF-005")]
    public void Directions_BackwardAndPingPong()
    {
        EffectShapes.Directed(EffectDirection.Backward, 0.25).ShouldBe(-0.25);
        EffectShapes.Directed(EffectDirection.PingPong, 0.5).ShouldBe(0.5, 1e-9);
        EffectShapes.Directed(EffectDirection.PingPong, 1.5).ShouldBe(0.5, 1e-9);
        EffectShapes.Directed(EffectDirection.PingPong, 2.25).ShouldBe(2.25, 1e-9);
    }
}
