using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.Engine.Model;
using Luxia.Scenes.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>Colonne de couche (doc 60 §5) : en-tête, ◀ ▶ ■, master, scènes.</summary>
public sealed partial class ControlColumnViewModel : ViewModelBase
{
    private readonly Action<ControlColumnViewModel, double> _masterChanged;
    private readonly EngineEcho _echo = new();
    private bool _syncing;

    [ObservableProperty]
    private double _master = 100;

    [ObservableProperty]
    private bool _isPlaying;

    /// <summary>Vrai quand la scène qui joue a plusieurs étapes : ◀ ▶ ont alors un effet.</summary>
    [ObservableProperty]
    private bool _canStep;

    /// <summary>Position de défilement vertical de la colonne : gardée quand la liste des scènes est recréée (édition validée).</summary>
    public double ScrollOffset { get; set; }

    /// <summary>Crée la colonne.</summary>
    public ControlColumnViewModel(Layer layer, Action<ControlColumnViewModel, double> masterChanged)
    {
        ArgumentNullException.ThrowIfNull(layer);
        Layer = layer;
        _masterChanged = masterChanged;
        _master = Math.Round(layer.Master * 100);
    }

    /// <summary>Couche.</summary>
    public Layer Layer { get; }

    /// <summary>Titre (icône et nom).</summary>
    public string Title => string.IsNullOrEmpty(Layer.Icon) ? Layer.Name : $"{Layer.Icon}  {Layer.Name}";

    /// <summary>Couleur de la couche.</summary>
    public string Color => Layer.Color;

    /// <summary>Couche Flash : ses scènes ne jouent que tant qu'on les maintient (COU-005).</summary>
    public bool IsFlash => Layer.Kind == LayerKind.Flash;

    /// <summary>Aide de l'en-tête (F5 : chaque mot s'explique).</summary>
    public string Help => IsFlash
        ? $"Couche « {Layer.Name} » : ses scènes jouent tant qu'on les maintient (clic maintenu, touche, pad)."
        : $"Couche « {Layer.Name} » : une scène à la fois ; en lancer une autre fait un fondu croisé. Le master règle son intensité.";

    /// <summary>Scènes de la couche.</summary>
    public ObservableCollection<ControlSceneViewModel> Scenes { get; } = [];

    /// <summary>La couche n'a aucune scène.</summary>
    public bool IsEmpty => Scenes.Count == 0;

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
