using Dmx.Core.Dmx;
using Dmx.Core.Time;
using Dmx.Engine;
using Dmx.Messaging.Commands;
using Dmx.Output;
using Dmx.Output.Recording;

namespace Dmx.Integration.Tests;

/// <summary>
/// Non-régression des exemples de P0 (doc 40 §7, DEMO-3 ; doc 41 REF-4) : l'enregistrement de référence du
/// chenillard 1-180 est rejoué en temps virtuel et comparé à ce que produit le moteur aujourd'hui.
/// </summary>
public sealed class ReferenceShowP0Tests
{
    private static readonly string Recording = Path.Combine(
        AppContext.BaseDirectory, "samples", "Show de référence", "Enregistrements", "P0-chenillard-1-180.dmxrec");

    [Fact]
    [Trait("Exigence", "SORT-060")]
    [Trait("Exigence", "SORT-007")]
    public void ReferenceRecording_LightsChannels1To179InOrder_NeverSmoke()
    {
        var (header, frames) = RecordingReader.ReadAll(Recording);

        header.Universe.ShouldBe((ushort)1);
        frames.ShouldAllBe(f => f.Values[179] == 0);
        LitSequence(frames.Select(f => f.Values)).ShouldBe(Enumerable.Range(1, 179));
        frames.Where(f => f.Values.Any(v => v != 0)).ShouldAllBe(f => f.Values.Count(v => v != 0) == 1 && f.Values.Max() == 128);
    }

    [Fact]
    [Trait("Exigence", "GEN-001")]
    [Trait("Exigence", "SORT-060")]
    public void Engine_InVirtualTime_ReproducesReferenceRecording()
    {
        var (_, reference) = RecordingReader.ReadAll(Recording);
        var clock = new VirtualClock();
        using var stream = new MemoryStream();
        var sink = new RecordingSink(new RecordingWriter(stream, 1, 40, DateTime.UtcNow));
        var engine = new RenderEngine(sink, clock);

        engine.Send(new TestOutputCommand(CommandOrigin.Tool, true, 1, new ChannelRange(1, 180), [180], 128, TimeSpan.FromMilliseconds(250), Loop: false));
        for (var i = 0; i < 47 * 40; i++)
        {
            engine.Tick();
            clock.Advance(TimeSpan.FromMilliseconds(25));
        }

        stream.Position = 0;
        var (_, produced) = RecordingReader.ReadAll(stream);

        LitSequence(produced.Select(f => f.Values)).ShouldBe(LitSequence(reference.Select(f => f.Values)));
        var lastLit = produced.Last(f => f.Values.Any(v => v != 0)).Elapsed;
        lastLit.ShouldBe(TimeSpan.FromSeconds(44.75), TimeSpan.FromMilliseconds(30));
    }

    /// <summary>Suite des canaux allumés, sans répétition consécutive.</summary>
    private static List<int> LitSequence(IEnumerable<byte[]> frames)
    {
        var sequence = new List<int>();
        foreach (var frame in frames)
        {
            var lit = Array.FindIndex(frame, v => v != 0) + 1;
            if (lit > 0 && (sequence.Count == 0 || sequence[^1] != lit))
            {
                sequence.Add(lit);
            }
        }

        return sequence;
    }

    private sealed class RecordingSink(RecordingWriter writer) : IFrameSink
    {
        public void Submit(int universe, DmxFrame frame, TimeSpan timestamp) => writer.Append(frame.ReadOnlyValues, timestamp);
    }
}
