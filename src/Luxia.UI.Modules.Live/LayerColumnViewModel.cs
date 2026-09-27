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
    private readonly EngineEcho _echo = new();
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
        if (!_echo.Accept(percent) || Math.Abs(percent - Master) < 0.5)
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
            _echo.Sent(value);
            _masterChanged(this, value);
        }
    }
}
