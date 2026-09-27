using Luxia.Persistence;
using Luxia.Persistence.Json;

namespace Luxia.Midi;

/// <summary>Lecture / écriture de <c>midi.json</c> (doc 50).</summary>
public static class MidiStore
{
    /// <summary>Nom du fichier.</summary>
    public const string FileName = "midi.json";

    /// <summary>Type de document « midi ».</summary>
    public static readonly DocumentType<MidiSettings> DocumentType = new("midi", MidiSettings.CurrentFormatVersion, []);

    /// <summary>Charge les réglages MIDI ; absent = affectation par défaut.</summary>
    public static (MidiSettings Value, string? Message) Load(string projectFolder) =>
        ProjectPartStore.Load(projectFolder, FileName, DocumentType, () => new MidiSettings());

    /// <summary>Enregistre les réglages MIDI.</summary>
    public static void Save(string projectFolder, MidiSettings settings) =>
        ProjectPartStore.Save(projectFolder, FileName, settings, DocumentType);
}
