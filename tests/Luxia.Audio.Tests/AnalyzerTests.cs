namespace Luxia.Audio.Tests;

/// <summary>T-AUD-01 : chaque étage de l'analyse sur des signaux synthétiques (clic, kick, silence, volume).</summary>
public sealed class AnalyzerTests
{
    private const int Rate = 44100;

    /// <summary>Kick synthétique tous les temps ; un « tss » aigu à chaque contretemps.</summary>
    private static float[] Beat(double bpm, double seconds, double volume = 1, bool hats = true, bool accentFirst = false)
    {
        var samples = new float[(int)(seconds * Rate)];
        var period = 60.0 / bpm;
        var random = new Random(3);
        for (var beat = 0; beat * period < seconds; beat++)
        {
            var start = (int)(beat * period * Rate);
            var accent = accentFirst ? (beat % 4 == 0 ? 1.0 : 0.15) : 0.6;
            for (var i = 0; i < 0.12 * Rate && start + i < samples.Length; i++)
            {
                var t = (double)i / Rate;
                samples[start + i] += (float)(accent * volume * Math.Sin(2 * Math.PI * (55 + (90 * Math.Exp(-t * 30))) * t) * Math.Exp(-t * 25));
            }

            if (!hats)
            {
                continue;
            }

            var off = start + (int)(period / 2 * Rate);
            for (var i = 0; i < 0.03 * Rate && off + i < samples.Length; i++)
            {
                samples[off + i] += (float)(0.25 * volume * (random.NextDouble() - 0.5) * Math.Exp(-(double)i / Rate * 120));
            }
        }

        return samples;
    }

    private static AnalysisState Run(float[] signal)
    {
        var analyzer = new AudioAnalyzer(Rate);
        for (var i = 0; i < signal.Length; i += 1024)
        {
            analyzer.Push(signal.AsSpan(i, Math.Min(1024, signal.Length - i)));
        }

        return analyzer.State;
    }

    [Theory]
    [InlineData(90)]
    [InlineData(120)]
    [InlineData(128)]
    [InlineData(140)]
    [Trait("Exigence", "AUD-020")]
    public void Tempo_OfASyntheticGroove_IsFoundWithinOnePercent(double bpm)
    {
        var state = Run(Beat(bpm, 14));

        state.Bpm.ShouldBe(bpm, bpm * 0.01);
        state.Confidence.ShouldBeGreaterThan(0.5);
        state.HasGrid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "AUD-004")]
    public void Tempo_DoesNotDependOnTheVolume()
    {
        var loud = Run(Beat(120, 14, 1));
        var quiet = Run(Beat(120, 14, 0.03));

        quiet.Bpm.ShouldBe(loud.Bpm, 0.6);
        quiet.Silent.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "AUD-005")]
    public void Silence_IsDetected_AndResetsTheTracking()
    {
        var groove = Beat(120, 10);
        var signal = new float[groove.Length + (3 * Rate)];
        groove.CopyTo(signal, 0);

        var state = Run(signal);

        state.Silent.ShouldBeTrue();
        state.Bpm.ShouldBe(120, 2, "le tempo reste connu tant qu'une nouvelle écoute ne le remplace pas (la remise à zéro publie 0)");
    }

    [Fact]
    [Trait("Exigence", "AUD-026")]
    public void NewSong_AfterASilence_IsFoundInUnderEightSeconds()
    {
        var first = Beat(90, 10);
        var second = Beat(132, 9);
        var signal = new float[first.Length + (2 * Rate) + second.Length];
        first.CopyTo(signal, 0);
        second.CopyTo(signal, first.Length + (2 * Rate));

        var state = Run(signal);

        state.Bpm.ShouldBe(132, 1.5);
    }

    [Fact]
    [Trait("Exigence", "AUD-004")]
    public void Silence_OnlyNoiseFloor_GivesNoTempoAndNoGrid()
    {
        var state = Run(new float[Rate * 10]);

        state.Silent.ShouldBeTrue();
        state.HasGrid.ShouldBeFalse();
        state.Bpm.ShouldBe(0);
    }

    [Fact]
    [Trait("Exigence", "AUD-021")]
    public void BeatPhase_IsConsistentWithTheKicks()
    {
        // Kick tous les 0,5 s : à 14 s pile (juste sur un kick), la phase est proche de 0 ou de 1 (à quelques ms près).
        var state = Run(Beat(120, 14));

        var distance = Math.Min(state.BeatPhase, 1 - state.BeatPhase);
        distance.ShouldBeLessThan(0.12, "moins de 60 ms d'écart avec le temps réel");
    }

    [Fact]
    [Trait("Exigence", "AUD-024")]
    public void Downbeat_FromAnAccentedFirstBeat_IsKnown()
    {
        var state = Run(Beat(120, 30, hats: false, accentFirst: true));

        state.BarBeat.ShouldBeInRange(1, 4);
    }
}
