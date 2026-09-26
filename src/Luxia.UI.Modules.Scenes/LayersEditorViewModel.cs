using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.Scenes.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Scenes;

/// <summary>
/// Éditeur de couches (COU-001) : créer, renommer, réordonner (l'ordre donne les priorités), supprimer ; propriétés du
/// doc 17 §1.2. Les modifications ne sont enregistrées qu'à « Enregistrer ».
/// </summary>
public sealed partial class LayersEditorViewModel : ViewModelBase
{
    private readonly LuxiaRuntime _runtime;
    private readonly IDialogService _dialogs;

    [ObservableProperty]
    private LayerRowViewModel? _selected;

    [ObservableProperty]
    private string? _message;

    /// <summary>Charge les couches du projet ouvert.</summary>
    public LayersEditorViewModel(LuxiaRuntime runtime, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(dialogs);
        _runtime = runtime;
        _dialogs = dialogs;
        var scenes = runtime.Project.Scenes.Scenes;
        foreach (var layer in runtime.Project.Layers.Layers.OrderBy(l => l.Priority))
        {
            Layers.Add(Row(layer, scenes));
        }

        Selected = Layers.FirstOrDefault();
    }

    /// <summary>Couches, de la priorité la plus basse (en haut) à la plus haute.</summary>
    public ObservableCollection<LayerRowViewModel> Layers { get; } = [];

    /// <summary>Levé après enregistrement ou abandon : la fenêtre se ferme.</summary>
    public event EventHandler? Closed;

    /// <summary>Enregistré au moins une fois.</summary>
    public bool Saved { get; private set; }

    [RelayCommand]
    private async Task AddAsync()
    {
        var name = await _dialogs.AskTextAsync("Nouvelle couche", "Nom de la couche :").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var row = Row(new Layer { Name = name.Trim() }, []);
        Layers.Add(row);
        Selected = row;
    }

    [RelayCommand]
    private void MoveUp() => Move(-1);

    [RelayCommand]
    private void MoveDown() => Move(1);

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Selected is not { } row)
        {
            return;
        }

        if (row.SceneCount > 0)
        {
            // Une scène appartient toujours à une couche (COU-002) : on ne laisse pas de scène orpheline.
            await _dialogs.ShowInfoAsync(
                "Supprimer la couche",
                $"La couche « {row.Name} » contient {row.ScenesLabel}. Déplacez-les d'abord vers une autre couche (réglage « Couche » de la scène).")
                .ConfigureAwait(true);
            return;
        }

        if (Layers.Count <= 1)
        {
            Message = "Il faut au moins une couche.";
            return;
        }

        var index = Layers.IndexOf(row);
        Layers.Remove(row);
        Selected = Layers[Math.Min(index, Layers.Count - 1)];
    }

    [RelayCommand]
    private void Save()
    {
        if (_runtime.Project.Folder is null)
        {
            return;
        }

        var layers = Layers.Select((row, index) => row.ToLayer(index + 1)).ToList();
        _runtime.Project.SaveLayers(_runtime.Project.Layers with { Layers = layers });
        Saved = true;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => Closed?.Invoke(this, EventArgs.Empty);

    private void Move(int delta)
    {
        if (Selected is not { } row)
        {
            return;
        }

        var index = Layers.IndexOf(row);
        var target = index + delta;
        if (target < 0 || target >= Layers.Count)
        {
            return;
        }

        Layers.Move(index, target);
        Selected = row;
    }

    private static LayerRowViewModel Row(Layer layer, IReadOnlyList<Scene> scenes)
    {
        var own = scenes.Where(s => s.LayerId == layer.Id).ToList();
        List<Choice<Guid?>> rest = [new(null, "Aucune"), .. own.Select(s => new Choice<Guid?>(s.Id, s.Name))];
        return new LayerRowViewModel(layer, rest, own.Count);
    }
}
