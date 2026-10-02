using Luxia.Engine.Model;

namespace Luxia.UI.Modules.Control;

/// <summary>Listes de choix en français de l'éditeur de scènes.</summary>
public static class SceneOptions
{
    /// <summary>Unités de durée (GEN-023).</summary>
    public static IReadOnlyList<Choice<DurationUnit>> Units { get; } =
    [
        new(DurationUnit.Seconds, "s"),
        new(DurationUnit.Beats, "temps"),
        new(DurationUnit.Bars, "mesures"),
    ];

    /// <summary>Modes de boucle (MOT-013).</summary>
    public static IReadOnlyList<Choice<LoopMode>> Loops { get; } =
    [
        new(LoopMode.Infinite, "Infini"),
        new(LoopMode.Once, "Une fois"),
        new(LoopMode.Count, "N fois"),
        new(LoopMode.PingPong, "Aller-retour"),
        new(LoopMode.Random, "Aléatoire"),
    ];

    /// <summary>Fins de scène (MOT-014).</summary>
    public static IReadOnlyList<Choice<EndMode>> Ends { get; } =
    [
        new(EndMode.Stop, "S'arrêter"),
        new(EndMode.Hold, "Rester sur la dernière étape"),
        new(EndMode.Chain, "Enchaîner sur…"),
    ];

    /// <summary>Événements qui font avancer d'étape (MOT-017).</summary>
    public static IReadOnlyList<Choice<StepAdvanceMode>> Advances { get; } =
    [
        new(StepAdvanceMode.Duration, "À la durée de l'étape"),
        new(StepAdvanceMode.Beat, "À chaque temps"),
        new(StepAdvanceMode.Bar, "À chaque mesure"),
        new(StepAdvanceMode.BassPulse, "Sur les basses (kick)"),
        new(StepAdvanceMode.TreblePulse, "Sur les aigus (caisse claire)"),
    ];

    /// <summary>
    /// Fréquence des étapes (essai P7, décision 5) : le code est le nombre d'événements entre deux étapes (1, 2, 4, 8) ou, à partir de 100,
    /// le nombre d'étapes par temps ou par mesure (102 = ×2, 104 = ×4).
    /// </summary>
    public static IReadOnlyList<Choice<int>> Frequencies { get; } =
    [
        new(104, "×4 (quatre fois plus vite)"),
        new(102, "×2 (deux fois plus vite)"),
        new(1, "×1 (à chaque événement)"),
        new(2, "÷ 2 (un sur deux)"),
        new(4, "÷ 4 (un sur quatre)"),
        new(8, "÷ 8 (un sur huit)"),
    ];

    /// <summary>Quantification du lancement (MOT-018).</summary>
    public static IReadOnlyList<Choice<LaunchQuantize>> Quantizes { get; } =
    [
        new(LaunchQuantize.None, "Sans attendre le temps"),
        new(LaunchQuantize.Beat, "Au prochain temps"),
        new(LaunchQuantize.Bar, "À la prochaine mesure"),
        new(LaunchQuantize.Phrase4, "À la prochaine phrase (4 mesures)"),
        new(LaunchQuantize.Phrase8, "À la prochaine phrase (8 mesures)"),
    ];

    /// <summary>Courbes de fondu (MOT-011).</summary>
    public static IReadOnlyList<Choice<FadeCurve>> Curves { get; } =
    [
        new(FadeCurve.Linear, "Linéaire"),
        new(FadeCurve.SCurve, "En S"),
        new(FadeCurve.Instant, "Instantanée"),
    ];

    /// <summary>Bascule des attributs discrets (MOT-012).</summary>
    public static IReadOnlyList<Choice<DiscreteSwitch>> Switches { get; } =
    [
        new(DiscreteSwitch.Start, "Au début"),
        new(DiscreteSwitch.Middle, "Au milieu"),
        new(DiscreteSwitch.End, "À la fin"),
    ];
}
