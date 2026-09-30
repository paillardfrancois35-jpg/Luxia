using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Établi d'édition : la session d'édition et les panneaux qui servent à concevoir une scène (Plan des appareils, Réglages
/// des appareils, Effets, Propriétés et étapes). C'est ce que montre la fenêtre d'édition (<see cref="EditorViewModel"/>,
/// chantier « Contrôle 2 ») ; l'écran de jeu (<see cref="GameViewModel"/>) n'en a plus rien. Un geste (glisser, frappe) est
/// écrit une demi-seconde après le dernier mouvement (C7) ; annuler / rétablir agissent sur la session.
/// </summary>
public sealed partial class EditBenchViewModel : ViewModelBase, IRefreshable
{
    /// <summary>Rafraîchissements (20 par seconde) sans nouveau mouvement avant d'écrire le geste : 0,5 s.</summary>
    public const int CommitDelayRefreshes = 10;

    private readonly LuxiaRuntime _runtime;
    private int _changes;
    private int _seenChanges;
    private int _countdown;

    [ObservableProperty]
    private string? _message;

    /// <summary>Crée l'établi ; le journal est celui de l'écran de jeu quand la fenêtre d'édition le fournit.</summary>
    public EditBenchViewModel(LuxiaRuntime runtime, IDialogService dialogs, JournalPanelViewModel? journal = null)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(dialogs);
        _runtime = runtime;
        Session = new ControlSession(runtime);
        Plan = new PlanPanelViewModel(runtime, Session);
        Settings = new SettingsPanelViewModel(runtime, Session, dialogs);
        Properties = new PropertiesPanelViewModel(runtime, Session);
        Effects = new EffectsPanelViewModel(runtime, Session, dialogs);
        Journal = journal ?? new JournalPanelViewModel(runtime);
        Session.Changed += (_, _) =>
        {
            _changes++;
            if (Session.Message is { } message)
            {
                Message = message;
            }
        };
    }

    /// <summary>Session d'édition (sélection, mode, scène éditée).</summary>
    public ControlSession Session { get; }

    /// <summary>Panneau Plan des appareils.</summary>
    public PlanPanelViewModel Plan { get; }

    /// <summary>Panneau Réglages des appareils.</summary>
    public SettingsPanelViewModel Settings { get; }

    /// <summary>Panneau Propriétés et étapes.</summary>
    public PropertiesPanelViewModel Properties { get; }

    /// <summary>Panneau Effets (doc 16 §6).</summary>
    public EffectsPanelViewModel Effects { get; }

    /// <summary>Journal où l'édition laisse ses traces.</summary>
    public JournalPanelViewModel Journal { get; }

    /// <summary>Mode AVEUGLE affiché.</summary>
    public bool IsBlind => Session.Mode == EditMode.Blind;

    /// <summary>Un geste attend d'être écrit : la fenêtre doit être rafraîchie même cachée, pour l'écrire.</summary>
    public bool NeedsBackgroundRefresh => Session.HasPendingCommit;

    /// <inheritdoc />
    public void Refresh()
    {
        Plan.Refresh();
        Settings.Refresh();
        Properties.Refresh();
        Effects.Refresh();
        CountDownCommit();
        Journal.Refresh();
    }

    /// <summary>Écrit tout de suite le geste en cours (fermeture, changement de scène).</summary>
    public void Flush()
    {
        if (Session.HasPendingCommit)
        {
            var description = Session.UndoDescription;
            Session.Commit();
            Journal.Log($"✎ enregistré : {description}");
        }
    }

    /// <summary>Choisit le mode (paramètre : live, edition, aveugle).</summary>
    [RelayCommand]
    private void SetMode(string? mode)
    {
        var (target, title) = mode switch
        {
            "edition" => (EditMode.Edit, "ÉDITION"),
            "aveugle" => (EditMode.Blind, "AVEUGLE"),
            _ => (EditMode.Live, "LIVE"),
        };
        Flush();
        Message = Session.SetMode(target);
        if (Message is null)
        {
            _runtime.TraceUi("Édition", $"mode {title}");
            Journal.Log($"Mode {title}");
        }

        OnPropertyChanged(nameof(IsBlind));
    }

    /// <summary>Annuler (Ctrl+Z).</summary>
    [RelayCommand]
    public void Undo()
    {
        var description = Session.UndoDescription;
        Session.Undo();
        if (description is not null)
        {
            Journal.Log($"↶ annulé : {description}");
        }
    }

    /// <summary>Rétablir (Ctrl+Y).</summary>
    [RelayCommand]
    public void Redo()
    {
        var description = Session.RedoDescription;
        Session.Redo();
        if (description is not null)
        {
            Journal.Log($"↷ rétabli : {description}");
        }
    }

    private void CountDownCommit()
    {
        if (!Session.HasPendingCommit)
        {
            _seenChanges = _changes;
            return;
        }

        if (_changes != _seenChanges)
        {
            _seenChanges = _changes;
            _countdown = CommitDelayRefreshes;
            return;
        }

        if (--_countdown <= 0)
        {
            Flush();
        }
    }
}
