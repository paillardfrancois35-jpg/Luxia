using System.Collections.ObjectModel;
using Dmx.UI.Controls;

namespace Dmx.UI.Modules.Library;

/// <summary>Fabricant dans l'arborescence de la bibliothèque (BIB-020).</summary>
/// <param name="name">Nom du fabricant.</param>
public sealed class ManufacturerNode(string name) : ViewModelBase
{
    /// <summary>Nom.</summary>
    public string Name { get; } = name;

    /// <summary>Modèles.</summary>
    public ObservableCollection<LibraryItemViewModel> Items { get; } = [];
}
