using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dmx.Fixtures;
using Dmx.Fixtures.Model;
using Dmx.Fixtures.Rules;
using Dmx.Persistence.Json;
using Dmx.UI.Controls;

namespace Dmx.UI.Modules.Library;

/// <summary>
/// Éditeur d'un modèle d'appareil (doc 12 §4). Le modèle immuable est la seule source de vérité : chaque
/// modification passe par <c>Apply</c>, qui enregistre l'état précédent (annuler / rétablir, BIB-024)
/// puis met à jour les modèles de vue sans les recréer (le champ en cours de saisie garde le focus).
/// </summary>
public sealed partial class FixtureEditorViewModel : ViewModelBase
{
    private readonly UndoHistory<FixtureType> _history = new();
    private string _savedJson;
    private bool _loading;

    [ObservableProperty]
    private string _manufacturer = string.Empty;

    [ObservableProperty]
    private string _model = string.Empty;

    [ObservableProperty]
    private string? _reference;

    [ObservableProperty]
    private Choice<FixtureCategory> _category = Choices.Categories[0];

    [ObservableProperty]
    private string? _author;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private string? _sourceType;

    [ObservableProperty]
    private string _beamAngle = string.Empty;

    [ObservableProperty]
    private string _panRange = string.Empty;

    [ObservableProperty]
    private string _tiltRange = string.Empty;

    [ObservableProperty]
    private string _power = string.Empty;

    [ObservableProperty]
    private string? _manual;

    [ObservableProperty]
    private ModeItemViewModel? _selectedMode;

    [ObservableProperty]
    private SlotViewModel? _selectedSlot;

    [ObservableProperty]
    private ChannelDetailViewModel? _selectedChannel;

    [ObservableProperty]
    private string? _channelToAdd;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _hasErrors;

    [ObservableProperty]
    private string _validationSummary = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _info = string.Empty;

    /// <summary>Ouvre un modèle dans l'éditeur.</summary>
    public FixtureEditorViewModel(FixtureType fixture, bool isReadOnly)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        Current = fixture;
        IsReadOnly = isReadOnly;
        _savedJson = Json(fixture);
        Sync();
        SelectedMode = Modes.FirstOrDefault();
    }

    /// <summary>Modèle en cours d'édition.</summary>
    public FixtureType Current { get; private set; }

    /// <summary>Générique livré : lecture seule (le dupliquer pour le modifier).</summary>
    public bool IsReadOnly { get; }

    /// <summary>Modifiable.</summary>
    public bool IsEditable => !IsReadOnly;

    /// <summary>Catégories.</summary>
    public IReadOnlyList<Choice<FixtureCategory>> Categories => Choices.Categories;

    /// <summary>Onglets de modes.</summary>
    public ObservableCollection<ModeItemViewModel> Modes { get; } = [];

    /// <summary>Positions du mode sélectionné.</summary>
    public ObservableCollection<SlotViewModel> Slots { get; } = [];

    /// <summary>Définitions disponibles pour l'ajout dans un mode (clé → libellé).</summary>
    public ObservableCollection<string> ChannelKeys { get; } = [];

    /// <summary>Roues.</summary>
    public ObservableCollection<WheelViewModel> Wheels { get; } = [];

    /// <summary>Problèmes de validation (BIB-004), mis à jour à chaque modification.</summary>
    public ObservableCollection<ValidationIssue> Issues { get; } = [];

    /// <summary>Levé après chaque modification (le test en direct se met à jour).</summary>
    public event EventHandler? Changed;

    /// <summary>Applique une modification (annulable).</summary>
    public void Apply(string description, Func<FixtureType, FixtureType> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        if (_loading || IsReadOnly)
        {
            return;
        }

        var updated = change(Current);
        if (Json(updated) == Json(Current))
        {
            return;
        }

        _history.Record(Current, description);
        Current = updated;
        Sync();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Marque l'état courant comme enregistré (après enregistrement dans la bibliothèque).</summary>
    public void MarkSaved(FixtureType saved)
    {
        ArgumentNullException.ThrowIfNull(saved);
        Current = saved;
        _savedJson = Json(saved);
        Sync();
    }

    /// <summary>Découverte : nouvelle borne dans les plages d'un canal (BIB-062).</summary>
    public void SplitChannelAt(string channelKey, int value) =>
        Apply("Nouvelle plage ici", f => FixtureEdits.SetCapabilities(f, channelKey, CapabilityTools.SplitAt(f.Channel(channelKey)?.Capabilities.OrderBy(c => c.Min).ToList() ?? [], value)));

    /// <summary>Glisser-déposer d'une position (BIB-021).</summary>
    public void MoveSlot(int from, int to)
    {
        if (SelectedMode is { } mode)
        {
            Apply("Déplacer un canal", f => FixtureEdits.MoveSlot(f, mode.Index, from, to));
            SelectedSlot = Slots.ElementAtOrDefault(to);
        }
    }

    partial void OnManufacturerChanged(string value) => Apply("Fabricant", f => f with { Manufacturer = value.Trim() }, loadingGuard: true);

    partial void OnModelChanged(string value) => Apply("Modèle", f => f with { Model = value.Trim() }, loadingGuard: true);

    partial void OnReferenceChanged(string? value) => Apply("Référence", f => f with { Reference = Blank(value) }, loadingGuard: true);

    partial void OnCategoryChanged(Choice<FixtureCategory> value) => Apply("Catégorie", f => f with { Category = value.Value }, loadingGuard: true);

    partial void OnAuthorChanged(string? value) => Apply("Auteur", f => f with { Author = Blank(value) }, loadingGuard: true);

    partial void OnNotesChanged(string? value) => Apply("Remarques", f => f with { Notes = Blank(value) }, loadingGuard: true);

    partial void OnManualChanged(string? value) => Apply("Notice", f => f with { Manual = Blank(value) }, loadingGuard: true);

    partial void OnSourceTypeChanged(string? value) => Apply("Type de source", f => f with { Physical = f.Physical with { SourceType = Blank(value) } }, loadingGuard: true);

    partial void OnBeamAngleChanged(string value) => Apply("Angle du faisceau", f => f with { Physical = f.Physical with { BeamAngle = Number(value) } }, loadingGuard: true);

    partial void OnPanRangeChanged(string value) => Apply("Amplitude Pan", f => f with { Physical = f.Physical with { PanRange = Number(value) } }, loadingGuard: true);

    partial void OnTiltRangeChanged(string value) => Apply("Amplitude Tilt", f => f with { Physical = f.Physical with { TiltRange = Number(value) } }, loadingGuard: true);

    partial void OnPowerChanged(string value) => Apply("Puissance", f => f with { Physical = f.Physical with { Power = Number(value) } }, loadingGuard: true);

    partial void OnSelectedModeChanged(ModeItemViewModel? value) => SyncSlots();

    partial void OnSelectedSlotChanged(SlotViewModel? value)
    {
        if (value?.Definition is { } definition)
        {
            if (SelectedChannel?.Key != definition.Key)
            {
                SelectedChannel = new ChannelDetailViewModel(this, definition.Key);
            }

            SelectedChannel.Load(Current, SelectedMode is { } m ? Current.Modes[m.Index] : null);
        }
        else
        {
            SelectedChannel = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (_history.Undo(Current) is { } previous)
        {
            Restore(previous);
        }
    }

    private bool CanUndo() => _history.CanUndo;

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (_history.Redo(Current) is { } next)
        {
            Restore(next);
        }
    }

    private bool CanRedo() => _history.CanRedo;

    [RelayCommand]
    private void AddMode()
    {
        Apply("Ajouter un mode", f => FixtureEdits.AddMode(f));
        SelectedMode = Modes.LastOrDefault();
    }

    [RelayCommand]
    private void RemoveMode()
    {
        if (SelectedMode is { } mode && Modes.Count > 1)
        {
            Apply("Supprimer un mode", f => FixtureEdits.RemoveMode(f, mode.Index));
            SelectedMode = Modes.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void AddExistingChannel()
    {
        var key = Current.Channels.FirstOrDefault(c => Label(c) == ChannelToAdd)?.Key;
        if (key is not null && SelectedMode is { } mode)
        {
            Apply("Ajouter un canal au mode", f => FixtureEdits.AddSlot(f, mode.Index, new ModeChannel(key)));
            SelectedSlot = Slots.LastOrDefault();
        }
    }

    [RelayCommand]
    private void NewChannel()
    {
        if (SelectedMode is not { } mode)
        {
            return;
        }

        Apply("Nouveau canal", f =>
        {
            var (withChannel, key) = FixtureEdits.NewChannel(f, string.Create(CultureInfo.CurrentCulture, $"Canal {f.Channels.Count + 1}"), AttributeKind.Generic);
            return FixtureEdits.AddSlot(withChannel, mode.Index, new ModeChannel(key));
        });
        SelectedSlot = Slots.LastOrDefault();
    }

    [RelayCommand]
    private void RemoveSlot()
    {
        if (SelectedSlot is { } slot && SelectedMode is { } mode)
        {
            Apply("Retirer un canal du mode", f => FixtureEdits.RemoveSlot(f, mode.Index, slot.Position - 1));
        }
    }

    [RelayCommand]
    private void MoveSlotUp()
    {
        if (SelectedSlot is { Position: > 1 } slot)
        {
            MoveSlot(slot.Position - 1, slot.Position - 2);
        }
    }

    [RelayCommand]
    private void MoveSlotDown()
    {
        if (SelectedSlot is { } slot && slot.Position < Slots.Count)
        {
            MoveSlot(slot.Position - 1, slot.Position);
        }
    }

    [RelayCommand]
    private void RemoveUnusedChannels() =>
        Apply("Supprimer les canaux inutilisés", f =>
        {
            var used = f.Modes.SelectMany(m => m.Channels).Select(s => s.Channel).ToHashSet(StringComparer.Ordinal);
            return f.Channels.Where(c => !used.Contains(c.Key)).Aggregate(f, (acc, c) => FixtureEdits.RemoveChannel(acc, c.Key));
        });

    [RelayCommand]
    private void AddColorWheel() => Apply("Ajouter une roue de couleur", f => FixtureEdits.AddWheel(f, WheelKind.Color));

    [RelayCommand]
    private void AddGoboWheel() => Apply("Ajouter une roue de gobos", f => FixtureEdits.AddWheel(f, WheelKind.Gobo));

    private void Apply(string description, Func<FixtureType, FixtureType> change, bool loadingGuard)
    {
        if (!(loadingGuard && _loading))
        {
            Apply(description, change);
        }
    }

    private void Restore(FixtureType state)
    {
        Current = state;
        Sync();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Met les modèles de vue en accord avec le modèle, en réutilisant ceux qui existent.</summary>
    private void Sync()
    {
        _loading = true;
        var f = Current;
        Manufacturer = f.Manufacturer;
        Model = f.Model;
        Reference = f.Reference;
        Category = Choices.Categories.First(c => c.Value == f.Category);
        Author = f.Author;
        Notes = f.Notes;
        Manual = f.Manual;
        SourceType = f.Physical.SourceType;
        BeamAngle = Text(f.Physical.BeamAngle);
        PanRange = Text(f.Physical.PanRange);
        TiltRange = Text(f.Physical.TiltRange);
        Power = Text(f.Physical.Power);

        var selectedModeIndex = SelectedMode?.Index ?? 0;
        if (Modes.Count != f.Modes.Count)
        {
            Modes.Clear();
            for (var i = 0; i < f.Modes.Count; i++)
            {
                Modes.Add(new ModeItemViewModel(this, i));
            }
        }

        for (var i = 0; i < f.Modes.Count; i++)
        {
            Modes[i].Load(f, f.Modes[i]);
        }

        if (Wheels.Count != f.Wheels.Count)
        {
            Wheels.Clear();
            for (var i = 0; i < f.Wheels.Count; i++)
            {
                Wheels.Add(new WheelViewModel(this, i));
            }
        }

        for (var i = 0; i < f.Wheels.Count; i++)
        {
            Wheels[i].Load(f.Wheels[i]);
        }

        ChannelKeys.Clear();
        foreach (var channel in f.Channels)
        {
            ChannelKeys.Add(Label(channel));
        }

        Issues.Clear();
        foreach (var issue in FixtureValidator.Validate(f))
        {
            Issues.Add(issue);
        }

        var errors = Issues.Count(i => i.Severity == IssueSeverity.Error);
        var warnings = Issues.Count - errors;
        HasErrors = errors > 0;
        ValidationSummary = Issues.Count == 0
            ? "Aucun problème."
            : string.Create(CultureInfo.CurrentCulture, $"{errors} erreur(s), {warnings} avertissement(s)");
        IsDirty = Json(f) != _savedJson;
        Title = f.DisplayName + (IsDirty ? " *" : string.Empty);
        Info = string.Create(
            CultureInfo.CurrentCulture,
            $"Version {f.Version} · {f.Source switch { FixtureSource.Ofl => "import OFL", FixtureSource.QlcPlus => "import QLC+", FixtureSource.Generic => "générique (lecture seule)", _ => "saisie" }} · {f.Channels.Count} définitions de canaux");

        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        _loading = false;

        SelectedMode = Modes.ElementAtOrDefault(Math.Min(selectedModeIndex, Modes.Count - 1));
        SyncSlots();
    }

    private void SyncSlots()
    {
        var position = SelectedSlot?.Position;
        Slots.Clear();
        if (SelectedMode is { } mode && mode.Index < Current.Modes.Count)
        {
            var slots = Current.Modes[mode.Index].Channels;
            for (var i = 0; i < slots.Count; i++)
            {
                Slots.Add(new SlotViewModel(i + 1, slots[i], Current.Channel(slots[i].Channel)));
            }
        }

        SelectedSlot = position is { } p ? Slots.ElementAtOrDefault(Math.Min(p, Slots.Count) - 1) : Slots.FirstOrDefault();
    }

    private static string Label(ChannelDefinition channel) => $"{channel.Name} [{AttributeCatalog.Label(channel.Attribute)}]";

    private static string Json(FixtureType fixture) => JsonSerializer.Serialize(fixture, DmxJson.Options);

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Text(double? value) => value?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;

    private static double? Number(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var v) && v > 0 ? v : null;
}
