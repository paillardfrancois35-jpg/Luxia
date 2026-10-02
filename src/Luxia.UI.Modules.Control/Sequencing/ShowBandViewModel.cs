using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Show.Model;
using Luxia.Show.Rules;
using Luxia.Show.Runtime;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control.Sequencing;

/// <summary>
/// Bandeau « Show en cours » de l'écran de jeu (Q44 solution C, maquettes 9 et 10 ; SHOW-026, LIVE-023) : une ligne qui n'apparaît
/// que quand un show ou une séquence joue (étape active, prochaines transitions, séquence en cours, Forcer, Arrêter) ; déplié, le
/// parcours, l'étape active et ses actions, chaque transition en attente avec son compte à rebours et son bouton « Forcer ».
/// </summary>
public sealed partial class ShowBandViewModel : ViewModelBase
{
    private readonly LuxiaRuntime _runtime;
    private readonly JournalPanelViewModel _journal;
    private string _signature = string.Empty;

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _color = "#39C5CF";

    [ObservableProperty]
    private string _steps = string.Empty;

    [ObservableProperty]
    private string _since = string.Empty;

    [ObservableProperty]
    private string _sequenceText = string.Empty;

    [ObservableProperty]
    private string _secondaryText = string.Empty;

    [ObservableProperty]
    private string _pathText = string.Empty;

    [ObservableProperty]
    private bool _hasShow;

    /// <summary>Crée le bandeau.</summary>
    public ShowBandViewModel(LuxiaRuntime runtime, JournalPanelViewModel journal)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(journal);
        _runtime = runtime;
        _journal = journal;
    }

    /// <summary>Transitions validées du show principal (en attente de leur condition, ou armées).</summary>
    public ObservableCollection<BandTransition> Transitions { get; } = [];

    /// <summary>Les deux ou trois premières, pour la ligne repliée.</summary>
    public ObservableCollection<BandTransition> Next { get; } = [];

    /// <summary>Étapes actives du show principal, avec leurs actions.</summary>
    public ObservableCollection<BandStep> ActiveSteps { get; } = [];

    /// <summary>Texte du bouton Déplier / Replier.</summary>
    public string ExpandText => IsExpanded ? "▴ Replier" : "▾ Détail";

    /// <summary>Show principal qui joue, ou nul.</summary>
    public Guid? ShowId { get; private set; }

    /// <summary>Relit ce qui joue (dix fois par seconde suffisent : l'état du séquenceur est publié à ce rythme).</summary>
    public void Refresh()
    {
        var state = _runtime.Sequencer.State;
        var main = state.MainShow;
        IsVisible = state.Shows.Count > 0 || state.Sequences.Count > 0;
        HasShow = main is not null;
        ShowId = main?.ShowId;
        Title = main?.Name ?? state.Sequences.Select(s => $"Séquence « {s.Name} »").FirstOrDefault() ?? (state.Shows.Count > 0 ? state.Shows[0].Name : string.Empty);
        Color = main?.Color ?? "#3FB950";
        if (main is not null)
        {
            Steps = string.Join(", ", main.ActiveSteps.Select(s => string.IsNullOrWhiteSpace(s.Name) ? s.Id : s.Name));
            var bars = main.ActiveSteps.Count > 0 ? Math.Floor(main.ActiveSteps.Max(s => s.SinceBeats) / Sequencer.BeatsPerBar) : 0;
            Since = main.Ended ? "terminé : tient sa dernière étape" : string.Create(CultureInfo.CurrentCulture, $"depuis {bars} mesure{(bars > 1 ? "s" : string.Empty)}");
            PathText = string.Join(" → ", main.Path);
        }
        else
        {
            Steps = string.Empty;
            Since = string.Empty;
            PathText = string.Empty;
        }

        SequenceText = string.Join(" · ", state.Sequences.Select(s => s.PositionBars < 0
            ? $"séquence « {s.Name} » : attend la mesure"
            : string.Create(CultureInfo.CurrentCulture, $"séquence « {s.Name} » {Math.Floor(s.PositionBars) + 1} / {s.Bars:0.##}")));
        SecondaryText = string.Join(" · ", state.Shows.Where(s => s.Secondary).Select(s => $"en parallèle : « {s.Name} » ({string.Join(", ", s.ActiveSteps.Select(a => a.Name))})"));

        var transitions = main?.Transitions.Select(t => new BandTransition(
            t.Index,
            t.Condition,
            string.Join(", ", t.ToNames),
            ShowTexts.Quantize(t.Quantize),
            t.Armed ? string.Create(CultureInfo.CurrentCulture, $"part dans {Math.Ceiling(t.BeatsLeft)} temps") : t.Hint.Length > 0 ? t.Hint : "attend la condition",
            t.Armed)).ToList() ?? [];
        var steps = main?.ActiveSteps.Select(s => new BandStep(s.Id, s.Name, string.Join(Environment.NewLine, s.Actions.Select(a => "• " + a)))).ToList() ?? [];
        var signature = string.Join("|", transitions) + "#" + string.Join("|", steps);
        if (signature != _signature)
        {
            _signature = signature;
            Replace(Transitions, transitions);
            Replace(Next, transitions.Take(3));
            Replace(ActiveSteps, steps);
        }
    }

    /// <summary>Déplie ou replie le détail.</summary>
    [RelayCommand]
    private void Toggle()
    {
        IsExpanded = !IsExpanded;
        OnPropertyChanged(nameof(ExpandText));
    }

    /// <summary>Force une transition (CMD-051), à sa quantification.</summary>
    [RelayCommand]
    public void Force(BandTransition? transition)
    {
        if (transition is null || ShowId is not { } show)
        {
            return;
        }

        _runtime.Engine.Send(new ForceTransitionCommand(CommandOrigin.User, show, transition.Index));
        _journal.Log($"⏭ forcer : {transition.Condition} → {transition.Target}");
        _runtime.TraceUi("Contrôle", $"forcer la transition {transition.Index + 1}");
    }

    /// <summary>Arrête le show principal (ou, sans show, les séquences).</summary>
    [RelayCommand]
    private void Stop()
    {
        _runtime.Engine.Send(new StopShowCommand(CommandOrigin.User, ShowId));
        _journal.Log(ShowId is null ? "■ séquences arrêtées" : $"■ show « {Title} » arrêté");
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}

/// <summary>Une transition du bandeau.</summary>
/// <param name="Index">Rang dans le show.</param>
/// <param name="Condition">Condition en clair.</param>
/// <param name="Target">Étapes aval.</param>
/// <param name="Quantize">Quantification en clair (vide : tout de suite).</param>
/// <param name="State">« attend la condition », « part dans 2 temps »…</param>
/// <param name="Armed">Condition vraie, en attente de sa frontière.</param>
public sealed record BandTransition(int Index, string Condition, string Target, string Quantize, string State, bool Armed)
{
    /// <summary>Couleur du voyant : orange quand elle va partir.</summary>
    public string Dot => Armed ? "#D29922" : "#8B949E";

    /// <summary>Texte court de la ligne repliée.</summary>
    public string Compact => Armed ? $"{Condition} → {Target} ({State})" : $"{Condition} → {Target}";
}

/// <summary>Une étape active du bandeau.</summary>
/// <param name="Id">Identifiant.</param>
/// <param name="Name">Nom.</param>
/// <param name="Actions">Actions, une par ligne.</param>
public sealed record BandStep(string Id, string Name, string Actions);
