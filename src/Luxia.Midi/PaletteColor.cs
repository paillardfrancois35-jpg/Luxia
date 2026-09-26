namespace Luxia.Midi;

/// <summary>Couleur de la palette d'un contrôleur RGB.</summary>
/// <param name="Index">Indice (vélocité à envoyer).</param>
/// <param name="Color">Couleur « #RRGGBB ».</param>
public sealed record PaletteColor(int Index, string Color);
