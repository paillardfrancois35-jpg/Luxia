namespace Luxia.UI.Controls;

/// <summary>Partie d'une zone saisie à la souris : le corps (déplacer) ou l'une des 8 poignées (redimensionner).</summary>
public enum PanTiltHandle
{
    /// <summary>Rien.</summary>
    None,

    /// <summary>Intérieur : déplace la zone.</summary>
    Body,

    /// <summary>Bord gauche (Pan minimal).</summary>
    Left,

    /// <summary>Bord droit (Pan maximal).</summary>
    Right,

    /// <summary>Bord bas (Tilt minimal).</summary>
    Bottom,

    /// <summary>Bord haut (Tilt maximal).</summary>
    Top,

    /// <summary>Coin bas gauche.</summary>
    BottomLeft,

    /// <summary>Coin bas droit.</summary>
    BottomRight,

    /// <summary>Coin haut gauche.</summary>
    TopLeft,

    /// <summary>Coin haut droit.</summary>
    TopRight,
}
