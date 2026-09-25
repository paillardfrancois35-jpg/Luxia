namespace Dmx.Fixtures.Model;

/// <summary>Roue de couleur ou de gobos (doc 12 §2.1, BIB-025).</summary>
public sealed record Wheel
{
    /// <summary>Clé unique dans le modèle.</summary>
    public required string Key { get; init; }

    /// <summary>Nom affiché.</summary>
    public required string Name { get; init; }

    /// <summary>Type.</summary>
    public WheelKind Kind { get; init; } = WheelKind.Color;

    /// <summary>Emplacements, dans l'ordre de la roue.</summary>
    public IReadOnlyList<WheelSlot> Slots { get; init; } = [];
}
