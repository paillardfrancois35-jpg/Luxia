namespace Luxia.UI.Controls;

/// <summary>Zone dessinée sur la grille Pan / Tilt.</summary>
/// <param name="Id">Identifiant de la zone.</param>
/// <param name="Name">Nom affiché (« Public », « Limites »…).</param>
/// <param name="Area">Rectangle, en valeurs normalisées.</param>
/// <param name="Kind">Zone interdite ou permise.</param>
public sealed record PanTiltZoneMarker(string Id, string Name, PanTiltRect Area, PanTiltZoneKind Kind);
