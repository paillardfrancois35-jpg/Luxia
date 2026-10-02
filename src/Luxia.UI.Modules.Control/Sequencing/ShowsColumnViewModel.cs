using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Show.Model;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control.Sequencing;

/// <summary>
/// Colonne « Shows » de l'écran de jeu (Q44 solution C, maquette 9 ; SHOW-006, LIVE-023) : les shows puis les séquences du projet,
/// avec les mêmes gestes que les scènes — un clic lance ou arrête, la bande ✎ ouvre la fenêtre d'édition, clic droit : Renommer,
/// Dupliquer, Supprimer.
/// </summary>
public sealed partial class ShowsColumnViewModel : ViewModelBase
{
    private readonly LuxiaRuntime _runtime;
    private readonly IDialogService _dialogs;

    /// <summary>Crée la colonne.</summary>
    public ShowsColumnViewModel(LuxiaRuntime runtime, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(dialogs);
        _runtime = runtime;
        _dialogs = dialogs;
        Rebuild();
    }

    /// <summary>Shows du projet.</summary>
    public ObservableCollection<ShowItemViewModel> Shows { get; } = [];

    /// <summary>Séquences du projet.</summary>
    public ObservableCollection<ShowItemViewModel> Sequences { get; } = [];

    /// <summary>Show ou séquence ouvert dans sa fenêtre d'édition (contour), fourni par l'écran de jeu.</summary>
    public Func<Guid?>? Edited { get; set; }

    /// <summary>La bande ✎ d'un show est cliquée.</summary>
    public event EventHandler<Guid>? EditShowRequested;

    /// <summary>La bande ✎ d'une séquence est cliquée.</summary>
    public event EventHandler<Guid>? EditSequenceRequested;

    /// <summary>Message à montrer (refus), ou nul.</summary>
    public event EventHandler<string>? MessageRaised;

    /// <summary>Refait la liste (projet ouvert ou modifié).</summary>
    public void Rebuild()
    {
        Shows.Clear();
        Sequences.Clear();
        foreach (var show in _runtime.Project.Shows.Shows)
        {
            Shows.Add(new ShowItemViewModel(show.Id, true, show.Name, show.Color, show.Secondary ? "secondaire" : null));
        }

        foreach (var sequence in _runtime.Project.Sequences.Sequences)
        {
            Sequences.Add(new ShowItemViewModel(sequence.Id, false, sequence.Name, sequence.Color, string.Create(CultureInfo.CurrentCulture, $"{sequence.Bars:0.##} mesures{(sequence.End == SequenceEnd.Loop ? ", en boucle" : string.Empty)}")));
        }

        Refresh();
    }

    /// <summary>Relit ce qui joue (état du séquenceur).</summary>
    public void Refresh()
    {
        var state = _runtime.Sequencer.State;
        var edited = Edited?.Invoke();
        foreach (var item in Shows)
        {
            var status = state.Shows.FirstOrDefault(s => s.ShowId == item.Id);
            item.IsActive = status is not null;
            item.State = status is null ? item.Detail ?? string.Empty
                : status.Ended ? "terminé : tient sa dernière étape"
                : $"étape {string.Join(", ", status.ActiveSteps.Select(s => string.IsNullOrWhiteSpace(s.Name) ? s.Id : s.Name))}";
            item.IsEditTarget = item.Id == edited;
        }

        foreach (var item in Sequences)
        {
            var status = state.Sequences.FirstOrDefault(s => s.SequenceId == item.Id);
            item.IsActive = status is not null;
            item.Progress = status is { Bars: > 0 } s ? Math.Clamp(s.PositionBars / s.Bars, 0, 1) : 0;
            item.State = status is null ? item.Detail ?? string.Empty
                : status.PositionBars < 0 ? "⏳ attend la mesure"
                : string.Create(CultureInfo.CurrentCulture, $"mesure {Math.Floor(status.PositionBars) + 1} / {status.Bars:0.##}{(status.OwnerShowId is not null ? " (lancée par un show)" : string.Empty)}");
            item.IsEditTarget = item.Id == edited;
        }
    }

    /// <summary>Clic : lancer ou arrêter (bascule tranchée par le séquenceur).</summary>
    public void Press(ShowItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _runtime.TraceUi("Contrôle", $"appui « {item.Name} » ({(item.IsShow ? "show" : "séquence")})");
        _runtime.Engine.Send(item.IsShow
            ? new LaunchShowCommand(CommandOrigin.User, item.Id, StopIfPlaying: true)
            : new LaunchSequenceCommand(CommandOrigin.User, item.Id, StopIfPlaying: true));
    }

    /// <summary>TEMPORAIRE (essai P8, exemple 13) : la trace de mise au point des shows enregistre.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TraceLabel))]
    public partial bool IsTracing { get; set; }

    /// <summary>TEMPORAIRE (essai P8, exemple 13) : texte du bouton de la trace.</summary>
    public string TraceLabel => IsTracing ? "■ Arrêter la trace" : "● Démarrer la trace";

    /// <summary>TEMPORAIRE (essai P8, exemple 13) : démarre ou arrête la trace (<see cref="ShowTrace"/>), à retirer après l'essai.</summary>
    [RelayCommand]
    private void ToggleTrace()
    {
        if (_runtime.Trace.IsRunning)
        {
            _runtime.Trace.Stop();
            MessageRaised?.Invoke(this, $"Trace arrêtée : {_runtime.Trace.FilePath}");
        }
        else
        {
            MessageRaised?.Invoke(this, $"Trace en cours : {_runtime.Trace.Start()}");
        }

        IsTracing = _runtime.Trace.IsRunning;
    }

    /// <summary>Bande ✎ : ouvrir la fenêtre d'édition.</summary>
    [RelayCommand]
    public void Edit(ShowItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        if (item.IsShow)
        {
            EditShowRequested?.Invoke(this, item.Id);
        }
        else
        {
            EditSequenceRequested?.Invoke(this, item.Id);
        }
    }

    /// <summary>Nouveau show, ouvert aussitôt dans sa fenêtre d'édition.</summary>
    [RelayCommand]
    private async Task NewShowAsync()
    {
        if (_runtime.Project.Folder is null || await _dialogs.AskTextAsync("Nouveau show", "Nom du show :").ConfigureAwait(true) is not { } name || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var show = new ShowDefinition { Name = name.Trim(), Steps = [new ShowStep { Id = "0", Name = "Début", Initial = true }] };
        _runtime.Project.SaveShows(_runtime.Project.Shows with { Shows = [.. _runtime.Project.Shows.Shows, show] });
        Rebuild();
        EditShowRequested?.Invoke(this, show.Id);
    }

    /// <summary>Nouvelle séquence (8 mesures), ouverte aussitôt dans sa fenêtre d'édition.</summary>
    [RelayCommand]
    private async Task NewSequenceAsync()
    {
        if (_runtime.Project.Folder is null || await _dialogs.AskTextAsync("Nouvelle séquence", "Nom de la séquence :").ConfigureAwait(true) is not { } name || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var sequence = new Sequence { Name = name.Trim(), Bars = 8 };
        _runtime.Project.SaveSequences(_runtime.Project.Sequences with { Sequences = [.. _runtime.Project.Sequences.Sequences, sequence] });
        Rebuild();
        EditSequenceRequested?.Invoke(this, sequence.Id);
    }

    /// <summary>Renomme.</summary>
    [RelayCommand]
    private async Task RenameAsync(ShowItemViewModel? item)
    {
        if (item is null || Busy(item) || await _dialogs.AskTextAsync("Renommer", "Nouveau nom :", item.Name).ConfigureAwait(true) is not { } name || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        Change(item, s => s with { Name = name.Trim() }, s => s with { Name = name.Trim() });
    }

    /// <summary>Duplique, juste après l'original.</summary>
    [RelayCommand]
    private void Duplicate(ShowItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var project = _runtime.Project;
        if (item.IsShow)
        {
            var list = project.Shows.Shows.ToList();
            var index = list.FindIndex(s => s.Id == item.Id);
            list.Insert(index + 1, list[index] with { Id = Guid.NewGuid(), Name = $"{item.Name} (copie)" });
            project.SaveShows(project.Shows with { Shows = list });
        }
        else
        {
            var list = project.Sequences.Sequences.ToList();
            var index = list.FindIndex(s => s.Id == item.Id);
            list.Insert(index + 1, list[index] with { Id = Guid.NewGuid(), Name = $"{item.Name} (copie)" });
            project.SaveSequences(project.Sequences with { Sequences = list });
        }

        Rebuild();
    }

    /// <summary>Supprime, après confirmation (les shows qui l'utilisent sont signalés par la validation).</summary>
    [RelayCommand]
    private async Task DeleteAsync(ShowItemViewModel? item)
    {
        if (item is null || Busy(item))
        {
            return;
        }

        var project = _runtime.Project;
        var users = item.IsShow
            ? project.Shows.Shows.Where(s => s.Steps.Any(x => x.MacroShowId == item.Id)).Select(s => s.Name).ToList()
            : project.Shows.Shows.Where(s => s.Steps.Any(x => x.Actions.Any(a => a.SequenceId == item.Id)) || s.Transitions.Any(t => t.Condition.SequenceId == item.Id)).Select(s => s.Name).ToList();
        var text = $"Supprimer « {item.Name} » ?" + (users.Count == 0 ? string.Empty : $"{Environment.NewLine}{Environment.NewLine}Utilisé(e) par : {string.Join(", ", users)} (ces shows le signaleront comme erreur).");
        if (!await _dialogs.ConfirmAsync("Supprimer", text).ConfigureAwait(true))
        {
            return;
        }

        if (item.IsShow)
        {
            _runtime.Engine.Send(new StopShowCommand(CommandOrigin.User, item.Id));
            project.SaveShows(project.Shows with { Shows = [.. project.Shows.Shows.Where(s => s.Id != item.Id)] });
        }
        else
        {
            _runtime.Engine.Send(new StopSequenceCommand(CommandOrigin.User, item.Id));
            project.SaveSequences(project.Sequences with { Sequences = [.. project.Sequences.Sequences.Where(s => s.Id != item.Id)] });
        }

        Rebuild();
    }

    // Un objet ouvert dans sa fenêtre d'édition se modifie là-bas.
    private bool Busy(ShowItemViewModel item)
    {
        if (Edited?.Invoke() != item.Id)
        {
            return false;
        }

        MessageRaised?.Invoke(this, $"« {item.Name} » est ouvert(e) dans sa fenêtre d'édition : validez ou annulez d'abord.");
        return true;
    }

    private void Change(ShowItemViewModel item, Func<ShowDefinition, ShowDefinition> show, Func<Sequence, Sequence> sequence)
    {
        var project = _runtime.Project;
        if (item.IsShow)
        {
            project.SaveShows(project.Shows with { Shows = [.. project.Shows.Shows.Select(s => s.Id == item.Id ? show(s) : s)] });
        }
        else
        {
            project.SaveSequences(project.Sequences with { Sequences = [.. project.Sequences.Sequences.Select(s => s.Id == item.Id ? sequence(s) : s)] });
        }

        Rebuild();
    }
}

/// <summary>Bouton d'un show ou d'une séquence de la colonne « Shows ».</summary>
public sealed partial class ShowItemViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Fill), nameof(TextColor), nameof(ShowsProgress))]
    private bool _isActive;

    [ObservableProperty]
    private string _state = string.Empty;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditBandFill), nameof(OutlineThickness))]
    private bool _isEditTarget;

    /// <summary>Crée le bouton.</summary>
    public ShowItemViewModel(Guid id, bool isShow, string name, string color, string? detail)
    {
        Id = id;
        IsShow = isShow;
        Name = name;
        Color = color is { Length: 7 } ? color : "#39C5CF";
        Detail = detail;
        _state = detail ?? string.Empty;
    }

    /// <summary>Identifiant.</summary>
    public Guid Id { get; }

    /// <summary>Show (sinon séquence).</summary>
    public bool IsShow { get; }

    /// <summary>Nom.</summary>
    public string Name { get; }

    /// <summary>Couleur.</summary>
    public string Color { get; }

    /// <summary>Texte affiché à l'arrêt (longueur d'une séquence, « secondaire »).</summary>
    public string? Detail { get; }

    /// <summary>Fond du bouton : la couleur quand il joue.</summary>
    public string Fill => IsActive ? "#D9" + Color[1..] : "#21262D";

    /// <summary>Texte lisible sur le fond.</summary>
    public string TextColor => IsActive && Luminance(Color) > 0.6 ? "#0D1117" : "#E6EDF3";

    /// <summary>Bande ✎ en couleur d'édition quand l'objet est ouvert.</summary>
    public string EditBandFill => IsEditTarget ? ControlColors.Edit : "#161B22";

    /// <summary>Contour quand l'objet est ouvert.</summary>
    public Avalonia.Thickness OutlineThickness => new(IsEditTarget ? 2 : 0);

    /// <summary>Barre d'avancement : une séquence qui joue.</summary>
    public bool ShowsProgress => !IsShow && IsActive;

    private static double Luminance(string hex)
    {
        var c = Avalonia.Media.Color.Parse(hex);
        return ((0.299 * c.R) + (0.587 * c.G) + (0.114 * c.B)) / 255;
    }
}
