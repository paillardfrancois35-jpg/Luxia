namespace Luxia.Audio.Tests;

/// <summary>T-AUD-01 : impulsions, énergie, break et drop sur des signaux synthétiques.</summary>
public sealed class PulseAndEnergyTests
{
    private const int Rate = 44100;

    private static void Mix(float[] target, double startSeconds, double seconds, double bpm, bool kick, bool hats, double pad, double volume = 1)
    {
        var random = new Random(5);
        var start = (int)(startSeconds * Rate);
        var length = (int)(seconds * Rate);
        var period = 60.0 / bpm;
        for (var i = 0; i < length && start + i < target.Length; i++)
        {
            var t = (double)i / Rate;
            var sample = pad * (Math.Sin(2 * Math.PI * 440 * t) + (0.6 * Math.Sin(2 * Math.PI * 660 * t)));
            var inBeat = t % period;
            if (kick && inBeat < 0.12)
            {
                sample += 0.8 * Math.Sin(2 * Math.PI * (55 + (90 * Math.Exp(-inBeat * 30))) * inBeat) * Math.Exp(-inBeat * 25);
            }

            var inOff = (t + (period / 2)) % period;
            if (hats && inOff < 0.03)
            {
                sample += 0.3 * (random.NextDouble() - 0.5) * Math.Exp(-inOff * 120);
            }

            target[start + i] += (float)(sample * volume);
        }
    }

    private static (AudioAnalyzer Analyzer, List<AudioEvent> Events) Run(float[] signal)
    {
        var analyzer = new AudioAnalyzer(Rate);
        var events = new List<AudioEvent>();
        analyzer.EventRaised += events.Add;
        for (var i = 0; i < signal.Length; i += 1024)
        {
            analyzer.Push(signal.AsSpan(i, Math.Min(1024, signal.Length - i)));
        }

        return (analyzer, events);
    }

    [Fact]
    [Trait("Exigence", "AUD-040")]
    public void Pulses_KickAndHats_AreCountedPerBand()
    {
        var signal = new float[Rate * 14];
        Mix(signal, 0, 14, 120, kick: true, hats: true, pad: 0);

        var (analyzer, _) = Run(signal);
        var (bass, treble) = analyzer.TakePulses();

        // 28 kicks et 28 charlestons en 14 s ; on en attend au moins 85 % des kicks et peu de fausses détections.
        bass.ShouldBeInRange(24, 32);
        treble.ShouldBeInRange(20, 34);
        analyzer.TakePulses().ShouldBe((0, 0), "les compteurs repartent de zéro");
    }

    [Fact]
    [Trait("Exigence", "AUD-042")]
    public void Pulses_LongDeadTime_ThinsThemOut()
    {
        var signal = new float[Rate * 14];
        Mix(signal, 0, 14, 120, kick: true, hats: false, pad: 0);
        var analyzer = new AudioAnalyzer(Rate) { BassDeadSeconds = 0.9 };
        for (var i = 0; i < signal.Length; i += 1024)
        {
            analyzer.Push(signal.AsSpan(i, Math.Min(1024, signal.Length - i)));
        }

        analyzer.TakePulses().Bass.ShouldBeLessThan(20);
    }

    [Fact]
    [Trait("Exigence", "AUD-041")]
    public void Pulses_CarryAStrength_BetweenZeroAndOne()
    {
        var signal = new float[Rate * 10];
        Mix(signal, 0, 10, 120, kick: true, hats: false, pad: 0);

        var (analyzer, _) = Run(signal);

        analyzer.State.BassPulseStrength.ShouldBeInRange(0.01, 1);
    }

    [Fact]
    [Trait("Exigence", "AUD-060")]
    public void Energy_ALoudGroove_IsHigherThanAQuietPad()
    {
        var calm = new float[Rate * 20];
        Mix(calm, 0, 20, 120, kick: false, hats: false, pad: 0.05);
        var groove = new float[Rate * 20];
        Mix(groove, 0, 6, 120, kick: false, hats: false, pad: 0.05);
        Mix(groove, 6, 14, 120, kick: true, hats: true, pad: 0.05);

        var quiet = Run(calm).Analyzer.State;
        var loud = Run(groove).Analyzer.State;

        loud.Energy.ShouldBeGreaterThan(quiet.Energy + 0.2);
        loud.EnergyLevel.ShouldNotBe(EnergyLevel.Calm);
    }

    [Fact]
    [Trait("Exigence", "AUD-004")]
    public void Energy_DoesNotDependOnTheVolume()
    {
        var loud = new float[Rate * 25];
        Mix(loud, 0, 6, 120, kick: false, hats: false, pad: 0.05);
        Mix(loud, 6, 19, 120, kick: true, hats: true, pad: 0.05);
        var quiet = new float[Rate * 25];
        Mix(quiet, 0, 6, 120, kick: false, hats: false, pad: 0.05, volume: 0.1);
        Mix(quiet, 6, 19, 120, kick: true, hats: true, pad: 0.05, volume: 0.1);

        var a = Run(loud).Analyzer.State;
        var b = Run(quiet).Analyzer.State;

        b.Energy.ShouldBe(a.Energy, 0.2);
    }

    [Fact]
    [Trait("Exigence", "AUD-062")]
    public void BreakThenDrop_AreDetected_InOrder()
    {
        // 14 s de groove, 7 s de break (plus de basses ni de kick, un simple nappage), puis retour brutal.
        var signal = new float[Rate * 36];
        Mix(signal, 0, 14, 120, kick: true, hats: true, pad: 0.03);
        Mix(signal, 14, 7, 120, kick: false, hats: false, pad: 0.03, volume: 0.6);
        Mix(signal, 21, 15, 120, kick: true, hats: true, pad: 0.03);

        var (_, events) = Run(signal);

        var kinds = events.Where(e => e.Kind is AudioEventKind.Break or AudioEventKind.Drop).ToList();
        kinds.Select(e => e.Kind).ShouldBe([AudioEventKind.Break, AudioEventKind.Drop]);
        kinds[0].Seconds.ShouldBeInRange(15, 21);
        kinds[1].Seconds.ShouldBeInRange(20.5, 24);
    }

    [Fact]
    [Trait("Exigence", "AUD-005")]
    public void Silence_RaisesAnEvent_AndResumeAnother()
    {
        var signal = new float[Rate * 14];
        Mix(signal, 0, 5, 120, kick: true, hats: false, pad: 0);
        Mix(signal, 9, 5, 120, kick: true, hats: false, pad: 0);

        var (_, events) = Run(signal);

        events.Select(e => e.Kind).ShouldContain(AudioEventKind.Silence);
        events.Select(e => e.Kind).ShouldContain(AudioEventKind.Resumed);
        events.First(e => e.Kind == AudioEventKind.Silence).Seconds.ShouldBeInRange(5, 7);
    }

    [Fact]
    [Trait("Exigence", "AUD-061")]
    public void EnergyLevel_DoesNotOscillate_OnAStableGroove()
    {
        var signal = new float[Rate * 40];
        Mix(signal, 0, 40, 120, kick: true, hats: true, pad: 0.03);

        var (_, events) = Run(signal);

        events.Count(e => e.Kind == AudioEventKind.EnergyChanged).ShouldBeLessThan(6);
    }

    private static void Kick(float[] target, double atSeconds, double amplitude)
    {
        var start = (int)(atSeconds * Rate);
        for (var i = 0; i < 0.12 * Rate && start + i < target.Length; i++)
        {
            var time = (double)i / Rate;
            target[start + i] += (float)(amplitude * Math.Sin(2 * Math.PI * (55 + (90 * Math.Exp(-time * 30))) * time) * Math.Exp(-time * 25));
        }
    }

    [Fact]
    [Trait("Exigence", "AUD-042")]
    public void Sensitivity_ChangesTheNumberOfPulses_Visibly()
    {
        // Kicks forts et faibles en alternance : une sensibilité basse ne garde que les forts, une haute garde tout.
        var signal = new float[Rate * 24];
        for (var beat = 0; beat < 48; beat++)
        {
            Kick(signal, beat * 0.5, beat % 2 == 0 ? 0.8 : 0.3);
        }

        int Count(double sensitivity)
        {
            var analyzer = new AudioAnalyzer(Rate) { PulseSensitivity = sensitivity };
            for (var i = 0; i < signal.Length; i += 1024)
            {
                analyzer.Push(signal.AsSpan(i, Math.Min(1024, signal.Length - i)));
            }

            return analyzer.TakePulses().Bass;
        }

        var low = Count(0.1);
        var high = Count(0.9);
        high.ShouldBeGreaterThan(low + 8, "la sensibilité doit se voir (essai P7, exemple 20)");
    }

    [Fact]
    [Trait("Exigence", "AUD-040")]
    public void Pulses_BassGuitarNotes_AreNotKicks_ButDrumKicksAre()
    {
        // 12 s de notes de basse (attaque douce, tenues, sans claquement), puis 12 s de kicks avec leur claquement (essai P7, exemple 19).
        var signal = new float[Rate * 24];
        var random = new Random(3);
        for (var beat = 0; beat < 24; beat++)
        {
            var note = beat % 2 == 0 ? 55.0 : 73.4;
            var start = (int)(beat * 0.5 * Rate);
            for (var i = 0; i < 0.45 * Rate && start + i < signal.Length; i++)
            {
                var t = (double)i / Rate;
                var envelope = Math.Min(1, t / 0.02) * Math.Exp(-t * 4);
                signal[start + i] += (float)(0.7 * envelope * (Math.Sin(2 * Math.PI * note * t) + (0.3 * Math.Sin(2 * Math.PI * 2 * note * t))));
            }
        }

        for (var beat = 0; beat < 24; beat++)
        {
            var at = 12 + (beat * 0.5);
            Kick(signal, at, 0.8);
            var start = (int)(at * Rate);
            for (var i = 0; i < 0.02 * Rate; i++)
            {
                signal[start + i] += (float)(0.5 * (random.NextDouble() - 0.5) * Math.Exp(-i / (0.004 * Rate)));
            }
        }

        var analyzer = new AudioAnalyzer(Rate);
        for (var i = 0; i < 12 * Rate; i += 1024)
        {
            analyzer.Push(signal.AsSpan(i, Math.Min(1024, (12 * Rate) - i)));
        }

        var notes = analyzer.TakePulses().Bass;
        for (var i = 12 * Rate; i < signal.Length; i += 1024)
        {
            analyzer.Push(signal.AsSpan(i, Math.Min(1024, signal.Length - i)));
        }

        var kicks = analyzer.TakePulses().Bass;
        notes.ShouldBeLessThan(6, $"des notes de basse ne sont pas des kicks ({notes} notes, {kicks} kicks)");
        kicks.ShouldBeGreaterThan(17, $"les coups de batterie sont comptés ({notes} notes, {kicks} kicks)");
    }

    [Fact]
    [Trait("Exigence", "AUD-040")]
    public void Pulses_NoBassPassage_GivesNoBassPulse()
    {
        // 12 s de kicks, puis 12 s sans basses (nappe et charleston) : aucune impulsion des basses dans la seconde partie.
        var signal = new float[Rate * 24];
        Mix(signal, 0, 12, 120, kick: true, hats: false, pad: 0.03);
        Mix(signal, 12, 12, 120, kick: false, hats: true, pad: 0.03);
        var analyzer = new AudioAnalyzer(Rate);
        for (var i = 0; i < 12 * Rate; i += 1024)
        {
            analyzer.Push(signal.AsSpan(i, Math.Min(1024, (12 * Rate) - i)));
        }

        analyzer.TakePulses();
        for (var i = 12 * Rate; i < signal.Length; i += 1024)
        {
            analyzer.Push(signal.AsSpan(i, Math.Min(1024, signal.Length - i)));
        }

        analyzer.TakePulses().Bass.ShouldBeLessThan(3, "essai P7, exemple 19 : des impulsions sur les passages sans basses");
    }

    [Fact]
    [Trait("Exigence", "AUD-061")]
    public void EnergyLevel_OnAStablePassage_StaysStable()
    {
        var signal = new float[Rate * 60];
        Mix(signal, 0, 20, 120, kick: false, hats: false, pad: 0.04);
        Mix(signal, 20, 40, 120, kick: true, hats: true, pad: 0.04);

        var (_, events) = Run(signal);

        // Une fois le groove établi (après 30 s), le niveau ne change plus (essai P7, exemple 22).
        events.Count(e => e.Kind == AudioEventKind.EnergyChanged && e.Seconds > 32).ShouldBeLessThan(2);
    }

    [Fact]
    [Trait("Exigence", "AUD-020")]
    public void Tempo_AfterAPause_DoesNotDropBeforeComingBack()
    {
        var groove = new float[Rate * 10];
        Mix(groove, 0, 10, 120, kick: true, hats: true, pad: 0);
        var analyzer = new AudioAnalyzer(Rate);
        void PushAll(float[] samples)
        {
            for (var i = 0; i < samples.Length; i += 1024)
            {
                analyzer.Push(samples.AsSpan(i, Math.Min(1024, samples.Length - i)));
            }
        }

        PushAll(groove);
        analyzer.State.Bpm.ShouldBe(120, 2);

        PushAll(new float[Rate * 4]);

        // Reprise du même morceau : à aucun moment le tempo ne s'écarte de plus de 5 % (essai P7, exemple 17).
        var worst = 0.0;
        for (var i = 0; i < groove.Length; i += 1024)
        {
            analyzer.Push(groove.AsSpan(i, Math.Min(1024, groove.Length - i)));
            var bpm = analyzer.State.Bpm;
            if (bpm > 0)
            {
                worst = Math.Max(worst, Math.Abs(bpm - 120));
            }
        }

        worst.ShouldBeLessThan(6);
    }
}
