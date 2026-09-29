namespace Luxia.Scenes.Model;

/// <summary>Couleur d'un effet de couleur (EFF-004) : une couleur logique, ou une palette couleur suivie si elle change (PAL-005).</summary>
public sealed record EffectColor
{
    /// <summary>Couleur logique.</summary>
    public LogicalColor? Color { get; init; }

    /// <summary>Palette couleur référencée.</summary>
    public Guid? PaletteId { get; init; }

    /// <summary>Une palette.</summary>
    public static EffectColor Palette(Guid paletteId) => new() { PaletteId = paletteId };

    /// <summary>Une couleur « #RRGGBB ».</summary>
    public static EffectColor Hex(string hex) => new() { Color = LogicalColor.FromHex(hex) };
}
