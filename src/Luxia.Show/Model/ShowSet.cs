namespace Luxia.Show.Model;

/// <summary>Shows d'un projet, enregistrés dans <c>shows.json</c> (doc 50).</summary>
public sealed record ShowSet
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Shows, dans l'ordre d'affichage.</summary>
    public IReadOnlyList<ShowDefinition> Shows { get; init; } = [];
}
