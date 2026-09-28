namespace Luxia.Scenes.Model;

/// <summary>Type de palette (doc 17 §2.1).</summary>
public enum PaletteKind
{
    /// <summary>Couleur « intention » (+ valeurs propres à un modèle).</summary>
    Color,

    /// <summary>Pan/Tilt par appareil (par lieu en P5, PAL-004).</summary>
    Position,

    /// <summary>Gobo, rotation, prisme, focus, zoom… par modèle ou par appareil.</summary>
    Beam,

    /// <summary>Niveau d'intensité nommé.</summary>
    Intensity,

    /// <summary>Thème : combinaison de couleurs (« Latino », « Froid »), pour les effets d'alternance et le Directeur (PAL-010).</summary>
    Theme,
}
