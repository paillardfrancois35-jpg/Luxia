using Luxia.Messaging.Commands;
using static Luxia.Midi.Tests.MidiTestData;

namespace Luxia.Midi.Tests;

/// <summary>Détection, branchement à chaud, deux contrôleurs (MIDI-001, 005, 006) avec des ports simulés.</summary>
public sealed class MidiServiceTests
{
    private readonly FakePorts _ports = new();
    private readonly List<Command> _commands = [];

    [Fact]
    [Trait("Exigence", "MIDI-001")]
    [Trait("Exigence", "MIDI-005")]
    [Trait("Exigence", "GEN-072")]
    public void BothModels_AreDetected_AndDriveTheEngine()
    {
        using var service = Service();
        _ports.Plug("APC MINI");
        _ports.Plug("APC mini mk2");

        service.Scan();

        service.Connected.Count.ShouldBe(2);
        _ports.Receive("APC MINI", Press(Pad(Mk1, 1, 1)));
        _ports.Receive("APC mini mk2", Press(Pad(Mk2, 2, 1)));
        service.DrainForTests();
        _commands.OfType<LaunchSceneCommand>().Select(c => c.SceneId).ShouldBe([Red, Circle]);
    }

    [Fact]
    [Trait("Exigence", "MIDI-006")]
    [Trait("Exigence", "GEN-073")]
    [Trait("Exigence", "MIDI-003")]
    public void Unplug_ThenReplug_RestoresTheLeds()
    {
        using var service = Service();
        _ports.Plug("APC mini mk2");
        service.Scan();
        service.UpdateLeds();
        _ports.Sent("APC mini mk2").Count.ShouldBe(80);

        _ports.Unplug("APC mini mk2");
        service.Scan();
        service.Connected.ShouldBeEmpty();

        _ports.Plug("APC mini mk2");
        service.Scan();
        service.UpdateLeds();
        service.Connected.ShouldHaveSingleItem();
        _ports.Sent("APC mini mk2").Count.ShouldBe(80, "toutes les LED renvoyées au rebranchement");
    }

    [Fact]
    [Trait("Exigence", "MIDI-003")]
    public void Dispose_TurnsAllLedsOff()
    {
        var service = Service();
        _ports.Plug("APC MINI");
        service.Scan();
        service.UpdateLeds();

        service.Dispose();

        _ports.LastSent("APC MINI").ShouldAllBe(m => m.Data2 == 0);
    }

    [Fact]
    [Trait("Exigence", "ERG-038")]
    [Trait("Exigence", "MIDI-005")]
    public void WithDimmers_TheSecondPlatineServesThemWhileTheFirstKeepsTheScenes()
    {
        var slot = new MidiDimmerSlot(Guid.NewGuid(), "PAR");
        var layout = Layout() with { Dimmers = [slot] };
        using var service = new MidiService(_ports, new Sink(_commands), () => Snapshot(), () => layout);
        _ports.Plug("APC MINI");
        _ports.Plug("APC mini mk2");
        service.Scan();

        service.Connected.ShouldBe(["APC mini MK1 (APC MINI)", "APC mini MK2 (APC mini mk2) – dimmers de groupe"]);
        _ports.Receive("APC MINI", Press(Pad(Mk1, 1, 1)));
        _ports.Receive("APC mini mk2", Press(Pad(Mk2, 2, 1)));
        _ports.Receive("APC mini mk2", Fader(Mk2, 1, 1));
        service.DrainForTests();

        _commands.OfType<LaunchSceneCommand>().Select(c => c.SceneId).ShouldBe([Red], "seule la première platine lance des scènes");
        _commands.OfType<SetGroupDimmerCommand>().Select(c => c.GroupId).ShouldBe([slot.GroupId]);
    }

    private MidiService Service() => new(_ports, new Sink(_commands), () => Snapshot(), () => Layout());

    private sealed class Sink(List<Command> commands) : ICommandSink
    {
        public void Send(Command command) => commands.Add(command);
    }

    private sealed class FakePorts : IMidiPorts
    {
        private readonly List<string> _present = [];
        private readonly Dictionary<string, Action<MidiMessage>> _receivers = [];
        private readonly Dictionary<string, List<MidiMessage>> _sent = [];

        public void Plug(string name)
        {
            _present.Add(name);
            _sent[name] = [];
        }

        public void Unplug(string name) => _present.Remove(name);

        public void Receive(string name, MidiMessage message) => _receivers[name](message);

        public List<MidiMessage> Sent(string name) => _sent[name];

        public List<MidiMessage> LastSent(string name) => [.. _sent[name].TakeLast(80)];

        public IReadOnlyList<string> Inputs() => [.. _present];

        public IReadOnlyList<string> Outputs() => [.. _present];

        public IDisposable OpenInput(string name, Action<MidiMessage> received)
        {
            _receivers[name] = received;
            return new Closer();
        }

        public IMidiOutput OpenOutput(string name) => new Output(_sent[name]);

        private sealed class Closer : IDisposable
        {
            public void Dispose()
            {
            }
        }

        private sealed class Output(List<MidiMessage> sent) : IMidiOutput
        {
            public void Send(MidiMessage message) => sent.Add(message);

            public void Dispose()
            {
            }
        }
    }
}
