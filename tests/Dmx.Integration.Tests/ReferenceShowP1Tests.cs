using Dmx.Core.Snapshots;
using Dmx.Core.Time;
using Dmx.Engine;
using Dmx.Messaging.Commands;
using Dmx.Persistence;

namespace Dmx.Integration.Tests;

/// <summary>Non-régression des instantanés de console du show de référence (P1, doc 41 §11).</summary>
public sealed class ReferenceShowP1Tests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");

    /// <summary>Canaux à ne jamais toucher dans un instantané de démonstration : fumée, canal Reset des lyres (BIB-095 :
    /// confirmé le 25/09 par Open Fixture Library, le canal Reset est le dernier de chaque mode, donc 121 et 136 ici).</summary>
    private static readonly int[] Forbidden = [180, 121, 136];

    public static TheoryData<string> SnapshotNames => [.. Load().Snapshots.Select(s => s.Name)];

    [Fact]
    [Trait("Exigence", "CONS-010")]
    public void ReferenceShow_LoadsWithSnapshots()
    {
        var report = ProjectStore.Open(Folder);

        report.Succeeded.ShouldBeTrue();
        report.Messages.ShouldBeEmpty();
        Load().Snapshots.Count.ShouldBeGreaterThanOrEqualTo(6);
        Load().Snapshots.ShouldAllBe(s => s.Category == "Phase P1");
    }

    [Theory]
    [MemberData(nameof(SnapshotNames))]
    [Trait("Exigence", "CONS-010")]
    public void Snapshot_RecalledByEngine_ProducesExactlyItsChannels(string name)
    {
        var snapshot = Load().Snapshots.Single(s => s.Name == name);
        var sink = new LastFrameSink();
        var engine = new RenderEngine(sink, new VirtualClock());

        engine.Send(new OverrideChannelsCommand(CommandOrigin.Tool, 1, [.. snapshot.Channels.Select(c => new ChannelValue(c.Channel, c.Value))]));
        engine.Tick();

        var expected = new byte[512];
        foreach (var c in snapshot.Channels)
        {
            expected[c.Channel - 1] = c.Value;
        }

        sink.Frame.ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(SnapshotNames))]
    public void Snapshot_NeverTouchesSmokeOrResetChannels(string name)
    {
        var snapshot = Load().Snapshots.Single(s => s.Name == name);

        snapshot.Channels.Where(c => Forbidden.Contains(c.Channel)).ShouldAllBe(c => c.Value == 0);
    }

    private static ConsoleData Load() =>
        ProjectPartStore.Load(Folder, ProjectPartStore.ConsoleFileName, ProjectPartStore.ConsoleType, () => new ConsoleData()).Value;

    private sealed class LastFrameSink : Core.Dmx.IFrameSink
    {
        public byte[] Frame { get; private set; } = [];

        public void Submit(int universe, Core.Dmx.DmxFrame frame, TimeSpan timestamp) => Frame = frame.ToArray();
    }
}
