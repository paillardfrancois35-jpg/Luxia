namespace Dmx.UI.Controls;

/// <summary>Segment affiché par <see cref="RangeBar"/>.</summary>
/// <param name="Min">Borne basse (0-255).</param>
/// <param name="Max">Borne haute (0-255).</param>
/// <param name="Color">Couleur (#RRGGBB) ; null = couleur automatique.</param>
public sealed record RangeSegment(int Min, int Max, string? Color = null);
