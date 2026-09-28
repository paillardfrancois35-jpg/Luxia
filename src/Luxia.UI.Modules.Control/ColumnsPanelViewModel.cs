using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Luxia.Engine;
using Luxia.Engine.Model;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Panneau Colonnes (doc 60 §5-6, E2) : les couches en colonnes, toutes leurs scènes (même masquées du Live, grisées).
/// On y joue (grande zone) et on y choisit la scène à éditer (bande ✎). Clic droit : Éditer, Renommer, Dupliquer,
/// Couleur, Couche, Supprimer (§4.4, mêmes verbes partout).
/// </summary>
public sealed partial class ColumnsPanelViewModel : ViewModelBase
{
    /// <summary>Couleurs proposées au clic droit (celles des scènes du show de référence).</summary>
    public static IReadOnlyList<string> SceneColors { get; } =
        ["#F85149", "#F0883E", "#E3B341", "#FFC773", "#3FB950", "#39C5CF", "#58A6FF", "#1F6FEB", "#8957E5", "#DB61A2", "#FFFFFF", "#8B949E"];

    private readonly LuxiaRuntime _runtime;
    private readonly ControlSession _session;
    private readonly IDialogService _dialogs;

    /// <summary>Crée le panneau.</summary>
    public ColumnsPanelViewModel(LuxiaRuntime runtime, ControlSession session, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(dialogs);
        _runtime = runtime;
        _session = session;
        _dialogs = dialogs;
        runtime.Project.Changed += (_, _) => Rebuild();
        runtime.Project.ShowDataChanged += (_, _) => Rebuild();
        session.Changed += (_, _) =>
        {
            MarkEditTarget();
            OnPropertyChanged(nameof(CanEdit));
        };
        Rebuild();
    }

    /// <summary>L'édition des scènes est permise (pas de verrou soirée).</summary>
    public bool CanEdit => !_session.IsLocked;

    /// <summary>Colonnes de couches.</summary>
    public ObservableCollection<ControlColumnViewModel> Columns { get; } = [];

    /// <summary>Largeur minimale de la grille des colonnes (défilement horizontal en dessous).</summary>
    public double MinWidth => Columns.Count * 120;

    /// <summary>Dernier message (clic refusé…), ou nul.</summary>
    public string? Message { get; private set; }

    /// <summary>Met à jour l'état de lecture depuis le moteur de sortie.</summary>
    public void Refresh()
    {
        var snapshot = _runtime.Engine.Snapshot;
        foreach (var column in Columns)
        {
            var playing = false;
            var canStep = false;
            foreach (var button in column.Scenes)
            {
                var playback = snapshot.ActivePlayback(button.Scene.Id);
                if (!button.WaitingForEngine(_runtime.Engine.TickCount))
                {
                    button.IsActive = playback is not null;
                }

                playing |= button.IsActive;
                button.Progress = playback?.StepProgress ?? 0;
                button.ShowsProgress = playback is { StepCount: > 1 };
                canStep |= button.ShowsProgress;
                button.State = playback is { StepCount: > 1 } p
                    ? string.Create(CultureInfo.CurrentCulture, $"étape {p.StepIndex + 1} / {p.StepCount}")
                    : playback is not null ? "joue" : string.Empty;
            }

            column.IsPlaying = playing;
            column.CanStep = canStep;
            var index = snapshot.Show.IndexOfLayer(column.Layer.Id);
            if (index >= 0 && index < snapshot.LayerMasters.Length)
            {
                column.SyncMaster(Math.Round(snapshot.LayerMasters[index] * 100));
            }
        }
    }

    /// <summary>Appui sur la grande zone (LIVE-003) : lancer, arrêter, ou flash maintenu dans une couche Flash.</summary>
    public void Press(ControlSceneViewModel scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        _runtime.TraceUi("Contrôle", $"appui « {scene.Name} »{(scene.IsActive ? " (affichée active)" : string.Empty)}");
        if (scene.Column.IsFlash)
        {
            _runtime.Engine.Send(new FlashSceneCommand(CommandOrigin.User, scene.Scene.Id, Pressed: true));
            return;
        }

        // Le moteur tranche « lancer ou arrêter » (course écran / moteur, doc 03 §11).
        var toggle = _runtime.Project.Live.ActiveSceneClick == ActiveSceneClick.Stop;
        _runtime.Engine.Send(new LaunchSceneCommand(CommandOrigin.User, scene.Scene.Id, StopIfPlaying: toggle));
        scene.ExpectActive(!toggle || !scene.IsActive, _runtime.Engine.TickCount);
    }

    /// <summary>Relâche : fin du flash pour une couche Flash.</summary>
    public void Release(ControlSceneViewModel scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (scene.Column.IsFlash)
        {
            _runtime.Engine.Send(new FlashSceneCommand(CommandOrigin.User, scene.Scene.Id, Pressed: false));
        }
    }

    /// <summary>Bande ✎ : choisit la scène à éditer (un second clic la libère).</summary>
    [RelayCommand]
    public void ChooseForEdit(ControlSceneViewModel? scene)
    {
        if (scene is null)
        {
            return;
        }

        _runtime.TraceUi("Contrôle", $"édition de « {scene.Name} »");
        _session.ChooseScene(_session.EditScene?.Id == scene.Scene.Id ? null : scene.Scene.Id);
    }

    /// <summary>Arrête la couche (CMD-012).</summary>
    [RelayCommand]
    private void StopLayer(ControlColumnViewModel? column)
    {
        if (column is not null)
        {
            _runtime.Engine.Send(new StopLayerCommand(CommandOrigin.User, column.Layer.Id));
        }
    }

    /// <summary>Étape précédente de la scène qui joue dans la couche (CMD-015).</summary>
    [RelayCommand]
    private void PreviousStep(ControlColumnViewModel? column) => StepLayer(column, StepDirection.Previous);

    /// <summary>Étape suivante de la scène qui joue dans la couche (CMD-015).</summary>
    [RelayCommand]
    private void NextStep(ControlColumnViewModel? column) => StepLayer(column, StepDirection.Next);

    /// <summary>Nouvelle scène dans la couche, choisie aussitôt pour l'édition.</summary>
    [RelayCommand]
    private async Task NewSceneAsync(ControlColumnViewModel? column)
    {
        if (column is null || _runtime.Project.Folder is null)
        {
            return;
        }

        var name = await _dialogs.AskTextAsync("Nouvelle scène", $"Nom de la scène (couche « {column.Layer.Name} ») :").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var scene = new Scene { Name = name.Trim(), LayerId = column.Layer.Id, Color = column.Layer.Color };
        _session.ChangeScenes(set => set with { Scenes = [.. set.Scenes, scene] }, "Nouvelle scène");
        _session.ChooseScene(scene.Id);
    }

    /// <summary>Renomme une scène.</summary>
    [RelayCommand]
    private async Task RenameAsync(ControlSceneViewModel? scene)
    {
        if (scene is null)
        {
            return;
        }

        var name = await _dialogs.AskTextAsync("Renommer la scène", "Nouveau nom :", scene.Name).ConfigureAwait(true);
        if (!string.IsNullOrWhiteSpace(name) && name.Trim() != scene.Name)
        {
            Change(scene.Scene.Id, s => s with { Name = name.Trim() }, "Renommer la scène");
        }
    }

    /// <summary>Duplique une scène, juste après elle.</summary>
    [RelayCommand]
    private void Duplicate(ControlSceneViewModel? scene)
    {
        if (scene is null)
        {
            return;
        }

        var copy = scene.Scene with { Id = Guid.NewGuid(), Name = $"{scene.Name} (copie)" };
        _session.ChangeScenes(
            set =>
            {
                var list = set.Scenes.ToList();
                list.Insert(list.FindIndex(s => s.Id == scene.Scene.Id) + 1, copy);
                return set with { Scenes = list };
            },
            "Dupliquer la scène");
    }

    /// <summary>Change la couleur d'une scène (paramètre : « id|#RRGGBB »).</summary>
    [RelayCommand]
    private void SetColor(string? request)
    {
        if (request?.Split('|') is [var id, var color] && Guid.TryParse(id, out var sceneId))
        {
            Change(sceneId, s => s with { Color = color }, "Couleur de la scène");
        }
    }

    /// <summary>Déplace une scène dans une autre couche (paramètre : « idScène|idCouche », COU-002).</summary>
    [RelayCommand]
    private void MoveToLayer(string? request)
    {
        if (request?.Split('|') is [var id, var layer] && Guid.TryParse(id, out var sceneId) && Guid.TryParse(layer, out var layerId))
        {
            Change(sceneId, s => s with { LayerId = layerId }, "Changer de couche");
        }
    }

    /// <summary>Montre ou masque une scène dans l'écran Live.</summary>
    [RelayCommand]
    private void ToggleVisibleInLive(ControlSceneViewModel? scene)
    {
        if (scene is not null)
        {
            Change(scene.Scene.Id, s => s with { VisibleInLive = !s.VisibleInLive }, scene.HiddenInLive ? "Montrer dans le Live" : "Masquer du Live");
        }
    }

    /// <summary>Supprime une scène, après le rapport de ses utilisations (SCN-013, GEN-103) ; annulable.</summary>
    [RelayCommand]
    private async Task DeleteAsync(ControlSceneViewModel? scene)
    {
        if (scene is null)
        {
            return;
        }

        var usages = SceneUsage.SceneUsages(_runtime.Project.Scenes, scene.Scene.Id);
        var text = usages.Count == 0
            ? $"Supprimer la scène « {scene.Name} » ? (Ctrl+Z pour la retrouver)"
            : $"La scène « {scene.Name} » est utilisée par :{Environment.NewLine}{string.Join(Environment.NewLine, usages)}{Environment.NewLine}{Environment.NewLine}La supprimer quand même ? (ces enchaînements s'arrêteront à la place ; Ctrl+Z pour la retrouver)";
        if (!await _dialogs.ConfirmAsync("Supprimer la scène", text).ConfigureAwait(true))
        {
            return;
        }

        _runtime.Engine.Send(new StopSceneCommand(CommandOrigin.User, scene.Scene.Id));
        _session.ChangeScenes(set => set with { Scenes = [.. set.Scenes.Where(s => s.Id != scene.Scene.Id)] }, "Supprimer la scène");
    }

    /// <summary>Couches proposées pour « Changer de couche ».</summary>
    public IReadOnlyList<Layer> Layers => [.. _runtime.Project.Layers.Layers.OrderBy(l => l.Priority)];

    private void Change(Guid sceneId, Func<Scene, Scene> change, string description)
    {
        // La scène éditée passe par la session (geste en cours compris) ; les autres, par l'ensemble des scènes.
        if (_session.EditScene?.Id == sceneId)
        {
            _session.UpdateScene(change, description);
            _session.Commit();
        }
        else
        {
            _session.ChangeScenes(set => set with { Scenes = [.. set.Scenes.Select(s => s.Id == sceneId ? change(s) : s)] }, description);
        }
    }

    private void StepLayer(ControlColumnViewModel? column, StepDirection direction)
    {
        if (column?.Scenes.FirstOrDefault(s => s.IsActive) is { } playing)
        {
            _runtime.Engine.Send(new StepSceneCommand(CommandOrigin.User, playing.Scene.Id, direction));
        }
    }

    private void Rebuild()
    {
        var project = _runtime.Project;
        Columns.Clear();
        foreach (var layer in project.Layers.Layers.OrderBy(l => l.Priority).Where(l => !project.Live.HiddenLayerIds.Contains(l.Id)))
        {
            var column = new ControlColumnViewModel(layer, (c, value) => _runtime.Engine.Send(new SetLayerMasterCommand(CommandOrigin.User, c.Layer.Id, value / 100)));
            foreach (var scene in project.Scenes.Scenes.Where(s => s.LayerId == layer.Id))
            {
                column.Scenes.Add(new ControlSceneViewModel(scene, column));
            }

            Columns.Add(column);
        }

        OnPropertyChanged(nameof(MinWidth));
        OnPropertyChanged(nameof(Layers));
        MarkEditTarget();
        Refresh();
    }

    private void MarkEditTarget()
    {
        var edited = _session.EditScene?.Id;
        var color = _session.Mode switch
        {
            EditMode.Edit => ControlColors.Edit,
            EditMode.Blind => ControlColors.Blind,
            _ => ControlColors.Accent,
        };
        foreach (var button in Columns.SelectMany(c => c.Scenes))
        {
            button.IsEditTarget = button.Scene.Id == edited;
            button.EditColor = color;
        }
    }
}
