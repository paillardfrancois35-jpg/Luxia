namespace Luxia.UI.Modules.Control.Docking;

/// <summary>Un panneau de l'écran Contrôle : identifiant stable (enregistré dans la disposition), titre, aide (F6).</summary>
/// <param name="Id">Identifiant stable.</param>
/// <param name="Title">Titre de l'onglet.</param>
/// <param name="Help">Aide courte : ce que c'est, à quoi ça sert.</param>
public sealed record ControlPanel(string Id, string Title, string Help);

/// <summary>Panneaux de l'écran Contrôle (doc 60 §6).</summary>
public static class ControlPanels
{
    /// <summary>Couches et scènes.</summary>
    public const string Columns = "colonnes";

    /// <summary>Propriétés de la scène éditée.</summary>
    public const string Properties = "proprietes";

    /// <summary>Plan des appareils (sélection).</summary>
    public const string Plan = "plan";

    /// <summary>Réglages des appareils sélectionnés.</summary>
    public const string Settings = "reglages";

    /// <summary>Journal.</summary>
    public const string Journal = "journal";

    /// <summary>Looks (ERG-023).</summary>
    public const string Looks = "looks";

    /// <summary>Pilote automatique (F10, réservé jusqu'à P10).</summary>
    public const string Pilot = "pilote";

    /// <summary>Tous les panneaux.</summary>
    public static IReadOnlyList<ControlPanel> All { get; } =
    [
        new(Columns, "Colonnes", "Une colonne par couche. La grande zone d'un bouton joue ou arrête la scène (maintenir dans une couche Flash) ; la bande ✎ la choisit pour l'éditer. Clic droit : renommer, dupliquer, couleur, couche, supprimer."),
        new(Properties, "Propriétés", "La scène choisie avec ✎ : nom, couche, vitesse, fondus, étapes. Tout est enregistré dès la saisie ; Ctrl+Z annule."),
        new(Plan, "Plan des appareils", "Le lieu vu de dessus, avec les couleurs réellement émises (l'aperçu en AVEUGLE). C'est ici qu'on choisit les appareils : clic, Ctrl + clic pour ajouter, glisser pour un rectangle."),
        new(Settings, "Réglages des appareils", "Les réglages des appareils sélectionnés sur le plan. En LIVE : surcharges temporaires (pastille jaune). En ÉDITION / AVEUGLE : écrits dans l'étape choisie (pastille verte)."),
        new(Journal, "Journal", "Ce qui vient de se passer, le plus récent en haut : scènes, sûreté, enregistrements, annulations."),
        new(Looks, "Looks", "Un look est une liste d'actions appelée d'un clic : « Temps mort » = tout arrêter, lancer l'ambre, master Intensité à 40 %. On le crée en capturant ce qui joue. Le pilote automatique s'en servira pour réagir à la musique."),
        new(Pilot, "Pilote automatique", "Ce que joue le mode automatique, pourquoi, et les boutons d'intervention (les looks). Le pilote lui-même arrive en P10."),
    ];

    /// <summary>Panneau par identifiant (inconnu : un panneau vide nommé d'après l'identifiant).</summary>
    public static ControlPanel Get(string? id) =>
        All.FirstOrDefault(p => p.Id == id) ?? new ControlPanel(id ?? "?", id ?? "?", "Panneau inconnu (disposition d'une autre version).");
}
