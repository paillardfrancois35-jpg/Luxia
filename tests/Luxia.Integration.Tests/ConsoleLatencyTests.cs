using System.Diagnostics;
using Luxia.Core.Dmx;
using Luxia.Core.Time;
using Luxia.Engine;
using Luxia.Engine.Timing;
using Luxia.Messaging.Commands;
using Luxia.Output;
using Luxia.Output.Recording;

namespace Luxia.Integration.Tests;

/// <summary>T-CONS-03 et T-CONS-04 : un fader réglé se retrouve dans la trame émise, en moins de 50 ms.</summary>
public sealed class ConsoleLatencyTests
{
    [Fact]
    [Trait("Exigence", "CONS-003")]
    [Trait("Exigence", "GEN-090")]
    public void Override_ReachesDriver_InLessThan50Milliseconds()
    {
        var clock = new SystemClock();
        using var router = new OutputRouter();
        var probe = new ProbeDriver(channel: 42, value: 99);
        router.Attach(1, probe);
        var engine = new RenderEngine(router, clock);
        using var loop = new TickLoop(engine.Tick, clock);
        loop.Start();
        SpinWait.SpinUntil(() => probe.Status.State == Messaging.Events.OutputConnectionState.Connected, 2000).ShouldBeTrue();

        var latencies = new List<TimeSpan>();
        for (var i = 0; i < 10; i++)
        {
            probe.Reset();
            var sent = Stopwatch.GetTimestamp();
            engine.Send(new OverrideChannelsCommand(CommandOrigin.User, 1, [new ChannelValue(42, 99)]));
            probe.Seen.Wait(TimeSpan.FromSeconds(1)).ShouldBeTrue();
            latencies.Add(Stopwatch.GetElapsedTime(sent, probe.SeenAt));
            engine.Send(ReleaseOverridesCommand.All(CommandOrigin.User));
            Thread.Sleep(60);
        }

        loop.Stop();
        latencies.Max().ShouldBeLessThan(TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    [Trait("Exigence", "CONS-003")]
    [Trait("Exigence", "SORT-060")]
    public void Override_AppearsInRecordedFrame_OnNextTick()
    {
        var clock = new VirtualClock();
        using var stream = new MemoryStream();
        var writer = new RecordingWriter(stream, 1, 40, DateTime.UtcNow);
        var engine = new RenderEngine(new WriterSink(writer), clock);

        engine.Tick();
        engine.Send(new OverrideChannelsCommand(CommandOrigin.User, 1, [new ChannelValue(7, 123)]));
        clock.Advance(TimeSpan.FromMilliseconds(25));
        engine.Tick();

        stream.Position = 0;
        var (_, frames) = RecordingReader.ReadAll(stream);
        frames[0].Values[6].ShouldBe((byte)0);
        frames[1].Values[6].ShouldBe((byte)123);
        frames[1].Elapsed.ShouldBe(TimeSpan.FromMilliseconds(25));
    }

    private sealed class WriterSink(RecordingWriter writer) : IFrameSink
    {
        public void Submit(int universe, DmxFrame frame, TimeSpan timestamp) => writer.Append(frame.ReadOnlyValues, timestamp);
    }

    private sealed class ProbeDriver(int channel, byte value) : OutputDriver("sonde", "Sonde", null)
    {
        public ManualResetEventSlim Seen { get; } = new(false);

        public long SeenAt { get; private set; }

        public void Reset() => Seen.Reset();

        protected override bool Connect() => true;

        protected override void Write(ReadOnlySpan<byte> frame, TimeSpan timestamp)
        {
            if (!Seen.IsSet && frame[channel - 1] == value)
            {
                SeenAt = Stopwatch.GetTimestamp();
                Seen.Set();
            }
        }

        protected override void Disconnect()
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Seen.Dispose();
            }
        }
    }
}
