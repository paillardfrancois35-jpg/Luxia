namespace Luxia.Midi;

/// <summary>Message MIDI court (3 octets).</summary>
/// <param name="Status">Octet d'état (type et canal).</param>
/// <param name="Data1">Note ou numéro de contrôleur.</param>
/// <param name="Data2">Vélocité ou valeur.</param>
public readonly record struct MidiMessage(byte Status, byte Data1, byte Data2)
{
    /// <summary>Type sans le canal (0x80 note-off, 0x90 note-on, 0xB0 contrôleur).</summary>
    public int Kind => Status & 0xF0;

    /// <summary>Note enfoncée (note-on de vélocité non nulle).</summary>
    public bool IsNoteOn => Kind == 0x90 && Data2 > 0;

    /// <summary>Note relâchée (note-off, ou note-on de vélocité nulle).</summary>
    public bool IsNoteOff => Kind == 0x80 || (Kind == 0x90 && Data2 == 0);

    /// <summary>Changement de contrôleur (fader).</summary>
    public bool IsControlChange => Kind == 0xB0;

    /// <summary>Note-on (LED) sur un canal.</summary>
    public static MidiMessage NoteOn(int channel, int note, int velocity) =>
        new((byte)(0x90 | (channel & 0x0F)), (byte)(note & 0x7F), (byte)(velocity & 0x7F));

    /// <summary>Message empaqueté pour Windows (état | donnée 1 &lt;&lt; 8 | donnée 2 &lt;&lt; 16).</summary>
    public uint Pack() => (uint)(Status | (Data1 << 8) | (Data2 << 16));

    /// <summary>Message depuis sa forme empaquetée.</summary>
    public static MidiMessage Unpack(uint packed) => new((byte)(packed & 0xFF), (byte)((packed >> 8) & 0x7F), (byte)((packed >> 16) & 0x7F));
}
