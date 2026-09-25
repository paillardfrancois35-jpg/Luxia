using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Dmx.Fixtures.Model;
using Dmx.UI.Controls;

namespace Dmx.UI.Modules.Console;

/// <summary>Un canal d'un appareil dans le composant « faders d'un appareil » (CONS-060).</summary>
public sealed partial class FixtureChannelViewModel : ViewModelBase
{
    private readonly PhysicalInfo? _physical;

    [ObservableProperty]
    private int _value;

    [ObservableProperty]
    private bool _isOverridden;

    [ObservableProperty]
    private string _currentRange = string.Empty;

    [ObservableProperty]
    private bool _isDiscovering;

    /// <summary>Crée la tranche.</summary>
    public FixtureChannelViewModel(int position, int absoluteChannel, ChannelDefinition definition, ChannelPart part, PhysicalInfo? physical = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _physical = physical;
        Position = position;
        AbsoluteChannel = absoluteChannel;
        Definition = definition;
        Part = part;
        Ranges = part == ChannelPart.Fine ? [] : [.. definition.Capabilities.OrderBy(c => c.Min).Select(c => new RangeButton(c))];
        Segments = [.. Ranges.Select(r => new RangeSegment(r.Capability.Min, r.Capability.Max, r.Capability.Colors.Count > 0 ? r.Capability.Colors[0] : null))];
        UpdateRange();
    }

    /// <summary>Position dans le mode (1 = adresse de l'appareil).</summary>
    public int Position { get; }

    /// <summary>Canal DMX réel (adresse + position - 1).</summary>
    public int AbsoluteChannel { get; }

    /// <summary>Définition du canal.</summary>
    public ChannelDefinition Definition { get; }

    /// <summary>Octet grossier ou fin.</summary>
    public ChannelPart Part { get; }

    /// <summary>En-tête « 3 · canal 113 ».</summary>
    public string Header => string.Create(CultureInfo.CurrentCulture, $"{Position} · canal {AbsoluteChannel}");

    /// <summary>Nom du canal.</summary>
    public string Title => Part == ChannelPart.Fine ? $"{Definition.Name} (fin)" : Definition.Name;

    /// <summary>Attribut en clair.</summary>
    public string AttributeLabel => AttributeCatalog.Label(Definition.Attribute);

    /// <summary>Plages sous forme de boutons.</summary>
    public IReadOnlyList<RangeButton> Ranges { get; }

    /// <summary>Plages pour la barre 0-255.</summary>
    public IReadOnlyList<RangeSegment> Segments { get; }

    /// <summary>Le canal a des plages.</summary>
    public bool HasRanges => Ranges.Count > 0;

    partial void OnValueChanged(int value) => UpdateRange();

    private void UpdateRange()
    {
        // GEN-021 : nom de plage, degrés (Pan / Tilt), % (intensités), sinon valeur brute.
        CurrentRange = Part == ChannelPart.Fine
            ? string.Create(CultureInfo.CurrentCulture, $"{Value} (octet fin)")
            : Dmx.Fixtures.Rules.DmxConversion.Describe(Definition, Value, _physical);
    }
}

