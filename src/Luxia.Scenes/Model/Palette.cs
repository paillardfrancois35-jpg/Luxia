namespace Luxia.Scenes.Model;

/// <summary>
/// Palette (doc 17 §2) : valeur nommée et réutilisable que les scènes référencent (SCN-008) ;
/// la modifier met à jour toutes les scènes qui l'utilisent (PAL-005).
/// </summary>
public sealed record Palette
{
    /// <summary>Identifiant stable (GEN-052).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom.</summary>
    public required string Name { get; init; }

    /// <summary>Type.</summary>
    public PaletteKind Kind { get; init; } = PaletteKind.Color;

    /// <summary>Couleur d'affichage du bouton « #RRGGBB » (GEN-106) ; pour une palette couleur, sa couleur si absente.</summary>
    public string? Color { get; init; }

    /// <summary>Icône facultative (GEN-106).</summary>
    public string? Icon { get; init; }

    /// <summary>Couleur logique (palette couleur).</summary>
    public LogicalColor? Light { get; init; }

    /// <summary>Niveau (palette intensité).</summary>
    public double? Level { get; init; }

    /// <summary>
    /// Valeurs par appareil ou par modèle : Pan/Tilt (position), faisceau, ou affinage d'une couleur pour un modèle
    /// (la valeur spécifique prime sur la traduction automatique, PAL-002).
    /// </summary>
    public IReadOnlyList<PaletteValue> Values { get; init; } = [];

    /// <summary>Couleur du bouton à afficher.</summary>
    public string DisplayColor() => Color ?? Light?.Hex ?? "#58A6FF";
}
