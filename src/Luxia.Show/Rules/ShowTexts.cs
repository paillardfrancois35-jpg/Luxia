using System.Globalization;
using Luxia.Engine.Model;
using Luxia.Show.Model;

namespace Luxia.Show.Rules;

/// <summary>
/// Textes en français des shows (supervision, cartes de l'éditeur, journal) : une réceptivité (« au drop », « après 16 mesures »),
/// une quantification (« à la prochaine mesure »), une action (« joue « Bleu lent » »).
/// </summary>
public static class ShowTexts
{
    private static readonly string[] EnergyNames = ["Calme", "Groove", "Énergique", "Explosif"];

    /// <summary>Nom d'un niveau d'énergie (0 Calme à 3 Explosif).</summary>
    public static string EnergyName(int level) => EnergyNames[Math.Clamp(level, 0, 3)];

    /// <summary>Libellé d'une transition : le sien, sinon sa condition décrite.</summary>
    public static string Label(ShowTransition transition, Func<Guid, string?>? names = null)
    {
        ArgumentNullException.ThrowIfNull(transition);
        return string.IsNullOrWhiteSpace(transition.Label) ? Describe(transition.Condition, names) : transition.Label;
    }

    /// <summary>Une réceptivité en clair (« au drop », « énergie ≥ Groove », « après 16 mesures »).</summary>
    public static string Describe(ShowCondition condition, Func<Guid, string?>? names = null)
    {
        ArgumentNullException.ThrowIfNull(condition);
        string Name(Guid? id) => id is { } value ? names?.Invoke(value) ?? "?" : "?";
        return condition.Kind switch
        {
            ConditionKind.Always => "aussitôt",
            ConditionKind.Manual => "à la main",
            ConditionKind.After => $"après {Duration(condition.Duration ?? Engine.Model.Duration.Zero)}",
            ConditionKind.SceneEnded => $"fin de « {Name(condition.SceneId)} »",
            ConditionKind.SequenceEnded => $"fin de « {Name(condition.SequenceId)} »",
            ConditionKind.SequenceLoops => string.Create(CultureInfo.CurrentCulture, $"« {Name(condition.SequenceId)} » jouée {condition.Count} fois"),
            ConditionKind.Drop => "au drop",
            ConditionKind.Break => "au break",
            ConditionKind.BuildUp => "à la montée",
            ConditionKind.Silence => "au silence",
            ConditionKind.Resumed => "à la reprise du son",
            ConditionKind.SongChanged => "au morceau suivant",
            ConditionKind.EnergyAbove => string.Create(CultureInfo.CurrentCulture, $"énergie qui passe au-dessus de {condition.Value * 100:0} %"),
            ConditionKind.EnergyBelow => string.Create(CultureInfo.CurrentCulture, $"énergie qui passe sous {condition.Value * 100:0} %"),
            ConditionKind.EnergyLevel => EnergyRange(condition),
            ConditionKind.Style => $"style {string.Join(" ou ", condition.Styles)}",
            ConditionKind.Tempo => string.Create(CultureInfo.CurrentCulture, $"tempo de {condition.Min ?? 0:0} à {condition.Max ?? 400:0} BPM"),
            ConditionKind.Random => string.Create(CultureInfo.CurrentCulture, $"{condition.Value * 100:0} % de chances {Every(condition.Every)}"),
            ConditionKind.Variable => string.Create(CultureInfo.CurrentCulture, $"{condition.Variable} {Symbol(condition.Comparison)} {condition.Value:0.##}"),
            ConditionKind.All => string.Join(" et ", condition.Conditions.Select(c => Wrap(c, names))),
            ConditionKind.Any => string.Join(" ou ", condition.Conditions.Select(c => Wrap(c, names))),
            ConditionKind.Not => $"pas {(condition.Conditions.Count > 0 ? Wrap(condition.Conditions[0], names) : "?")}",
            _ => condition.Kind.ToString(),
        };
    }

    /// <summary>Une quantification en clair (« à la prochaine mesure »), vide pour « tout de suite ».</summary>
    public static string Quantize(ShowQuantize quantize) => quantize switch
    {
        ShowQuantize.Beat => "au prochain temps",
        ShowQuantize.Bar => "à la prochaine mesure",
        ShowQuantize.Phrase4 => "à la prochaine phrase (4 mesures)",
        ShowQuantize.Phrase8 => "à la prochaine phrase (8 mesures)",
        ShowQuantize.Phrase16 => "à la prochaine phrase (16 mesures)",
        _ => string.Empty,
    };

    /// <summary>Une action d'étape en clair (« joue « Bleu lent » », « flash « Blanc » 0,5 s »).</summary>
    public static string Describe(ShowAction action, Func<Guid, string?>? names = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        string Name(Guid? id) => id is { } value ? names?.Invoke(value) ?? "?" : "?";
        return action.Kind switch
        {
            ShowActionKind.Play => $"joue « {Name(action.SceneId)} »",
            ShowActionKind.PlaySequence => $"joue la séquence « {Name(action.SequenceId)} »",
            ShowActionKind.Launch => $"lance « {Name(action.SceneId)} » et la laisse",
            ShowActionKind.LaunchSequence => $"lance la séquence « {Name(action.SequenceId)} » et la laisse",
            ShowActionKind.Stop => $"arrête « {Name(action.SceneId)} »",
            ShowActionKind.StopSequence => $"arrête la séquence « {Name(action.SequenceId)} »",
            ShowActionKind.StopLayer => $"arrête la couche « {Name(action.LayerId)} »",
            ShowActionKind.LayerLevel => string.Create(CultureInfo.CurrentCulture, $"niveau de « {Name(action.LayerId)} » à {action.Value * 100:0} %"),
            ShowActionKind.Speed => string.Create(CultureInfo.CurrentCulture, $"vitesse de « {Name(action.SceneId)} » × {action.Value:0.##}"),
            ShowActionKind.Flash => string.Create(CultureInfo.CurrentCulture, $"flash « {Name(action.SceneId)} » {action.Seconds:0.##} s"),
            ShowActionKind.Smoke => string.Create(CultureInfo.CurrentCulture, $"fumée {action.Seconds:0.##} s"),
            ShowActionKind.Blackout => string.Create(CultureInfo.CurrentCulture, $"noir {action.Seconds:0.##} s"),
            ShowActionKind.Variable => string.Create(CultureInfo.CurrentCulture, $"{action.Variable} {(action.Operation == VariableOperation.Set ? "=" : "+")} {action.Value:0.##}"),
            _ => action.Kind.ToString(),
        };
    }

    /// <summary>Une durée en clair : « 4 s », « 2 temps », « 16 mesures ».</summary>
    public static string Duration(Duration duration) => duration.Unit switch
    {
        DurationUnit.Beats => string.Create(CultureInfo.CurrentCulture, $"{duration.Value:0.##} temps"),
        DurationUnit.Bars => string.Create(CultureInfo.CurrentCulture, $"{duration.Value:0.##} mesure{(duration.Value > 1 ? "s" : string.Empty)}"),
        _ => string.Create(CultureInfo.CurrentCulture, $"{duration.Value:0.##} s"),
    };

    private static string EnergyRange(ShowCondition condition)
    {
        var min = (int)Math.Round(condition.Min ?? 0);
        var max = (int)Math.Round(condition.Max ?? 3);
        return (min, max) switch
        {
            (_, >= 3) when min > 0 => $"énergie ≥ {EnergyName(min)}",
            (<= 0, _) when max < 3 => $"énergie ≤ {EnergyName(max)}",
            _ when min == max => $"énergie {EnergyName(min)}",
            _ => $"énergie de {EnergyName(min)} à {EnergyName(max)}",
        };
    }

    private static string Every(ShowQuantize every) => every switch
    {
        ShowQuantize.Beat => "à chaque temps",
        ShowQuantize.Phrase4 => "à chaque phrase de 4 mesures",
        ShowQuantize.Phrase8 => "à chaque phrase de 8 mesures",
        ShowQuantize.Phrase16 => "à chaque phrase de 16 mesures",
        _ => "à chaque mesure",
    };

    private static string Symbol(Comparison comparison) => comparison switch
    {
        Comparison.AtMost => "≤",
        Comparison.EqualTo => "=",
        Comparison.NotEqualTo => "≠",
        _ => "≥",
    };

    private static string Wrap(ShowCondition condition, Func<Guid, string?>? names) =>
        condition.Kind is ConditionKind.All or ConditionKind.Any ? $"({Describe(condition, names)})" : Describe(condition, names);
}
