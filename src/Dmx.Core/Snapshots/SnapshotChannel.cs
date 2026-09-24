namespace Dmx.Core.Snapshots;

/// <summary>Canal d'un instantané.</summary>
/// <param name="Channel">Canal (1 à 512).</param>
/// <param name="Value">Valeur 0-255.</param>
public sealed record SnapshotChannel(int Channel, byte Value);
