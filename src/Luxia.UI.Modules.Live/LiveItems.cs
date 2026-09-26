using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.Engine.Model;
using Luxia.Scenes.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Live;

/// <summary>Une colonne de couche en Live (LIVE-002) : ses scènes visibles, son arrêt et son master.</summary>
public sealed partial class LayerColumnViewModel : ViewModelBase
{
    private readonly Action<LayerColumnViewModel, double> _masterChanged;
    private bool _syncing;

    [ObservableProperty]
    private double _master = 100;

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Crée la colonne.</summary>
    public LayerColumnViewModel(Layer layer, Action<LayerColumnViewModel, double> masterChanged)
    {
        ArgumentNullException.ThrowIfNull(layer);
        Layer = layer;
        _masterChanged = masterChanged;
        _master = Math.Round(layer.Master * 100);
    }

    /// <summary>Couche.</summary>
    public Layer Layer { get; }

    /// <summary>Titre (icône et nom).</summary>
    public string Title => string.IsNullOrEmpty(Layer.Icon) ? Layer.Name : $"{Layer.Icon} {Layer.Name}";

    /// <summary>Couleur de la couche.</summary>
    public string Color => Layer.Color;

    /// <summary>Couche Flash : ses scènes ne jouent que tant qu'on les maintient (COU-005).</summary>
    public bool IsFlash => Layer.Kind == LayerKind.Flash;

    /// <summary>Scènes visibles en Live, dans l'ordre de la couche.</summary>
    public ObservableCollection<LiveSceneViewModel> Scenes { get; } = [];

    /// <summary>Met à jour le master depuis le moteur (sans renvoyer de commande).</summary>
    public void SyncMaster(double percent)
    {
        if (Math.Abs(percent - Master) < 0.5)
        {
            return;
        }

        _syncing = true;
        Master = percent;
        _syncing = false;
    }

    partial void OnMasterChanged(double value)
    {
        if (!_syncing)
        {
            _masterChanged(this, value);
        }
    }
}

/// <summary>Un bouton de scène en Live : couleur de la scène, active ou non, progression (LIVE-002).</summary>
public sealed partial class LiveSceneViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _state = string.Empty;

    /// <summary>Crée le bouton.</summary>
    public LiveSceneViewModel(Scene scene, LayerColumnViewModel column, int index)
    {
        ArgumentNullException.ThrowIfNull(scene);
        Scene = scene;
        Column = column;
        Index = index;
    }

    /// <summary>Scène.</summary>
    public Scene Scene { get; }

    /// <summary>Colonne (couche).</summary>
    public LayerColumnViewModel Column { get; }

    /// <summary>Rang dans la colonne (touches 1 à 9, LIVE-040).</summary>
    public int Index { get; }

    /// <summary>Nom.</summary>
    public string Name => Scene.Name;

    /// <summary>Couleur de la scène (GEN-106).</summary>
    public string Color => Scene.Color;

    /// <summary>Raccourci clavier affiché (1 à 9).</summary>
    public string Key => Index <= 9 ? Index.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
}

/// <summary>Sélection proposée pour les palettes rapides (LIVE-005).</summary>
public sealed partial class QuickSelectionViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Crée l'élément.</summary>
    public QuickSelectionViewModel(string label, ValueTarget target, string color)
    {
        Label = label;
        Target = target;
        Color = color;
    }

    /// <summary>Libellé.</summary>
    public string Label { get; }

    /// <summary>Cible des palettes.</summary>
    public ValueTarget Target { get; }

    /// <summary>Couleur d'affichage.</summary>
    public string Color { get; }
}

/// <summary>Palette rapide en Live (LIVE-005).</summary>
/// <param name="Palette">Palette.</param>
public sealed record QuickPaletteViewModel(Palette Palette)
{
    /// <summary>Nom.</summary>
    public string Name => Palette.Name;

    /// <summary>Couleur du bouton.</summary>
    public string Color => Palette.DisplayColor();
}
