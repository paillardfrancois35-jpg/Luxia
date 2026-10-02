using Luxia.Engine.Model;

namespace Luxia.Show.Model;

/// <summary>
/// Transition d'un show (doc 20 §3.1) : passage des étapes amont (toutes actives, convergence en ET) vers les étapes aval
/// (activées ensemble, divergence en ET) quand la réceptivité est vraie, à la frontière musicale choisie.
/// </summary>
public sealed record ShowTransition
{
    /// <summary>Étapes amont (identifiants d'étape).</summary>
    public IReadOnlyList<string> From { get; init; } = [];

    /// <summary>Étapes aval.</summary>
    public IReadOnlyList<string> To { get; init; } = [];

    /// <summary>Réceptivité.</summary>
    public ShowCondition Condition { get; init; } = new();

    /// <summary>Frontière musicale attendue avant de franchir (quantification).</summary>
    public ShowQuantize Quantize { get; init; } = ShowQuantize.None;

    /// <summary>Poids pour le tirage au sort (SHOW-028).</summary>
    public double Weight { get; init; } = 1;

    /// <summary>Libellé facultatif (sinon, la réceptivité décrite).</summary>
    public string? Label { get; init; }
}

/// <summary>
/// Réceptivité (doc 20 §3.2, SHOW-022) : une condition simple, ou une combinaison ET / OU / NON de conditions
/// (<see cref="Conditions"/>). Les événements (drop, break…) sont vrais au tick où ils arrivent ; une transition quantifiée les
/// retient jusqu'à la frontière musicale (D39).
/// </summary>
public sealed record ShowCondition
{
    /// <summary>Nature.</summary>
    public ConditionKind Kind { get; init; } = ConditionKind.Always;

    /// <summary>Durée de <see cref="ConditionKind.After"/> (secondes, temps ou mesures, depuis l'activation de l'étape).</summary>
    public Duration? Duration { get; init; }

    /// <summary>Seuil (énergie 0 à 1), probabilité (0 à 1), valeur comparée d'une variable.</summary>
    public double Value { get; init; }

    /// <summary>Borne basse (niveau d'énergie 0 à 3, tempo en BPM).</summary>
    public double? Min { get; init; }

    /// <summary>Borne haute (niveau d'énergie 0 à 3, tempo en BPM).</summary>
    public double? Max { get; init; }

    /// <summary>Scène visée (<see cref="ConditionKind.SceneEnded"/>).</summary>
    public Guid? SceneId { get; init; }

    /// <summary>Séquence visée (<see cref="ConditionKind.SequenceEnded"/>, <see cref="ConditionKind.SequenceLoops"/>).</summary>
    public Guid? SequenceId { get; init; }

    /// <summary>Nombre de passages (<see cref="ConditionKind.SequenceLoops"/>).</summary>
    public int Count { get; init; } = 1;

    /// <summary>Styles (<see cref="ConditionKind.Style"/>).</summary>
    public IReadOnlyList<string> Styles { get; init; } = [];

    /// <summary>Fréquence du tirage de <see cref="ConditionKind.Random"/> : à chaque temps, mesure ou phrase.</summary>
    public ShowQuantize Every { get; init; } = ShowQuantize.Bar;

    /// <summary>Variable comparée (<see cref="ConditionKind.Variable"/>).</summary>
    public string? Variable { get; init; }

    /// <summary>Comparaison de la variable.</summary>
    public Comparison Comparison { get; init; } = Comparison.AtLeast;

    /// <summary>Conditions combinées (<see cref="ConditionKind.All"/>, <see cref="ConditionKind.Any"/>, <see cref="ConditionKind.Not"/>).</summary>
    public IReadOnlyList<ShowCondition> Conditions { get; init; } = [];
}

/// <summary>Nature d'une réceptivité (doc 20 §3.2).</summary>
public enum ConditionKind
{
    /// <summary>Toujours vraie (enchaînement immédiat, ou à la frontière musicale si la transition est quantifiée).</summary>
    Always,

    /// <summary>Jamais vraie d'elle-même : seulement forcée (bouton, touche, pad MIDI, CMD-051).</summary>
    Manual,

    /// <summary>Après une durée d'activité de l'étape.</summary>
    After,

    /// <summary>La scène lancée par l'étape est terminée.</summary>
    SceneEnded,

    /// <summary>La séquence est terminée.</summary>
    SequenceEnded,

    /// <summary>La séquence a bouclé au moins N fois.</summary>
    SequenceLoops,

    /// <summary>Drop.</summary>
    Drop,

    /// <summary>Break.</summary>
    Break,

    /// <summary>Montée.</summary>
    BuildUp,

    /// <summary>Plus aucun son.</summary>
    Silence,

    /// <summary>Le son reprend après un silence.</summary>
    Resumed,

    /// <summary>Morceau changé (avant P9 : reprise après un silence ou saut de tempo, D38).</summary>
    SongChanged,

    /// <summary>L'énergie passe au-dessus du seuil (front montant).</summary>
    EnergyAbove,

    /// <summary>L'énergie passe sous le seuil (front descendant).</summary>
    EnergyBelow,

    /// <summary>Le niveau d'énergie est entre <see cref="ShowCondition.Min"/> et <see cref="ShowCondition.Max"/>.</summary>
    EnergyLevel,

    /// <summary>Le style courant est l'un des styles donnés (P9 ; en P8, style simulé).</summary>
    Style,

    /// <summary>Le tempo est entre <see cref="ShowCondition.Min"/> et <see cref="ShowCondition.Max"/>.</summary>
    Tempo,

    /// <summary>Probabilité <see cref="ShowCondition.Value"/> à chaque frontière <see cref="ShowCondition.Every"/>.</summary>
    Random,

    /// <summary>Comparaison d'une variable du show.</summary>
    Variable,

    /// <summary>Toutes les conditions (ET).</summary>
    All,

    /// <summary>Au moins une condition (OU).</summary>
    Any,

    /// <summary>La condition est fausse (NON).</summary>
    Not,
}

/// <summary>Comparaison d'une variable à une valeur.</summary>
public enum Comparison
{
    /// <summary>≥.</summary>
    AtLeast,

    /// <summary>≤.</summary>
    AtMost,

    /// <summary>=.</summary>
    EqualTo,

    /// <summary>≠.</summary>
    NotEqualTo,
}
