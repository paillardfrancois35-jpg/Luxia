using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Engine;
using Luxia.Engine.Model;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Scenes;

/// <summary>
/// Écran « Scènes » (doc 16) : liste des scènes (SCN-001, SCN-012), lecture (CMD-010, 011, 012), éditeur et
/// programmeur (doc 16 §4), palettes (doc 17 §2). Annuler / rétablir sur toutes les scènes (SCN-039).
/// </summary>
public sealed partial class ScenesViewModel : ViewModelBase, IRefreshable
{
    private const string AllLabel = "Toutes";

    private readonly LuxiaRuntime _runtime;
    private readonly IDialogService _dialogs;
    private readonly UndoHistory<SceneSet> _history = new();

    [ObservableProperty]
    private bool _hasProject;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private string _filter = string.Empty;

    [ObservableProperty]
    private string _categoryFilter = AllLabel;

    [ObservableProperty]
    private Choice<Guid?>? _layerFilter;

    [ObservableProperty]
    private bool _onlyVisible;

    [ObservableProperty]
    private SceneRowViewModel? _selectedScene;

    [ObservableProperty]
    private string _undoText = "Annuler";

    [ObservableProperty]
    private string _redoText = "Rétablir";

    /// <summary>Crée l'écran.</summary>
    public ScenesViewModel(LuxiaRuntime runtime, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(dialogs);
        _runtime = runtime;
        _dialogs = dialogs;
        Programmer = new ProgrammerViewModel(runtime);
        Editor = new SceneEditorViewModel(runtime, this, Programmer);
        Palettes = new PalettesViewModel(runtime, dialogs, Programmer);
        runtime.Project.Changed += (_, _) =>
        {
            _history.Clear();
            Programmer.ReleasePushed();
            Programmer.Clear();
            ReloadAll();
        };
        runtime.Show.Compiled += (_, _) => Programmer.ReloadPatch();
        ReloadAll();
    }

    /// <summary>Programmeur.</summary>
    public ProgrammerViewModel Programmer { get; }

    /// <summary>Éditeur de la scène sélectionnée.</summary>
    public SceneEditorViewModel Editor { get; }

    /// <summary>Palettes.</summary>
    public PalettesViewModel Palettes { get; }

    /// <summary>Scènes affichées (filtrées).</summary>
    public ObservableCollection<SceneRowViewModel> Scenes { get; } = [];

    /// <summary>Catégories pour le filtre.</summary>
    public ObservableCollection<string> Categories { get; } = [];

    /// <summary>Couches pour le filtre.</summary>
    public ObservableCollection<Choice<Guid?>> LayerFilters { get; } = [];

    /// <summary>Contenu à surveiller en arrière-plan : aucun (les surcharges du programmeur restent posées).</summary>
    public bool NeedsBackgroundRefresh => false;

    /// <inheritdoc />
    public void Refresh()
    {
        var snapshot = Programmer.TargetEngine.Snapshot;
        foreach (var row in Scenes)
        {
            var playback = snapshot.Playbacks.Where(p => p.SceneId == row.Id).Select(p => (PlaybackInfo?)p).LastOrDefault();
            row.IsPlaying = playback is not null;
            row.PlayState = playback is { } info
                ? string.Create(CultureInfo.CurrentCulture, $"{StateLabel(info.State)} · étape {info.StepIndex + 1}/{info.StepCount}")
                : string.Empty;
        }

        var current = Editor.Scene is { } scene ? snapshot.Playbacks.Where(p => p.SceneId == scene.Id).Select(p => (int?)p.StepIndex).LastOrDefault() : null;
        Editor.ShowPlayingStep(current);
        Programmer.Refresh();
    }

    /// <summary>Applique une modification à une scène, l'enregistre et la garde dans l'historique (SCN-039).</summary>
    public void UpdateScene(Guid sceneId, Func<Scene, Scene> change, string description)
    {
        ArgumentNullException.ThrowIfNull(change);
        var before = _runtime.Project.Scenes;
        var after = before with { Scenes = [.. before.Scenes.Select(s => s.Id == sceneId ? change(s) : s)] };
        Commit(before, after, description);
    }

    /// <summary>Nouvelle scène vide dans la couche Couleurs (SCN-001).</summary>
    [RelayCommand]
    private async Task NewSceneAsync()
    {
        if (_runtime.Project.Folder is null)
        {
            return;
        }

        var name = await _dialogs.AskTextAsync("Nouvelle scène", "Nom de la scène :").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var layers = _runtime.Project.Layers.Layers;
        var layer = layers.FirstOrDefault(l => l.Id == LayerSet.ColorsLayerId) ?? (layers.Count > 0 ? layers[0] : null);
        var scene = new Scene { Name = name.Trim(), LayerId = layer?.Id ?? LayerSet.ColorsLayerId, Category = CategoryFilter == AllLabel ? null : CategoryFilter };
        var before = _runtime.Project.Scenes;
        Commit(before, before with { Scenes = [.. before.Scenes, scene] }, "Nouvelle scène");
        SelectedScene = Scenes.FirstOrDefault(s => s.Id == scene.Id);
    }

    /// <summary>Duplique la scène sélectionnée (SCN-001).</summary>
    [RelayCommand]
    private void DuplicateScene()
    {
        if (SelectedScene?.Scene is not { } source)
        {
            return;
        }

        var copy = source with { Id = Guid.NewGuid(), Name = $"{source.Name} (copie)" };
        var before = _runtime.Project.Scenes;
        var list = before.Scenes.ToList();
        list.Insert(list.FindIndex(s => s.Id == source.Id) + 1, copy);
        Commit(before, before with { Scenes = list }, "Dupliquer la scène");
        SelectedScene = Scenes.FirstOrDefault(s => s.Id == copy.Id);
    }

    /// <summary>Supprime la scène sélectionnée, après le rapport de ses utilisations (SCN-013, GEN-103).</summary>
    [RelayCommand]
    private async Task DeleteSceneAsync()
    {
        if (SelectedScene?.Scene is not { } scene)
        {
            return;
        }

        var usages = SceneUsage.SceneUsages(_runtime.Project.Scenes, scene.Id);
        var text = usages.Count == 0
            ? $"Supprimer la scène « {scene.Name} » ? Elle n'est utilisée nulle part."
            : $"La scène « {scene.Name} » est utilisée par :{Environment.NewLine}{string.Join(Environment.NewLine, usages)}{Environment.NewLine}{Environment.NewLine}La supprimer quand même ? (ces enchaînements s'arrêteront à la place)";
        if (!await _dialogs.ConfirmAsync("Supprimer la scène", text).ConfigureAwait(true))
        {
            return;
        }

        _runtime.Engine.Send(new StopSceneCommand(CommandOrigin.User, scene.Id));
        var before = _runtime.Project.Scenes;
        Commit(before, before with { Scenes = [.. before.Scenes.Where(s => s.Id != scene.Id)] }, "Supprimer la scène");
    }

    /// <summary>Lance une scène depuis la liste (CMD-010).</summary>
    [RelayCommand]
    private void Launch(SceneRowViewModel? row)
    {
        if (row is not null)
        {
            Programmer.TargetEngine.Send(new LaunchSceneCommand(CommandOrigin.User, row.Id));
        }
    }

    /// <summary>Arrête une scène avec son fondu de sortie (CMD-011).</summary>
    [RelayCommand]
    private void Stop(SceneRowViewModel? row)
    {
        if (row is not null)
        {
            Programmer.TargetEngine.Send(new StopSceneCommand(CommandOrigin.User, row.Id));
        }
    }

    /// <summary>Arrête toutes les scènes (CMD-012 sur toutes les couches).</summary>
    [RelayCommand]
    private void StopAll() => Programmer.TargetEngine.Send(new StopLayerCommand(CommandOrigin.User));

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        var current = _runtime.Project.Scenes;
        if (_history.Undo(current) is { } previous)
        {
            Save(previous);
        }
    }

    private bool CanUndo() => _history.CanUndo;

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        var current = _runtime.Project.Scenes;
        if (_history.Redo(current) is { } next)
        {
            Save(next);
        }
    }

    private bool CanRedo() => _history.CanRedo;

    partial void OnFilterChanged(string value) => RebuildList();

    partial void OnCategoryFilterChanged(string value) => RebuildList();

    partial void OnLayerFilterChanged(Choice<Guid?>? value) => RebuildList();

    partial void OnOnlyVisibleChanged(bool value) => RebuildList();

    partial void OnSelectedSceneChanged(SceneRowViewModel? value) => Editor.Load(value?.Scene);

    private void Commit(SceneSet before, SceneSet after, string description)
    {
        _history.Record(before, description);
        Save(after);
    }

    private void Save(SceneSet scenes)
    {
        _runtime.Project.SaveScenes(scenes);
        RebuildList();
        UndoText = _history.UndoDescription is { } undo ? $"Annuler : {undo}" : "Annuler";
        RedoText = _history.RedoDescription is { } redo ? $"Rétablir : {redo}" : "Rétablir";
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private void ReloadAll()
    {
        HasProject = _runtime.Project.Folder is not null;
        Programmer.ReloadPatch();
        Palettes.Reload();
        LayerFilters.Clear();
        LayerFilters.Add(new Choice<Guid?>(null, AllLabel));
        foreach (var layer in _runtime.Project.Layers.Layers)
        {
            LayerFilters.Add(new Choice<Guid?>(layer.Id, layer.Name));
        }

        LayerFilter = LayerFilters[0];
        RebuildList();
    }

    /// <summary>Recalcule la liste filtrée (GEN-105) en gardant la scène sélectionnée.</summary>
    private void RebuildList()
    {
        var selectedId = SelectedScene?.Id;
        var layers = _runtime.Project.Layers.Layers.ToDictionary(l => l.Id, l => l.Name);
        var all = _runtime.Project.Scenes.Scenes;

        var categories = all.Select(s => s.Category).OfType<string>().Distinct().Order(StringComparer.CurrentCulture).ToList();
        if (!Categories.SequenceEqual(categories.Prepend(AllLabel)))
        {
            var keep = CategoryFilter;
            Categories.Clear();
            Categories.Add(AllLabel);
            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            CategoryFilter = Categories.Contains(keep) ? keep : AllLabel;
        }

        var visible = all.Where(s =>
            (Filter.Length == 0 || s.Name.Contains(Filter, StringComparison.CurrentCultureIgnoreCase))
            && (CategoryFilter == AllLabel || s.Category == CategoryFilter)
            && (LayerFilter?.Value is not { } layer || s.LayerId == layer)
            && (!OnlyVisible || s.VisibleInLive));

        Scenes.Clear();
        foreach (var scene in visible)
        {
            Scenes.Add(new SceneRowViewModel(scene, layers.GetValueOrDefault(scene.LayerId, "(couche inconnue)")));
        }

        SelectedScene = Scenes.FirstOrDefault(s => s.Id == selectedId);

        Message = HasProject ? null : "Ouvrez ou créez un projet (menu Projet) pour créer des scènes.";
    }

    private static string StateLabel(PlaybackState state) => state switch
    {
        PlaybackState.FadingIn => "▶ entrée",
        PlaybackState.FadingOut => "■ sortie",
        _ => "▶ en cours",
    };
}
