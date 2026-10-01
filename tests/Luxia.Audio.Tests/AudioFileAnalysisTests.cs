using NAudio.Wave;

namespace Luxia.Audio.Tests;

/// <summary>Analyse d'un fichier sans carte son (T-AUD-02) : un WAV synthétique de kicks à 120 BPM écrit dans un dossier temporaire.</summary>
public sealed class AudioFileAnalysisTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "luxia-audio-tests", Guid.NewGuid().ToString("N"));

    public AudioFileAnalysisTests() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_folder, true);
        }
        catch (IOException)
        {
            // Dossier temporaire : sans importance.
        }
    }

    [Fact]
    [Trait("Exigence", "AUD-020")]
    public void WavOfKicksAt120_IsAnalysedAt120_WithPulses()
    {
        var path = Path.Combine(_folder, "kicks.wav");
        using (var writer = new WaveFileWriter(path, new WaveFormat(44100, 16, 1)))
        {
            var period = 0.5;
            for (var i = 0; i < 44100 * 14; i++)
            {
                var t = (double)i / 44100;
                var inBeat = t % period;
                var value = inBeat < 0.12 ? 0.8 * Math.Sin(2 * Math.PI * (55 + (90 * Math.Exp(-inBeat * 30))) * inBeat) * Math.Exp(-inBeat * 25) : 0;
                writer.WriteSample((float)value);
            }
        }

        var result = AudioFileAnalysis.Analyze(path);

        result.DurationSeconds.ShouldBe(14, 0.5);
        result.Bpm.ShouldBe(120, 3);
        result.BassPulsesPerSecond.ShouldBeGreaterThan(1.2);
        result.Timeline.ShouldNotBeEmpty();
    }
}
