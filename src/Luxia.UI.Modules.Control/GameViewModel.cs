using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Écran de jeu (chantier « Contrôle 2 », ERG-032, maquette 5) : ce qu'il faut pour jouer, sans mode à garder en tête.
/// Colonnes (grandes cibles), groupes dimmer, looks, pilote automatique, journal, Stop / Tout stopper, verrou soirée.
/// Les seules retouches en direct sont les dimmers de groupe (temporaires, jamais enregistrées) ; concevoir une scène se
/// fait dans la fenêtre d'édition, ouverte par la bande ✎ (<see cref="EditRequested"/>, ERG-033).
/// </summary>
public sealed partial class GameViewModel : ViewModelBase, IRefreshable
{
    private readonly LuxiaRuntime _runtime;
    private readonly IDialogService _dialogs;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private bool _hasProject;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private string _undoText = "Rien à annuler";

    [ObservableProperty]
    private string _redoText = "Rien à rétablir";

    [ObservableProperty]
    private bool _hasRetouches;

    [ObservableProperty]
    private string _retouchText = string.Empty;

    [ObservableProperty]
    private string _retouchBorder = "#30363D";

    [ObservableProperty]
    private string _retouchForeground = "#8B949E";

    private Guid? _editedSceneId;

    /// <summary>Crée l'écran de jeu.</summary>
    public GameViewModel(LuxiaRuntime runtime, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(dialogs);
        _dialogs = dialogs;
        _runtime = runtime;

        // La session sert ici au verrou soirée et à l'annulation des opérations sur les scènes (nouvelle, dupliquer,
        // supprimer…) : elle reste en LIVE, l'édition d'une scène a sa propre session (fenêtre d'édition).
        Session = new ControlSession(runtime);
        Journal = new JournalPanelViewModel(runtime);
        Columns = new ColumnsPanelViewModel(runtime, Session, dialogs) { EditedScene = () => _editedSceneId };
        Looks = new LooksPanelViewModel(runtime, Session, dialogs, Journal);
        Dimmers = new DimmersPanelViewModel(runtime, Journal);
        Tempo = new TempoBarViewModel(runtime, Journal);
        Editor = new EditorViewModel(runtime, dialogs, Journal);
        SequenceEditor = new Sequencing.SequenceEditorViewModel(runtime, dialogs, Journal);
        ShowEditor = new Sequencing.ShowEditorViewModel(runtime, dialogs, Journal);
        Band = new Sequencing.ShowBandViewModel(runtime, Journal);
        Columns.Shows.Edited = () => SequenceEditor.EditedId ?? ShowEditor.EditedId;
        Columns.Shows.EditShowRequested += (_, id) => EditShow(id);
        Columns.Shows.EditSequenceRequested += (_, id) => EditSequence(id);
        Columns.Shows.MessageRaised += (_, text) => Message = text;
        SequenceEditor.Closed += (_, _) => Columns.Shows.Refresh();
        ShowEditor.Closed += (_, _) => Columns.Shows.Refresh();
        Editor.Closed += (_, _) => EditedSceneId = null;
        Columns.EditRequested += (_, id) => Edit(id);
        Columns.MessageChanged += (_, _) => Message = Columns.Message;

        // Une scène jouée ou arrêtée dans les colonnes pendant l'édition : l'étape éditée, montrée par-dessus, ne la recouvre
        // plus (essai 1.007.080) ; elle reprend au premier réglage ou au choix d'une autre étape.
        Columns.ScenePlayed += (_, _) =>
        {
            if (Editor.IsOpen)
            {
                Editor.Session.SuspendShow();
            }
        };
        Session.Changed += (_, _) => UpdateState();
        runtime.Project.Changed += (_, _) => HasProject = runtime.Project.Folder is not null;
        HasProject = runtime.Project.Folder is not null;
        UpdateState();
    }

    /// <summary>Session (verrou soirée, annulation des opérations sur les scènes) : toujours en LIVE.</summary>
    public ControlSession Session { get; }

    /// <summary>Panneau Colonnes.</summary>
    public ColumnsPanelViewModel Columns { get; }

    /// <summary>Bloc BPM de l'en-tête (Q42) : source, tempo, TAP, ×2, ÷2, compteur des temps.</summary>
    public TempoBarViewModel Tempo { get; }

    /// <summary>Panneau Groupes dimmer.</summary>
    public DimmersPanelViewModel Dimmers { get; }

    /// <summary>Panneau Looks (et panneau Pilote automatique, qui montre les mêmes looks).</summary>
    public LooksPanelViewModel Looks { get; }

    /// <summary>Panneau Journal.</summary>
    public JournalPanelViewModel Journal { get; }

    /// <summary>Fenêtre d'édition d'une scène (brouillon, ERG-033) : ouverte par la bande ✎.</summary>
    public EditorViewModel Editor { get; }

    /// <summary>Fenêtre d'édition d'une séquence (P8).</summary>
    public Sequencing.SequenceEditorViewModel SequenceEditor { get; }

    /// <summary>Fenêtre d'édition d'un show (P8).</summary>
    public Sequencing.ShowEditorViewModel ShowEditor { get; }

    /// <summary>Bandeau « Show en cours » (SHOW-026).</summary>
    public Sequencing.ShowBandViewModel Band { get; }

    /// <summary>La fenêtre d'édition d'une séquence doit s'afficher.</summary>
    public event EventHandler? SequenceEditRequested;

    /// <summary>La fenêtre d'édition d'un show doit s'afficher.</summary>
    public event EventHandler? ShowEditRequested;

    /// <summary>
    /// Éditeur de couches (COU-001), venu de l'ancien écran Scènes (lot 7 de P8) ; <c>null</c> sans projet ou sous le verrou
    /// soirée (le message dit pourquoi).
    /// </summary>
    public LayersEditorViewModel? CreateLayersEditor()
    {
        if (Session.IsLocked)
        {
            Message = ControlSession.LockedReason;
            return null;
        }

        return _runtime.Project.Folder is null ? null : new LayersEditorViewModel(_runtime, _dialogs);
    }

    /// <summary>Ouvre une séquence dans sa fenêtre d'édition.</summary>
    public void EditSequence(Guid id)
    {
        if (Session.IsLocked)
        {
            Message = ControlSession.LockedReason;
            return;
        }

        if (_runtime.Project.Sequences.Sequences.FirstOrDefault(s => s.Id == id) is not { } sequence)
        {
            return;
        }

        Message = SequenceEditor.Open(sequence);
        SequenceEditRequested?.Invoke(this, EventArgs.Empty);
        Columns.Shows.Refresh();
    }

    /// <summary>Ouvre un show dans sa fenêtre d'édition.</summary>
    public void EditShow(Guid id)
    {
        if (Session.IsLocked)
        {
            Message = ControlSession.LockedReason;
            return;
        }

        if (_runtime.Project.Shows.Shows.FirstOrDefault(s => s.Id == id) is not { } show)
        {
            return;
        }

        Message = ShowEditor.Open(show);
        ShowEditRequested?.Invoke(this, EventArgs.Empty);
        Columns.Shows.Refresh();
    }

    /// <summary>
    /// La fenêtre d'édition doit être montrée (ou ramenée au premier plan) pour cette scène. Le verrou soirée et les autres
    /// refus sont déjà traités ici : rien n'est levé si l'édition n'est pas permise.
    /// </summary>
    public event EventHandler<Guid>? EditRequested;

    /// <summary>Levé pour amener un panneau au premier plan (réaffiché s'il était fermé).</summary>
    public event EventHandler<string>? PanelRequested;

    /// <summary>Scène ouverte dans la fenêtre d'édition (contour vert dans les colonnes), ou <c>null</c>.</summary>
    public Guid? EditedSceneId
    {
        get => _editedSceneId;
        set
        {
            if (_editedSceneId == value)
            {
                return;
            }

            _editedSceneId = value;
            Columns.RefreshEditTarget();
        }
    }

    /// <summary>Dossier des dispositions de panneaux (sur le poste, C10).</summary>
    public string LayoutFolder => Path.Combine(_runtime.Paths.AppDataRoot, "dispositions");

    /// <inheritdoc />
    public bool NeedsBackgroundRefresh => false;

    /// <summary>Amène un panneau au premier plan (identifiant de <see cref="Docking.ControlPanels"/>).</summary>
    public void RequestPanel(string id) => PanelRequested?.Invoke(this, id);

    /// <inheritdoc />
    public void Refresh()
    {
        Columns.Refresh();
        Band.Refresh();
        Dimmers.Refresh();
        Tempo.Refresh();
        Journal.Refresh();
        UpdateState();
    }

    /// <summary>Écrit ce qui attend encore (changement d'écran, fermeture) : rien ici, tout est écrit tout de suite.</summary>
    public void Flush() => Session.Commit();

    /// <summary>
    /// Demande l'édition d'une scène : refusée sous le verrou soirée (on ne fait que jouer), sinon l'hôte ouvre la fenêtre.
    /// </summary>
    public void Edit(Guid sceneId)
    {
        if (Session.IsLocked)
        {
            Message = ControlSession.LockedReason;
            return;
        }

        if (Editor.Open(sceneId) is { } reason)
        {
            // Une autre scène a des modifications : la fenêtre passe au premier plan pour qu'on la valide ou l'annule.
            Message = reason;
            if (Editor.SceneId is { } open)
            {
                EditRequested?.Invoke(this, open);
            }

            return;
        }

        Message = null;
        EditedSceneId = sceneId;
        EditRequested?.Invoke(this, sceneId);
    }

    /// <summary>Verrou soirée (E7, §4.6) : jouer seulement ; l'édition des scènes est bloquée.</summary>
    [RelayCommand]
    private void ToggleLock()
    {
        if (!Session.IsLocked && (Editor.IsOpen || SequenceEditor.IsOpen || ShowEditor.IsOpen))
        {
            Message = "Fermez d'abord la fenêtre d'édition (Valider ou Annuler) avant de poser le verrou soirée.";
            return;
        }

        Session.SetLocked(!Session.IsLocked);
        Message = null;
        Journal.Log(Session.IsLocked ? "🔒 Verrou soirée posé : jouer seulement" : "🔓 Verrou soirée levé");
        _runtime.TraceUi("Contrôle", Session.IsLocked ? "verrou posé" : "verrou levé");
    }

    /// <summary>
    /// « ■ Stop » et « ■ Tout stopper » (CMD-012, paramètre « tout ») : toutes les scènes, sauf ou avec les couches
    /// protégées (Ambiance par défaut, COU-007). Toujours permis, verrou compris : c'est jouer.
    /// </summary>
    [RelayCommand]
    private void StopAll(string? everything)
    {
        var all = everything == "tout";
        _runtime.Engine.Send(new StopLayerCommand(CommandOrigin.User, Everything: all));
        Journal.Log(all ? "■ tout stoppé, couches protégées comprises" : "■ stop (sauf couches protégées)");
        _runtime.TraceUi("Contrôle", all ? "tout stopper" : "stop");
    }

    /// <summary>« Libérer tout » (Échap) : remet les dimmers de groupe à 100 % (les retouches en direct).</summary>
    [RelayCommand]
    private void ReleaseAll()
    {
        Dimmers.ResetAllCommand.Execute(null);
        Session.ReleaseAll();
        Journal.Log("Libérer tout : plus aucune retouche en direct");
    }

    /// <summary>Annuler (Ctrl+Z) : la dernière opération sur les scènes (nouvelle, dupliquer, supprimer, renommer…).</summary>
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

    private void UpdateState()
    {
        IsLocked = Session.IsLocked;
        UndoText = Session.UndoDescription is { } undo ? $"Annuler : {undo} (Ctrl+Z)" : "Rien à annuler";
        RedoText = Session.RedoDescription is { } redo ? $"Rétablir : {redo} (Ctrl+Y)" : "Rien à rétablir";
        var retouched = Dimmers.Faders.Where(f => f.IsRetouched).Select(f => $"{f.Name} — dimmer {f.Percent:0} %").ToList();
        HasRetouches = retouched.Count > 0;
        RetouchText = HasRetouches
            ? "Retouches en direct (temporaires, rien n'est enregistré) :  " + string.Join(" · ", retouched)
            : "Aucune retouche en direct : les dimmers de groupe sont à 100 %.";
        RetouchBorder = HasRetouches ? "#D29922" : "#30363D";
        RetouchForeground = HasRetouches ? "#E6EDF3" : "#8B949E";
        if (Session.Message is { } message)
        {
            Message = message;
        }
    }
}
