using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Fixtures;
using Luxia.Fixtures.Import;
using Luxia.Fixtures.Rules;
using Luxia.Hosting;
using Luxia.UI.Controls;
using Luxia.UI.Modules.Console;

namespace Luxia.UI.Modules.Library;

/// <summary>
/// Écran « Bibliothèque » (doc 12 §4-6) : liste des modèles par fabricant avec recherche et filtres (BIB-020),
/// éditeur (BIB-021 à 027), test en direct (BIB-060 à 063), import OFL / QLC+ (BIB-080 à 083).
/// </summary>
public sealed partial class LibraryViewModel : ViewModelBase, IRefreshable
{
    /// <summary>Filtre « tous les fabricants / toutes les catégories ».</summary>
    public const string All = "Tous";

    private readonly LuxiaRuntime _runtime;
    private readonly IDialogService _dialogs;
    private bool _changingSelection;

    [ObservableProperty]
    private string _search = string.Empty;

    [ObservableProperty]
    private string _manufacturerFilter = All;

    [ObservableProperty]
    private string _categoryFilter = All;

    [ObservableProperty]
    private object? _selectedNode;

    [ObservableProperty]
    private FixtureEditorViewModel? _editor;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _progress = string.Empty;

    /// <summary>Écrase un modèle déjà présent (même identifiant, ou même fabricant et nom) au lieu de l'ignorer (BIB-098).</summary>
    [ObservableProperty]
    private bool _overwriteExisting;

    [ObservableProperty]
    private decimal _testAddress = 1;

    [ObservableProperty]
    private string _testSheet = string.Empty;

    /// <summary>Crée l'écran.</summary>
    public LibraryViewModel(LuxiaRuntime runtime, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(dialogs);
        _runtime = runtime;
        _dialogs = dialogs;
        LiveTest = new FixtureFadersViewModel(runtime);
        LiveTest.NewBoundaryRequested += async (_, e) => await OnNewBoundaryRequestedAsync(e.ChannelKey, e.Value).ConfigureAwait(true);
        if (runtime.Library.Messages.Count > 0)
        {
            Message = string.Join(" ", runtime.Library.Messages);
        }

        Rebuild();
    }

    /// <summary>Arborescence filtrée.</summary>
    public ObservableCollection<ManufacturerNode> Groups { get; } = [];

    /// <summary>Fabricants (filtre).</summary>
    public ObservableCollection<string> Manufacturers { get; } = [];

    /// <summary>Catégories (filtre).</summary>
    public IReadOnlyList<string> CategoryNames { get; } = [All, .. Choices.Categories.Select(c => c.Label)];

    /// <summary>Rapport du dernier import (BIB-082).</summary>
    public ObservableCollection<string> ImportReport { get; } = [];

    /// <summary>Test en direct (composant de la console, CONS-060).</summary>
    public FixtureFadersViewModel LiveTest { get; }

    /// <summary>Dossier de la bibliothèque.</summary>
    public string Folder => _runtime.Library.Folder;

    /// <inheritdoc />
    public void Refresh() => LiveTest.Refresh();

    /// <summary>Ouvre un modèle par son identifiant (tests, démonstration).</summary>
    public void Open(Guid id)
    {
        var item = Groups.SelectMany(g => g.Items).FirstOrDefault(i => i.Entry.Fixture.Id == id);
        if (item is not null)
        {
            SelectedNode = item;
        }
    }

    partial void OnSearchChanged(string value) => Rebuild();

    partial void OnManufacturerFilterChanged(string value) => Rebuild();

    partial void OnCategoryFilterChanged(string value) => Rebuild();

    partial void OnSelectedNodeChanged(object? oldValue, object? newValue)
    {
        if (_changingSelection || newValue is not LibraryItemViewModel item)
        {
            return;
        }

        if (Editor is { IsDirty: true } editor && editor.Current.Id != item.Entry.Fixture.Id)
        {
            _ = ConfirmSwitchAsync(item, oldValue);
            return;
        }

        OpenEditor(item.Entry);
    }

    [RelayCommand]
    private void New()
    {
        LiveTest.Stop();
        Editor = new FixtureEditorViewModel(FixtureEdits.NewFixture(), isReadOnly: false);
        Message = "Nouveau modèle : renseignez le fabricant, le modèle, les canaux et les modes, puis « Enregistrer ».";
    }

    /// <summary>Duplique le modèle ouvert (BIB-010) : c'est aussi la façon de modifier un générique.</summary>
    [RelayCommand]
    private void Duplicate()
    {
        if (Editor is { } editor)
        {
            LiveTest.Stop();
            var copy = FixtureEdits.Derive(editor.Current);
            Editor = new FixtureEditorViewModel(copy, isReadOnly: false);
            Message = $"Copie de {editor.Current.DisplayName} : modifiez-la puis enregistrez.";
        }
    }

    /// <summary>Enregistre dans la bibliothèque, si le modèle n'a pas d'erreur (BIB-004).</summary>
    [RelayCommand]
    private void Save()
    {
        if (Editor is not { IsReadOnly: false } editor)
        {
            return;
        }

        var errors = FixtureValidator.Validate(editor.Current).Where(i => i.Severity == IssueSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            Message = $"Enregistrement impossible : {errors.Count} erreur(s). Voir l'onglet « Validation ».";
            editor.SelectedTabIndex = FixtureEditorViewModel.ValidationTabIndex;
            return;
        }

        var entry = _runtime.Library.Save(editor.Current);
        editor.MarkSaved(entry.Fixture);
        Rebuild();
        Select(entry.Fixture.Id);
        Message = string.Create(CultureInfo.CurrentCulture, $"{entry.Fixture.DisplayName} enregistré (version {entry.Fixture.Version}) : {entry.FilePath}");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Editor is not { IsReadOnly: false } editor)
        {
            return;
        }

        if (!await _dialogs.ConfirmAsync("Supprimer le modèle", $"Supprimer définitivement {editor.Current.DisplayName} de la bibliothèque ?").ConfigureAwait(true))
        {
            return;
        }

        LiveTest.Stop();
        _runtime.Library.Delete(editor.Current.Id);
        Editor = null;
        Rebuild();
        Message = $"{editor.Current.DisplayName} supprimé.";
    }

    /// <summary>
    /// « Nouvelle plage ici » (BIB-062) : demande le nom de la nouvelle plage tout de suite, en rappelant le canal,
    /// la valeur observée et la plage précédente qui vient d'être coupée (BIB-099), pendant que l'observation est fraîche.
    /// </summary>
    private async Task OnNewBoundaryRequestedAsync(string channelKey, int value)
    {
        if (Editor is not { IsReadOnly: false } editor)
        {
            return;
        }

        var channel = editor.Current.Channel(channelKey);
        var previous = channel?.CapabilityAt(value);
        var context = previous is { } p
            ? $"Canal « {channel!.Name} », valeur {value}. Plage précédente coupée ici : {p.Min}-{p.Max} « {p.Label} »."
            : $"Canal « {channel?.Name} », valeur {value}.";

        var name = await _dialogs.AskTextAsync("Nouvelle plage ici", $"{context}\nNom de la nouvelle plage :", "Nouvelle plage").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        editor.SplitChannelAt(channelKey, value, name.Trim());
    }

    [RelayCommand]
    private async Task ImportFilesAsync()
    {
        var files = await _dialogs.PickFilesAsync("Importer des modèles (OFL .json, QLC+ .qxf, modèles LuXia)", true, ".json", ".qxf").ConfigureAwait(true);
        if (files.Count > 0)
        {
            await ImportAsync(files).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task ImportFolderAsync()
    {
        var folder = await _dialogs.PickFolderAsync("Dossier à importer (tous les .json et .qxf, sous-dossiers compris)").ConfigureAwait(true);
        if (folder is not null)
        {
            await ImportAsync(FixtureImporter.FindFiles(folder)).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Importe des fichiers hors du fil de l'interface (GEN-109, BIB-083) puis les enregistre dans la bibliothèque.
    /// Un modèle déjà présent (même fabricant et même nom) n'est pas écrasé.
    /// </summary>
    public async Task ImportAsync(IReadOnlyList<string> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        IsBusy = true;
        ImportReport.Clear();
        var progress = new Progress<(int Done, int Total)>(p => Progress = string.Create(CultureInfo.CurrentCulture, $"Import : {p.Done} / {p.Total}"));
        var results = await Task.Run(() => FixtureImporter.ImportFiles(files, progress)).ConfigureAwait(true);

        var imported = 0;
        foreach (var result in results)
        {
            if (result.Fixture is not { } fixture)
            {
                ImportReport.Add($"✗ {result.Summary}");
                continue;
            }

            var existing = _runtime.Library.Entries.FirstOrDefault(e =>
                e.Fixture.Id == fixture.Id
                || (string.Equals(e.Fixture.Manufacturer, fixture.Manufacturer, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(e.Fixture.Model, fixture.Model, StringComparison.OrdinalIgnoreCase)));
            if (existing is not null && !OverwriteExisting)
            {
                ImportReport.Add($"= {fixture.DisplayName} : déjà dans la bibliothèque, non importé.");
                continue;
            }

            var errors = FixtureValidator.Validate(fixture).Count(i => i.Severity == IssueSeverity.Error);
            var toSave = existing is { IsBuiltIn: false } ? fixture with { Id = existing.Fixture.Id } : fixture;
            _runtime.Library.Save(toSave);
            imported++;
            var summary = existing is not null ? $"{fixture.DisplayName} remplacé" : result.Summary;
            ImportReport.Add($"✓ {summary}{(errors > 0 ? $" – {errors} erreur(s) de validation à corriger" : string.Empty)}");
            foreach (var note in result.Notes)
            {
                ImportReport.Add($"    · {note}");
            }
        }

        Rebuild();
        IsBusy = false;
        Progress = string.Empty;
        Message = string.Create(CultureInfo.CurrentCulture, $"Import terminé : {imported} modèle(s) ajouté(s) sur {results.Count} fichier(s).");
    }

    /// <summary>Démarre le test en direct du mode sélectionné à l'adresse choisie (BIB-060).</summary>
    [RelayCommand]
    private void StartLiveTest()
    {
        if (Editor?.SelectedMode is not { } mode || Editor.Current.Modes.Count <= mode.Index)
        {
            Message = "Choisissez un mode à tester.";
            return;
        }

        var fixtureMode = Editor.Current.Modes[mode.Index];
        var error = LiveTest.Start(Editor.Current, fixtureMode, (int)TestAddress);
        Message = error ?? "Test en direct : les canaux sont pris à leur valeur par défaut. « Arrêter » les libère.";
        TestSheet = error is null ? FixtureRules.SettingSheet(Editor.Current, fixtureMode, (int)TestAddress) : string.Empty;
    }

    [RelayCommand]
    private void StopLiveTest()
    {
        LiveTest.Stop();
        TestSheet = string.Empty;
        Message = "Test en direct arrêté : canaux libérés.";
    }

    private async Task ConfirmSwitchAsync(LibraryItemViewModel item, object? previous)
    {
        var target = item.Entry.Fixture.Id;
        var choice = await _dialogs.AskSaveAsync("Modifications non enregistrées", $"Enregistrer les modifications de {Editor!.Current.DisplayName} ?").ConfigureAwait(true);
        if (choice == SaveChoice.Save)
        {
            Save();
        }

        if (choice == SaveChoice.Discard || (choice == SaveChoice.Save && Editor is { IsDirty: false }))
        {
            _changingSelection = true;
            OpenEditor(Groups.SelectMany(g => g.Items).FirstOrDefault(i => i.Entry.Fixture.Id == target)?.Entry ?? item.Entry);
            _changingSelection = false;
            Select(target);
        }
        else
        {
            _changingSelection = true;
            SelectedNode = previous;
            _changingSelection = false;
        }
    }

    private void OpenEditor(LibraryEntry entry)
    {
        LiveTest.Stop();
        TestSheet = string.Empty;
        Editor = new FixtureEditorViewModel(entry.Fixture, entry.IsBuiltIn);
        Message = entry.IsBuiltIn ? "Générique livré avec l'application : « Dupliquer » pour le modifier." : null;
    }

    private void Select(Guid id)
    {
        _changingSelection = true;
        SelectedNode = Groups.SelectMany(g => g.Items).FirstOrDefault(i => i.Entry.Fixture.Id == id);
        _changingSelection = false;
    }

    private void Rebuild()
    {
        var entries = _runtime.Library.Entries;
        var manufacturer = ManufacturerFilter;
        Manufacturers.Clear();
        Manufacturers.Add(All);
        foreach (var name in entries.Select(e => e.Fixture.Manufacturer).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            Manufacturers.Add(name);
        }

        if (!Manufacturers.Contains(manufacturer))
        {
            manufacturer = All;
        }

        var search = Search.Trim();
        var filtered = entries.Where(e =>
            (manufacturer == All || string.Equals(e.Fixture.Manufacturer, manufacturer, StringComparison.OrdinalIgnoreCase))
            && (CategoryFilter == All || Choices.Label(e.Fixture.Category) == CategoryFilter)
            && (search.Length == 0
                || e.Fixture.DisplayName.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                || (e.Fixture.Reference?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)));

        // BIB-093 : une recherche ou un filtre actif déplie automatiquement les groupes qui contiennent un résultat.
        var filterActive = search.Length > 0 || manufacturer != All || CategoryFilter != All;

        Groups.Clear();
        foreach (var group in filtered.GroupBy(e => e.Fixture.Manufacturer, StringComparer.OrdinalIgnoreCase))
        {
            var node = new ManufacturerNode(group.Key) { IsExpanded = filterActive };
            foreach (var entry in group)
            {
                node.Items.Add(new LibraryItemViewModel(entry));
            }

            Groups.Add(node);
        }
    }
}
