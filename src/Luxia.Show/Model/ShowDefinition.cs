namespace Luxia.Show.Model;

/// <summary>
/// Show (doc 20 §3) : un graphe d'étapes et de transitions, de type Grafcet, qui pilote scènes, séquences et couches. Le nom
/// « Show » désigne déjà le projet de l'application (<c>Luxia.Show</c>) : le type s'appelle donc « définition de show ».
/// </summary>
public sealed record ShowDefinition
{
    /// <summary>Identifiant stable (GEN-052).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nom.</summary>
    public required string Name { get; init; }

    /// <summary>Couleur d'affichage « #RRGGBB ».</summary>
    public string Color { get; init; } = "#39C5CF";

    /// <summary>Catégorie (« Phase P8 », « Proposé par IA »…).</summary>
    public string? Category { get; init; }

    /// <summary>Notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Rôle pour le Directeur (SHOW-030, doc 22 §2).</summary>
    public ShowRole Role { get; init; } = ShowRole.Main;

    /// <summary>Styles visés (SHOW-030) ; vide = tous.</summary>
    public IReadOnlyList<string> Styles { get; init; } = [];

    /// <summary>Niveau d'énergie minimal visé (0 Calme à 3 Explosif, SHOW-030).</summary>
    public int EnergyMin { get; init; }

    /// <summary>Niveau d'énergie maximal visé (0 à 3, SHOW-030).</summary>
    public int EnergyMax { get; init; } = 3;

    /// <summary>Poids dans le choix du Directeur (SHOW-030).</summary>
    public double Weight { get; init; } = 1;

    /// <summary>Durée maximale de jeu avant rotation, en minutes (SHOW-030) ; <c>null</c> = aucune.</summary>
    public double? MaxMinutes { get; init; }

    /// <summary>
    /// Show secondaire (SHOW-031) : il joue en parallèle du show principal (« ambiance UV et fumée ») et n'arrête pas
    /// celui-ci quand on le lance.
    /// </summary>
    public bool Secondary { get; init; }

    /// <summary>Ce qui se passe quand toutes les étapes actives sont des fins (R6).</summary>
    public ShowEnd AtEnd { get; init; } = ShowEnd.Hold;

    /// <summary>Variables du show (compteurs, SHOW-029).</summary>
    public IReadOnlyList<ShowVariable> Variables { get; init; } = [];

    /// <summary>Étapes.</summary>
    public IReadOnlyList<ShowStep> Steps { get; init; } = [];

    /// <summary>Transitions, dans l'ordre de priorité (la première vraie l'emporte en divergence OU, R4).</summary>
    public IReadOnlyList<ShowTransition> Transitions { get; init; } = [];
}

/// <summary>Rôle d'un show pour le Directeur (doc 22 §2).</summary>
public enum ShowRole
{
    /// <summary>Jeu normal.</summary>
    Main,

    /// <summary>Court, entre deux morceaux.</summary>
    Transition,

    /// <summary>Silence, pause.</summary>
    Waiting,

    /// <summary>Morceau lent.</summary>
    Slow,

    /// <summary>Premier morceau du mode automatique.</summary>
    Opening,
}

/// <summary>Fin d'un show (R6) : toutes ses étapes actives n'ont plus de transition sortante.</summary>
public enum ShowEnd
{
    /// <summary>Le show tient ses dernières étapes (leurs scènes continuent) jusqu'à ce qu'on l'arrête (D39).</summary>
    Hold,

    /// <summary>Le show s'arrête, et ses scènes avec.</summary>
    Stop,

    /// <summary>Le show reprend à ses étapes initiales.</summary>
    Restart,
}

/// <summary>Variable d'un show (SHOW-029) : un nombre, remis à sa valeur initiale au lancement.</summary>
/// <param name="Name">Nom (« refrains »).</param>
/// <param name="Initial">Valeur au lancement.</param>
public sealed record ShowVariable(string Name, double Initial = 0);

/// <summary>
/// Étape d'un show (doc 20 §3.1) : état actif qui porte des actions. Une étape sans transition sortante est une fin (R6).
/// </summary>
public sealed record ShowStep
{
    /// <summary>Identifiant court et lisible, unique dans le show (« 0 », « 2a », « refrain »), D39.</summary>
    public required string Id { get; init; }

    /// <summary>Nom.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Étape initiale (au moins une par show).</summary>
    public bool Initial { get; init; }

    /// <summary>
    /// Macro-étape (SHOW-020) : le show joué tant que l'étape est active ; ses transitions sortantes ne sont validées qu'une fois
    /// ce sous-show arrivé à sa fin.
    /// </summary>
    public Guid? MacroShowId { get; init; }

    /// <summary>Choix entre plusieurs transitions vraies au même instant (R4) : priorité ou tirage au sort pondéré.</summary>
    public StepChoice Choice { get; init; } = StepChoice.Priority;

    /// <summary>Tirage au sort : éviter de reprendre deux fois de suite la même transition (SHOW-028).</summary>
    public bool AvoidRepeat { get; init; }

    /// <summary>Actions de l'étape.</summary>
    public IReadOnlyList<ShowAction> Actions { get; init; } = [];
}

/// <summary>Choix d'une divergence OU (R4).</summary>
public enum StepChoice
{
    /// <summary>La première transition vraie, dans l'ordre du show.</summary>
    Priority,

    /// <summary>Tirage au sort pondéré parmi les transitions vraies (SHOW-028).</summary>
    Random,
}
