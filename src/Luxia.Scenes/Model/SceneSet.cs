namespace Luxia.Scenes.Model;

/// <summary>Scènes d'un projet, enregistrées dans <c>scènes.json</c> (doc 50).</summary>
public sealed record SceneSet
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Catégorie réservée au contenu généré par une IA de conception (GEN-133).</summary>
    public const string AiCategory = "Proposé par IA";

    /// <summary>Scènes, dans l'ordre d'affichage.</summary>
    public IReadOnlyList<Scene> Scenes { get; init; } = [];
}
