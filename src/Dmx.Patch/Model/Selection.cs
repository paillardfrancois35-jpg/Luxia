namespace Dmx.Patch.Model;

/// <summary>
/// Sélection manuelle (INST-030, INST-032) : liste ordonnée d'appareils (ou de leurs cellules), nommée et colorée.
/// Les sélections <b>automatiques</b> (Tous, par modèle, par catégorie — INST-031) ne sont pas enregistrées ici :
/// elles sont recalculées à la volée par <see cref="Dmx.Patch.Rules.AutoSelections"/>, toujours à jour.
/// </summary>
public sealed record Selection
{
    /// <summary>Identifiant stable (GEN-052).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom affiché.</summary>
    public required string Name { get; init; }

    /// <summary>Couleur d'affichage (« #RRGGBB »).</summary>
    public string Color { get; init; } = "#58A6FF";

    /// <summary>Éléments, dans l'ordre (sert aux chenillards et décalages de phase).</summary>
    public IReadOnlyList<SelectionItem> Items { get; init; } = [];
}
