using Luxia.Show.Model;

namespace Luxia.Show.Runtime;

/// <summary>
/// Ce que jouent les shows et les séquences, pour la supervision (SHOW-026, LIVE-023) : copie immuable publiée par le séquenceur
/// au plus dix fois par seconde (et à chaque changement d'étape), lue par l'interface à son rythme.
/// </summary>
/// <param name="Shows">Shows qui jouent (principal puis secondaires).</param>
/// <param name="Sequences">Séquences qui jouent (lancées à la main ou par un show).</param>
public sealed record SequencerState(IReadOnlyList<ShowStatus> Shows, IReadOnlyList<SequenceStatus> Sequences)
{
    /// <summary>Rien ne joue.</summary>
    public static SequencerState Empty { get; } = new([], []);

    /// <summary>Le show principal qui joue, s'il y en a un.</summary>
    public ShowStatus? MainShow => Shows.FirstOrDefault(s => !s.Secondary);
}

/// <summary>Un show qui joue.</summary>
/// <param name="ShowId">Show.</param>
/// <param name="Name">Nom.</param>
/// <param name="Color">Couleur d'affichage.</param>
/// <param name="Secondary">Show secondaire (SHOW-031).</param>
/// <param name="ActiveSteps">Étapes actives.</param>
/// <param name="Transitions">Transitions validées (étapes amont actives), en attente de leur condition ou armées.</param>
/// <param name="Path">Dernières étapes activées, de la plus ancienne à la plus récente.</param>
/// <param name="Variables">Variables du show et leur valeur.</param>
/// <param name="Ended">Arrivé à sa fin : il tient ses dernières étapes (R6, D39).</param>
public sealed record ShowStatus(
    Guid ShowId,
    string Name,
    string Color,
    bool Secondary,
    IReadOnlyList<StepStatus> ActiveSteps,
    IReadOnlyList<TransitionStatus> Transitions,
    IReadOnlyList<string> Path,
    IReadOnlyList<(string Name, double Value)> Variables,
    bool Ended = false);

/// <summary>Une étape active.</summary>
/// <param name="Id">Identifiant (« 2a »).</param>
/// <param name="Name">Nom.</param>
/// <param name="SinceBeats">Temps écoulés depuis son activation.</param>
/// <param name="SinceSeconds">Secondes écoulées depuis son activation.</param>
/// <param name="Actions">Actions de l'étape, en clair.</param>
/// <param name="Macro">Sous-show d'une macro-étape, s'il joue.</param>
public sealed record StepStatus(string Id, string Name, double SinceBeats, double SinceSeconds, IReadOnlyList<string> Actions, ShowStatus? Macro);

/// <summary>Une transition validée (ses étapes amont sont actives).</summary>
/// <param name="Index">Rang dans le show (pour la forcer, CMD-051).</param>
/// <param name="From">Étapes amont.</param>
/// <param name="To">Étapes aval.</param>
/// <param name="ToNames">Noms des étapes aval.</param>
/// <param name="Condition">Réceptivité en clair.</param>
/// <param name="Quantize">Quantification.</param>
/// <param name="Armed">Condition vraie : la transition attend sa frontière musicale.</param>
/// <param name="BeatsLeft">Armée : temps restants avant de partir.</param>
/// <param name="Hint">Précision utile (« vraie dans 4 mesures », « 2 / 3 »), ou vide.</param>
public sealed record TransitionStatus(
    int Index,
    IReadOnlyList<string> From,
    IReadOnlyList<string> To,
    IReadOnlyList<string> ToNames,
    string Condition,
    ShowQuantize Quantize,
    bool Armed,
    double BeatsLeft,
    string Hint);

/// <summary>Une séquence qui joue.</summary>
/// <param name="SequenceId">Séquence.</param>
/// <param name="Name">Nom.</param>
/// <param name="PositionBars">Position dans le passage en mesures (négative : attend son départ).</param>
/// <param name="Bars">Longueur en mesures.</param>
/// <param name="Loops">Passages terminés.</param>
/// <param name="OwnerShowId">Show qui la joue, ou <c>null</c>.</param>
public sealed record SequenceStatus(Guid SequenceId, string Name, double PositionBars, double Bars, int Loops, Guid? OwnerShowId);
