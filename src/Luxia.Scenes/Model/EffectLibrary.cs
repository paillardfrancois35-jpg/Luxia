namespace Luxia.Scenes.Model;

/// <summary>Bibliothèque d'effets d'un projet, enregistrée dans <c>effets.json</c> (doc 50, EFF-007).</summary>
public sealed record EffectLibrary
{
    /// <summary>Version courante du format de fichier.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Modèles, dans l'ordre d'affichage.</summary>
    public IReadOnlyList<EffectTemplate> Templates { get; init; } = [];
}
