namespace Luxia.UI.Controls;

/// <summary>Nouvelle visée demandée pour un appareil.</summary>
/// <param name="Id">Identifiant de l'appareil (celui du <see cref="PanTiltMarker"/>).</param>
/// <param name="Pan">Pan normalisé demandé (0-1).</param>
/// <param name="Tilt">Tilt normalisé demandé (0-1).</param>
public sealed record PanTiltTarget(string Id, double Pan, double Tilt);
