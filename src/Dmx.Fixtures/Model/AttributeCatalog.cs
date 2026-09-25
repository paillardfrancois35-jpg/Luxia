using System.Collections.Frozen;

namespace Dmx.Fixtures.Model;

/// <summary>Catalogue des attributs avec leurs libellés et propriétés (doc 12 §2.3).</summary>
public static class AttributeCatalog
{
    private static readonly FrozenDictionary<AttributeKind, AttributeInfo> Infos = new AttributeInfo[]
    {
        new(AttributeKind.Intensity, "Intensité", AttributeFamily.Intensity, false, SafetyTags.None),
        new(AttributeKind.CellIntensity, "Intensité cellule", AttributeFamily.Intensity, false, SafetyTags.None),
        new(AttributeKind.Red, "Rouge", AttributeFamily.Color, true, SafetyTags.None),
        new(AttributeKind.Green, "Vert", AttributeFamily.Color, true, SafetyTags.None),
        new(AttributeKind.Blue, "Bleu", AttributeFamily.Color, true, SafetyTags.None),
        new(AttributeKind.White, "Blanc", AttributeFamily.Color, true, SafetyTags.None),
        new(AttributeKind.WarmWhite, "Blanc chaud", AttributeFamily.Color, true, SafetyTags.None),
        new(AttributeKind.Amber, "Ambre", AttributeFamily.Color, true, SafetyTags.None),
        new(AttributeKind.Uv, "UV", AttributeFamily.Color, true, SafetyTags.None),
        new(AttributeKind.Cyan, "Cyan", AttributeFamily.Color, true, SafetyTags.None),
        new(AttributeKind.Magenta, "Magenta", AttributeFamily.Color, true, SafetyTags.None),
        new(AttributeKind.Yellow, "Jaune", AttributeFamily.Color, true, SafetyTags.None),
        new(AttributeKind.Lime, "Lime", AttributeFamily.Color, true, SafetyTags.None),
        new(AttributeKind.ColorWheel, "Roue de couleur", AttributeFamily.Color, false, SafetyTags.None),
        new(AttributeKind.ColorMacro, "Macro couleur", AttributeFamily.Color, false, SafetyTags.None),
        new(AttributeKind.ColorTemperature, "Température de couleur", AttributeFamily.Color, false, SafetyTags.None),
        new(AttributeKind.Pan, "Pan", AttributeFamily.Position, false, SafetyTags.Movement),
        new(AttributeKind.Tilt, "Tilt", AttributeFamily.Position, false, SafetyTags.Movement),
        new(AttributeKind.PanContinuous, "Pan continu", AttributeFamily.Position, false, SafetyTags.Movement),
        new(AttributeKind.TiltContinuous, "Tilt continu", AttributeFamily.Position, false, SafetyTags.Movement),
        new(AttributeKind.PanTiltSpeed, "Vitesse Pan/Tilt", AttributeFamily.Position, false, SafetyTags.None),
        new(AttributeKind.Shutter, "Strobe / Obturateur", AttributeFamily.Beam, false, SafetyTags.Strobe),
        new(AttributeKind.Gobo, "Gobo", AttributeFamily.Beam, false, SafetyTags.None),
        new(AttributeKind.GoboRotation, "Rotation gobo", AttributeFamily.Beam, false, SafetyTags.None),
        new(AttributeKind.Prism, "Prisme", AttributeFamily.Beam, false, SafetyTags.None),
        new(AttributeKind.PrismRotation, "Rotation prisme", AttributeFamily.Beam, false, SafetyTags.None),
        new(AttributeKind.Focus, "Focus", AttributeFamily.Beam, false, SafetyTags.None),
        new(AttributeKind.Zoom, "Zoom", AttributeFamily.Beam, false, SafetyTags.None),
        new(AttributeKind.Iris, "Iris", AttributeFamily.Beam, false, SafetyTags.None),
        new(AttributeKind.Frost, "Frost", AttributeFamily.Beam, false, SafetyTags.None),
        new(AttributeKind.Rotation, "Rotation", AttributeFamily.EffectMotion, false, SafetyTags.Movement),
        new(AttributeKind.RotationSpeed, "Vitesse de rotation", AttributeFamily.EffectMotion, false, SafetyTags.None),
        new(AttributeKind.Program, "Programme interne", AttributeFamily.Programs, false, SafetyTags.None),
        new(AttributeKind.ProgramSpeed, "Vitesse programme", AttributeFamily.Programs, false, SafetyTags.None),
        new(AttributeKind.SoundSensitivity, "Sensibilité son", AttributeFamily.Programs, false, SafetyTags.None),
        new(AttributeKind.Mode, "Mode (sélecteur de fonction)", AttributeFamily.Programs, false, SafetyTags.None),
        new(AttributeKind.Smoke, "Fumée", AttributeFamily.Atmosphere, false, SafetyTags.Smoke),
        new(AttributeKind.Fan, "Ventilateur", AttributeFamily.Atmosphere, false, SafetyTags.None),
        new(AttributeKind.Reset, "Reset", AttributeFamily.Control, false, SafetyTags.None),
        new(AttributeKind.Maintenance, "Maintenance", AttributeFamily.Control, false, SafetyTags.None),
        new(AttributeKind.LampControl, "Lampe on/off", AttributeFamily.Control, false, SafetyTags.None),
        new(AttributeKind.Generic, "Générique", AttributeFamily.Other, false, SafetyTags.None),
        new(AttributeKind.NoFunction, "Sans fonction", AttributeFamily.Other, false, SafetyTags.None),
    }.ToFrozenDictionary(i => i.Attribute);

    /// <summary>Tous les attributs, dans l'ordre du catalogue.</summary>
    public static IReadOnlyList<AttributeInfo> All { get; } = [.. Enum.GetValues<AttributeKind>().Select(a => Infos[a])];

    /// <summary>Description d'un attribut.</summary>
    public static AttributeInfo Get(AttributeKind attribute) => Infos[attribute];

    /// <summary>Libellé français.</summary>
    public static string Label(AttributeKind attribute) => Infos[attribute].Label;

    /// <summary>Attribut d'intensité (soumis au Grand Master et au blackout).</summary>
    public static bool IsIntensity(AttributeKind attribute) => Infos[attribute].Family == AttributeFamily.Intensity;
}
