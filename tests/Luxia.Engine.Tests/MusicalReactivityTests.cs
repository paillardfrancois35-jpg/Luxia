using Luxia.Engine.Model;
using Luxia.Messaging.Commands;
using static Luxia.Engine.Tests.ShowBuilder;

namespace Luxia.Engine.Tests;

/// <summary>T-AUD-04, T-MOT-06 : scènes au temps — avance à l'événement, quantification, horloge propre, effets calés.</summary>
public sealed class MusicalReactivityTests
{
    private readonly ShowBuilder _show = new();
    private readonly TestFixture _par;
    private readonly EngineLayer _layer;

    public MusicalReactivityTests()
    {
        _par = _show.Par7(1);
        _layer = _show.Layer("Tout", 1);
    }

    private EngineScene TwoSteps(StepAdvanceMode advance = StepAdvanceMode.Beat, int every = 1, LaunchQuantize quantize = LaunchQuantize.None, double? ownBpm = null, int multiplier = 1) =>
        _show.Scene(new EngineScene
        {
            Id = Guid.NewGuid(),
            Name = "Au temps",
            LayerId = _layer.Id,
            Advance = advance,
            AdvanceEvery = every,
            AdvanceMultiplier = multiplier,
            Quantize = quantize,
            OwnBpm = ownBpm,
            Steps = [Step(0, 60, V(_par["r"], 1)), Step(0, 60, V(_par["g"], 1))],
        });

    private (EngineHarness Engine, EngineScene Scene) Play(EngineScene scene)
    {
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();
        return (engine, scene);
    }

    [Theory]
    [InlineData(StepAdvanceMode.Beat, 1, 0.5)]
    [InlineData(StepAdvanceMode.Beat, 2, 1.0)]
    [InlineData(StepAdvanceMode.Bar, 1, 2.0)]
    [InlineData(StepAdvanceMode.BassPulse, 1, 0.5)]
    [InlineData(StepAdvanceMode.TreblePulse, 1, 0.5)]
    [Trait("Exigence", "MOT-017")]
    [Trait("Exigence", "SCN-052")]
    public void Step_AdvancesOnTheMusicalEvent_AndPulsesFallBackToBeatsWithoutAudio(StepAdvanceMode mode, int every, double atSeconds)
    {
        var (engine, scene) = Play(TwoSteps(advance: mode, every: every));

        // La scène démarre au tick du lancement (t = 25 ms) ; l'événement tombe à atSeconds sur l'horloge.
        engine.Run(atSeconds - 0.05);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(0);
        engine.Run(0.05);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(1);
    }

    [Theory]
    [InlineData(StepAdvanceMode.Beat, 2, 0.25)]
    [InlineData(StepAdvanceMode.Beat, 4, 0.125)]
    [InlineData(StepAdvanceMode.Bar, 4, 0.5)]
    [Trait("Exigence", "MOT-017")]
    public void Step_WithAMultiplier_AdvancesSeveralTimesPerBeatOrBar(StepAdvanceMode mode, int multiplier, double atSeconds)
    {
        var (engine, scene) = Play(TwoSteps(advance: mode, multiplier: multiplier));

        engine.Run(atSeconds - 0.05);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(0);
        engine.Run(0.06);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "MOT-017")]
    public void Step_AutoAdvance_MakesAShortFlash_WhileTheNextStepWaitsForTheEvent()
    {
        var scene = _show.Scene(new EngineScene
        {
            Id = Guid.NewGuid(),
            Name = "Flash bref",
            LayerId = _layer.Id,
            Advance = StepAdvanceMode.Beat,
            Steps = [Step(0, 0.15, V(_par["r"], 1)) with { AutoAdvance = true }, Step(0, 60, V(_par["r"], 0))],
        });
        var (engine, _) = Play(scene);

        // Premier temps : le flash s'allume ; il s'éteint tout seul au bout de 0,15 s, bien avant le temps suivant (0,5 s).
        engine.Run(0.3);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(1, "le flash est fini, on attend le prochain temps");
        engine.Run(0.25);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(0, "un nouveau temps rallume le flash");
        engine.Run(0.2);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "MOT-017")]
    public void Step_EventAdvance_IgnoresTheHoldDuration_AndFollowsTempoChanges()
    {
        var (engine, scene) = Play(TwoSteps());
        engine.Send(new SetTempoSourceCommand(CommandOrigin.Tool, TempoSourceKind.Fixed, 240));
        engine.Run(0.4);
        engine.Playback(scene)!.Value.StepIndex.ShouldBeGreaterThan(0);
    }

    private sealed class Feed : Luxia.Engine.Timing.IAudioFeed
    {
        public int Bass { get; set; }

        public double Energy { get; set; }

        public Luxia.Engine.Timing.AudioReading Read()
        {
            var reading = new Luxia.Engine.Timing.AudioReading(true, 120, 0.9, true, 0, 0, Bass, 0, Energy);
            Bass = 0;
            return reading;
        }
    }

    [Fact]
    [Trait("Exigence", "MOT-017")]
    public void Step_OnBassPulses_AdvancesOnTheKicks_NotOnTheBeats()
    {
        var (engine, scene) = Play(TwoSteps(advance: StepAdvanceMode.BassPulse));
        var feed = new Feed();
        engine.Engine.SetAudioFeed(feed);

        // Sans impulsion, même après plusieurs temps (2 s), la scène ne bouge pas : le son est entendu.
        engine.Run(2);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(0);

        feed.Bass = 1;
        engine.Tick();
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(1);
        feed.Bass = 1;
        engine.Tick();
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(0);
    }

    [Fact]
    [Trait("Exigence", "SCN-051")]
    public void EnergySpeed_MakesTheSceneFasterWhenTheMusicIsMoreEnergetic()
    {
        EngineScene Scene(string name) => _show.Scene(new EngineScene
        {
            Id = Guid.NewGuid(),
            Name = name,
            LayerId = _layer.Id,
            EnergySpeed = true,
            Steps = [Step(0, 1, V(_par["r"], 1)), Step(0, 1, V(_par["g"], 1))],
        });

        int StepAfter(double energy, double seconds)
        {
            var scene = Scene("Énergie " + energy);
            var engine = new EngineHarness(_show.Build());
            engine.Engine.SetAudioFeed(new Feed { Energy = energy });
            engine.Launch(scene);
            engine.Tick();
            engine.Run(seconds);
            return engine.Playback(scene)!.Value.StepIndex;
        }

        // Étape d'1 s : énergie 0 → 0,5× (2 s par étape) ; énergie 1 → 1,8× (0,56 s par étape).
        StepAfter(0, 0.9).ShouldBe(0);
        StepAfter(1, 0.9).ShouldBe(1);
        StepAfter(0.5, 0.9).ShouldBe(0);
    }

    [Fact]
    [Trait("Exigence", "MOT-020")]
    public void OwnClock_ScenePlaysAtItsOwnTempo_WhileTheMainClockIsFaster()
    {
        var (engine, scene) = Play(TwoSteps(ownBpm: 60));

        // Main à 120 BPM : un temps toutes les 0,5 s ; la scène est à 60 BPM : un temps toutes les secondes.
        engine.Run(0.9);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(0);
        engine.Run(0.2);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "MOT-020")]
    public void OwnClock_MusicalDurationsUseTheSceneTempo()
    {
        var scene = _show.Scene(new EngineScene
        {
            Id = Guid.NewGuid(),
            Name = "Lent",
            LayerId = _layer.Id,
            OwnBpm = 30,
            Steps = [Step(0, 0, V(_par["r"], 1)) with { Hold = Duration.FromBeats(1) }, Step(0, 0, V(_par["g"], 1)) with { Hold = Duration.FromBeats(1) }],
        });
        var (engine, _) = Play(scene);

        // 1 temps à 30 BPM = 2 s, alors que l'horloge principale est à 120 BPM.
        engine.Run(1.9);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(0);
        engine.Run(0.2);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "MOT-018")]
    public void Quantize_Bar_LaunchWaitsForTheNextBar_AndIsPublishedWhileWaiting()
    {
        var scene = TwoSteps(advance: StepAdvanceMode.Duration, quantize: LaunchQuantize.Bar);
        var engine = new EngineHarness(_show.Build());
        engine.Run(0.3);
        engine.Launch(scene);
        engine.Tick();

        engine.Playback(scene).ShouldBeNull();
        engine.Engine.Snapshot.PendingLaunches.Select(p => p.SceneId).ShouldBe([scene.Id]);
        engine.Engine.Snapshot.PendingLaunches[0].BeatsRemaining.ShouldBeInRange(2.5, 3.5);

        // 120 BPM : la mesure suivante commence à 2,0 s.
        engine.Run(1.65);
        engine.Playback(scene).ShouldBeNull();
        engine.Tick();
        engine.Playback(scene).ShouldNotBeNull();
        engine.Engine.Snapshot.PendingLaunches.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "MOT-018")]
    public void Quantize_Beat_OnTheBeat_StartsImmediately()
    {
        var scene = TwoSteps(advance: StepAdvanceMode.Duration, quantize: LaunchQuantize.Beat);
        var engine = new EngineHarness(_show.Build());
        engine.Run(0.475);
        engine.Launch(scene);
        engine.Tick();
        engine.Playback(scene).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Exigence", "MOT-018")]
    public void Quantize_Phrase4_WaitsSixteenBeats()
    {
        var scene = TwoSteps(advance: StepAdvanceMode.Duration, quantize: LaunchQuantize.Phrase4);
        var engine = new EngineHarness(_show.Build());
        engine.Run(0.3);
        engine.Launch(scene);
        engine.Tick();
        engine.Run(7.5);
        engine.Playback(scene).ShouldBeNull();
        engine.Run(0.2);
        engine.Playback(scene).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Exigence", "MOT-018")]
    public void Quantize_PressingAgain_CancelsTheWait_AndStopCancelsIt()
    {
        var scene = TwoSteps(advance: StepAdvanceMode.Duration, quantize: LaunchQuantize.Bar);
        var engine = new EngineHarness(_show.Build());
        engine.Run(0.3);
        engine.Send(new LaunchSceneCommand(CommandOrigin.User, scene.Id, StopIfPlaying: true));
        engine.Tick();
        engine.Engine.Snapshot.PendingLaunches.Count.ShouldBe(1);

        engine.Send(new LaunchSceneCommand(CommandOrigin.User, scene.Id, StopIfPlaying: true));
        engine.Tick();
        engine.Engine.Snapshot.PendingLaunches.ShouldBeEmpty();

        engine.Launch(scene);
        engine.Tick();
        engine.Stop(scene);
        engine.Run(3);
        engine.Playback(scene).ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "MOT-062")]
    public void MusicalEffect_CycleStartsOnTheClock_NotOnTheLaunch()
    {
        var effect = new EngineEffect
        {
            Id = Guid.NewGuid(),
            Name = "Une mesure",
            Shape = EffectShape.Sine,
            Period = new Duration(1, DurationUnit.Bars),
            Channels = [new EffectChannel(_par["dim"], 0, 0, 0.5, 1)],
        };
        var scene = _show.Scene(new EngineScene
        {
            Id = Guid.NewGuid(),
            Name = "Cercle",
            LayerId = _layer.Id,
            Steps = [new EngineStep { Fade = Duration.Zero, Hold = Duration.FromSeconds(60), Values = [], Effects = [effect] }],
        });
        var engine = new EngineHarness(_show.Build());
        engine.Run(0.3);
        engine.Launch(scene);
        engine.Tick();

        // Lancée en milieu de mesure : à la fin du 2e temps (t = 1,0 s, horloge à 2 temps sur 4) le sinus est à son sommet.
        engine.Run(0.675);
        engine.Value(_par["dim"]).ShouldBe(1, 1e-6);
        // Une mesure (2 s) plus tard, il y est de nouveau : le cycle suit l'horloge.
        engine.Run(2);
        engine.Value(_par["dim"]).ShouldBe(1, 1e-6);
    }

    [Fact]
    [Trait("Exigence", "MOT-062")]
    public void MusicalEffect_FollowsTheClockWhenItIsResynchronised()
    {
        var effect = new EngineEffect
        {
            Id = Guid.NewGuid(),
            Name = "Un temps",
            Shape = EffectShape.Sine,
            Period = Duration.FromBeats(4),
            Channels = [new EffectChannel(_par["dim"], 0, 0, 0.5, 1)],
        };
        var scene = _show.Scene(new EngineScene
        {
            Id = Guid.NewGuid(),
            Name = "Cercle",
            LayerId = _layer.Id,
            Steps = [new EngineStep { Fade = Duration.Zero, Hold = Duration.FromSeconds(60), Values = [], Effects = [effect] }],
        });
        var engine = new EngineHarness(_show.Build());
        engine.Launch(scene);
        engine.Tick();
        engine.Run(0.7);

        // « Le 1 est maintenant » : la position de l'horloge saute au début de mesure, le cycle saute avec elle.
        engine.Send(new AdjustTempoCommand(CommandOrigin.User, TempoAdjustment.ResyncBar));
        engine.Tick();
        engine.Value(_par["dim"]).ShouldBe(0, 0.01);
    }
}
