using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Fixtures.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Library;

/// <summary>Emplacement d'une roue : nom, couleurs, image.</summary>
public sealed partial class WheelSlotRowViewModel : ViewModelBase
{
    private readonly WheelViewModel _wheel;
    private bool _loading;
    private WheelSlot _slot = new(string.Empty, []);

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _colors = string.Empty;

    [ObservableProperty]
    private string? _image;

    [ObservableProperty]
    private string _swatch = "#00000000";

    /// <summary>Crée la ligne.</summary>
    public WheelSlotRowViewModel(WheelViewModel wheel, int index, WheelSlot slot)
    {
        _wheel = wheel;
        Index = index;
        Load(slot);
    }

    /// <summary>Rang dans la roue.</summary>
    public int Index { get; }

    /// <summary>Recharge depuis le modèle.</summary>
    public void Load(WheelSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        _loading = true;
        _slot = slot;
        Name = slot.Name;
        Colors = string.Join(' ', slot.Colors);
        Image = slot.Image;
        Swatch = slot.Colors.Count > 0 ? slot.Colors[0] : "#00000000";
        _loading = false;
    }

    partial void OnNameChanged(string value) => Commit(_slot with { Name = value.Trim() });

    partial void OnColorsChanged(string value) => Commit(_slot with
    {
        Colors = [.. value.Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries).Select(c => c.StartsWith('#') ? c.ToUpperInvariant() : "#" + c.ToUpperInvariant())],
    });

    partial void OnImageChanged(string? value) => Commit(_slot with { Image = string.IsNullOrWhiteSpace(value) ? null : value });

    [RelayCommand]
    private void Remove() => _wheel.RemoveSlot(Index);

    private void Commit(WheelSlot slot)
    {
        if (!_loading)
        {
            _wheel.ReplaceSlot(Index, slot);
        }
    }
}
