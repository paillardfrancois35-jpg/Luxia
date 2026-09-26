using System.Text.Json.Serialization;

namespace Luxia.Scenes.Model;

/// <summary>
/// Valeur donnée par une plage d'un canal (SCN-008) : bornes DMX de la plage et position dans la plage.
/// Les bornes sont recopiées depuis la bibliothèque : si la définition change, la valeur reste dans ces bornes.
/// </summary>
/// <param name="Min">Borne basse (0-255).</param>
/// <param name="Max">Borne haute (0-255).</param>
/// <param name="Position">Position 0-1 dans la plage ; <c>null</c> = valeur médiane (celle d'un clic sur la plage, BIB-061).</param>
public sealed record RangeValue(int Min, int Max, double? Position = null)
{
    /// <summary>Octet DMX correspondant, recalculé : jamais enregistré.</summary>
    [JsonIgnore]
    public int Dmx => Position is { } p
        ? (int)Math.Round(Min + ((Max - Min) * Math.Clamp(p, 0, 1)))
        : (Min + Max + 1) / 2;
}
