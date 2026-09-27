namespace Luxia.Midi;

/// <summary>Port de sortie MIDI ouvert.</summary>
public interface IMidiOutput : IDisposable
{
    /// <summary>Envoie un message court.</summary>
    void Send(MidiMessage message);
}
