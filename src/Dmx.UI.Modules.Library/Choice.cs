using Dmx.Fixtures.Model;

namespace Dmx.UI.Modules.Library;

/// <summary>Élément de liste déroulante : une valeur et son libellé français.</summary>
/// <typeparam name="T">Type de valeur.</typeparam>
/// <param name="Value">Valeur.</param>
/// <param name="Label">Libellé.</param>
public sealed record Choice<T>(T Value, string Label)
{
    /// <inheritdoc />
    public override string ToString() => Label;
}

/// <summary>Listes de choix en français pour l'éditeur.</summary>
public static class Choices
{
    /// <summary>Catégories d'appareils.</summary>
    public static IReadOnlyList<Choice<FixtureCategory>> Categories { get; } =
    [
        new(FixtureCategory.Par, "PAR"),
        new(FixtureCategory.LedBar, "Barre LED"),
        new(FixtureCategory.MovingHead, "Lyre"),
        new(FixtureCategory.Effect, "Effet"),
        new(FixtureCategory.Strobe, "Stroboscope"),
        new(FixtureCategory.Uv, "UV"),
        new(FixtureCategory.Smoke, "Machine à fumée"),
        new(FixtureCategory.Laser, "Laser"),
        new(FixtureCategory.Dimmer, "Gradateur / décodeur"),
        new(FixtureCategory.Other, "Autre"),
    ];

    /// <summary>Types de plages.</summary>
    public static IReadOnlyList<Choice<CapabilityKind>> CapabilityKinds { get; } =
    [
        new(CapabilityKind.Fixed, "Fixe"),
        new(CapabilityKind.Progressive, "Progressif"),
        new(CapabilityKind.WheelSlot, "Emplacement de roue"),
        new(CapabilityKind.Rotation, "Rotation"),
        new(CapabilityKind.Program, "Programme"),
        new(CapabilityKind.NoFunction, "Sans fonction"),
        new(CapabilityKind.Closed, "Arrêt / fermé"),
        new(CapabilityKind.Open, "Ouvert"),
    ];

    /// <summary>Effets de strobe (null = sans objet).</summary>
    public static IReadOnlyList<Choice<StrobeEffect?>> StrobeEffects { get; } =
    [
        new(null, "—"),
        new(StrobeEffect.Closed, "Fermé"),
        new(StrobeEffect.Open, "Ouvert"),
        new(StrobeEffect.Strobe, "Strobe"),
        new(StrobeEffect.Pulse, "Pulsation"),
        new(StrobeEffect.Random, "Aléatoire"),
    ];

    /// <summary>« Suit l'intensité » : automatique (déduit) ou imposé.</summary>
    public static IReadOnlyList<Choice<bool?>> FollowsIntensity { get; } =
    [
        new(null, "Automatique"),
        new(true, "Oui"),
        new(false, "Non"),
    ];

    /// <summary>Types de roue.</summary>
    public static IReadOnlyList<Choice<WheelKind>> WheelKinds { get; } =
    [
        new(WheelKind.Color, "Couleurs"),
        new(WheelKind.Gobo, "Gobos"),
    ];

    /// <summary>Libellé d'une catégorie.</summary>
    public static string Label(FixtureCategory category) => Categories.First(c => c.Value == category).Label;
}
