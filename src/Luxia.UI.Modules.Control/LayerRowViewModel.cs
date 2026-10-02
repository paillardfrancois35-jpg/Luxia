using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.Engine.Model;
using Luxia.Scenes.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>Une couche dans l'éditeur de couches (doc 17 §1.2, COU-001).</summary>
public sealed partial class LayerRowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _color;

    [ObservableProperty]
    private bool _exclusive;

    [ObservableProperty]
    private Choice<LayerKind> _kind;

    [ObservableProperty]
    private Choice<IntensityMode> _intensity;

    [ObservableProperty]
    private decimal _crossFadeSeconds;

    [ObservableProperty]
    private decimal _masterPercent;

    [ObservableProperty]
    private bool _keepOnStopAll;

    [ObservableProperty]
    private Choice<Guid?> _restScene;

    [ObservableProperty]
    private int _sceneCount;

    /// <summary>Crée la ligne depuis une couche.</summary>
    public LayerRowViewModel(Layer layer, IReadOnlyList<Choice<Guid?>> restChoices, int sceneCount)
    {
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(restChoices);
        Source = layer;
        _name = layer.Name;
        _color = layer.Color;
        _exclusive = layer.Exclusive;
        _kind = KindOptions.First(k => k.Value == layer.Kind);
        _intensity = IntensityOptions.First(k => k.Value == layer.IntensityMode);
        _crossFadeSeconds = (decimal)Math.Round(layer.CrossFade.Unit == DurationUnit.Seconds ? layer.CrossFade.Value : layer.CrossFade.ToSeconds(120), 2);
        _masterPercent = (decimal)Math.Round(layer.Master * 100);
        _keepOnStopAll = layer.KeepOnStopAll;
        RestChoices = [.. restChoices];
        _restScene = RestChoices.FirstOrDefault(c => c.Value == layer.RestSceneId) ?? RestChoices[0];
        _sceneCount = sceneCount;
    }

    /// <summary>Types de couche.</summary>
    public static IReadOnlyList<Choice<LayerKind>> KindOptions { get; } =
    [
        new(LayerKind.Normal, "Normale"),
        new(LayerKind.Flash, "Flash (maintien)"),
    ];

    /// <summary>Modes d'intensité (doc 15 §5.2).</summary>
    public static IReadOnlyList<Choice<IntensityMode>> IntensityOptions { get; } =
    [
        new(IntensityMode.Htp, "HTP (le plus fort)"),
        new(IntensityMode.Priority, "Prioritaire"),
        new(IntensityMode.Additive, "Additif"),
        new(IntensityMode.Multiplicative, "Multiplicatif"),
    ];

    /// <summary>Couche d'origine (identifiant, familles, icône conservés).</summary>
    public Layer Source { get; }

    /// <summary>Scènes de repos possibles (scènes de la couche, ou aucune).</summary>
    public ObservableCollection<Choice<Guid?>> RestChoices { get; }

    /// <summary>Libellé du nombre de scènes.</summary>
    public string ScenesLabel => SceneCount == 0 ? "aucune scène" : SceneCount == 1 ? "1 scène" : $"{SceneCount} scènes";

    /// <summary>Couche modifiée, à la priorité donnée.</summary>
    public Layer ToLayer(int priority) => Source with
    {
        Name = string.IsNullOrWhiteSpace(Name) ? Source.Name : Name.Trim(),
        Color = Color,
        Priority = priority,
        Exclusive = Exclusive,
        Kind = Kind.Value,
        IntensityMode = Intensity.Value,
        CrossFade = Duration.FromSeconds(Math.Clamp((double)CrossFadeSeconds, 0, 600)),
        Master = Math.Clamp((double)MasterPercent / 100, 0, 1),
        KeepOnStopAll = KeepOnStopAll,
        RestSceneId = RestScene.Value,
    };

    partial void OnSceneCountChanged(int value) => OnPropertyChanged(nameof(ScenesLabel));
}
