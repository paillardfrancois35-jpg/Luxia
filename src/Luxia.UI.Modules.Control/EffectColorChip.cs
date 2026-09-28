using CommunityToolkit.Mvvm.ComponentModel;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>Pastille d'une palette couleur, cochable, pour composer une alternance ou un dégradé (EFF-004).</summary>
public sealed partial class EffectColorChip : ViewModelBase
{
    private readonly EffectsPanelViewModel _owner;
    private bool _loading;

    [ObservableProperty]
    private bool _isChecked;

    /// <summary>Crée la pastille.</summary>
    public EffectColorChip(EffectsPanelViewModel owner, Guid paletteId, string name, string color)
    {
        _owner = owner;
        PaletteId = paletteId;
        Name = name;
        Color = color;
    }

    /// <summary>Palette couleur.</summary>
    public Guid PaletteId { get; }

    /// <summary>Nom de la palette.</summary>
    public string Name { get; }

    /// <summary>Couleur « #RRGGBB ».</summary>
    public string Color { get; }

    /// <summary>Coche sans enregistrer (relecture de l'effet).</summary>
    public void SetChecked(bool value)
    {
        _loading = true;
        IsChecked = value;
        _loading = false;
    }

    partial void OnIsCheckedChanged(bool value)
    {
        if (!_loading)
        {
            _owner.OnChipToggled();
        }
    }
}
