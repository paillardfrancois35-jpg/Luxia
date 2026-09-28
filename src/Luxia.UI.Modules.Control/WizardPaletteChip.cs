using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>Palette cochable d'un assistant de création (SCN-014) : une couleur ou une position, dans l'ordre de la liste.</summary>
public sealed partial class WizardPaletteChip : ViewModelBase
{
    [ObservableProperty]
    private bool _isChecked;

    /// <summary>Crée la pastille.</summary>
    public WizardPaletteChip(Guid paletteId, string name, string color)
    {
        PaletteId = paletteId;
        Name = name;
        Color = color;
    }

    /// <summary>Palette.</summary>
    public Guid PaletteId { get; }

    /// <summary>Nom.</summary>
    public string Name { get; }

    /// <summary>Couleur du bouton « #RRGGBB ».</summary>
    public string Color { get; }
}
