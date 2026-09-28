namespace Luxia.Engine.Model;

/// <summary>Émetteurs rouge, vert et bleu d'une même cellule d'appareil (indices de paramètres), pour le fondu par la teinte (MOT-054).</summary>
/// <param name="Red">Paramètre rouge.</param>
/// <param name="Green">Paramètre vert.</param>
/// <param name="Blue">Paramètre bleu.</param>
public readonly record struct ColorGroup(int Red, int Green, int Blue);
