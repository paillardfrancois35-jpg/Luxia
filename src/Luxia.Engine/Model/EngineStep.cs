namespace Luxia.Engine.Model;

/// <summary>Étape d'une scène compilée : fondu d'entrée puis maintien (MOT-010).</summary>
public sealed record EngineStep
{
    /// <summary>Fondu d'entrée.</summary>
    public Duration Fade { get; init; }

    /// <summary>Maintien après le fondu.</summary>
    public Duration Hold { get; init; } = Duration.FromSeconds(1);

    /// <summary>Courbe du fondu (MOT-011).</summary>
    public FadeCurve Curve { get; init; } = FadeCurve.Linear;

    /// <summary>Bascule des attributs discrets (MOT-012).</summary>
    public DiscreteSwitch Switch { get; init; } = DiscreteSwitch.Start;

    /// <summary>Valeurs de l'étape : un paramètre n'y figure qu'une fois.</summary>
    public IReadOnlyList<StepValue> Values { get; init; } = [];

    /// <summary>Effets générés de l'étape (EFF-001) ; ils entrent et sortent avec le fondu de l'étape (MOT-063).</summary>
    public IReadOnlyList<EngineEffect> Effects { get; init; } = [];

    /// <summary>
    /// Fondu des couleurs par la teinte (MOT-054) : rouge → vert passe par le jaune au lieu d'un brun terne.
    /// Faux = interpolation directe des émetteurs.
    /// </summary>
    public bool HueFade { get; init; }

    /// <summary>
    /// Quand la scène avance sur un événement musical (temps, mesure, impulsion), l'étape passe quand même à la suivante au bout de son
    /// fondu et de son maintien : un flash bref sur un kick (allumage au kick, extinction après quelques dixièmes de seconde).
    /// </summary>
    public bool AutoAdvance { get; init; }
}
