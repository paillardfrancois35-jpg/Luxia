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
        clock.LatencySeconds.ShouldBe(0.5);
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

    private sealed class FakeFeed : IAudioFeed
    {
        public AudioReading Reading { get; set; }

        public AudioReading Read() => Reading;
    }

    [Fact]
    [Trait("Exigence", "AUD-020")]
    public void Audio_Source_FollowsTheTempoAndThePhaseOfTheFeed()
    {
        var engine = new EngineHarness(new ShowBuilder().Build());
        var feed = new FakeFeed { Reading = new AudioReading(true, 130, 0.9, true, 0.25, 2, 0, 0) };
        engine.Engine.SetAudioFeed(feed);
        engine.Send(new SetTempoSourceCommand(CommandOrigin.Tool, TempoSourceKind.Audio));
        engine.Run(1);

        engine.Engine.Bpm.ShouldBe(130, 0.5);
        engine.Engine.Snapshot.Tempo.Source.ShouldBe(TempoSourceKind.Audio);
        engine.Engine.Snapshot.Tempo.Confidence.ShouldBe(0.9);
    }

    [Fact]
    [Trait("Exigence", "GEN-034")]
    public void Audio_Source_KeepsTheLastTempoWhenTheSoundIsLostOrUnsure()
    {
        var engine = new EngineHarness(new ShowBuilder().Build());
        var feed = new FakeFeed { Reading = new AudioReading(true, 100, 0.9, true, 0, 1, 0, 0) };
        engine.Engine.SetAudioFeed(feed);
        engine.Send(new SetTempoSourceCommand(CommandOrigin.Tool, TempoSourceKind.Audio));
        engine.Run(0.5);
        engine.Engine.Bpm.ShouldBe(100, 0.5);

        feed.Reading = new AudioReading(false, 0, 0, false, 0, 0, 0, 0);
        engine.Run(2);
        engine.Engine.Bpm.ShouldBe(100, 0.5);

        feed.Reading = new AudioReading(true, 170, 0.1, true, 0, 1, 0, 0);
        engine.Run(1);
        engine.Engine.Bpm.ShouldBe(100, 0.5);
    }

    [Fact]
    [Trait("Exigence", "AUD-026")]
    public void Audio_Source_SnapsToANewSongTempo()
    {
        var engine = new EngineHarness(new ShowBuilder().Build());
        var feed = new FakeFeed { Reading = new AudioReading(true, 90, 0.9, true, 0, 1, 0, 0) };
        engine.Engine.SetAudioFeed(feed);
        engine.Send(new SetTempoSourceCommand(CommandOrigin.Tool, TempoSourceKind.Audio));
        engine.Run(0.5);

        feed.Reading = new AudioReading(true, 128, 0.9, true, 0, 1, 0, 0);
        engine.Tick();

        engine.Engine.Bpm.ShouldBe(128, 0.01);
    }

    private static int FollowBarFor(MusicalClock clock, int audioBarBeat, int ticks)
    {
        // L'écoute donne toujours le même temps dans la mesure ; sa phase est celle de l'horloge (aucun recalage de phase).
        for (var i = 0; i < ticks; i++)
        {
            clock.Advance(0.025);
            var phase = clock.BeatPosition - Math.Floor(clock.BeatPosition);
            clock.FollowAudio(new AudioReading(true, 120, 0.9, true, phase, audioBarBeat, 0, 0));
        }

        return clock.BeatInBar;
    }

    [Fact]
    [Trait("Exigence", "AUD-024")]
    public void Audio_Downbeat_ThatDisagreesForASecond_MovesTheBarPosition()
    {
        var clock = new MusicalClock();
        clock.UseSource(TempoSourceKind.Audio);
        FollowBarFor(clock, 1, 10);
        var before = clock.BeatIndex;

        // L'écoute dit toujours « temps suivant » : après chaque seconde de désaccord, l'horloge avance d'un temps.
        FollowBarFor(clock, (clock.BeatInBar % 4) + 1, 160);

        (clock.BeatIndex - before).ShouldBeGreaterThan(9, "8 temps d'horloge en 4 s, plus les sauts de recalage du premier temps");
    }

    [Fact]
    [Trait("Exigence", "AUD-024")]
    public void ManualBarResync_IsNotOverruledByTheGuessedDownbeat_UntilANewSong()
    {
        var clock = new MusicalClock();
        clock.UseSource(TempoSourceKind.Audio);
        FollowBarFor(clock, 1, 10);
        clock.ResyncBar();
        var afterManual = clock.BeatIndex;

        // Même désaccord pendant 4 s : le « 1 » posé à la main tient, le temps n'avance qu'avec l'horloge.
        FollowBarFor(clock, (clock.BeatInBar % 4) + 1, 160);
        (clock.BeatIndex - afterManual).ShouldBeInRange(7, 9);

        // Un autre morceau (tempo très différent) lève la garde : l'écoute reprend la main sur le premier temps.
        clock.FollowAudio(new AudioReading(true, 90, 0.9, true, 0, 2, 0, 0));
        var restart = clock.BeatIndex;
        for (var i = 0; i < 80; i++)
        {
            clock.Advance(0.025);
            clock.FollowAudio(new AudioReading(true, 90, 0.9, true, clock.BeatPosition - Math.Floor(clock.BeatPosition), (clock.BeatInBar % 4) + 1, 0, 0));
        }

        (clock.BeatIndex - restart).ShouldBeGreaterThan(3, "deux secondes à 90 BPM = 3 temps ; le désaccord a été corrigé en plus");
    }

    private static MusicalClock AudioClock(double raw, int ticks)
    {
        var clock = new MusicalClock();
        clock.UseSource(TempoSourceKind.Audio);
        FeedRaw(clock, raw, ticks);
        return clock;
    }

    private static void FeedRaw(MusicalClock clock, double raw, int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            clock.Advance(0.025);
            clock.FollowAudio(new AudioReading(true, raw, 0.9, true, clock.BeatPosition - Math.Floor(clock.BeatPosition), 0, 0, 0));
        }
    }

    [Fact]
    [Trait("Exigence", "AUD-023")]
    public void Audio_FollowHeard_ForgetsTheCorrections_AndGivesTheMusicsTempoBack()
    {
        var clock = AudioClock(90, 40);
        clock.HeardBpm.ShouldBe(90, 0.01);

        clock.Scale(2);
        clock.Scale(2);
        FeedRaw(clock, 90, 40);
        clock.Bpm.ShouldBe(360, 1, "deux clics sur ×2 : ×4");
        clock.HeardBpm.ShouldBe(90, 0.01, "le tempo entendu ne bouge pas");

        clock.FollowHeard();
        clock.Bpm.ShouldBe(90, 0.01);
        FeedRaw(clock, 90, 80);
        clock.Bpm.ShouldBe(90, 0.5, "la correction est oubliée pour la suite du morceau");

        clock.UseSource(TempoSourceKind.Fixed);
        clock.HeardBpm.ShouldBe(0, "hors source Audio, pas de tempo entendu");
    }

    [Fact]
    [Trait("Exigence", "AUD-023")]
    public void Audio_TimesTwo_IsKeptWhileTheSameSongPlays()
    {
        var clock = AudioClock(64, 40);
        clock.Bpm.ShouldBe(64, 0.5);

        clock.Scale(2);
        FeedRaw(clock, 64, 80);

        clock.Bpm.ShouldBe(128, 0.5);
    }

    [Fact]
    [Trait("Exigence", "AUD-023")]
    public void Audio_TimesTwo_BeatsFollowTheDoubledTempo_NotTheMusicsOwn()
    {
        // Musique à 100 BPM dont la grille est connue ; l'utilisateur double : l'horloge doit battre deux fois par temps entendu,
        // sur les temps entendus (essai P7, exemple 28 b : les voyants suivaient le tempo d'origine).
        var clock = new MusicalClock();
        clock.UseSource(TempoSourceKind.Audio);
        double music = 0;
        void Run(int ticks, Action? each = null)
        {
            for (var i = 0; i < ticks; i++)
            {
                music += 0.025 * 100 / 60.0;
                clock.Advance(0.025);
                clock.FollowAudio(new AudioReading(true, 100, 0.9, true, music - Math.Floor(music), 0, 0, 0));
                each?.Invoke();
            }
        }

        Run(120);
        clock.Scale(2);
        Run(80);

        var crossed = 0;
        var worst = 0.0;
        Run(160, () =>
        {
            crossed += clock.BeatsCrossed;
            var target = 2 * music;
            var error = (clock.BeatPosition - target) % 1;
            error -= Math.Round(error);
            worst = Math.Max(worst, Math.Abs(error));
        });

        // 4 s à 100 BPM doublé : 4 × 200 / 60 = 13,3 temps de l'horloge ; la phase reste calée sur les temps entendus doublés.
        crossed.ShouldBeInRange(12, 15);
        worst.ShouldBeLessThan(0.2);
    }

    [Fact]
    [Trait("Exigence", "AUD-023")]
    public void Audio_TimesTwo_FollowsTheAnalysisWhenItChangesOctaveItself()
    {
        var clock = AudioClock(64, 40);
        clock.Scale(2);
        FeedRaw(clock, 64, 20);

        // L'analyse bascule d'elle-même à 128 : la correction de l'utilisateur ne doit pas doubler une seconde fois.
        FeedRaw(clock, 128, 80);

        clock.Bpm.ShouldBe(128, 0.5);
    }

    [Fact]
    [Trait("Exigence", "AUD-026")]
    public void Audio_NewSong_KeepsTheOctaveCorrection_UntilTheHeardTempoIsClicked()
    {
        var clock = AudioClock(64, 40);
        clock.Scale(2);
        FeedRaw(clock, 64, 20);

        FeedRaw(clock, 70, 40);
        clock.Bpm.ShouldBe(140, 0.5, "essai P8 : le coefficient ×2 est maintenu au morceau suivant");

        clock.FollowHeard();
        FeedRaw(clock, 70, 40);
        clock.Bpm.ShouldBe(70, 0.5);
    }

    [Fact]
    [Trait("Exigence", "AUD-005")]
    public void Audio_OctaveCorrection_SurvivesASilence()
    {
        var clock = AudioClock(64, 40);
        clock.Scale(2);
        for (var i = 0; i < 70; i++)
        {
            clock.Advance(0.025);
            clock.FollowAudio(new AudioReading(false, 0, 0, false, 0, 0, 0, 0));
        }

        FeedRaw(clock, 64, 80);

        clock.Bpm.ShouldBe(128, 1, "essai P8 : le coefficient est maintenu après un silence");
    }

    [Fact]
    [Trait("Exigence", "AUD-020")]
    public void OtherSources_IgnoreTheFeed()
    {
        var engine = new EngineHarness(new ShowBuilder().Build());
        engine.Engine.SetAudioFeed(new FakeFeed { Reading = new AudioReading(true, 150, 1, true, 0, 1, 0, 0) });
        engine.Run(1);

        engine.Engine.Bpm.ShouldBe(120);
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

    [Fact]
    [Trait("Exigence", "EVT-024")]
    public void TempoChanged_IsPublished_OnlyForARealChange()
    {
        var bus = new CapturingBus();
        var engine = new EngineHarness(new ShowBuilder().Build(), bus: bus);
        engine.Tick();
        bus.Of<Messaging.Events.TempoChanged>().ShouldHaveSingleItem().Bpm.ShouldBe(120);

        engine.Send(new SetTempoSourceCommand(CommandOrigin.Tool, TempoSourceKind.Fixed, 120.5));
        engine.Run(0.5);
        bus.Of<Messaging.Events.TempoChanged>().Count.ShouldBe(1, "moins de 1 BPM : rien");

        engine.Send(new SetTempoSourceCommand(CommandOrigin.Tool, TempoSourceKind.Fixed, 128));
        engine.Run(0.5);
        bus.Of<Messaging.Events.TempoChanged>()[^1].Bpm.ShouldBe(128);
        bus.Of<Messaging.Events.TempoChanged>().Count.ShouldBe(2, "un seul événement, pas un par tick");
    }
}
