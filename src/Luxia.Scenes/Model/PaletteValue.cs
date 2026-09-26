using Luxia.Fixtures.Model;

namespace Luxia.Scenes.Model;

/// <summary>
/// Valeur d'une palette pour un appareil précis ou pour tous les appareils d'un modèle (doc 17 §2.1, PAL-002) :
/// un attribut (ou un canal précis) → une valeur normalisée.
/// </summary>
public sealed record PaletteValue
{
    /// <summary>Appareil visé (position : Pan/Tilt par lyre).</summary>
    public Guid? FixtureId { get; init; }

    /// <summary>Modèle visé (faisceau par modèle, affinage d'une couleur pour un modèle).</summary>
    public Guid? FixtureTypeId { get; init; }

    /// <summary>Attribut.</summary>
    public AttributeKind? Attribute { get; init; }

    /// <summary>Canal précis (clé de la définition).</summary>
    public string? Channel { get; init; }

    /// <summary>Valeur normalisée 0-1.</summary>
    public double Level { get; init; }
}
