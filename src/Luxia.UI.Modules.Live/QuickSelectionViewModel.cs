using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.Scenes.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Live;

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
