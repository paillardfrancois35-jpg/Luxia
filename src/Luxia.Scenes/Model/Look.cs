namespace Luxia.Scenes.Model;

/// <summary>
/// Look (« préréglage de soirée », doc 60 §4.8, F1, glossaire) : une liste nommée d'actions — lancer ou arrêter des
/// scènes, arrêter des couches, régler des masters — appelée d'un geste. Par exemple « Temps mort » : tout arrêter,
/// lancer « Ambre – couleur seule », Grand Master à 40 %. Le même objet servira aux interventions du mode automatique
/// (P10 : « la musique se calme → look calme »).
/// </summary>
public sealed record Look
{
    /// <summary>Identifiant stable (GEN-052).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom.</summary>
    public required string Name { get; init; }

    /// <summary>Couleur d'affichage « #RRGGBB ».</summary>
    public string Color { get; init; } = "#8957E5";

    /// <summary>Explication courte (pour qui l'appelle, et pour l'IA qui l'écrit).</summary>
    public string? Notes { get; init; }

    /// <summary>Actions, jouées dans l'ordre.</summary>
    public IReadOnlyList<LookAction> Actions { get; init; } = [];
}
