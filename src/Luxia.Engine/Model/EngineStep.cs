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
}
