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

        public string? FollowedDefaultId => ThrowWhenDisposed ? throw new InvalidCastException("périphérique libéré") : _followed;

        public bool ThrowWhenDisposed { get; init; }

        private readonly string? _followed;

        public FakeSource(string? followed = null) => _followed = followed;

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

        public string? DefaultId { get; set; }

        public bool ThrowWhenDisposed { get; init; }

        public string? DefaultOutputId() => DefaultId;

        public List<string?> Requested { get; } = [];

        public IReadOnlyList<AudioDeviceInfo> Devices() => [new AudioDeviceInfo("haut-parleurs", "Haut-parleurs", false), new AudioDeviceInfo("micro", "Micro USB", true)];

        public IAudioSource CreateLoopback() => Create(null);

        public IAudioSource Create(string? deviceId)
        {
            Requested.Add(deviceId);
            if (Fail)
            {
                throw new InvalidOperationException("périphérique absent");
            }

            var source = new FakeSource(deviceId is null ? DefaultId : null) { ThrowWhenDisposed = ThrowWhenDisposed };
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
    [Trait("Exigence", "AUD-002")]
    public async Task DefaultOutputChange_WithoutSystemNotification_ReconnectsAndWarns()
    {
        var factory = new FakeFactory { DefaultId = "casque" };
        using var listener = new AudioListener(factory, NullLogger.Instance);
        listener.Start();
        factory.Created.Count.ShouldBe(1);

        // Windows change la sortie par défaut sans prévenir la source (essai P7, exemple 15) : l'écoute le voit seule.
        factory.DefaultId = "haut-parleurs";
        await WaitAsync(() => factory.Created.Count == 2 && factory.Created[1].Started, 6000);

        factory.Created.Count.ShouldBe(2);
        factory.Created[1].Started.ShouldBeTrue();
        listener.Notice.ShouldNotBeNull();
        listener.Notice.ShouldContain("Changement de périphérique");
    }

    [Fact]
    [Trait("Exigence", "AUD-006")]
    public async Task RapidStartStop_WithASourceThatFailsOnceReleased_NeverThrowsOutOfATimer()
    {
        // Plantage 1.009.090 : le contrôle périodique lisait l'identifiant d'un périphérique libéré par un arrêt concurrent.
        var unhandled = new List<Exception>();
        void Handler(object? s, UnhandledExceptionEventArgs e) => unhandled.Add((Exception)e.ExceptionObject);
        AppDomain.CurrentDomain.UnhandledException += Handler;
        try
        {
            var factory = new FakeFactory { DefaultId = "casque", ThrowWhenDisposed = true };
            using var listener = new AudioListener(factory, NullLogger.Instance);
            for (var i = 0; i < 3; i++)
            {
                listener.Start();
                await Task.Delay(1400, TestContext.Current.CancellationToken);
                listener.Stop();
            }

            // La source lève à chaque lecture de son identifiant (périphérique COM libéré) : le contrôle périodique doit l'encaisser.
            listener.Start();
            await Task.Delay(1400, TestContext.Current.CancellationToken);
            listener.IsListening.ShouldBeTrue();
        }
        finally
        {
            AppDomain.CurrentDomain.UnhandledException -= Handler;
        }

        unhandled.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "AUD-006")]
    public void AFailingEventSubscriber_NeverStopsTheListening()
    {
        var factory = new FakeFactory();
        using var listener = new AudioListener(factory, NullLogger.Instance);
        listener.EventRaised += (_, _) => throw new FormatException("abonné défaillant");
        listener.Start();

        var source = factory.Created[0];
        source.Emit(Kicks(128, 20));
        source.Emit(new float[Rate * 3]);
        source.Emit(Kicks(128, 10));

        listener.RecentEvents.ShouldNotBeEmpty();
        factory.Created.Count.ShouldBe(1);
        listener.Read().Live.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "AUD-005")]
    public async Task Listening_AfterAPauseWithoutBlocks_ComesBackWhenTheMusicReturns()
    {
        var factory = new FakeFactory();
        using var listener = new AudioListener(factory, NullLogger.Instance);
        listener.Start();
        factory.Created[0].Emit(Kicks(128, 10));
        listener.State.Silent.ShouldBeFalse();

        // Pause : la boucle WASAPI ne livre plus rien pendant que le silence est numérique (essai P7, exemple 21 : drop jamais vu).
        await Task.Delay(3000, TestContext.Current.CancellationToken);
        listener.State.Silent.ShouldBeTrue();

        factory.Created[0].Emit(Kicks(128, 6));
        listener.State.Silent.ShouldBeFalse();
        listener.Read().Live.ShouldBeTrue();
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

        await WaitAsync(() => factory.Created[0].Disposed);
        await WaitAsync(() => factory.Created.Count == 2);
        factory.Created[1].Started.ShouldBeTrue();
        listener.IsListening.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "AUD-002")]
    public async Task DeviceChange_LeavesAVisibleNotice_ThenTheRecoveryOne()
    {
        var factory = new FakeFactory();
        using var listener = new AudioListener(factory, NullLogger.Instance);
        listener.Start();
        listener.Notice.ShouldBeNull();

        factory.Created[0].Stop(null);

        listener.Notice.ShouldNotBeNull().ShouldContain("Changement de périphérique");
        listener.NoticeAgeSeconds.ShouldBeLessThan(5);
        await WaitAsync(() => factory.Created.Count == 2);
        await WaitAsync(() => listener.Notice!.Contains("reprise", StringComparison.Ordinal) || listener.Status.Contains("Haut-parleurs", StringComparison.Ordinal));
        listener.Status.ShouldContain("Haut-parleurs simulés");
    }

    [Fact]
    [Trait("Exigence", "AUD-006")]
    public async Task StopWhileBlocksArrive_NeverBlocks()
    {
        // L'écoute ne doit jamais libérer la source en tenant son verrou (interblocage vu en analyse de code, C3).
        var factory = new FakeFactory();
        using var listener = new AudioListener(factory, NullLogger.Instance);
        for (var round = 0; round < 25; round++)
        {
            listener.Start();
            var source = factory.Created[^1];
            var feeding = Task.Run(() => source.Emit(Kicks(120, 3)), TestContext.Current.CancellationToken);
            await Task.Delay(5, TestContext.Current.CancellationToken);
            var stopping = Task.Run(listener.Stop, TestContext.Current.CancellationToken);
            (await Task.WhenAny(Task.WhenAll(feeding, stopping), Task.Delay(5000, TestContext.Current.CancellationToken))).IsCompleted.ShouldBeTrue();
            Task.WhenAll(feeding, stopping).IsCompleted.ShouldBeTrue("arrêt et blocs en cours se terminent");
        }
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
