namespace Luxia.Tools.Prototype.Docking;

/// <summary>Un panneau connu du prototype : identifiant stable (enregistré dans la disposition), titre, rôle.</summary>
/// <param name="Id">Identifiant stable.</param>
/// <param name="Title">Titre de l'onglet.</param>
/// <param name="Help">Aide courte (F6 : ce que c'est, à quoi ça sert).</param>
/// <param name="IsPlaceholder">Panneau prévu mais pas encore réalisé : il n'affiche que son rôle.</param>
internal sealed record PanelInfo(string Id, string Title, string Help, bool IsPlaceholder);

/// <summary>Panneaux du prototype (doc 60 §6) : les réalisés et, pour éprouver l'ancrage, ceux qui ne sont que prévus.</summary>
internal static class PanelCatalog
{
    public const string Columns = "colonnes";
    public const string Properties = "proprietes";
    public const string FixturePlan = "plan";
    public const string Settings = "reglages";
    public const string Position = "position";
    public const string Color = "couleur";
    public const string Log = "journal";
    public const string Gallery = "galerie";
    public const string Metrics = "mesures";
    public const string Pilot = "pilote";

    public static IReadOnlyList<PanelInfo> All { get; } =
    [
        new(Columns, "Colonnes", "Les couches en colonnes et leurs scènes : on joue ici (grande zone du bouton) et on choisit la scène à éditer (bande de droite).", true),
        new(Properties, "Propriétés", "Réglages de la scène ou de la couche choisie : nom, couleur, vitesse, fondus, étapes.", true),
        new(FixturePlan, "Plan des appareils", "Le plan du lieu, vu de dessus : on y sélectionne les appareils pour tous les autres panneaux.", true),
        new(Settings, "Réglages des appareils", "Les réglages des appareils sélectionnés sur le plan, par famille (Intensité, Couleur, Position, Faisceau, Autres, Faders). La pastille dit si la valeur est une surcharge LIVE (jaune) ou enregistrée dans la scène (verte).", true),
        new(Position, "Position", "Visée des lyres sur la grille Pan / Tilt, et zones interdites ou permises.", false),
        new(Color, "Couleur", "Couleur des appareils sélectionnés : teinte, saturation, intensité, favoris.", false),
        new(Log, "Journal", "Ce qui vient de se passer, du plus récent au plus ancien.", false),
        new(Gallery, "Galerie", "Tous les composants communs, dans tous leurs états : référence des captures.", false),
        new(Metrics, "Mesures", "Fluidité et mémoire du prototype, pour valider Avalonia 12 et Dock.", false),
        new(Pilot, "Pilote automatique", "Ce que joue le mode automatique, pourquoi, et les boutons d'intervention (looks). Vide jusqu'à P10 (F10).", true),
    ];

    public static PanelInfo Get(string? id) =>
        All.FirstOrDefault(p => p.Id == id) ?? new PanelInfo(id ?? "?", id ?? "?", "Panneau inconnu.", true);
}
