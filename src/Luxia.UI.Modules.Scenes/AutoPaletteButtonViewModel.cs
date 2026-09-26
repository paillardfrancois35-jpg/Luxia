using CommunityToolkit.Mvvm.Input;
using Luxia.Fixtures.Model;
using Luxia.Scenes.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Scenes;

/// <summary>Bouton de palette automatique (PAL-003) : une plage de la bibliothèque, disponible sans rien créer.</summary>
public sealed partial class AutoPaletteButtonViewModel : ViewModelBase
{
    private readonly ProgrammerViewModel _programmer;

    /// <summary>Crée le bouton.</summary>
    public AutoPaletteButtonViewModel(ProgrammerViewModel programmer, AutoPalette palette)
    {
        ArgumentNullException.ThrowIfNull(programmer);
        ArgumentNullException.ThrowIfNull(palette);
        _programmer = programmer;
        Palette = palette;
    }

    /// <summary>Palette automatique.</summary>
    public AutoPalette Palette { get; }

    /// <summary>Libellé « Attribut : plage ».</summary>
    public string Title => $"{AttributeCatalog.Label(Palette.Attribute)} : {Palette.Label}";

    /// <summary>Couleur de l'emplacement, sinon gris.</summary>
    public string Color => Palette.Color ?? "#30363D";

    [RelayCommand]
    private void Apply() => _programmer.ApplyAutoPalette(Palette);
}
