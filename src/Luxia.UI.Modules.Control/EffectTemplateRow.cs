using Luxia.Scenes.Model;

namespace Luxia.UI.Modules.Control;

/// <summary>Un modèle de la bibliothèque d'effets (EFF-007), dans la liste du panneau Effets.</summary>
/// <param name="Template">Modèle.</param>
/// <param name="Title">Icône de la forme et nom.</param>
/// <param name="Category">Catégorie (Intensité, Mouvement, Couleur).</param>
/// <param name="Description">Ce qu'on voit.</param>
public sealed record EffectTemplateRow(EffectTemplate Template, string Title, string Category, string Description)
{
    /// <inheritdoc />
    public override string ToString() => Title;
}
