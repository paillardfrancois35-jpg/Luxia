using Luxia.Engine.Model;

namespace Luxia.Scenes.Model;

/// <summary>Étape d'une scène (doc 16 §2, SCN-003) : fondu d'entrée, maintien, courbe, valeurs.</summary>
public sealed record SceneStep
{
    /// <summary>Nom facultatif.</summary>
    public string? Name { get; init; }

    /// <summary>Fondu d'entrée (secondes ou temps musicaux, GEN-023).</summary>
    public Duration Fade { get; init; }

    /// <summary>Maintien après le fondu.</summary>
    public Duration Hold { get; init; } = Duration.FromSeconds(1);

    /// <summary>Courbe du fondu (MOT-011).</summary>
    public FadeCurve Curve { get; init; } = FadeCurve.Linear;

    /// <summary>Bascule des attributs discrets (MOT-012).</summary>
    public DiscreteSwitch Switch { get; init; } = DiscreteSwitch.Start;

    /// <summary>Valeurs, dans l'ordre : à cible égale, la dernière l'emporte ; une cible plus précise l'emporte toujours.</summary>
    public IReadOnlyList<SceneValue> Values { get; init; } = [];
}
