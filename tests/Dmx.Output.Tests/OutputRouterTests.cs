using Dmx.Core.Dmx;
using Dmx.Messaging.Events;
using Dmx.Output.Drivers;

namespace Dmx.Output.Tests;

/// <summary>T-SORT-02 : routage, indépendance des pilotes, seule la trame la plus récente.</summary>
public sealed class OutputRouterTests
{
    [Fact]
    [Trait("Exigence", "SORT-001")]
    public async Task Submit_OneUniverseToTwoDrivers_BothReceiveIdenticalFrames()
    {
        using var router = new OutputRouter();
        var a = new CollectingDriver("a");
        var b = new CollectingDriver("b");
        router.Attach(1, a);
        router.Attach(1, b);

        var frame = new DmxFrame { [1] = 42 };
        router.Submit(1, frame, TimeSpan.Zero);

        await Eventually(() => a.Count > 0 && b.Count > 0);
        a.Last[0].ShouldBe((byte)42);
        b.Last.ShouldBe(a.Last);
    }

    [Fact]
    [Trait("Exigence", "SORT-001")]
    public async Task Submit_OtherUniverse_IsNotRouted()
    {
        using var router = new OutputRouter();
        var a = new CollectingDriver("a");
        router.Attach(2, a);

        router.Submit(1, new DmxFrame(), TimeSpan.Zero);
        await Task.Delay(100);

        a.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Exigence", "SORT-002")]
    [Trait("Exigence", "SORT-003")]
    public async Task SlowDriver_DoesNotDelayOthers_AndKeepsOnlyLatestFrame()
    {
        using var router = new OutputRouter();
        var slow = new CollectingDriver("lent", writeDelay: TimeSpan.FromMilliseconds(200));
        var fast = new CollectingDriver("rapide");
        router.Attach(1, slow);
        router.Attach(1, fast);
        await Eventually(() => slow.Status.State == OutputConnectionState.Connected && fast.Status.State == OutputConnectionState.Connected);

        var frame = new DmxFrame();
        var started = DateTime.UtcNow;
        for (var i = 1; i <= 40; i++)
        {
            frame[1] = (byte)i;
            router.Submit(1, frame, TimeSpan.FromMilliseconds(25 * i));
            await Task.Delay(25);
        }

        var submitDuration = DateTime.UtcNow - started;

        // Le pilote rapide a reçu (presque) toutes les trames ; le lent en a sauté, sans file d'attente.
        await Eventually(() => fast.Last[0] == 40 && slow.Last[0] == 40);
        fast.Count.ShouldBeGreaterThan(30);
        slow.Count.ShouldBeLessThan(15);
        submitDuration.ShouldBeLessThan(TimeSpan.FromSeconds(2));
    }

    [Fact]
    [Trait("Exigence", "SORT-004")]
    public async Task StateChanges_ArePublishedOnBus()
    {
        await using var bus = new EventBus();
        var events = new List<OutputStateChanged>();
        using var _ = bus.Subscribe<OutputStateChanged>(e => { lock (events) { events.Add(e); } });
        using var router = new OutputRouter(bus);

        router.Attach(1, new NullOutputDriver());
        await Eventually(() => { lock (events) { return events.Any(e => e.State == OutputConnectionState.Connected); } });

        events.First(e => e.State == OutputConnectionState.Connected).DriverId.ShouldBe(NullOutputDriver.DriverId);
    }

    [Fact]
    [Trait("Exigence", "SORT-005")]
    public void Submit_WithoutAnyDriver_DoesNothing()
    {
        using var router = new OutputRouter();

        Should.NotThrow(() => router.Submit(1, new DmxFrame(), TimeSpan.Zero));
    }

    [Fact]
    [Trait("Exigence", "SORT-022")]
    [Trait("Exigence", "SORT-013")]
    public async Task WriteError_GoesToErrorThenReconnects()
    {
        using var router = new OutputRouter();
        var flaky = new CollectingDriver("instable") { FailNextWrite = true };
        router.Attach(1, flaky);

        var frame = new DmxFrame();
        await Eventually(
            () =>
            {
                router.Submit(1, frame, TimeSpan.Zero);
                return flaky.Status.ErrorCount == 1 && flaky.Status.State == OutputConnectionState.Connected && flaky.Count > 0;
            },
            TimeSpan.FromSeconds(4));
    }

    internal static async Task Eventually(Func<bool> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(3));
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Condition non atteinte dans le délai.");
            }

            await Task.Delay(20);
        }
    }

    private sealed class CollectingDriver(string id, TimeSpan writeDelay = default) : OutputDriver(id, id, null)
    {
        private readonly Lock _lock = new();
        private readonly List<byte[]> _frames = [];

        public bool FailNextWrite { get; set; }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _frames.Count;
                }
            }
        }

        public byte[] Last
        {
            get
            {
                lock (_lock)
                {
                    return _frames.Count == 0 ? new byte[512] : _frames[^1];
                }
            }
        }

        protected override bool Connect() => true;

        protected override void Write(ReadOnlySpan<byte> frame, TimeSpan timestamp)
        {
            if (FailNextWrite)
            {
                FailNextWrite = false;
                throw new IOException("échec simulé");
            }

            if (writeDelay > TimeSpan.Zero)
            {
                Thread.Sleep(writeDelay);
            }

            lock (_lock)
            {
                _frames.Add(frame.ToArray());
            }
        }

        protected override void Disconnect()
        {
        }
    }
}
