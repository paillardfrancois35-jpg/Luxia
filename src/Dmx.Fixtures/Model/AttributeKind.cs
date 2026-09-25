namespace Dmx.Fixtures.Model;

/// <summary>
/// Catalogue des attributs (doc 12 §2.3) : chaque canal est lié à un attribut.
/// Catalogue fermé, étendu uniquement par une nouvelle version de l'application.
/// </summary>
public enum AttributeKind
{
    /// <summary>Intensité (gradateur maître).</summary>
    Intensity,

    /// <summary>Intensité d'une cellule (tête, segment).</summary>
    CellIntensity,

    /// <summary>Rouge.</summary>
    Red,

    /// <summary>Vert.</summary>
    Green,

    /// <summary>Bleu.</summary>
    Blue,

    /// <summary>Blanc.</summary>
    White,

    /// <summary>Blanc chaud.</summary>
    WarmWhite,

    /// <summary>Ambre.</summary>
    Amber,

    /// <summary>UV (émetteur).</summary>
    Uv,

    /// <summary>Cyan.</summary>
    Cyan,

    /// <summary>Magenta.</summary>
    Magenta,

    /// <summary>Jaune.</summary>
    Yellow,

    /// <summary>Lime.</summary>
    Lime,

    /// <summary>Roue de couleur.</summary>
    ColorWheel,

    /// <summary>Macro couleur (couleurs préenregistrées).</summary>
    ColorMacro,

    /// <summary>Température de couleur.</summary>
    ColorTemperature,

    /// <summary>Pan.</summary>
    Pan,

    /// <summary>Tilt.</summary>
    Tilt,

    /// <summary>Pan continu.</summary>
    PanContinuous,

    /// <summary>Tilt continu.</summary>
    TiltContinuous,

    /// <summary>Vitesse Pan/Tilt.</summary>
    PanTiltSpeed,

    /// <summary>Strobe / obturateur.</summary>
    Shutter,

    /// <summary>Gobo.</summary>
    Gobo,

    /// <summary>Rotation du gobo.</summary>
    GoboRotation,

    /// <summary>Prisme.</summary>
    Prism,

    /// <summary>Rotation du prisme.</summary>
    PrismRotation,

    /// <summary>Focus.</summary>
    Focus,

    /// <summary>Zoom.</summary>
    Zoom,

    /// <summary>Iris.</summary>
    Iris,

    /// <summary>Frost.</summary>
    Frost,

    /// <summary>Rotation (moteur d'effet).</summary>
    Rotation,

    /// <summary>Vitesse de rotation.</summary>
    RotationSpeed,

    /// <summary>Programme interne.</summary>
    Program,

    /// <summary>Vitesse du programme.</summary>
    ProgramSpeed,

    /// <summary>Sensibilité au son.</summary>
    SoundSensitivity,

    /// <summary>Mode (sélecteur de fonction).</summary>
    Mode,

    /// <summary>Fumée (débit / déclenchement).</summary>
    Smoke,

    /// <summary>Ventilateur.</summary>
    Fan,

    /// <summary>Reset.</summary>
    Reset,

    /// <summary>Maintenance.</summary>
    Maintenance,

    /// <summary>Lampe allumée / éteinte.</summary>
    LampControl,

    /// <summary>Générique.</summary>
    Generic,

    /// <summary>Sans fonction.</summary>
    NoFunction,
}
