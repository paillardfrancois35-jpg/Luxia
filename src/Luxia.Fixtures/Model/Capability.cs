using System.Text.Json.Serialization;

namespace Luxia.Fixtures.Model;

/// <summary>
/// Plage d'un canal (doc 12 §2.5) : intervalle de valeurs DMX avec une signification.
/// </summary>
public sealed record Capability
{
    /// <summary>Borne basse incluse (0-255).</summary>
    public required int Min { get; init; }

    /// <summary>Borne haute incluse (0-255).</summary>
    public required int Max { get; init; }

    /// <summary>Type.</summary>
    public CapabilityKind Kind { get; init; } = CapabilityKind.Fixed;

    /// <summary>Libellé affiché (« Strobe lent → rapide »).</summary>
    public required string Label { get; init; }

    /// <summary>Pour un canal Strobe / Obturateur : effet de la plage.</summary>
    public StrobeEffect? Strobe { get; init; }

    /// <summary>Pour une plage progressive : nature et valeurs de début / fin.</summary>
    public ProgressiveParameter? Parameter { get; init; }

    /// <summary>Couleurs (#RRGGBB) d'un emplacement de roue ou d'une macro : 1, ou 2 pour une demi-couleur (BIB-008).</summary>
    public IReadOnlyList<string> Colors { get; init; } = [];

    /// <summary>Numéro d'emplacement dans la roue du canal (1 = premier), si c'est un emplacement de roue.</summary>
    public int? WheelSlot { get; init; }

    /// <summary>Génère un bouton de palette automatique (doc 17).</summary>
    public bool AutoPalette { get; init; }

    /// <summary>Valeur médiane de la plage (émise par un clic sur la plage, BIB-061 ; CONS-023), recalculée depuis Min/Max : jamais enregistrée.</summary>
    [JsonIgnore]
    public int Median => (Min + Max + 1) / 2;

    /// <summary>La valeur appartient à la plage.</summary>
    public bool Contains(int value) => value >= Min && value <= Max;
}
