using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Panneau Looks (doc 60 §4.8, F1, ERG-023) : un look est une liste nommée d'actions (lancer, arrêter, masters)
/// appelée d'un clic — « Temps mort », « Retour de la piste »… On le crée en capturant ce qui joue. Les mêmes looks
/// seront les boutons d'intervention du pilote automatique (F10, P10).
/// </summary>
public sealed partial class LooksPanelViewModel : ViewModelBase
{
    private readonly LuxiaRuntime _runtime;
    private readonly ControlSession _session;
    private readonly IDialogService _dialogs;
    private readonly JournalPanelViewModel _journal;

    [ObservableProperty]
    private string? _message;

    /// <summary>Crée le panneau.</summary>
    public LooksPanelViewModel(LuxiaRuntime runtime, ControlSession session, IDialogService dialogs, JournalPanelViewModel journal)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(journal);
        _runtime = runtime;
        _session = session;
        _dialogs = dialogs;
        _journal = journal;
        runtime.Project.Changed += (_, _) => Rebuild();
        runtime.Project.ShowDataChanged += (_, _) => Rebuild();
        session.Changed += (_, _) => OnPropertyChanged(nameof(CanEdit));
        Rebuild();
    }

    /// <summary>Looks du projet.</summary>
    public ObservableCollection<LookButtonViewModel> Looks { get; } = [];

    /// <summary>Aucun look pour l'instant.</summary>
    public bool IsEmpty => Looks.Count == 0;

    /// <summary>Créer, modifier, supprimer est permis (pas de verrou soirée).</summary>
    public bool CanEdit => !_session.IsLocked;

    /// <summary>Joue un look : ses actions partent au moteur, dans l'ordre (origine Utilisateur).</summary>
    [RelayCommand]
    public void Play(LookButtonViewModel? look)
    {
        if (look is null)
        {
            return;
        }

        foreach (var command in LookRules.Commands(look.Look, CommandOrigin.User))
        {
            _runtime.Engine.Send(command);
        }

        _runtime.TraceUi("Contrôle", $"look « {look.Name} »");
        _journal.Log($"✦ look « {look.Name} »");
    }

    /// <summary>Crée un look qui refait ce qui joue maintenant.</summary>
    [RelayCommand]
    private async Task CaptureAsync()
    {
        if (!CanEdit || _runtime.Project.Folder is null)
        {
            Message = CanEdit ? null : ControlSession.LockedReason;
            return;
        }

        var name = await _dialogs.AskTextAsync("Nouveau look", "Nom du look (il refera ce qui joue maintenant : scènes, masters) :").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var look = LookRules.Capture(name.Trim(), _runtime.Engine.Snapshot);
        Save(set => set with { Looks = [.. set.Looks, look] });
        _journal.Log($"✦ look créé : « {look.Name} » ({look.Actions.Count} action(s))");
    }

    /// <summary>Remplace les actions d'un look par ce qui joue maintenant.</summary>
    [RelayCommand]
    private async Task UpdateFromStateAsync(LookButtonViewModel? look)
    {
        if (look is null || !CanEdit)
        {
            return;
        }

        if (!await _dialogs.ConfirmAsync("Mettre à jour le look", $"Remplacer les actions de « {look.Name} » par ce qui joue maintenant ?").ConfigureAwait(true))
        {
            return;
        }

        var captured = LookRules.Capture(look.Name, _runtime.Engine.Snapshot);
        Save(set => set with { Looks = [.. set.Looks.Select(l => l.Id == look.Look.Id ? l with { Actions = captured.Actions } : l)] });
    }

    /// <summary>Renomme un look.</summary>
    [RelayCommand]
    private async Task RenameAsync(LookButtonViewModel? look)
    {
        if (look is null || !CanEdit)
        {
            return;
        }

        var name = await _dialogs.AskTextAsync("Renommer le look", "Nouveau nom :", look.Name).ConfigureAwait(true);
        if (!string.IsNullOrWhiteSpace(name))
        {
            Save(set => set with { Looks = [.. set.Looks.Select(l => l.Id == look.Look.Id ? l with { Name = name.Trim() } : l)] });
        }
    }

    /// <summary>Change la couleur d'un look (paramètre : « id|#RRGGBB »).</summary>
    [RelayCommand]
    private void SetColor(string? request)
    {
        if (CanEdit && request?.Split('|') is [var id, var color] && Guid.TryParse(id, out var lookId))
        {
            Save(set => set with { Looks = [.. set.Looks.Select(l => l.Id == lookId ? l with { Color = color } : l)] });
        }
    }

    /// <summary>Supprime un look, après confirmation (GEN-103).</summary>
    [RelayCommand]
    private async Task DeleteAsync(LookButtonViewModel? look)
    {
        if (look is null || !CanEdit)
        {
            return;
        }

        if (await _dialogs.ConfirmAsync("Supprimer le look", $"Supprimer le look « {look.Name} » ? (les scènes ne sont pas touchées)").ConfigureAwait(true))
        {
            Save(set => set with { Looks = [.. set.Looks.Where(l => l.Id != look.Look.Id)] });
        }
    }

    private void Save(Func<LookSet, LookSet> change)
    {
        try
        {
            _runtime.Project.SaveLooks(change(_runtime.Project.Looks));
            Message = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Message = $"Enregistrement des looks impossible pour l'instant ({ex.Message}).";
        }
    }

    private void Rebuild()
    {
        var project = _runtime.Project;
        Looks.Clear();
        foreach (var look in project.Looks.Looks)
        {
            Looks.Add(new LookButtonViewModel(look, [.. look.Actions.Select(a => LookRules.Describe(a, project.Scenes, project.Layers))]));
        }

        OnPropertyChanged(nameof(IsEmpty));
    }
}
