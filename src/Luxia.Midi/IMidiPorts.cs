namespace Luxia.Midi;

/// <summary>Accès aux ports MIDI du poste (Windows : winmm ; tests : ports simulés).</summary>
public interface IMidiPorts
{
    /// <summary>Noms des ports d'entrée présents.</summary>
    IReadOnlyList<string> Inputs();

    /// <summary>Noms des ports de sortie présents.</summary>
    IReadOnlyList<string> Outputs();

    /// <summary>
    /// Ouvre un port d'entrée ; <paramref name="received"/> est appelé sur un fil du système, à chaque message (il doit
    /// rendre la main vite). Disposer le résultat ferme le port.
    /// </summary>
    IDisposable OpenInput(string name, Action<MidiMessage> received);

    /// <summary>Ouvre un port de sortie (LED).</summary>
    IMidiOutput OpenOutput(string name);
}
