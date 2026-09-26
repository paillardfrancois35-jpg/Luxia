using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Fixtures;
using Luxia.Fixtures.Model;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Patch;
using Luxia.Patch.Model;
using Luxia.Patch.Rules;
using Luxia.UI.Controls;
using Microsoft.Extensions.Logging;

namespace Luxia.UI.Modules.Installation;

/// <summary>
/// Écran « Installation » (doc 13) : univers, patch, sélections, lieux, fiche d'installation.
/// Toute modification est enregistrée aussitôt dans le projet (<c>installation.json</c>, <c>lieux.json</c>).
/// </summary>
public sealed partial class InstallationViewModel : ViewModelBase, IRefreshable
{
    private static readonly TimeSpan IdentifyPeriod = TimeSpan.FromMilliseconds(400);

    private readonly LuxiaRuntime _runtime;
    private readonly IDialogService _dialogs;
    private readonly ILogger<InstallationViewModel> _logger;
    private IReadOnlyList<(int Channel, byte Value)> _identifyChannels = [];
    private long _identifyStart;
    private Guid? _identifyingFixtureId;
    private bool? _identifyLastOn;

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
    public InstallationViewModel(LuxiaRuntime runtime, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(dialogs);
        _runtime = runtime;
        _dialogs = dialogs;
        _logger = runtime.Loggers.CreateLogger<InstallationViewModel>();
        _runtime.Project.Changed += (_, _) => LoadAll();
        LoadAll();
    }

    /// <summary>Univers de l'installation (INST-001).</summary>
    public ObservableCollection<int> Universes { get; } = [];

    /// <summary>Modèles de la bibliothèque partagée, pour le patch.</summary>
    public ObservableCollection<LibraryEntry> LibraryModels { get; } = [];

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
    public bool NeedsBackgroundRefresh => _identifyingFixtureId is not null;

    /// <inheritdoc />
    public void Refresh()
    {
        if (_identifyingFixtureId is null)
        {
            return;
        }

        var elapsed = Stopwatch.GetElapsedTime(_identifyStart);
        var on = elapsed.Ticks / IdentifyPeriod.Ticks % 2 == 0;
        if (_identifyLastOn != on)
        {
            _identifyLastOn = on;
            _logger.LogInformation("Identifier : bascule à {Etat} ({N} canal/canaux)", on ? "ON" : "OFF", _identifyChannels.Count);
        }

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
            _logger.LogWarning("Ajouter : mode « {Mode} » introuvable dans « {Modele} ».", SelectedMode, SelectedModel.Fixture.DisplayName);
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
        _logger.LogInformation("Ajouter : {N} appareil(s) « {Modele} » ({Mode}) à partir de l'adresse {Adresse}, univers {Univers}.", added.Count, SelectedModel.Fixture.DisplayName, mode.Name, (int)NewAddress, (int)NewUniverse);
    }

    /// <summary>Renomme un appareil (INST-017).</summary>
    internal void RenameFixture(PatchRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (string.IsNullOrWhiteSpace(row.EditName))
        {
            return;
        }

        _logger.LogInformation("Renommer : « {Ancien} » → « {Nouveau} ».", row.Name, row.EditName.Trim());
        Replace(row.Fixture with { Name = row.EditName.Trim() });
    }

    /// <summary>Déplace un appareil (INST-015) : les sélections et l'univers émis restent cohérents.</summary>
    internal void MoveFixture(PatchRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);
        _logger.LogInformation("Déplacer : « {Nom} » univers {AncienUnivers}→{NouvelUnivers}, adresse {AncienneAdresse}→{NouvelleAdresse}.", row.Name, row.Universe, row.EditUniverse, row.Address, row.EditAddress);
        Replace(row.Fixture with { Universe = row.EditUniverse, Address = row.EditAddress });
    }

    /// <summary>Change le mode d'un appareil, après confirmation s'il y a un impact (INST-016).</summary>
    internal async Task ChangeModeAsync(PatchRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (row.Type is not { } type)
        {
            _logger.LogWarning("Changer de mode : « {Nom} » sans modèle résolu.", row.Name);
            return;
        }

        if (row.EditModeName == row.ModeName)
        {
            return;
        }

        var impact = FixtureUpdateImpact.ForModeChange(type, row.ModeName, row.EditModeName);
        _logger.LogInformation("Changer de mode : « {Nom} » {Ancien}→{Nouveau} — impact : {Impact}", row.Name, row.ModeName, row.EditModeName, impact.Summary());

        // SC-03 : les scènes gardent leurs valeurs (elles visent des attributs, pas des canaux) ; seules celles dont
        // l'attribut n'existe pas dans le nouveau mode seront ignorées, et on les annonce.
        var newMode = type.Modes.FirstOrDefault(m => m.Name == row.EditModeName);
        var scenes = newMode is not null && _runtime.Show.Patch.Find(row.Id) is { } info
            ? Luxia.Scenes.Rules.SceneUsage.ModeChangeImpact(_runtime.Project.Scenes, info, newMode, _runtime.Show.Patch)
            : [];
        var message = impact.IsEmpty ? string.Empty : impact.Summary();
        if (scenes.Count > 0)
        {
            message += $"{(message.Length > 0 ? Environment.NewLine + Environment.NewLine : string.Empty)}Scènes concernées (valeurs ignorées dans le nouveau mode, le reste est conservé) :{Environment.NewLine}{string.Join(Environment.NewLine, scenes)}";
        }

        if (message.Length > 0 && !await _dialogs.ConfirmAsync("Changer de mode", $"{row.Name} : {message}{Environment.NewLine}{Environment.NewLine}Continuer ?").ConfigureAwait(true))
        {
            _logger.LogInformation("Changer de mode : annulé par l'utilisateur pour « {Nom} ».", row.Name);
            return;
        }

        Replace(row.Fixture with { ModeName = row.EditModeName });
    }

    /// <summary>
    /// Reprend la version courante du modèle depuis la bibliothèque partagée (GEN-053), après confirmation s'il y a
    /// un impact sur le mode utilisé. Sans effet si la bibliothèque partagée n'a pas de version plus récente que
    /// celle déjà copiée dans le projet (message « Aucune mise à jour disponible » : ce n'est pas une erreur).
    /// </summary>
    internal async Task UpdateFromLibraryAsync(PatchRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);
        var shared = _runtime.Library.Entries.FirstOrDefault(e => e.Fixture.Id == row.Fixture.FixtureTypeId)?.Fixture;
        if (shared is null || row.Type is null || shared.Version == row.Type.Version)
        {
            _logger.LogInformation(
                "Mettre à jour : « {Nom} » — rien à faire (bibliothèque partagée : version {VersionPartagee}, copie du projet : version {VersionProjet}).",
                row.Name, shared?.Version, row.Type?.Version);
            Message = "Aucune mise à jour disponible pour ce modèle.";
            return;
        }

        var impact = FixtureUpdateImpact.ForLibraryUpdate(row.Type, shared, row.ModeName);
        _logger.LogInformation("Mettre à jour : « {Nom} » v{Ancien}→v{Nouveau} — impact : {Impact}", row.Name, row.Type.Version, shared.Version, impact.Summary());
        if (!impact.IsEmpty && !await _dialogs.ConfirmAsync("Mettre à jour le modèle", $"{row.Name} : {impact.Summary()} Continuer ?").ConfigureAwait(true))
        {
            _logger.LogInformation("Mettre à jour : annulé par l'utilisateur pour « {Nom} ».", row.Name);
            return;
        }

        _runtime.Project.FixtureLibrary!.UpdateFrom(shared);
        LoadAll();
        Message = $"Modèle « {shared.DisplayName} » mis à jour (version {shared.Version}).";
    }

    /// <summary>Supprime un appareil, après confirmation (INST-020, GEN-103).</summary>
    internal async Task DeleteFixtureAsync(PatchRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (!await _dialogs.ConfirmAsync("Supprimer l'appareil", $"Supprimer définitivement « {row.Name} » du patch ?").ConfigureAwait(true))
        {
            return;
        }

        _logger.LogInformation("Supprimer : « {Nom} » (adresse {Adresse}) retiré du patch.", row.Name, row.Address);
        var installation = _runtime.Project.Installation;
        SaveInstallation(installation with { Fixtures = [.. installation.Fixtures.Where(f => f.Id != row.Id)] });
        Message = $"« {row.Name} » supprimé du patch.";
    }

    /// <summary>Identifie (ou arrête d'identifier) un appareil (CMD-023, INST-019, CONS-024).</summary>
    internal void ToggleIdentifyFixture(PatchRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);
        _logger.LogInformation("Identifier : clic sur « {Nom} » (adresse {Adresse}, mode {Mode})", row.Name, row.Address, row.ModeName);
        var wasIdentifying = _identifyingFixtureId == row.Id;
        StopIdentify();
        if (wasIdentifying)
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
            _logger.LogInformation("Identifier : arrêt, {N} canal/canaux libéré(s) : {Canaux}", _identifyChannels.Count, string.Join(", ", _identifyChannels.Select(c => c.Channel)));
        }

        _identifyingFixtureId = null;
        _identifyChannels = [];
        _identifyLastOn = null;
    }

    private void StartIdentify(PatchRowViewModel row)
    {
        if (row.Type is not { } type)
        {
            _logger.LogWarning("Identifier : « {Nom} » sans modèle résolu dans la copie du projet (fixtureTypeId {Id}) : rien à identifier.", row.Name, row.Fixture.FixtureTypeId);
            return;
        }

        var mode = type.Modes.FirstOrDefault(m => m.Name == row.ModeName);
        if (mode is null)
        {
            _logger.LogWarning("Identifier : mode « {Mode} » introuvable dans le modèle « {Modele} » pour « {Nom} ».", row.ModeName, type.DisplayName, row.Name);
            return;
        }

        _identifyChannels = IdentifyRules.IdentifyChannels(type, mode, row.Address);
        if (_identifyChannels.Count == 0)
        {
            _logger.LogWarning(
                "Identifier : aucun canal d'intensité ni de valeur d'identification trouvé pour « {Nom} » ({Modele}, mode {Mode}, adresse {Adresse}) : rien ne s'allume.",
                row.Name, type.DisplayName, mode.Name, row.Address);
            return;
        }

        _identifyingFixtureId = row.Id;
        _identifyStart = Stopwatch.GetTimestamp();
        row.Identifying = true;
        _logger.LogInformation(
            "Identifier : « {Nom} » démarré, {N} canal/canaux : {Canaux}",
            row.Name, _identifyChannels.Count, string.Join(", ", _identifyChannels.Select(c => $"{c.Channel}={c.Value}")));
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
        _logger.LogInformation("Sélection : « {Nom} » créée ({N} appareils).", selection.Name, selection.Items.Count);
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

        _logger.LogInformation("Sélection : « {Nom} » supprimée.", SelectedSelection.Name);
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

        _logger.LogInformation("Sélection : « {Nom} » réordonnée ({Operation}).", SelectedSelection.Name, operation);
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

        _logger.LogInformation("Lieu : « {Nom} » créé.", NewVenueName.Trim());
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
        _logger.LogInformation("Lieu : « {Nom} » dupliqué en « {Copie} ».", SelectedVenue.Name, copy.Name);
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

        _logger.LogInformation("Lieu : « {Nom} » activé.", SelectedVenue.Name);
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

        _logger.LogInformation("Lieu : « {Nom} » supprimé.", SelectedVenue.Name);
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
        _logger.LogInformation("Lieu : « {Nom} » — {N} placement(s) enregistré(s).", updated.Name, updated.Placements.Count);
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
    private void SaveInstallation(Luxia.Patch.Model.Installation installation)
    {
        _runtime.Project.SaveInstallation(installation);
        LoadAll();
    }

    private int ChannelCountOf(PatchedFixture fixture) =>
        TypeOf(fixture)?.Modes.FirstOrDefault(m => m.Name == fixture.ModeName)?.ChannelCount ?? 0;

    private FixtureType? TypeOf(PatchedFixture fixture) => _runtime.Project.FixtureLibrary?.Find(fixture.FixtureTypeId);

    private void LoadAll()
    {
        try
        {
            LoadAllCore();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rechargement de l'écran Installation : échec inattendu.");
            Message = "Erreur inattendue au rechargement de l'écran (voir le journal technique).";
        }
    }

    private void LoadAllCore()
    {
        HasProject = _runtime.Project.Folder is not null;
        LibraryModels.Clear();
        foreach (var entry in _runtime.Library.Entries)
        {
            LibraryModels.Add(entry);
        }

        var installation = _runtime.Project.Installation;

        // Ne jamais vider puis remplir : le ComboBox lié à SelectedUniverse (int, non annulable) passe alors
        // par une sélection nulle le temps de la reconstruction, et la liaison bidirectionnelle lève
        // System.InvalidCastException en essayant de repousser ce null vers un int. On ne touche donc la
        // collection que pour les univers réellement ajoutés ou retirés.
        var wantedUniverses = installation.Universes.Select(u => u.Number).Order().ToList();
        foreach (var obsolete in Universes.Where(u => !wantedUniverses.Contains(u)).ToList())
        {
            Universes.Remove(obsolete);
        }

        foreach (var missing in wantedUniverses.Where(u => !Universes.Contains(u)))
        {
            Universes.Add(missing);
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

    private void LoadSelections(Luxia.Patch.Model.Installation installation)
    {
        string NameOf(Guid id) => installation.Fixtures.FirstOrDefault(f => f.Id == id)?.Name ?? "?";
        Selections.Clear();
        foreach (var selection in installation.Selections)
        {
            Selections.Add(new SelectionRowViewModel(selection, NameOf));
        }

        AutoSelections.Clear();
        foreach (var auto in Luxia.Patch.Rules.AutoSelections.Build(installation.Fixtures, TypeOf))
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
            Venues.Add(new VenueRowViewModel(venue, fixtures, venue.Id == venueSet.Active.Id));
        }

        SelectedVenue = Venues.FirstOrDefault(v => v.Id == venueSet.Active.Id) ?? Venues.FirstOrDefault();
    }

    private void LoadInstallationSheet(Luxia.Patch.Model.Installation installation)
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
