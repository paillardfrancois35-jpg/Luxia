using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Dmx.Fixtures;
using Dmx.Fixtures.Model;
using Dmx.Fixtures.Rules;
using Dmx.UI.Controls;

namespace Dmx.UI.Modules.Library;

/// <summary>Onglet de mode : nom, nom court, réglage sur l'appareil, nombre de canaux (BIB-005, BIB-021).</summary>
public sealed partial class ModeItemViewModel : ViewModelBase
{
    private readonly FixtureEditorViewModel _editor;
    private bool _loading;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _shortName;

    [ObservableProperty]
    private string? _deviceSetting;

    [ObservableProperty]
    private string _header = string.Empty;

    [ObservableProperty]
    private string _sheet = string.Empty;

    /// <summary>Crée l'onglet.</summary>
    public ModeItemViewModel(FixtureEditorViewModel editor, int index)
    {
        _editor = editor;
        Index = index;
    }

    /// <summary>Rang du mode dans le modèle.</summary>
    public int Index { get; }

    /// <summary>Recharge depuis le modèle.</summary>
    public void Load(FixtureType fixture, FixtureMode mode)
    {
        ArgumentNullException.ThrowIfNull(mode);
        _loading = true;
        Name = mode.Name;
        ShortName = mode.ShortName;
        DeviceSetting = mode.DeviceSetting;
        Header = string.Create(CultureInfo.CurrentCulture, $"{mode.Name} ({mode.ChannelCount})");
        Sheet = FixtureRules.SettingSheet(fixture, mode);
        _loading = false;
    }

    partial void OnNameChanged(string value) => Edit("Renommer le mode", m => m with { Name = value.Trim() });

    partial void OnShortNameChanged(string? value) => Edit("Nom court du mode", m => m with { ShortName = Blank(value) });

    partial void OnDeviceSettingChanged(string? value) => Edit("Réglage sur l'appareil", m => m with { DeviceSetting = Blank(value) });

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void Edit(string description, Func<FixtureMode, FixtureMode> change)
    {
        if (!_loading)
        {
            _editor.Apply(description, f => FixtureEdits.UpdateMode(f, Index, change));
        }
    }
}
