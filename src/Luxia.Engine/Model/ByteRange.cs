namespace Luxia.Engine.Model;

/// <summary>Plage de valeurs DMX (bornes incluses).</summary>
/// <param name="Min">Borne basse.</param>
/// <param name="Max">Borne haute.</param>
/// <param name="Increasing">La vitesse croît avec la valeur (faux pour « rapide → lent »).</param>
public readonly record struct ByteRange(int Min, int Max, bool Increasing = true)
{
    /// <summary>La valeur appartient à la plage.</summary>
    public bool Contains(int value) => value >= Min && value <= Max;
}
