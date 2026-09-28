namespace Luxia.UI.Controls;

/// <summary>Demande de sélection faite sur le plan (E5, SIM-010).</summary>
/// <param name="Fixtures">Appareils visés (vide : clic dans le vide).</param>
/// <param name="Additive">Ctrl : les appareils visés s'ajoutent (ou, pour un seul appareil cliqué, basculent).</param>
public sealed record FixtureSelectionRequest(IReadOnlyList<Guid> Fixtures, bool Additive);
