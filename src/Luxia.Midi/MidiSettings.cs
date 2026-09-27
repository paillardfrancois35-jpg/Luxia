namespace Luxia.Midi;

/// <summary>
/// Réglages MIDI du projet, enregistrés dans <c>midi.json</c> (doc 50) : affectations modifiées (MIDI-007). Absent =
/// affectation par défaut du doc 18b §3 sur tous les contrôleurs.
/// </summary>
public sealed record MidiSettings
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Affectations modifiées.</summary>
    public IReadOnlyList<MidiBinding> Bindings { get; init; } = [];
}
