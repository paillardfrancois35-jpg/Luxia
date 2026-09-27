using Avalonia.Media;

namespace Luxia.UI.Controls;

/// <summary>Point de visée d'un appareil sur la grille Pan / Tilt.</summary>
/// <param name="Id">Identifiant de l'appareil (rendu tel quel dans les demandes).</param>
/// <param name="Label">Nom court affiché à côté du point.</param>
/// <param name="Pan">Pan normalisé (0-1).</param>
/// <param name="Tilt">Tilt normalisé (0-1).</param>
/// <param name="Color">Couleur du point (souvent la couleur émise par l'appareil).</param>
/// <param name="IsSelected">L'appareil fait partie de la sélection : c'est lui que la grille déplace.</param>
public sealed record PanTiltMarker(string Id, string Label, double Pan, double Tilt, Color Color, bool IsSelected);
