namespace Luxia.UI.Modules.Scenes;

/// <summary>Bouton de plage d'un outil d'attribut (SCN-031) : libellé, bornes, couleur d'emplacement éventuelle.</summary>
/// <param name="Label">Libellé de la plage.</param>
/// <param name="Min">Borne basse (0-255).</param>
/// <param name="Max">Borne haute (0-255).</param>
/// <param name="Color">Couleur de l'emplacement de roue (« #RRGGBB »), sinon gris.</param>
public sealed record RangeChoice(string Label, int Min, int Max, string Color);
