namespace Luxia.Midi;

/// <summary>
/// Ce que joue le contrôleur, fourni par l'hôte à partir du projet (mêmes colonnes et mêmes boutons que l'écran Live,
/// doc 18b §3) : le module MIDI ne connaît ni les couches ni les scènes du projet.
/// </summary>
public sealed record MidiLayout
{
    /// <summary>Aucune colonne (pas de projet ouvert).</summary>
    public static MidiLayout Empty { get; } = new();

    /// <summary>Colonnes (couches), dans l'ordre du Live.</summary>
    public IReadOnlyList<MidiColumn> Columns { get; init; } = [];

    /// <summary>Scène du bouton FLASH.</summary>
    public Guid? FlashSceneId { get; init; }

    /// <summary>Scène du bouton STROBE.</summary>
    public Guid? StrobeSceneId { get; init; }

    /// <summary>Durée d'une rafale de fumée, en secondes.</summary>
    public double SmokeBurstSeconds { get; init; } = 3;

    /// <summary>Un appui sur la scène qui joue la relance (au lieu de l'arrêter).</summary>
    public bool ActiveClickRestarts { get; init; }

    /// <summary>Affectations modifiées (<c>midi.json</c>, MIDI-007) : elles remplacent l'affectation par défaut du contrôle.</summary>
    public IReadOnlyList<MidiBinding> Bindings { get; init; } = [];
}
