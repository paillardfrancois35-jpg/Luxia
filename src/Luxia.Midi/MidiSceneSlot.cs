namespace Luxia.Midi;

/// <summary>Une scène sur un pad.</summary>
/// <param name="SceneId">Scène.</param>
/// <param name="Color">Couleur de la scène « #RRGGBB » (pads RGB, MIDI-010).</param>
public sealed record MidiSceneSlot(Guid SceneId, string Color);
