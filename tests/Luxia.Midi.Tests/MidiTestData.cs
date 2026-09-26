using Luxia.Engine;
using Luxia.Engine.Model;
using Luxia.Messaging.Commands;

namespace Luxia.Midi.Tests;

/// <summary>Disposition et états du moteur de test : trois couches (Couleurs, Mouvements, Flashs).</summary>
internal static class MidiTestData
{
    public static readonly Guid Colors = Guid.NewGuid();
    public static readonly Guid Moves = Guid.NewGuid();
    public static readonly Guid Flashes = Guid.NewGuid();
    public static readonly Guid Red = Guid.NewGuid();
    public static readonly Guid Blue = Guid.NewGuid();
    public static readonly Guid Circle = Guid.NewGuid();
    public static readonly Guid WhiteFlash = Guid.NewGuid();
    public static readonly Guid Strobe = Guid.NewGuid();

    public static ControllerProfile Mk1 => ControllerProfiles.All.Single(p => p.ShortName == "APC mini MK1");

    public static ControllerProfile Mk2 => ControllerProfiles.All.Single(p => p.ShortName == "APC mini MK2");

    public static MidiLayout Layout(IReadOnlyList<MidiBinding>? bindings = null) => new()
    {
        Columns =
        [
            new MidiColumn(Colors, false, [new MidiSceneSlot(Red, "#FF0000"), new MidiSceneSlot(Blue, "#0000FF")]),
            new MidiColumn(Moves, false, [new MidiSceneSlot(Circle, "#58A6FF")]),
            new MidiColumn(Flashes, true, [new MidiSceneSlot(WhiteFlash, "#FFFFFF"), new MidiSceneSlot(Strobe, "#FFFFFF")]),
        ],
        FlashSceneId = WhiteFlash,
        StrobeSceneId = Strobe,
        Bindings = bindings ?? [],
    };

    public static EngineSnapshot Snapshot(
        IReadOnlyList<PlaybackInfo>? playbacks = null,
        bool blackout = false,
        bool frozen = false,
        double grandMaster = 1,
        double colorsMaster = 1,
        bool smokeMachine = true)
    {
        var layers = new[] { Colors, Moves, Flashes }.Select((id, i) => new EngineLayer { Id = id, Priority = i + 1 }).ToList();
        var safety = smokeMachine
            ? new SafetyModel { SmokeChannels = [new GuardedChannel { FixtureId = Guid.NewGuid(), Label = "Fumée", Universe = 1, Channel = 180 }] }
            : SafetyModel.None;
        return new EngineSnapshot
        {
            Show = new ShowModel([], layers, [], null, safety),
            Values = [],
            Sources = [],
            Overrides = [],
            Playbacks = playbacks ?? [],
            LayerMasters = [colorsMaster, 1, 1],
            Blackout = blackout,
            Frozen = frozen,
            GrandMaster = grandMaster,
        };
    }

    public static PlaybackInfo Playing(Guid scene, Guid layer, PlaybackState state = PlaybackState.Running, bool flash = false) =>
        new(scene, layer, state, 0, 1, 0, 1, false, flash);

    /// <summary>Note d'un pad (colonne, ligne ; ligne 1 = en haut) selon le profil.</summary>
    public static byte Pad(ControllerProfile profile, int column, int row) =>
        (byte)(profile.GridBottomLeftNote + ((8 - row) * 8) + column - 1);

    public static MidiMessage Press(int note) => new(0x90, (byte)note, 127);

    public static MidiMessage Release(int note) => new(0x80, (byte)note, 0);

    public static MidiMessage Fader(ControllerProfile profile, int index, double value) =>
        new(0xB0, (byte)profile.Faders[index - 1], (byte)Math.Round(value * 127));

    public static T Single<T>(IReadOnlyList<Command> commands)
        where T : Command => commands.ShouldHaveSingleItem().ShouldBeOfType<T>();
}
