using Luxia.Scenes.Model;

namespace Luxia.UI.Modules.Scenes;

/// <summary>
/// Raccourci de sélection du programmeur (SCN-030) : une sélection enregistrée ou automatique. Les valeurs réglées
/// après l'avoir choisie visent la sélection elle-même (SCN-007 : un appareil ajouté plus tard sera piloté aussi).
/// </summary>
/// <param name="Label">Libellé.</param>
/// <param name="Target">Cible correspondante.</param>
/// <param name="Members">Appareils membres, dans l'ordre.</param>
/// <param name="Color">Couleur d'affichage.</param>
public sealed record SelectionShortcut(string Label, ValueTarget Target, IReadOnlyList<Guid> Members, string Color);
