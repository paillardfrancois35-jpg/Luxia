using Luxia.Scenes.Model;

namespace Luxia.Scenes.Rules;

/// <summary>
/// Jeu de palettes livré avec tout projet (PAL-009) : 13 couleurs « intention », plus les niveaux d'intensité
/// du show de référence (doc 41 §5). Identifiants fixes : un projet sans <c>palettes.json</c> y retrouve toujours
/// les mêmes palettes.
/// </summary>
public static class DefaultPalettes
{
    /// <summary>Palettes couleur par défaut : (nom, identifiant, couleur logique).</summary>
    public static IReadOnlyList<Palette> Colors { get; } =
    [
        Color(1, "Blanc", new LogicalColor { R = 1, G = 1, B = 1 }),
        Color(2, "Blanc chaud", new LogicalColor { R = 1, G = 0.78, B = 0.45 }),
        Color(3, "Rouge", new LogicalColor { R = 1 }),
        Color(4, "Orange", new LogicalColor { R = 1, G = 0.4 }),
        Color(5, "Ambre", new LogicalColor { R = 1, G = 0.5 }),
        Color(6, "Jaune", new LogicalColor { R = 1, G = 0.9 }),
        Color(7, "Vert", new LogicalColor { G = 1 }),
        Color(8, "Cyan", new LogicalColor { G = 1, B = 1 }),
        Color(9, "Bleu", new LogicalColor { B = 1 }),
        Color(10, "Lavande", new LogicalColor { R = 0.6, G = 0.45, B = 1 }),
        Color(11, "Magenta", new LogicalColor { R = 1, B = 1 }),
        Color(12, "Rose", new LogicalColor { R = 1, G = 0.35, B = 0.6 }),
        Color(13, "UV", new LogicalColor { R = 0.3, B = 1, Uv = 1 }),
    ];

    /// <summary>Palettes d'intensité par défaut (doc 41 §5).</summary>
    public static IReadOnlyList<Palette> Intensities { get; } =
    [
        Intensity(101, "Plein", 1),
        Intensity(102, "70 %", 0.7),
        Intensity(103, "50 %", 0.5),
        Intensity(104, "Veilleuse", 0.15),
    ];

    /// <summary>Nouveau jeu de palettes par défaut.</summary>
    public static PaletteSet Create() => new() { Palettes = [.. Colors, .. Intensities] };

    private static Palette Color(int number, string name, LogicalColor light) => new()
    {
        Id = IdFor(number),
        Name = name,
        Kind = PaletteKind.Color,
        Light = light,
    };

    private static Palette Intensity(int number, string name, double level) => new()
    {
        Id = IdFor(number),
        Name = name,
        Kind = PaletteKind.Intensity,
        Level = level,
        Color = "#E3B341",
    };

    private static Guid IdFor(int number) => new($"9a1e0001-0000-4000-8000-{number:D12}");
}
