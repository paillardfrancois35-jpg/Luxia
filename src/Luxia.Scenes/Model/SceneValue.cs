using Luxia.Engine.Model;
using Luxia.Fixtures.Model;

namespace Luxia.Scenes.Model;

/// <summary>
/// Valeur d'une étape (doc 16 §2) : une cible × un attribut → une valeur directe, une plage, une couleur logique
/// ou une référence de palette (SCN-008). Une scène ne contient <b>que</b> les attributs qu'elle touche.
/// </summary>
/// <remarks>
/// Formes admises (une seule) : <see cref="Attribute"/> (ou <see cref="Channel"/>) avec <see cref="Level"/> ou
/// <see cref="Range"/> ; <see cref="Color"/> seule ; <see cref="PaletteId"/> seule (le type de la palette dit
/// quels attributs elle règle).
/// </remarks>
public sealed record SceneValue
{
    /// <summary>Cible.</summary>
    public required ValueTarget Target { get; init; }

    /// <summary>Attribut visé sur chaque membre de la cible (tous les canaux de cet attribut, dans la cellule visée).</summary>
    public AttributeKind? Attribute { get; init; }

    /// <summary>Canal précis (clé de la définition) : pour un appareil qui a plusieurs canaux du même attribut.</summary>
    public string? Channel { get; init; }

    /// <summary>Valeur normalisée 0-1 (GEN-020).</summary>
    public double? Level { get; init; }

    /// <summary>Plage et position dans la plage.</summary>
    public RangeValue? Range { get; init; }

    /// <summary>Couleur logique (GEN-022), convertie selon les émetteurs de chaque appareil.</summary>
    public LogicalColor? Color { get; init; }

    /// <summary>Palette référencée (SCN-008, PAL-005) : suivie si elle change.</summary>
    public Guid? PaletteId { get; init; }

    /// <summary>Fondu propre à cette valeur (SCN-011) ; <c>null</c> = fondu de l'étape.</summary>
    public Duration? Fade { get; init; }

    /// <summary>Retard avant le fondu (SCN-010).</summary>
    public Duration? Delay { get; init; }

    /// <summary>
    /// « Fan » temporel (SCN-010) : retard supplémentaire réparti linéairement sur les membres de la cible,
    /// dans l'ordre de la sélection (0 pour le premier, <see cref="Spread"/> pour le dernier).
    /// </summary>
    public Duration? Spread { get; init; }
}
