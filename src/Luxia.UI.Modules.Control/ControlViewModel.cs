using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Écran « Contrôle » (doc 60 §6, E2) : jouer et corriger au même endroit. Réunit la session d'édition et les
/// panneaux ancrables (Colonnes, Propriétés, Plan des appareils, Réglages des appareils, Journal), le sélecteur
/// LIVE / ÉDITION / AVEUGLE et son bandeau (§4.1), annuler / rétablir (§4.2). Un geste (glisser, frappe) est écrit
/// une demi-seconde après le dernier mouvement (C7).
/// </summary>
public sealed partial class ControlViewModel : ViewModelBase, IRefreshable
{
    /// <summary>Rafraîchissements (20 par seconde) sans nouveau mouvement avant d'écrire le geste : 0,5 s.</summary>
    public const int CommitDelayRefreshes = 10;

    private readonly LuxiaRuntime _runtime;
    private int _changes;
    private int _seenChanges;
    private int _countdown;

    [ObservableProperty]
    private string _modeTitle = "LIVE";

    [ObservableProperty]
    private string _modeText = string.Empty;

    [ObservableProperty]
    private string _modeColor = ControlColors.Live;

    [ObservableProperty]
    private string _modeActionText = "Libérer tout";

    [ObservableProperty]
    private bool _hasModeAction;

    [ObservableProperty]
    private string _undoText = "Annuler (Ctrl+Z)";

    [ObservableProperty]
    private string _redoText = "Rétablir (Ctrl+Y)";

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private bool _hasProject;

    [ObservableProperty]
    private bool _isLocked;

    /// <summary>Crée l'écran.</summary>
    public ControlViewModel(LuxiaRuntime runtime, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(dialogs);
        _runtime = runtime;
        Session = new ControlSession(runtime);
        Columns = new ColumnsPanelViewModel(runtime, Session, dialogs);
        Plan = new PlanPanelViewModel(runtime, Session);
        Settings = new SettingsPanelViewModel(runtime, Session, dialogs);
        Properties = new PropertiesPanelViewModel(runtime, Session);
        Journal = new JournalPanelViewModel(runtime);
        Looks = new LooksPanelViewModel(runtime, Session, dialogs, Journal);
        Session.Changed += (_, _) =>
        {
            _changes++;
            UpdateBand();
        };
        Settings.ZoneEditingChanged += (_, _) => UpdateBand();
        runtime.Project.Changed += (_, _) => HasProject = runtime.Project.Folder is not null;
        HasProject = runtime.Project.Folder is not null;
        UpdateBand();
    }

    /// <summary>Session d'édition (sélection, mode, scène éditée).</summary>
    public ControlSession Session { get; }

    /// <summary>Panneau Colonnes.</summary>
    public ColumnsPanelViewModel Columns { get; }

    /// <summary>Panneau Plan des appareils.</summary>
    public PlanPanelViewModel Plan { get; }

    /// <summary>Panneau Réglages des appareils.</summary>
    public SettingsPanelViewModel Settings { get; }

    /// <summary>Panneau Propriétés.</summary>
    public PropertiesPanelViewModel Properties { get; }

    /// <summary>Panneau Journal.</summary>
    public JournalPanelViewModel Journal { get; }

    /// <summary>Panneau Looks (et panneau Pilote automatique, qui montre les mêmes looks).</summary>
    public LooksPanelViewModel Looks { get; }

    /// <summary>Disposition affichée : Contrôle ou Spectacle (doc 60 §6).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsControlLayout), nameof(IsShowLayout))]
    private Docking.ControlLayoutPreset _layoutPreset;

    /// <summary>Disposition Contrôle affichée.</summary>
    public bool IsControlLayout => LayoutPreset == Docking.ControlLayoutPreset.Control;

    /// <summary>Disposition Spectacle affichée.</summary>
    public bool IsShowLayout => LayoutPreset == Docking.ControlLayoutPreset.Show;

    /// <summary>Choisit la disposition (paramètre : controle, spectacle).</summary>
    [RelayCommand]
    private void SetLayout(string? preset)
    {
        LayoutPreset = preset == "spectacle" ? Docking.ControlLayoutPreset.Show : Docking.ControlLayoutPreset.Control;
        _runtime.TraceUi("Contrôle", $"disposition {preset}");
    }

    /// <summary>Dossier des dispositions de panneaux (sur le poste, C10).</summary>
    public string LayoutFolder => Path.Combine(_runtime.Paths.AppDataRoot, "dispositions");

    /// <summary>Mode LIVE affiché.</summary>
    public bool IsLive => Session.Mode == EditMode.Live;

    /// <summary>Mode ÉDITION affiché.</summary>
    public bool IsEdit => Session.Mode == EditMode.Edit;

    /// <summary>Mode AVEUGLE affiché.</summary>
    public bool IsBlind => Session.Mode == EditMode.Blind;

    /// <summary>Un geste attend d'être écrit : l'écran doit être rafraîchi même caché, pour l'écrire.</summary>
    public bool NeedsBackgroundRefresh => Session.HasPendingCommit;

    /// <inheritdoc />
    public void Refresh()
    {
        Columns.Refresh();
        Plan.Refresh();
        Settings.Refresh();
        Properties.Refresh();
        CountDownCommit();
        Journal.Refresh();
    }

    /// <summary>Écrit tout de suite le geste en cours (changement d'écran, fermeture).</summary>
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
        var target = mode switch
        {
            "edition" => EditMode.Edit,
            "aveugle" => EditMode.Blind,
            _ => EditMode.Live,
        };
        Flush();
        Message = Session.SetMode(target);
        if (Message is null)
        {
            _runtime.TraceUi("Contrôle", $"mode {ModeTitle}");
            Journal.Log($"Mode {ModeTitle}");
        }

        // Les boutons sont des bascules : on les remet d'accord avec le mode réel (un refus laisse l'ancien).
        OnPropertyChanged(nameof(IsLive));
        OnPropertyChanged(nameof(IsEdit));
        OnPropertyChanged(nameof(IsBlind));
    }

    /// <summary>Verrou soirée (E7, §4.6) : jouer seulement ; l'édition des scènes et des zones est bloquée.</summary>
    [RelayCommand]
    private void ToggleLock()
    {
        Settings.IsZoneEditing = false;
        Session.SetLocked(!Session.IsLocked);
        Message = null;
        Journal.Log(Session.IsLocked ? "🔒 Verrou soirée posé : jouer seulement" : "🔓 Verrou soirée levé");
        _runtime.TraceUi("Contrôle", Session.IsLocked ? "verrou posé" : "verrou levé");
    }

    /// <summary>Action du bandeau : « Libérer tout » (LIVE), « Revenir en LIVE », « Terminer les zones ».</summary>
    [RelayCommand]
    private void ModeAction()
    {
        if (Settings.IsZoneEditing)
        {
            Settings.IsZoneEditing = false;
        }
        else if (Session.Mode == EditMode.Live)
        {
            Session.ReleaseAll();
            Journal.Log("Libérer tout : les scènes reprennent la main");
        }
        else
        {
            SetMode("live");
        }
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

    private void UpdateBand()
    {
        var scene = Session.EditScene?.Name;
        var step = Session.EditStep + 1;
        if (Settings.IsZoneEditing)
        {
            // C4 : les zones appartiennent au lieu, pas à la scène.
            (ModeTitle, ModeColor, ModeActionText) = ("ZONES", ControlColors.Zones, "Terminer les zones");
            ModeText = $"Vous dessinez les zones du lieu « {_runtime.Project.Venues.Active.Name} » : elles valent pour toutes les scènes et s'appliquent tout de suite (Ctrl+Z pour annuler).";
        }
        else
        {
            switch (Session.Mode)
            {
                case EditMode.Edit:
                    (ModeTitle, ModeColor, ModeActionText) = ("ÉDITION", ControlColors.Edit, "Revenir en LIVE");
                    ModeText = $"Vos réglages s'écrivent tout de suite dans « {scene} », étape {step}, et sortent. Ctrl+Z pour annuler.";
                    break;
                case EditMode.Blind:
                    (ModeTitle, ModeColor, ModeActionText) = ("AVEUGLE 👁", ControlColors.Blind, "Revenir en LIVE");
                    ModeText = $"Vos réglages s'écrivent dans « {scene} », étape {step}, sans changer la sortie. Le plan montre l'aperçu.";
                    break;
                default:
                    (ModeTitle, ModeColor, ModeActionText) = ("LIVE", ControlColors.Live, "Libérer tout");
                    var count = Session.LiveFixtureCount;
                    ModeText = count == 0
                        ? "Vos réglages sont des surcharges temporaires par-dessus les scènes : rien n'est enregistré. Aucune en cours."
                        : $"Vos réglages sont des surcharges temporaires par-dessus les scènes : rien n'est enregistré. En cours : {count} appareil(s).";
                    break;
            }
        }

        IsLocked = Session.IsLocked;
        if (IsLocked && Session.Mode == EditMode.Live && !Settings.IsZoneEditing)
        {
            ModeText = "🔒 Verrou soirée : on joue et on retouche en direct ; l'édition des scènes et des zones est bloquée. " + ModeText;
        }

        HasModeAction = Settings.IsZoneEditing || Session.Mode != EditMode.Live || Session.LiveFixtureCount > 0;
        UndoText = Session.UndoDescription is { } undo ? $"Annuler : {undo} (Ctrl+Z)" : "Rien à annuler";
        RedoText = Session.RedoDescription is { } redo ? $"Rétablir : {redo} (Ctrl+Y)" : "Rien à rétablir";
        if (Session.Message is { } message)
        {
            Message = message;
        }

        OnPropertyChanged(nameof(IsLive));
        OnPropertyChanged(nameof(IsEdit));
        OnPropertyChanged(nameof(IsBlind));
    }
}
