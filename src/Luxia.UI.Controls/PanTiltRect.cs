namespace Luxia.UI.Controls;

/// <summary>
/// Rectangle de la grille Pan / Tilt, en valeurs normalisées 0-1 (comme les zones interdites de LuXia, INST-053).
/// </summary>
/// <param name="PanMin">Pan minimal.</param>
/// <param name="PanMax">Pan maximal.</param>
/// <param name="TiltMin">Tilt minimal.</param>
/// <param name="TiltMax">Tilt maximal.</param>
public readonly record struct PanTiltRect(double PanMin, double PanMax, double TiltMin, double TiltMax)
{
    /// <summary>Largeur en Pan.</summary>
    public double PanSize => PanMax - PanMin;

    /// <summary>Hauteur en Tilt.</summary>
    public double TiltSize => TiltMax - TiltMin;

    /// <summary>Le point est dans le rectangle (bords compris).</summary>
    public bool Contains(double pan, double tilt) => pan >= PanMin && pan <= PanMax && tilt >= TiltMin && tilt <= TiltMax;
}
