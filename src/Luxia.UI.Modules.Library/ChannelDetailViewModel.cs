using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Fixtures;
using Luxia.Fixtures.Model;
using Luxia.Fixtures.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Library;

/// <summary>
/// Détail d'une définition de canal (doc 12 §2.4) : attribut, cellule, résolution, valeurs, « Suit l'intensité »,
/// étiquettes de sûreté, roue, plages avec barre 0-255 (BIB-006, BIB-007, BIB-022, BIB-023).
/// </summary>
public sealed partial class ChannelDetailViewModel : ViewModelBase
{
    private readonly FixtureEditorViewModel _editor;
    private bool _loading;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private AttributeInfo _attribute = AttributeCatalog.All[0];

    [ObservableProperty]
    private decimal _cell;

    [ObservableProperty]
    private bool _is16Bit;

    [ObservableProperty]
    private decimal _default;

    [ObservableProperty]
    private string _rest = string.Empty;

    [ObservableProperty]
    private string _identify = string.Empty;

    [ObservableProperty]
    private bool _inverted;

    [ObservableProperty]
    private Choice<bool?> _followsIntensity = Choices.FollowsIntensity[0];

    [ObservableProperty]
    private string _followsIntensityHint = string.Empty;

    [ObservableProperty]
    private bool _safetyAuto = true;

    [ObservableProperty]
    private bool _safetyStrobe;

    [ObservableProperty]
    private bool _safetySmoke;

    [ObservableProperty]
    private bool _safetyMovement;

    [ObservableProperty]
    private Choice<string?> _wheel = new(null, "—");

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private IReadOnlyList<RangeSegment> _segments = [];

    [ObservableProperty]
    private int _selectedRangeIndex = -1;

    [ObservableProperty]
    private decimal _splitCount = 8;

    /// <summary>Crée le détail.</summary>
    public ChannelDetailViewModel(FixtureEditorViewModel editor, string key)
    {
        _editor = editor;
        Key = key;
    }

    /// <summary>Clé de la définition.</summary>
    public string Key { get; }

    /// <summary>Catalogue des attributs (liste déroulante).</summary>
    public static IReadOnlyList<AttributeInfo> Attributes => AttributeCatalog.All;

    /// <summary>Choix « Suit l'intensité ».</summary>
    public static IReadOnlyList<Choice<bool?>> FollowsChoices => Choices.FollowsIntensity;

    /// <summary>Roues disponibles.</summary>
    public ObservableCollection<Choice<string?>> Wheels { get; } = [];

    /// <summary>Plages.</summary>
    public ObservableCollection<CapabilityRowViewModel> Ranges { get; } = [];

    /// <summary>Recharge depuis le modèle.</summary>
    public void Load(FixtureType fixture, FixtureMode? mode)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        var channel = fixture.Channel(Key);
        if (channel is null)
        {
            return;
        }

        _loading = true;
        Name = channel.Name;
        Attribute = AttributeCatalog.Get(channel.Attribute);
        Cell = channel.Cell;
        Is16Bit = channel.Resolution == ChannelResolution.Bit16;
        Default = channel.Default;
        Rest = channel.Rest?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        Identify = channel.Identify?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        Inverted = channel.Inverted;
        FollowsIntensity = Choices.FollowsIntensity.First(c => c.Value == channel.FollowsIntensity);
        FollowsIntensityHint = mode is null
            ? string.Empty
            : $"déduit pour « {mode.Name} » : {(FixtureRules.DeducedFollowsIntensity(fixture, mode, channel) ? "oui" : "non")}{(FixtureRules.HasVirtualIntensity(fixture, mode) ? " (intensité virtuelle)" : string.Empty)}";

        var safety = FixtureRules.Safety(channel);
        SafetyAuto = channel.Safety is null;
        SafetyStrobe = safety.HasFlag(SafetyTags.Strobe);
        SafetySmoke = safety.HasFlag(SafetyTags.Smoke);
        SafetyMovement = safety.HasFlag(SafetyTags.Movement);

        Wheels.Clear();
        Wheels.Add(new Choice<string?>(null, "—"));
        foreach (var w in fixture.Wheels)
        {
            Wheels.Add(new Choice<string?>(w.Key, w.Name));
        }

        Wheel = Wheels.FirstOrDefault(w => w.Value == channel.Wheel) ?? Wheels[0];
        Notes = channel.Notes;

        var sorted = channel.Capabilities.OrderBy(c => c.Min).ToList();
        if (Ranges.Count == sorted.Count)
        {
            for (var i = 0; i < sorted.Count; i++)
            {
                Ranges[i].Load(sorted[i]);
            }
        }
        else
        {
            Ranges.Clear();
            for (var i = 0; i < sorted.Count; i++)
            {
                Ranges.Add(new CapabilityRowViewModel(this, i, sorted[i]));
            }
        }

        Segments = [.. sorted.Select(c => new RangeSegment(c.Min, c.Max, c.Colors.Count > 0 ? c.Colors[0] : null))];
        SelectedRangeIndex = Math.Min(SelectedRangeIndex, sorted.Count - 1);
        _loading = false;
    }

    /// <summary>Remplace une plage (ligne du tableau).</summary>
    public void ReplaceRange(int index, Capability capability, string description) =>
        EditRanges(description, list => list[index] = capability);

    /// <summary>Supprime une plage.</summary>
    public void RemoveRange(int index) => EditRanges("Supprimer une plage", list => list.RemoveAt(index));

    /// <summary>Déplace la frontière entre deux plages (barre 0-255, BIB-022).</summary>
    public void MoveBoundary(int index, int newMax) =>
        Apply("Déplacer une frontière", f => FixtureEdits.SetCapabilities(f, Key, CapabilityTools.MoveBoundary(Current(f), index, newMax)));

    /// <summary>Crée une borne à une valeur (découverte, BIB-062).</summary>
    public void SplitAt(int value) =>
        Apply("Nouvelle plage ici", f => FixtureEdits.SetCapabilities(f, Key, CapabilityTools.SplitAt(Current(f), value)));

    partial void OnNameChanged(string value) => Edit("Nom du canal", c => c with { Name = value.Trim() });

    partial void OnAttributeChanged(AttributeInfo value) => Edit("Attribut", c => c with { Attribute = value.Attribute });

    partial void OnCellChanged(decimal value) => Edit("Cellule", c => c with { Cell = (int)value });

    partial void OnIs16BitChanged(bool value)
    {
        if (!_loading)
        {
            Apply("Résolution", f => FixtureEdits.SetResolution(f, Key, value ? ChannelResolution.Bit16 : ChannelResolution.Bit8));
        }
    }

    partial void OnDefaultChanged(decimal value) => Edit("Valeur par défaut", c => c with { Default = (int)Math.Clamp(value, 0, 255) });

    partial void OnRestChanged(string value) => Edit("Valeur de repos", c => c with { Rest = ParseOptional(value) });

    partial void OnIdentifyChanged(string value) => Edit("Valeur d'identification", c => c with { Identify = ParseOptional(value) });

    partial void OnInvertedChanged(bool value) => Edit("Inversion", c => c with { Inverted = value });

    partial void OnFollowsIntensityChanged(Choice<bool?> value) => Edit("Suit l'intensité", c => c with { FollowsIntensity = value.Value });

    partial void OnSafetyAutoChanged(bool value) => EditSafety();

    partial void OnSafetyStrobeChanged(bool value) => EditSafety();

    partial void OnSafetySmokeChanged(bool value) => EditSafety();

    partial void OnSafetyMovementChanged(bool value) => EditSafety();

    partial void OnWheelChanged(Choice<string?> value) => Edit("Roue", c => c with { Wheel = value?.Value });

    partial void OnNotesChanged(string? value) => Edit("Remarques", c => c with { Notes = string.IsNullOrWhiteSpace(value) ? null : value });

    [RelayCommand]
    private void AddRange() =>
        Apply("Ajouter une plage", f => FixtureEdits.SetCapabilities(f, Key, CapabilityTools.FillNextGap(Current(f))));

    [RelayCommand]
    private void SplitRanges()
    {
        var count = (int)Math.Clamp(SplitCount, 1, 256);
        Apply(string.Create(CultureInfo.CurrentCulture, $"Découper en {count} plages"), f => FixtureEdits.SetCapabilities(f, Key, CapabilityTools.Split(count)));
    }

    [RelayCommand]
    private void ClearRanges() => Apply("Effacer les plages", f => FixtureEdits.SetCapabilities(f, Key, []));

    private List<Capability> Current(FixtureType fixture) =>
        fixture.Channel(Key)?.Capabilities.OrderBy(c => c.Min).ToList() ?? [];

    private void EditRanges(string description, Action<List<Capability>> change) =>
        Apply(description, f =>
        {
            var list = Current(f).ToList();
            change(list);
            return FixtureEdits.SetCapabilities(f, Key, list);
        });

    private void EditSafety()
    {
        if (_loading)
        {
            return;
        }

        var tags = (SafetyStrobe ? SafetyTags.Strobe : SafetyTags.None)
            | (SafetySmoke ? SafetyTags.Smoke : SafetyTags.None)
            | (SafetyMovement ? SafetyTags.Movement : SafetyTags.None);
        Edit("Étiquettes de sûreté", c => c with { Safety = SafetyAuto ? null : tags });
    }

    private void Edit(string description, Func<ChannelDefinition, ChannelDefinition> change)
    {
        if (!_loading)
        {
            Apply(description, f => FixtureEdits.UpdateChannel(f, Key, change));
        }
    }

    private void Apply(string description, Func<FixtureType, FixtureType> change) => _editor.Apply(description, change);

    private static int? ParseOptional(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? Math.Clamp(v, 0, 255) : null;
}
