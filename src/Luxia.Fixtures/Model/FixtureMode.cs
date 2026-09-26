using System.Text.Json.Serialization;

namespace Luxia.Fixtures.Model;

/// <summary>Mode d'un modèle : liste ordonnée des canaux (doc 12 §2.1, BIB-002, BIB-005).</summary>
public sealed record FixtureMode
{
    /// <summary>Nom (« 7 canaux »).</summary>
    public required string Name { get; init; }

    /// <summary>Nom court (« 7CH »).</summary>
    public string? ShortName { get; init; }

    /// <summary>Réglage à faire sur l'appareil (« A001 »), affiché à l'installation (BIB-005).</summary>
    public string? DeviceSetting { get; init; }

    /// <summary>Canaux dans l'ordre : la position 1 est à l'adresse de l'appareil.</summary>
    public IReadOnlyList<ModeChannel> Channels { get; init; } = [];

    /// <summary>Nombre de canaux occupés, recalculé depuis Channels : jamais enregistré.</summary>
    [JsonIgnore]
    public int ChannelCount => Channels.Count;
}
