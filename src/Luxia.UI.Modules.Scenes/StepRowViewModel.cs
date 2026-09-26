using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.Engine.Model;
using Luxia.Scenes.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Scenes;

/// <summary>Une étape dans le bandeau des étapes (SCN-002) : numéro, durées, nombre de valeurs, case de sélection groupée (SCN-004).</summary>
public sealed partial class StepRowViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isChecked;

    [ObservableProperty]
    private bool _isCurrent;

    [ObservableProperty]
    private bool _isPlaying;

    /// <summary>Crée la ligne.</summary>
    public StepRowViewModel(int index, SceneStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        Index = index;
        Step = step;
    }

    /// <summary>Rang (0 = première).</summary>
    public int Index { get; }

    /// <summary>Étape.</summary>
    public SceneStep Step { get; }

    /// <summary>Titre « 1 · Nom ».</summary>
    public string Title => string.IsNullOrWhiteSpace(Step.Name)
        ? (Index + 1).ToString(CultureInfo.CurrentCulture)
        : string.Create(CultureInfo.CurrentCulture, $"{Index + 1} · {Step.Name}");

    /// <summary>Durées « fondu / maintien ».</summary>
    public string Timing => $"{Format(Step.Fade)} / {Format(Step.Hold)}";

    /// <summary>Nombre de valeurs.</summary>
    public string ValueCount => string.Create(CultureInfo.CurrentCulture, $"{Step.Values.Count} valeur(s)");

    /// <summary>Durée lisible (« 1,5 s », « 2 t », « 1 m »).</summary>
    public static string Format(Duration duration)
    {
        var unit = duration.Unit switch
        {
            DurationUnit.Beats => "t",
            DurationUnit.Bars => "m",
            _ => "s",
        };
        return string.Create(CultureInfo.CurrentCulture, $"{Math.Round(duration.Value, 3)} {unit}");
    }
}
