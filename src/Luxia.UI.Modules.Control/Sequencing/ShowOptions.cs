using Luxia.Engine.Model;
using Luxia.Show.Model;
using Luxia.UI.Modules.Scenes;

namespace Luxia.UI.Modules.Control.Sequencing;

/// <summary>Listes de choix en français des éditeurs de séquence et de show.</summary>
public static class ShowOptions
{
    /// <summary>Frontières musicales (démarrage d'une séquence, franchissement d'une transition).</summary>
    public static IReadOnlyList<Choice<ShowQuantize>> Quantizes { get; } =
    [
        new(ShowQuantize.None, "Tout de suite"),
        new(ShowQuantize.Beat, "Au prochain temps"),
        new(ShowQuantize.Bar, "À la prochaine mesure"),
        new(ShowQuantize.Phrase4, "À la prochaine phrase (4 mesures)"),
        new(ShowQuantize.Phrase8, "À la prochaine phrase (8 mesures)"),
        new(ShowQuantize.Phrase16, "À la prochaine phrase (16 mesures)"),
    ];

    /// <summary>Vitesses relatives d'une séquence (SHOW-008).</summary>
    public static IReadOnlyList<Choice<double>> Speeds { get; } =
    [
        new(0.5, "½ temps"),
        new(1, "normale"),
        new(2, "double"),
    ];

    /// <summary>Grilles magnétiques de la frise (SHOW-002), en mesures.</summary>
    public static IReadOnlyList<Choice<double>> Snaps { get; } =
    [
        new(1, "mesure"),
        new(0.25, "temps"),
        new(0.125, "½ temps"),
    ];

    /// <summary>Unités de la durée d'un bloc : vrai = mesures, faux = temps.</summary>
    public static IReadOnlyList<Choice<bool>> LengthUnits { get; } =
    [
        new(true, "mesures"),
        new(false, "temps"),
    ];

    /// <summary>Réceptivités simples, éditables dans une carte (les combinaisons ET / OU / NON s'écrivent dans le fichier).</summary>
    public static IReadOnlyList<Choice<ConditionKind>> Conditions { get; } =
    [
        new(ConditionKind.After, "après une durée"),
        new(ConditionKind.Drop, "au drop"),
        new(ConditionKind.Break, "au break"),
        new(ConditionKind.BuildUp, "à la montée"),
        new(ConditionKind.SongChanged, "au morceau suivant"),
        new(ConditionKind.Silence, "au silence"),
        new(ConditionKind.Resumed, "à la reprise du son"),
        new(ConditionKind.EnergyLevel, "niveau d'énergie"),
        new(ConditionKind.EnergyAbove, "énergie qui passe au-dessus de…"),
        new(ConditionKind.EnergyBelow, "énergie qui passe sous…"),
        new(ConditionKind.SequenceEnded, "fin d'une séquence"),
        new(ConditionKind.SequenceLoops, "séquence jouée N fois"),
        new(ConditionKind.SceneEnded, "fin d'une scène"),
        new(ConditionKind.Variable, "variable"),
        new(ConditionKind.Random, "au hasard"),
        new(ConditionKind.Style, "style"),
        new(ConditionKind.Tempo, "tempo entre…"),
        new(ConditionKind.Manual, "à la main seulement"),
        new(ConditionKind.Always, "aussitôt"),
    ];

    /// <summary>Niveaux d'énergie.</summary>
    public static IReadOnlyList<Choice<int>> EnergyLevels { get; } =
    [
        new(0, "Calme"),
        new(1, "Groove"),
        new(2, "Énergique"),
        new(3, "Explosif"),
    ];

    /// <summary>Unités d'une durée de transition.</summary>
    public static IReadOnlyList<Choice<DurationUnit>> DurationUnits { get; } =
    [
        new(DurationUnit.Bars, "mesures"),
        new(DurationUnit.Beats, "temps"),
        new(DurationUnit.Seconds, "s"),
    ];

    /// <summary>Comparaisons d'une variable.</summary>
    public static IReadOnlyList<Choice<Comparison>> Comparisons { get; } =
    [
        new(Comparison.AtLeast, "≥"),
        new(Comparison.AtMost, "≤"),
        new(Comparison.EqualTo, "="),
        new(Comparison.NotEqualTo, "≠"),
    ];

    /// <summary>Actions d'une étape (SHOW-021).</summary>
    public static IReadOnlyList<Choice<ShowActionKind>> Actions { get; } =
    [
        new(ShowActionKind.Play, "jouer une scène (pendant l'étape)"),
        new(ShowActionKind.PlaySequence, "jouer une séquence (pendant l'étape)"),
        new(ShowActionKind.Launch, "lancer une scène et la laisser"),
        new(ShowActionKind.LaunchSequence, "lancer une séquence et la laisser"),
        new(ShowActionKind.Stop, "arrêter une scène"),
        new(ShowActionKind.StopSequence, "arrêter une séquence"),
        new(ShowActionKind.StopLayer, "arrêter une couche"),
        new(ShowActionKind.LayerLevel, "niveau d'une couche"),
        new(ShowActionKind.Speed, "vitesse d'une scène"),
        new(ShowActionKind.Flash, "flash d'une scène"),
        new(ShowActionKind.Smoke, "rafale de fumée"),
        new(ShowActionKind.Blackout, "noir court"),
        new(ShowActionKind.Variable, "compter (variable + 1)"),
    ];

    /// <summary>Rôles pour le Directeur (SHOW-030).</summary>
    public static IReadOnlyList<Choice<ShowRole>> Roles { get; } =
    [
        new(ShowRole.Main, "Principal"),
        new(ShowRole.Transition, "Transition"),
        new(ShowRole.Waiting, "Attente"),
        new(ShowRole.Slow, "Slow"),
        new(ShowRole.Opening, "Ouverture"),
    ];

    /// <summary>Fins d'un show (R6, D39).</summary>
    public static IReadOnlyList<Choice<ShowEnd>> Ends { get; } =
    [
        new(ShowEnd.Hold, "tenir la dernière étape"),
        new(ShowEnd.Stop, "s'arrêter"),
        new(ShowEnd.Restart, "reprendre au début"),
    ];
}
