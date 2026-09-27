namespace Luxia.UI.Controls;

/// <summary>Sens d'une zone dessinée sur la grille Pan / Tilt (F7).</summary>
public enum PanTiltZoneKind
{
    /// <summary>Le faisceau ne doit pas y viser (public, miroir…) : dessinée en rouge.</summary>
    Forbidden,

    /// <summary>Limites de l'appareil : on ne sort pas de ce rectangle ; l'extérieur est assombri.</summary>
    Allowed,
}
