namespace Luxia.UI.Modules.Control.Docking;

/// <summary>Un panneau de l'écran Contrôle : identifiant stable (enregistré dans la disposition), titre, aide (F6).</summary>
/// <param name="Id">Identifiant stable.</param>
/// <param name="Title">Titre de l'onglet.</param>
/// <param name="Help">Aide courte : ce que c'est, à quoi ça sert.</param>
public sealed record ControlPanel(string Id, string Title, string Help);

/// <summary>Panneaux de l'écran de jeu (Colonnes, Groupes dimmer, Looks, Pilote, Journal) et de la fenêtre d'édition (Propriétés, Plan, Réglages, Effets), doc 60 §6.</summary>
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

    /// <summary>Effets de l'étape éditée (doc 16 §6).</summary>
    public const string Effects = "effets";

    /// <summary>Journal.</summary>
    public const string Journal = "journal";

    /// <summary>Looks (ERG-023).</summary>
    public const string Looks = "looks";

    /// <summary>Pilote automatique (F10, réservé jusqu'à P10).</summary>
    public const string Pilot = "pilote";

    /// <summary>Groupes dimmer (ERG-037) : un fader par groupe d'appareils qui a un dimmer.</summary>
    public const string Dimmers = "dimmers";

    /// <summary>Tous les panneaux connus (jeu et fenêtre d'édition).</summary>
    public static IReadOnlyList<ControlPanel> All { get; } =
    [
        new(Columns, "Colonnes", "Une colonne par couche. La grande zone d'un bouton joue ou arrête la scène (maintenir dans une couche Flash) ; la bande ✎ ouvre la fenêtre d'édition de la scène. Le niveau d'une couche multiplie ce qu'elle envoie. Clic droit : éditer, renommer, dupliquer, couleur, couche, supprimer."),
        new(Properties, "Propriétés", "La scène choisie avec ✎ : nom, couche, vitesse, fondus, étapes. Tout est enregistré dès la saisie ; Ctrl+Z annule."),
        new(Plan, "Plan des appareils", "Le lieu vu de dessus, avec les couleurs réellement émises (l'aperçu en AVEUGLE). C'est ici qu'on choisit les appareils : clic, Ctrl + clic pour ajouter, glisser pour un rectangle."),
        new(Settings, "Réglages des appareils", "Les réglages des appareils sélectionnés sur le plan. En LIVE : surcharges temporaires (pastille jaune). En ÉDITION / AVEUGLE : écrits dans l'étape choisie (pastille verte)."),
        new(Effects, "Effets", "Les effets de l'étape éditée : une forme (vague, cercle, arc-en-ciel…) qui tourne en boucle sur les appareils choisis, décalée d'un appareil à l'autre. Choisissez les appareils au plan, un modèle de la bibliothèque, « + Ajouter », puis réglez avec les molettes (glisser, molette de la souris, double-clic = valeur par défaut). En ÉDITION on le voit sur la sortie, en AVEUGLE sur l'aperçu."),
        new(Journal, "Journal", "Ce qui vient de se passer, le plus récent en haut : scènes, sûreté, enregistrements, annulations."),
        new(Looks, "Looks", "Un look est une liste d'actions appelée d'un clic : « Temps mort » = tout arrêter, lancer l'ambre, master Intensité à 40 %. On le crée en capturant ce qui joue. Le pilote automatique s'en servira pour réagir à la musique."),
        new(Dimmers, "Groupes dimmer", "Un fader par groupe d'appareils qui a un dimmer (PAR, UV, lyres…), rangé dans l'arbre de Installation › Gestion des dimmers. Il multiplie l'intensité de ses appareils, après les couches : 50 % sur un groupe, moitié moins de lumière. Les niveaux se multiplient le long de l'arbre. C'est une retouche en direct, jamais enregistrée ; les 8 premiers faders sont ceux de la seconde platine MIDI. Double-clic sur un fader : retour à 100 %."),
        new(Pilot, "Pilote automatique", "Ce que joue le mode automatique, pourquoi, et les boutons d'intervention (les looks). Le pilote lui-même arrive en P10."),
    ];

    /// <summary>Panneaux de l'écran de jeu, dans l'ordre du menu Panneaux ; les autres sont ceux de la fenêtre d'édition.</summary>
    public static IReadOnlyList<ControlPanel> Game { get; } = [.. new[] { Columns, Dimmers, Looks, Pilot, Journal }.Select(Get)];

    /// <summary>Panneau par identifiant (inconnu : un panneau vide nommé d'après l'identifiant).</summary>
    public static ControlPanel Get(string? id) =>
        All.FirstOrDefault(p => p.Id == id) ?? new ControlPanel(id ?? "?", id ?? "?", "Panneau inconnu (disposition d'une autre version).");
}
