namespace Luxia.Scenes.Model;

/// <summary>
/// Modèle d'effet de la bibliothèque (EFF-007) : réglages prêts à l'emploi, sans cible. L'appliquer à une étape en fait
/// une <b>copie</b> sur la sélection choisie : modifier ensuite le modèle ne change pas les scènes déjà faites.
/// </summary>
public sealed record EffectTemplate
{
    /// <summary>Identifiant stable (GEN-052).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom (« Vague douce »).</summary>
    public required string Name { get; init; }

    /// <summary>Catégorie d'affichage (« Intensité », « Mouvement », « Couleur »).</summary>
    public string? Category { get; init; }

    /// <summary>Explication courte : ce qu'on voit, à quoi ça sert.</summary>
    public string? Description { get; init; }

    /// <summary>Réglages (la cible est ignorée).</summary>
    public SceneEffect Effect { get; init; } = new();
}
