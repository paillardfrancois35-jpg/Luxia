using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.Engine.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Scenes;

/// <summary>
/// Saisie d'une durée : une quantité et son unité (secondes, temps, mesures ; GEN-023). Facultative pour un fondu
/// de scène (vide = valeur par défaut).
/// </summary>
public sealed partial class DurationField : ViewModelBase
{
    private bool _loading;

    [ObservableProperty]
    private decimal? _amount;

    [ObservableProperty]
    private Choice<DurationUnit> _unit = SceneOptions.Units[0];

    /// <summary>Levé quand l'utilisateur modifie la durée.</summary>
    public event EventHandler? Edited;

    /// <summary>Unités proposées.</summary>
    public static IReadOnlyList<Choice<DurationUnit>> Units => SceneOptions.Units;

    /// <summary>Durée saisie ; <c>null</c> si la case est vide.</summary>
    public Duration? Value => Amount is { } amount ? new Duration((double)Math.Max(0, amount), Unit.Value) : null;

    /// <summary>
    /// Fondu et maintien d'une étape écrits dans leur unité, pour la bande d'étapes : « 0,5 + 2 s », « 0 + 2 temps »,
    /// « 0,5 s + 1 mesure » (E2).
    /// </summary>
    public static string Describe(Duration fade, Duration hold) => fade.Unit == hold.Unit
        ? string.Create(CultureInfo.CurrentCulture, $"{fade.Value:0.##} + {Describe(hold)}")
        : $"{Describe(fade)} + {Describe(hold)}";

    /// <summary>Une durée écrite dans son unité : « 0,5 s », « 2 temps », « 1 mesure », « 4 mesures ».</summary>
    public static string Describe(Duration duration) => duration.Unit switch
    {
        DurationUnit.Beats => string.Create(CultureInfo.CurrentCulture, $"{duration.Value:0.##} temps"),
        DurationUnit.Bars => string.Create(CultureInfo.CurrentCulture, $"{duration.Value:0.##} mesure{(duration.Value > 1 ? "s" : string.Empty)}"),
        _ => string.Create(CultureInfo.CurrentCulture, $"{duration.Value:0.##} s"),
    };

    /// <summary>Affiche une durée sans lever <see cref="Edited"/>.</summary>
    public void Load(Duration? duration)
    {
        _loading = true;
        Amount = duration is { } d ? (decimal)Math.Round(d.Value, 3) : null;
        Unit = SceneOptions.Units.First(u => u.Value == (duration?.Unit ?? DurationUnit.Seconds));
        _loading = false;
    }

    partial void OnAmountChanged(decimal? value) => RaiseEdited();

    partial void OnUnitChanged(Choice<DurationUnit> value) => RaiseEdited();

    private void RaiseEdited()
    {
        if (!_loading)
        {
            Edited?.Invoke(this, EventArgs.Empty);
        }
    }
}
