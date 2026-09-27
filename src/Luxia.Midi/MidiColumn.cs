namespace Luxia.Midi;

/// <summary>Une colonne du contrôleur : une couche et ses scènes visibles en Live.</summary>
/// <param name="LayerId">Couche.</param>
/// <param name="IsFlash">Couche Flash : ses scènes jouent tant que le pad est maintenu.</param>
/// <param name="Scenes">Scènes, dans l'ordre (ligne 1 = première).</param>
public sealed record MidiColumn(Guid LayerId, bool IsFlash, IReadOnlyList<MidiSceneSlot> Scenes);
