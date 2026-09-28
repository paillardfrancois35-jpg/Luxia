namespace Luxia.Scenes.Model;

/// <summary>Looks d'un projet, enregistrés dans <c>looks.json</c> (doc 50).</summary>
public sealed record LookSet
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Looks, dans l'ordre d'affichage.</summary>
    public IReadOnlyList<Look> Looks { get; init; } = [];
}
