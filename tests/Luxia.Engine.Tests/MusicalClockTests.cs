using Luxia.Engine.Model;
using Luxia.Engine.Timing;
using Luxia.Messaging.Commands;
using static Luxia.Engine.Tests.ShowBuilder;

namespace Luxia.Engine.Tests;

/// <summary>T-AUD-03 et T-MOT-06 : horloge musicale (tap, fixe, ×2, ÷2, recalage) et durées musicales.</summary>
public sealed class MusicalClockTests
{
    [Fact]
    [Trait("Exigence", "GEN-023")]
    public void Clock_Fixed120_CountsBeatsAndBars()
    {
        var clock = new MusicalClock();
        clock.Advance(2.0);
        clock.BeatPosition.ShouldBe(4, 1e-9);
        clock.BeatInBar.ShouldBe(1);
        clock.Bar.ShouldBe(2);
        clock.Advance(0.6);
        clock.BeatInBar.ShouldBe(2);
        clock.Source.ShouldBe(TempoSourceKind.Fixed);
    }

    [Fact]
    [Trait("Exigence", "AUD-025")]
    public void Tap_FourTapsAtHalfSecond_Gives120()
    {
        var clock = new MusicalClock();
        clock.SetFixed(90);
        for (var i = 0; i < 4; i++)
        {
            clock.Tap(10 + (i * 0.5), 0);
        }

        clock.Bpm.ShouldBe(120, 0.01);
        clock.Source.ShouldBe(TempoSourceKind.Tap);
    }

    [Fact]
    [Trait("Exigence", "AUD-025")]
    public void Tap_ThreeTaps_DoNotChangeTheTempoYet()
    {
        var clock = new MusicalClock();
        clock.Tap(0, 0);
        clock.Tap(0.4, 0);
        clock.Tap(0.8, 0);
        clock.Bpm.ShouldBe(120);
        clock.Source.ShouldBe(TempoSourceKind.Fixed);
    }

    [Fact]
    [Trait("Exigence", "AUD-025")]
    public void Tap_TwoSecondsWithoutTap_RestartsTheCount()
    {
        var clock = new MusicalClock();
        clock.Tap(0, 0);
        clock.Tap(0.5, 0);
        clock.Tap(1, 0);
        clock.Tap(4, 0);
        clock.Tap(4.5, 0);
        clock.Tap(5, 0);
        clock.Source.ShouldBe(TempoSourceKind.Fixed);
        clock.Tap(5.5, 0);
        clock.Bpm.ShouldBe(120, 0.01);
        clock.Source.ShouldBe(TempoSourceKind.Tap);
    }

    [Fact]
    [Trait("Exigence", "AUD-025")]
    public void Tap_SnapsThePhaseToTheTap()
    {
        var clock = new MusicalClock();
        clock.Advance(0.3);
        clock.Tap(0.3, 0);
        clock.Phase.ShouldBe(0, 1e-6);
        clock.BeatPosition.ShouldBe(1, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "AUD-023")]
    public void Scale_DoublesAndHalves_WithinBounds()
    {
        var clock = new MusicalClock();
        clock.SetFixed(64);
        clock.Scale(2);
        clock.Bpm.ShouldBe(128);
        clock.Scale(0.5);
        clock.Bpm.ShouldBe(64);
        clock.SetFixed(300);
        clock.Scale(2);
        clock.Bpm.ShouldBe(MusicalClock.MaxBpm);
    }

    [Fact]
    [Trait("Exigence", "AUD-024")]
    public void ResyncBar_MakesTheCurrentBeatTheFirstOfABar()
    {
        var clock = new MusicalClock();
        clock.Advance(1.4);
        clock.BeatInBar.ShouldBe(3);
        clock.ResyncBar();
        clock.BeatInBar.ShouldBe(1);
        clock.BeatPosition.ShouldBe(4, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "GEN-035")]
    public void Latency_AdvancesTheEventsAndIsBounded()
    {
        var clock = new MusicalClock();
        clock.SetLatency(1);
        clock.LatencySeconds.ShouldBe(0.25);
        clock.SetLatency(0.25);
        clock.EffectivePosition.ShouldBe(0.5, 1e-9);
        clock.BeatPosition.ShouldBe(0);
    }

    [Fact]
    [Trait("Exigence", "CMD-041")]
    public void Engine_Commands_SetSourceAndTempo()
    {
        var show = new ShowBuilder();
        var engine = new EngineHarness(show.Build());
        engine.Send(new SetTempoSourceCommand(CommandOrigin.Tool, TempoSourceKind.Fixed, 90));
        engine.Tick();
        engine.Engine.Bpm.ShouldBe(90);
        engine.Engine.Snapshot.Tempo.Bpm.ShouldBe(90);

        engine.Send(new AdjustTempoCommand(CommandOrigin.Tool, TempoAdjustment.TimesTwo));
        engine.Tick();
        engine.Engine.Bpm.ShouldBe(180);

        engine.Send(new SetTempoSourceCommand(CommandOrigin.Tool, TempoSourceKind.Fixed, 5));
        engine.Tick();
        engine.Engine.Bpm.ShouldBe(180);
    }

    [Fact]
    [Trait("Exigence", "CMD-040")]
    public void Engine_TapCommands_ComputeTheTempo()
    {
        var engine = new EngineHarness(new ShowBuilder().Build());
        for (var i = 0; i < 4; i++)
        {
            engine.Send(new TapTempoCommand(CommandOrigin.User));
            engine.Run(0.5);
        }

        engine.Engine.Bpm.ShouldBe(120, 0.5);
        engine.Engine.Snapshot.Tempo.Source.ShouldBe(TempoSourceKind.Tap);
    }

    [Fact]
    [Trait("Exigence", "MOT-016")]
    public void Step_TempoHalvedMidStep_RemainingMusicalTimeDoubles()
    {
        var show = new ShowBuilder();
        var par = show.Par7(1);
        var layer = show.Layer("Tout", 1);
        var scene = show.Scene("Temps", layer, Step(0, 0, V(par["r"], 1)) with { Hold = Duration.FromBeats(2) }, Step(0, 0, V(par["g"], 1)) with { Hold = Duration.FromBeats(2) });
        var engine = new EngineHarness(show.Build());
        engine.Launch(scene);
        engine.Tick();

        engine.Run(0.5);
        engine.Send(new SetTempoSourceCommand(CommandOrigin.Tool, TempoSourceKind.Fixed, 60));
        engine.Tick();
        engine.Run(0.925);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(0);
        engine.Run(0.1);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "MOT-016")]
    public void Step_SecondsHold_IgnoresTheTempoChange()
    {
        var show = new ShowBuilder();
        var par = show.Par7(1);
        var layer = show.Layer("Tout", 1);
        var scene = show.Scene("Secondes", layer, Step(0, 1, V(par["r"], 1)), Step(0, 1, V(par["g"], 1)));
        var engine = new EngineHarness(show.Build());
        engine.Launch(scene);
        engine.Tick();
        engine.Run(0.5);
        engine.Send(new SetTempoSourceCommand(CommandOrigin.Tool, TempoSourceKind.Fixed, 60));
        engine.Tick();
        engine.Run(0.45);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(0);
        engine.Run(0.1);
        engine.Playback(scene)!.Value.StepIndex.ShouldBe(1);
    }
}
