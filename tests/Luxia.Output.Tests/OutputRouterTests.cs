using System.Diagnostics;
using Luxia.Core.Dmx;
using Luxia.Messaging.Events;
using Luxia.Output.Drivers;

namespace Luxia.Output.Tests;

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
        // Le pilote lent reste bloqué dans sa première écriture tant que le test ne le libère pas : pas de délai réel, donc
        // aucune dépendance à la charge de la machine (l'ancienne version, à 200 ms d'écriture et 25 ms entre trames,
        // comptait des trames sur une fenêtre de temps réel et échouait parfois pendant la série complète, docs/03 §11).
        using var gate = new ManualResetEventSlim(false);
        using var router = new OutputRouter();
        var slow = new CollectingDriver("lent", gate);
        var fast = new CollectingDriver("rapide");
        router.Attach(1, slow);
        router.Attach(1, fast);

        // Libéré quoi qu'il arrive : sinon l'arrêt du routeur attendrait indéfiniment le pilote bloqué.
        try
        {
            await Eventually(() => slow.Status.State == OutputConnectionState.Connected && fast.Status.State == OutputConnectionState.Connected);

            var frame = new DmxFrame { [1] = 1 };
            router.Submit(1, frame, TimeSpan.Zero);
            await Eventually(() => slow.IsBlocked && fast.Last[0] == 1);

            // Pendant que le lent est bloqué : chaque dépôt rend la main aussitôt et le rapide reçoit chaque trame.
            var submitting = new Stopwatch();
            for (var i = 2; i <= 40; i++)
            {
                frame[1] = (byte)i;
                submitting.Start();
                router.Submit(1, frame, TimeSpan.FromMilliseconds(25 * i));
                submitting.Stop();
                await Eventually(() => fast.Last[0] == i);
            }

            fast.Count.ShouldBe(40);
            slow.Count.ShouldBe(0);
            submitting.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1), "Submit ne doit jamais attendre un pilote");

            // Libéré, le lent termine la trame 1 puis n'écrit que la plus récente (40), sans file d'attente.
            gate.Set();
            await Eventually(() => slow.Last[0] == 40);
            slow.Count.ShouldBe(2);
            slow.Received.ShouldBe([(byte)1, (byte)40]);
        }
        finally
        {
            gate.Set();
        }
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

    private sealed class CollectingDriver(string id, ManualResetEventSlim? gate = null) : OutputDriver(id, id, null)
    {
        private readonly Lock _lock = new();
        private readonly List<byte[]> _frames = [];
        private bool _blocked;

        public bool FailNextWrite { get; set; }

        /// <summary>Vrai quand le pilote attend, dans une écriture, l'ouverture de sa barrière.</summary>
        public bool IsBlocked => Volatile.Read(ref _blocked);

        /// <summary>Canal 1 de chaque trame reçue, dans l'ordre.</summary>
        public byte[] Received
        {
            get
            {
                lock (_lock)
                {
                    return [.. _frames.Select(f => f[0])];
                }
            }
        }

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

            if (gate is not null && !gate.IsSet)
            {
                Volatile.Write(ref _blocked, true);
                gate.Wait();
                Volatile.Write(ref _blocked, false);
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
