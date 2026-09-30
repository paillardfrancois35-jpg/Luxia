using Luxia.Engine.Timing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Luxia.Audio.Tests;

/// <summary>T-AUD-04 : l'écoute branchée sur une source simulée (sans carte son) : tempo, silence, reconnexion.</summary>
public sealed class AudioListenerTests
{
    private const int Rate = 44100;

    private sealed class FakeSource : IAudioSource
    {
        public string Name => "Haut-parleurs simulés";

        public int SampleRate => Rate;

        public bool Started { get; private set; }

        public bool Disposed { get; private set; }

        public event EventHandler<AudioBlock>? BlockAvailable;

        public event EventHandler<Exception?>? Stopped;

        public void StartCapture() => Started = true;

        public void StopCapture() => Started = false;

        public void Dispose() => Disposed = true;

        public void Emit(float[] samples)
        {
            for (var i = 0; i < samples.Length; i += 1024)
            {
                BlockAvailable?.Invoke(this, new AudioBlock(samples.AsMemory(i, Math.Min(1024, samples.Length - i))));
            }
        }

        public void Stop(Exception? error) => Stopped?.Invoke(this, error);
    }

    private sealed class FakeFactory : IAudioSourceFactory
    {
        public List<FakeSource> Created { get; } = [];

        public bool Fail { get; set; }

        public IAudioSource CreateLoopback()
        {
            if (Fail)
            {
                throw new InvalidOperationException("périphérique absent");
            }

            var source = new FakeSource();
            Created.Add(source);
            return source;
        }
    }

    private static float[] Kicks(double bpm, double seconds)
    {
        var samples = new float[(int)(seconds * Rate)];
        var period = 60.0 / bpm;
        for (var beat = 0; beat * period < seconds; beat++)
        {
            var start = (int)(beat * period * Rate);
            for (var i = 0; i < 0.12 * Rate && start + i < samples.Length; i++)
            {
                var t = (double)i / Rate;
                samples[start + i] = (float)(0.7 * Math.Sin(2 * Math.PI * (55 + (90 * Math.Exp(-t * 30))) * t) * Math.Exp(-t * 25));
            }
        }

        return samples;
    }

    [Fact]
    [Trait("Exigence", "AUD-001")]
    public void Listening_AnalysesTheSource_AndFeedsTheEngine()
    {
        var factory = new FakeFactory();
        using var listener = new AudioListener(factory, NullLogger.Instance);
        listener.Start();
        factory.Created.Count.ShouldBe(1);
        factory.Created[0].Started.ShouldBeTrue();

        factory.Created[0].Emit(Kicks(124, 12));
        var reading = listener.Read();

        listener.Status.ShouldContain("Haut-parleurs simulés");
        reading.Live.ShouldBeTrue();
        reading.Bpm.ShouldBe(124, 1.5);
        reading.HasGrid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "AUD-005")]
    public void Listening_WithoutBlocks_IsNotLive()
    {
        var factory = new FakeFactory();
        using var listener = new AudioListener(factory, NullLogger.Instance);
        listener.Start();

        listener.Read().Live.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "AUD-002")]
    public async Task DeviceChange_ReconnectsOnTheNewDevice()
    {
        var factory = new FakeFactory();
        using var listener = new AudioListener(factory, NullLogger.Instance);
        listener.Start();

        factory.Created[0].Stop(null);

        factory.Created[0].Disposed.ShouldBeTrue();
        await WaitAsync(() => factory.Created.Count == 2);
        factory.Created[1].Started.ShouldBeTrue();
        listener.IsListening.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "AUD-006")]
    public async Task CaptureError_DoesNotThrow_AndRetries()
    {
        var factory = new FakeFactory { Fail = true };
        using var listener = new AudioListener(factory, NullLogger.Instance);

        listener.Start();

        listener.Status.ShouldContain("impossible");
        listener.Read().Live.ShouldBeFalse();
        factory.Fail = false;
        await WaitAsync(() => factory.Created.Count == 1, 4000);
        listener.Status.ShouldContain("Haut-parleurs simulés");
    }

    [Fact]
    [Trait("Exigence", "AUD-006")]
    public void Stop_DisposesTheSource_AndReadsSilence()
    {
        var factory = new FakeFactory();
        using var listener = new AudioListener(factory, NullLogger.Instance);
        listener.Start();
        factory.Created[0].Emit(Kicks(120, 10));

        listener.Stop();

        factory.Created[0].Disposed.ShouldBeTrue();
        listener.IsListening.ShouldBeFalse();
        listener.Read().Live.ShouldBeFalse();
    }

    private static async Task WaitAsync(Func<bool> condition, int milliseconds = 2000)
    {
        var until = DateTime.UtcNow.AddMilliseconds(milliseconds);
        while (!condition() && DateTime.UtcNow < until)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        condition().ShouldBeTrue();
    }
}
