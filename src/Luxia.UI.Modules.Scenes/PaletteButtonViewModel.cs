using CommunityToolkit.Mvvm.Input;
using Luxia.Scenes.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Scenes;

/// <summary>
/// Bouton d'une grille de palettes (PAL-007) : nom, couleur ; clic = appliquer à la sélection du programmeur ;
/// menu = mettre à jour depuis le programmeur (PAL-005), renommer, recolorer, déplacer, supprimer (PAL-006).
/// </summary>
public sealed partial class PaletteButtonViewModel : ViewModelBase
{
    private readonly PalettesViewModel _owner;

    /// <summary>Crée le bouton.</summary>
    public PaletteButtonViewModel(PalettesViewModel owner, Palette palette)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(palette);
        _owner = owner;
        Palette = palette;
    }

    /// <summary>Palette.</summary>
    public Palette Palette { get; }

    /// <summary>Nom affiché, avec l'icône éventuelle (GEN-106).</summary>
    public string Title => string.IsNullOrWhiteSpace(Palette.Icon) ? Palette.Name : $"{Palette.Icon} {Palette.Name}";

    /// <summary>Couleur du bouton.</summary>
    public string Color => Palette.DisplayColor();

    [RelayCommand]
    private void Apply() => _owner.Apply(this);

    [RelayCommand]
    private Task UpdateFromProgrammerAsync() => _owner.UpdateFromProgrammerAsync(this);

    [RelayCommand]
    private Task RenameAsync() => _owner.RenameAsync(this);

    [RelayCommand]
    private Task RecolorAsync() => _owner.RecolorAsync(this);

    [RelayCommand]
    private void MoveLeft() => _owner.Move(this, -1);

    [RelayCommand]
    private void MoveRight() => _owner.Move(this, +1);

    [RelayCommand]
    private Task DeleteAsync() => _owner.DeleteAsync(this);
}
