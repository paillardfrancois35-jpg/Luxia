namespace Dmx.Patch.Model;

/// <summary>Élément d'une sélection (INST-030) : un appareil, ou une de ses cellules (INST-034).</summary>
/// <param name="FixtureId">Appareil patché.</param>
/// <param name="Cell">Cellule (0 = appareil entier, doc 12 §2.6).</param>
public sealed record SelectionItem(Guid FixtureId, int Cell = 0);
