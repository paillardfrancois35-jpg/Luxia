using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dmx.Fixtures.Model;
using Dmx.UI.Controls;

namespace Dmx.UI.Modules.Library;

/// <summary>Ligne du tableau des plages d'un canal (BIB-022).</summary>
public sealed partial class CapabilityRowViewModel : ViewModelBase
{
    private readonly ChannelDetailViewModel _owner;
    private bool _loading;

    [ObservableProperty]
    private string _min = "0";

    [ObservableProperty]
    private string _max = "255";

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private Choice<CapabilityKind> _kind = Choices.CapabilityKinds[0];

    [ObservableProperty]
    private Choice<StrobeEffect?> _strobe = Choices.StrobeEffects[0];

    [ObservableProperty]
    private string _colors = string.Empty;

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Crée la ligne.</summary>
    public CapabilityRowViewModel(ChannelDetailViewModel owner, int index, Capability capability)
    {
        _owner = owner;
        Index = index;
        Load(capability);
    }

    /// <summary>Rang dans la liste triée.</summary>
    public int Index { get; }

    /// <summary>Plage actuelle.</summary>
    public Capability Capability { get; private set; } = null!;

    /// <summary>Paramètre progressif (lecture seule).</summary>
    public string ParameterText => Capability.Parameter is { } p
        ? string.Create(CultureInfo.CurrentCulture, $"{p.Nature} {p.Start} → {p.End}{(p.Unit is null ? string.Empty : " " + p.Unit)}")
        : string.Empty;

    /// <summary>Recharge depuis le modèle.</summary>
    public void Load(Capability capability)
    {
        ArgumentNullException.ThrowIfNull(capability);
        _loading = true;
        Capability = capability;
        Min = capability.Min.ToString(CultureInfo.InvariantCulture);
        Max = capability.Max.ToString(CultureInfo.InvariantCulture);
        Label = capability.Label;
        Kind = Choices.CapabilityKinds.First(k => k.Value == capability.Kind);
        Strobe = Choices.StrobeEffects.First(s => s.Value == capability.Strobe);
        Colors = string.Join(' ', capability.Colors);
        OnPropertyChanged(nameof(ParameterText));
        _loading = false;
    }

    partial void OnMinChanged(string value) => Commit("Borne basse", c => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? c with { Min = Math.Clamp(v, 0, 255) } : c);

    partial void OnMaxChanged(string value) => Commit("Borne haute", c => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? c with { Max = Math.Clamp(v, 0, 255) } : c);

    partial void OnLabelChanged(string value) => Commit("Libellé de plage", c => c with { Label = value.Trim() });

    partial void OnKindChanged(Choice<CapabilityKind> value) => Commit("Type de plage", c => c with { Kind = value.Value });

    partial void OnStrobeChanged(Choice<StrobeEffect?> value) => Commit("Effet de strobe", c => c with { Strobe = value.Value });

    partial void OnColorsChanged(string value) => Commit("Couleurs de plage", c => c with
    {
        Colors = [.. value.Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries).Select(x => x.StartsWith('#') ? x.ToUpperInvariant() : "#" + x.ToUpperInvariant())],
    });

    [RelayCommand]
    private void Remove() => _owner.RemoveRange(Index);

    private void Commit(string description, Func<Capability, Capability> change)
    {
        if (!_loading)
        {
            _owner.ReplaceRange(Index, change(Capability), description);
        }
    }
}
