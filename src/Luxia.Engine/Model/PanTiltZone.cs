namespace Luxia.Engine.Model;

/// <summary>Rectangle interdit en valeurs logiques normalisées de Pan et Tilt (0-1, avant inversion de montage).</summary>
/// <param name="PanMin">Pan minimal.</param>
/// <param name="PanMax">Pan maximal.</param>
/// <param name="TiltMin">Tilt minimal.</param>
/// <param name="TiltMax">Tilt maximal.</param>
public readonly record struct PanTiltZone(double PanMin, double PanMax, double TiltMin, double TiltMax)
{
    /// <summary>Le point est strictement à l'intérieur (le bord est autorisé).</summary>
    public bool Contains(double pan, double tilt) => pan > PanMin && pan < PanMax && tilt > TiltMin && tilt < TiltMax;
}
