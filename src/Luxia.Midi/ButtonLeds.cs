namespace Luxia.Midi;

/// <summary>Vélocités des LED des boutons ronds (0 = éteinte).</summary>
/// <param name="On">Allumée.</param>
/// <param name="Blink">Clignotante.</param>
public sealed record ButtonLeds(int On = 1, int Blink = 2);
