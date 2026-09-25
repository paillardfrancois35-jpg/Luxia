using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dmx.Fixtures;
using Dmx.Fixtures.Model;
using Dmx.Hosting;
using Dmx.Messaging.Commands;
using Dmx.Patch;
using Dmx.Patch.Model;
using Dmx.Patch.Rules;
using Dmx.UI.Controls;

namespace Dmx.UI.Modules.Installation;

/// <summary>
/// Écran « Installation » (doc 13) : univers, patch, sélections, lieux, fiche d'installation.
/// Toute modification est enregistrée aussitôt dans le projet (<c>installation.json</c>, <c>lieux.json</c>).
/// </summary>
public sealed partial class InstallationViewModel : ViewModelBase, IRefreshable
{
    private static readonly TimeSpan IdentifyPeriod = TimeSpan.FromMilliseconds(400);

    private readonly DmxRuntime _runtime;
    private readonly IDialogService _dialogs;
    private IReadOnlyList<(int Channel, byte Value)> _identifyChannels = [];
    private long _identifyStart;
    private Guid? _identifyingFixtureId;

    [ObservableProperty]
    private bool _hasProject;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private int _selectedUniverse = 1;

    [ObservableProperty]
    private LibraryEntry? _selectedModel;

    [ObservableProperty]
    private string? _selectedMode;

    [ObservableProperty]
    private decimal _newUniverse = 1;

    [ObservableProperty]
    private decimal _newAddress = 1;

    [ObservableProperty]
    private decimal _newCount = 1;

    [ObservableProperty]
    private decimal _newGap;

    [ObservableProperty]
    private string _newBaseName = string.Empty;

    [ObservableProperty]
    private bool _newAsTwins;

    [ObservableProperty]
    private VenueRowViewModel? _selectedVenue;

    [ObservableProperty]
    private string _newVenueName = string.Empty;

    [ObservableProperty]
    private string _newSelectionName = string.Empty;

    [ObservableProperty]
    private SelectionRowViewModel? _selectedSelection;

    /// <summary>Crée l'écran.</summary>
    public InstallationViewModel(DmxRuntime runtime, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(dialogs);
        _runtime = runtime;
        _dialogs = dialogs;
        _runtime.Project.Changed += (_, _) => LoadAll();
        LoadAll();
    }

    /// <summary>Univers de l'installation (INST-001).</summary>
    public ObservableCollection<int> Universes { get; } = [];

    /// <summary>Modèles de la bibliothèque partagée, pour le patch.</summary>
    public ObservableCollection<LibraryEntry> LibraryModels { get; } = [];

    /// <summary>Modes du modèle choisi.</summary>
    public ObservableCollection<string> AvailableModes { get; } = [];

    /// <summary>Appareils patchés (doc 13 §3).</summary>
    public ObservableCollection<PatchRowViewModel> PatchRows { get; } = [];

    /// <summary>Appareils de l'univers affiché, pour la barre d'univers (INST-003).</summary>
    public ObservableCollection<UniverseBarSegment> UniverseBarSegments { get; } = [];

    /// <summary>Sélections manuelles (INST-030, INST-032).</summary>
    public ObservableCollection<SelectionRowViewModel> Selections { get; } = [];

    /// <summary>Sélections automatiques (INST-031), toujours à jour.</summary>
    public ObservableCollection<AutoSelectionRowViewModel> AutoSelections { get; } = [];

    /// <summary>Lieux du projet (doc 13 §5).</summary>
    public ObservableCollection<VenueRowViewModel> Venues { get; } = [];

    /// <summary>Lignes de la fiche d'installation (INST-018).</summary>
    public ObservableCollection<string> InstallationSheet { get; } = [];

    /// <inheritdoc />
    public void Refresh()
    {
        if (_identifyingFixtureId is null)
        {
            return;
        }

        var elapsed = Stopwatch.GetElapsedTime(_identifyStart);
        var on = elapsed.Ticks / IdentifyPeriod.Ticks % 2 == 0;
        _runtime.SetChannels(1, [.. _identifyChannels.Select(c => new ChannelValue(c.Channel, on ? c.Value : (byte)0))]);
    }

    /// <summary>Adresse libre proposée pour le mode choisi (INST-012).</summary>
    [RelayCommand]
    private void SuggestAddress()
    {
        if (SelectedModel is null || SelectedMode is null)
        {
            return;
        }

        var channelCount = SelectedModel.Fixture.Modes.First(m => m.Name == SelectedMode).ChannelCount;
        var universe = (int)NewUniverse;
        var free = PatchRules.FindFreeAddress(_runtime.Project.Installation.Fixtures, universe, channelCount, ChannelCountOf);
        if (free is { } address)
        {
            NewAddress = address;
        }
        else
        {
            Message = "Aucune adresse libre pour ce mode dans cet univers.";
        }
    }

    /// <summary>Ajoute un (ou plusieurs) appareil(s) au patch (INST-010, INST-011).</summary>
    [RelayCommand]
    private void AddFixture()
    {
        if (!HasProject)
        {
            Message = "Ouvrez ou créez un projet pour patcher un appareil.";
            return;
        }

        if (SelectedModel is null || SelectedMode is null)
        {
            Message = "Choisissez un modèle et un mode.";
            return;
        }

        var mode = SelectedModel.Fixture.Modes.FirstOrDefault(m => m.Name == SelectedMode);
        if (mode is null)
        {
            return;
        }

        var count = Math.Max(1, (int)NewCount);
        var baseName = string.IsNullOrWhiteSpace(NewBaseName) ? SelectedModel.Fixture.Model : NewBaseName.Trim();
        var startNumber = _runtime.Project.Installation.Fixtures.Count + 1;
        var plan = PatchRules.PlanMultiple((int)NewAddress, count, mode.ChannelCount, baseName, (int)NewGap, startNumber);

        _runtime.Project.FixtureLibrary!.EnsureCopied(SelectedModel.Fixture);
        var twinGroup = NewAsTwins ? Guid.NewGuid() : (Guid?)null;
        var added = plan.Select(p => new PatchedFixture
        {
            FixtureTypeId = SelectedModel.Fixture.Id,
            ModeName = mode.Name,
            Universe = (int)NewUniverse,
            Address = NewAsTwins ? (int)NewAddress : p.Address,
            Name = p.Name,
            Number = p.Number,
            TwinGroupId = twinGroup,
        }).ToList();

        var installation = _runtime.Project.Installation;
        var universes = installation.Universes.Any(u => u.Number == (int)NewUniverse)
            ? installation.Universes
            : [.. installation.Universes, new PatchUniverse { Number = (int)NewUniverse }];
        SaveInstallation(installation with { Fixtures = [.. installation.Fixtures, .. added], Universes = universes });
        Message = $"{added.Count} appareil(s) ajouté(s).";
    }

    /// <summary>Renomme un appareil (INST-017).</summary>
    internal void RenameFixture(PatchRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (string.IsNullOrWhiteSpace(row.EditName))
        {
            return;
        }

        Replace(row.Fixture with { Name = row.EditName.Trim() });
    }

    /// <summary>Déplace un appareil (INST-015) : les sélections et l'univers émis restent cohérents.</summary>
    internal void MoveFixture(PatchRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);
        Replace(row.Fixture with { Universe = row.EditUniverse, Address = row.EditAddress });
    }

    /// <summary>Change le mode d'un appareil, après confirmation s'il y a un impact (INST-016).</summary>
    internal async Task ChangeModeAsync(PatchRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (row.Type is not { } type || row.EditModeName == row.ModeName)
        {
            return;
        }

        var impact = FixtureUpdateImpact.ForModeChange(type, row.ModeName, row.EditModeName);
        if (!impact.IsEmpty && !await _dialogs.ConfirmAsync("Changer de mode", $"{row.Name} : {impact.Summary()} Continuer ?").ConfigureAwait(true))
        {
            return;
        }

        Replace(row.Fixture with { ModeName = row.EditModeName });
    }

    /// <summary>Supprime un appareil, après confirmation (INST-020, GEN-103).</summary>
    internal async Task DeleteFixtureAsync(PatchRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (!await _dialogs.ConfirmAsync("Supprimer l'appareil", $"Supprimer définitivement « {row.Name} » du patch ?").ConfigureAwait(true))
        {
            return;
        }

        var installation = _runtime.Project.Installation;
        SaveInstallation(installation with { Fixtures = [.. installation.Fixtures.Where(f => f.Id != row.Id)] });
        Message = $"« {row.Name} » supprimé du patch.";
    }

    /// <summary>Identifie (ou arrête d'identifier) un appareil (CMD-023, INST-019, CONS-024).</summary>
    internal void ToggleIdentifyFixture(PatchRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);
        StopIdentify();
        if (_identifyingFixtureId == row.Id)
        {
            // Un second clic sur le même appareil : on vient de l'arrêter (StopIdentify ci-dessus), rien de plus.
            _identifyingFixtureId = null;
            return;
        }

        StartIdentify(row);
    }

    /// <summary>Identification en chenillard (INST-019) : arrête l'appareil courant et identifie le suivant du patch.</summary>
    [RelayCommand]
    private void IdentifyNext()
    {
        if (PatchRows.Count == 0)
        {
            return;
        }

        var index = _identifyingFixtureId is { } id ? PatchRows.ToList().FindIndex(r => r.Id == id) : -1;
        StopIdentify();
        var next = PatchRows[(index + 1) % PatchRows.Count];
        StartIdentify(next);
    }

    /// <summary>Arrête l'identification en cours.</summary>
    [RelayCommand]
    private void StopIdentify()
    {
        if (_identifyingFixtureId is not { } id)
        {
            return;
        }

        var row = PatchRows.FirstOrDefault(r => r.Id == id);
        if (row is not null)
        {
            row.Identifying = false;
        }

        if (_identifyChannels.Count > 0)
        {
            _runtime.ReleaseChannels(1, [.. _identifyChannels.Select(c => c.Channel)]);
        }

        _identifyingFixtureId = null;
        _identifyChannels = [];
    }

    private void StartIdentify(PatchRowViewModel row)
    {
        if (row.Type is not { } type)
        {
            return;
        }

        var mode = type.Modes.FirstOrDefault(m => m.Name == row.ModeName);
        if (mode is null)
        {
            return;
        }

        _identifyChannels = IdentifyRules.IdentifyChannels(type, mode, row.Address);
        if (_identifyChannels.Count == 0)
        {
            return;
        }

        _identifyingFixtureId = row.Id;
        _identifyStart = Stopwatch.GetTimestamp();
        row.Identifying = true;
    }

    /// <summary>Crée une sélection manuelle à partir des appareils cochés dans le patch (INST-030, INST-032).</summary>
    [RelayCommand]
    private void CreateSelection()
    {
        var checkedRows = PatchRows.Where(r => r.IsChecked).ToList();
        if (checkedRows.Count == 0 || string.IsNullOrWhiteSpace(NewSelectionName))
        {
            Message = "Cochez au moins un appareil et donnez un nom à la sélection.";
            return;
        }

        var selection = new Selection
        {
            Name = NewSelectionName.Trim(),
            Items = [.. checkedRows.Select(r => new SelectionItem(r.Id))],
        };
        var installation = _runtime.Project.Installation;
        SaveInstallation(installation with { Selections = [.. installation.Selections, selection] });
        foreach (var row in checkedRows)
        {
            row.IsChecked = false;
        }

        NewSelectionName = string.Empty;
        Message = $"Sélection « {selection.Name} » créée ({selection.Items.Count} appareils).";
    }

    /// <summary>Supprime la sélection affichée (GEN-103).</summary>
    [RelayCommand]
    private async Task DeleteSelectionAsync()
    {
        if (SelectedSelection is null)
        {
            return;
        }

        if (!await _dialogs.ConfirmAsync("Supprimer la sélection", $"Supprimer définitivement « {SelectedSelection.Name} » ?").ConfigureAwait(true))
        {
            return;
        }

        var installation = _runtime.Project.Installation;
        SaveInstallation(installation with { Selections = [.. installation.Selections.Where(s => s.Id != SelectedSelection.Id)] });
    }

    /// <summary>Réordonne la sélection affichée (INST-033).</summary>
    [RelayCommand]
    private void ReorderSelection(string? operation)
    {
        if (SelectedSelection is null || operation is null)
        {
            return;
        }

        var items = SelectedSelection.Selection.Items;
        var reordered = operation switch
        {
            "reverse" => SelectionRules.Reverse(items),
            "odd" => SelectionRules.Odd(items),
            "even" => SelectionRules.Even(items),
            "first-half" => SelectionRules.FirstHalf(items),
            "second-half" => SelectionRules.SecondHalf(items),
            "left-right" when _runtime.Project.Venues.Active is { } venue => SelectionRules.OrderByPosition(items, venue, PositionOrder.LeftToRight),
            "front-back" when _runtime.Project.Venues.Active is { } venue => SelectionRules.OrderByPosition(items, venue, PositionOrder.FrontToBack),
            "center-out" when _runtime.Project.Venues.Active is { } venue => SelectionRules.OrderByPosition(items, venue, PositionOrder.CenterOutward),
            _ => items,
        };
        var installation = _runtime.Project.Installation;
        var updated = SelectedSelection.Selection with { Items = reordered };
        SaveInstallation(installation with { Selections = [.. installation.Selections.Select(s => s.Id == updated.Id ? updated : s)] });
    }

    /// <summary>Crée un lieu (INST-050).</summary>
    [RelayCommand]
    private void CreateVenue()
    {
        if (string.IsNullOrWhiteSpace(NewVenueName))
        {
            return;
        }

        SaveVenueSet(_runtime.Project.Venues.Venues.Append(new Venue { Name = NewVenueName.Trim() }).ToList(), _runtime.Project.Venues.ActiveVenueId);
        NewVenueName = string.Empty;
    }

    /// <summary>Duplique le lieu affiché (INST-050).</summary>
    [RelayCommand]
    private void DuplicateVenue()
    {
        if (SelectedVenue is null)
        {
            return;
        }

        var copy = SelectedVenue.ToVenue() with { Id = Guid.NewGuid(), Name = $"{SelectedVenue.Name} (copie)" };
        SaveVenueSet(_runtime.Project.Venues.Venues.Append(copy).ToList(), _runtime.Project.Venues.ActiveVenueId);
    }

    /// <summary>Choisit le lieu actif (INST-050).</summary>
    [RelayCommand]
    private void ActivateVenue()
    {
        if (SelectedVenue is null)
        {
            return;
        }

        SaveVenueSet(_runtime.Project.Venues.Venues, SelectedVenue.Id);
    }

    /// <summary>Supprime le lieu affiché, après confirmation (INST-050, GEN-103) ; impossible s'il ne reste que lui.</summary>
    [RelayCommand]
    private async Task DeleteVenueAsync()
    {
        if (SelectedVenue is null || _runtime.Project.Venues.Venues.Count <= 1)
        {
            Message = "Impossible de supprimer le dernier lieu.";
            return;
        }

        if (!await _dialogs.ConfirmAsync("Supprimer le lieu", $"Supprimer définitivement « {SelectedVenue.Name} » ?").ConfigureAwait(true))
        {
            return;
        }

        var remaining = _runtime.Project.Venues.Venues.Where(v => v.Id != SelectedVenue.Id).ToList();
        var activeId = _runtime.Project.Venues.ActiveVenueId == SelectedVenue.Id ? remaining[0].Id : _runtime.Project.Venues.ActiveVenueId;
        SaveVenueSet(remaining, activeId);
    }

    /// <summary>Enregistre les positions et la présence du lieu affiché (INST-051, INST-052).</summary>
    [RelayCommand]
    private void SavePlacements()
    {
        if (SelectedVenue is null)
        {
            return;
        }

        var updated = SelectedVenue.ToVenue();
        var venues = _runtime.Project.Venues.Venues.Select(v => v.Id == updated.Id ? updated : v).ToList();
        SaveVenueSet(venues, _runtime.Project.Venues.ActiveVenueId);
        Message = $"Lieu « {updated.Name} » enregistré.";
    }

    private void SaveVenueSet(IReadOnlyList<Venue> venues, Guid? activeId)
    {
        _runtime.Project.SaveVenues(new VenueSet { Venues = venues, ActiveVenueId = activeId });
        LoadAll();
    }

    private void Replace(PatchedFixture updated)
    {
        var installation = _runtime.Project.Installation;
        var fixtures = installation.Fixtures.Select(f => f.Id == updated.Id ? updated : f).ToList();
        SaveInstallation(installation with { Fixtures = fixtures });
    }

    /// <summary>
    /// Enregistre l'installation et recharge aussitôt l'écran : <c>ProjectSession.Changed</c> ne se déclenche
    /// qu'à l'ouverture ou la création d'un projet (comme pour les instantanés de console), pas à chaque
    /// modification du patch ; chaque commande qui modifie l'installation passe donc par ici.
    /// </summary>
    private void SaveInstallation(Dmx.Patch.Model.Installation installation)
    {
        _runtime.Project.SaveInstallation(installation);
        LoadAll();
    }

    private int ChannelCountOf(PatchedFixture fixture) =>
        TypeOf(fixture)?.Modes.FirstOrDefault(m => m.Name == fixture.ModeName)?.ChannelCount ?? 0;

    private FixtureType? TypeOf(PatchedFixture fixture) => _runtime.Project.FixtureLibrary?.Find(fixture.FixtureTypeId);

    private void LoadAll()
    {
        HasProject = _runtime.Project.Folder is not null;
        LibraryModels.Clear();
        foreach (var entry in _runtime.Library.Entries)
        {
            LibraryModels.Add(entry);
        }

        var installation = _runtime.Project.Installation;
        Universes.Clear();
        foreach (var universe in installation.Universes.Select(u => u.Number).Order())
        {
            Universes.Add(universe);
        }

        if (!Universes.Contains(SelectedUniverse) && Universes.Count > 0)
        {
            SelectedUniverse = Universes[0];
        }

        PatchRows.Clear();
        foreach (var fixture in installation.Fixtures.OrderBy(f => f.Universe).ThenBy(f => f.Address))
        {
            PatchRows.Add(new PatchRowViewModel(fixture, TypeOf(fixture)));
        }

        var overlaps = PatchRules.DetectOverlaps(installation.Fixtures, ChannelCountOf);
        var overlapping = new HashSet<Guid>(overlaps.SelectMany(o => new[] { o.First.Id, o.Second.Id }));
        foreach (var row in PatchRows)
        {
            row.HasOverlap = overlapping.Contains(row.Id);
        }

        UniverseBarSegments.Clear();
        foreach (var row in PatchRows.Where(r => r.Universe == SelectedUniverse && r.ChannelCount > 0))
        {
            UniverseBarSegments.Add(new UniverseBarSegment(row.Address, row.Address + row.ChannelCount - 1, row.Color, row.Name));
        }

        LoadSelections(installation);
        LoadVenues();
        LoadInstallationSheet(installation);
    }

    private void LoadSelections(Dmx.Patch.Model.Installation installation)
    {
        string NameOf(Guid id) => installation.Fixtures.FirstOrDefault(f => f.Id == id)?.Name ?? "?";
        Selections.Clear();
        foreach (var selection in installation.Selections)
        {
            Selections.Add(new SelectionRowViewModel(selection, NameOf));
        }

        AutoSelections.Clear();
        foreach (var auto in Dmx.Patch.Rules.AutoSelections.Build(installation.Fixtures, TypeOf))
        {
            AutoSelections.Add(new AutoSelectionRowViewModel(auto.Title(CategoryLabel), auto.Items.Count));
        }
    }

    private void LoadVenues()
    {
        Venues.Clear();
        var venueSet = _runtime.Project.Venues;
        var fixtures = _runtime.Project.Installation.Fixtures;
        foreach (var venue in venueSet.Venues)
        {
            Venues.Add(new VenueRowViewModel(venue, fixtures));
        }

        SelectedVenue = Venues.FirstOrDefault(v => v.Id == venueSet.Active.Id) ?? Venues.FirstOrDefault();
    }

    private void LoadInstallationSheet(Dmx.Patch.Model.Installation installation)
    {
        InstallationSheet.Clear();
        foreach (var fixture in installation.Fixtures.OrderBy(f => f.Universe).ThenBy(f => f.Address))
        {
            var type = TypeOf(fixture);
            var mode = type?.Modes.FirstOrDefault(m => m.Name == fixture.ModeName);
            var setting = mode?.DeviceSetting ?? "—";
            InstallationSheet.Add(string.Create(
                CultureInfo.CurrentCulture,
                $"{fixture.Name} — {type?.DisplayName ?? "?"} ({mode?.ShortName ?? fixture.ModeName}) — régler « {setting} » — univers {fixture.Universe}, adresse {fixture.Address}"));
        }
    }

    private static string CategoryLabel(FixtureCategory category) => category switch
    {
        FixtureCategory.Par => "PAR",
        FixtureCategory.LedBar => "barres LED",
        FixtureCategory.MovingHead => "lyres",
        FixtureCategory.Effect => "effets",
        FixtureCategory.Strobe => "stroboscopes",
        FixtureCategory.Uv => "UV",
        FixtureCategory.Smoke => "machines à fumée",
        FixtureCategory.Laser => "lasers",
        FixtureCategory.Dimmer => "gradateurs",
        _ => "autres",
    };
}
