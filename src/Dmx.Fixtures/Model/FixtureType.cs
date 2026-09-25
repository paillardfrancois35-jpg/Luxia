namespace Dmx.Fixtures.Model;

/// <summary>
/// Modèle d'appareil (doc 12 §2) : identité, caractéristiques physiques, roues, définitions de canaux, modes.
/// Enregistré dans un fichier JSON par modèle (doc 12 §8).
/// </summary>
public sealed record FixtureType
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Identifiant stable (GEN-052).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Fabricant.</summary>
    public required string Manufacturer { get; init; }

    /// <summary>Nom du modèle.</summary>
    public required string Model { get; init; }

    /// <summary>Référence commerciale.</summary>
    public string? Reference { get; init; }

    /// <summary>Catégorie.</summary>
    public FixtureCategory Category { get; init; } = FixtureCategory.Other;

    /// <summary>Version, incrémentée à chaque enregistrement (BIB-009).</summary>
    public int Version { get; init; } = 1;

    /// <summary>Auteur de la définition.</summary>
    public string? Author { get; init; }

    /// <summary>Origine (saisie, OFL, QLC+, générique).</summary>
    public FixtureSource Source { get; init; } = FixtureSource.Manual;

    /// <summary>Modèle d'origine s'il a été dérivé d'un autre (BIB-010).</summary>
    public Guid? DerivedFrom { get; init; }

    /// <summary>Remarques.</summary>
    public string? Notes { get; init; }

    /// <summary>Notice (PDF) liée, chemin relatif ou absolu (BIB-027).</summary>
    public string? Manual { get; init; }

    /// <summary>Photo liée (BIB-027).</summary>
    public string? Photo { get; init; }

    /// <summary>Caractéristiques physiques.</summary>
    public PhysicalInfo Physical { get; init; } = new();

    /// <summary>Roues.</summary>
    public IReadOnlyList<Wheel> Wheels { get; init; } = [];

    /// <summary>Définitions de canaux.</summary>
    public IReadOnlyList<ChannelDefinition> Channels { get; init; } = [];

    /// <summary>Modes (au moins un, BIB-002).</summary>
    public IReadOnlyList<FixtureMode> Modes { get; init; } = [];

    /// <summary>Nom affiché « Fabricant Modèle ».</summary>
    public string DisplayName => $"{Manufacturer} {Model}";

    /// <summary>Définition de canal par sa clé.</summary>
    public ChannelDefinition? Channel(string key) => Channels.FirstOrDefault(c => c.Key == key);
}
