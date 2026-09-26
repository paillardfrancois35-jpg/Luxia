namespace Luxia.Midi;

/// <summary>Message note-on d'une LED : canal (0-15) et vélocité (ignorée pour un pad RGB, remplacée par la couleur).</summary>
/// <param name="Channel">Canal MIDI (0 = canal 1).</param>
/// <param name="Velocity">Vélocité.</param>
public sealed record LedSpec(int Channel, int Velocity);
