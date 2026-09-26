namespace Luxia.Scenes.Model;

/// <summary>Palettes d'un projet, enregistrées dans <c>palettes.json</c> (doc 50).</summary>
public sealed record PaletteSet
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Palettes, dans l'ordre des grilles (PAL-007).</summary>
    public IReadOnlyList<Palette> Palettes { get; init; } = [];
}
