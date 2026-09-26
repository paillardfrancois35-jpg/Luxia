using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Dmx.UI.Controls;

namespace Dmx.UI.Modules.Library;

/// <summary>Fabricant dans l'arborescence de la bibliothèque (BIB-020).</summary>
/// <param name="name">Nom du fabricant.</param>
public sealed partial class ManufacturerNode(string name) : ViewModelBase
{
    /// <summary>Nom.</summary>
    public string Name { get; } = name;

    /// <summary>Modèles.</summary>
    public ObservableCollection<LibraryItemViewModel> Items { get; } = [];

    /// <summary>Déplié : automatiquement quand une recherche ou un filtre est actif (BIB-093).</summary>
    [ObservableProperty]
    private bool _isExpanded;
}
