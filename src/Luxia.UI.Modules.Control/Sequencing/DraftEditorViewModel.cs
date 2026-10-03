using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.Scenes.Compilation;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control.Sequencing;

/// <summary>Ce qu'une fenêtre d'édition de séquence ou de show attend de son modèle de vue.</summary>
public interface IDraftEditor
{
    /// <summary>La fenêtre s'est fermée.</summary>
    event EventHandler? Closed;

    /// <summary>Rafraîchit ce qui dépend du moteur.</summary>
    void Refresh();

    /// <summary>Ctrl+Z.</summary>
    void Undo();

    /// <summary>Ctrl+Y.</summary>
    void Redo();

    /// <summary>La croix : ferme, ou demande s'il y a des modifications.</summary>
    Task<bool> ConfirmCloseAsync();

    /// <summary>Fermeture de l'application : abandonne le brouillon.</summary>
    void Abandon();
}

/// <summary>
/// Base des fenêtres d'édition d'une séquence et d'un show (P8, Q44 solution C) : même charte que la fenêtre d'édition des scènes
/// (ERG-033) — un brouillon, Appliquer (écrit sans fermer), Valider (écrit et ferme), Annuler (revient à l'état d'origine),
/// Ctrl+Z / Ctrl+Y, case Aveugle (l'essai ne joue que sur l'aperçu). Le brouillon est joué par les séquenceurs pendant l'édition.
/// </summary>
/// <typeparam name="T">Séquence ou show.</typeparam>
public abstract partial class DraftEditorViewModel<T> : ViewModelBase, IDraftEditor
    where T : class
{
    private readonly UndoHistory<T> _history = new();
    private T? _original;
    private bool _syncing;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private bool _hasChanges;

    [ObservableProperty]
    private bool _isBlind;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private string _itemColor = ControlColors.Edit;

    [ObservableProperty]
    private string _undoText = "Rien à annuler";

    [ObservableProperty]
    private string _redoText = "Rien à rétablir";

    /// <summary>Crée l'éditeur.</summary>
    protected DraftEditorViewModel(LuxiaRuntime runtime, IDialogService dialogs, JournalPanelViewModel? journal)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(dialogs);
        Runtime = runtime;
        Dialogs = dialogs;
        Journal = journal;
        Simulation = new SimulationViewModel(runtime, () => IsBlind, PlayCommandFor);

        // Projet ouvert ou créé pendant l'édition : le brouillon appartient à l'ancien projet, il ne doit jamais y être écrit.
        runtime.Project.Changed += (_, _) => Abandon();
    }

    /// <summary>Application.</summary>
    protected LuxiaRuntime Runtime { get; }

    /// <summary>Boîtes de dialogue.</summary>
    protected IDialogService Dialogs { get; }

    /// <summary>Journal de l'écran de jeu, ou nul.</summary>
    protected JournalPanelViewModel? Journal { get; }

    /// <summary>Essai sans musique : métronome, événements provoqués (SHOW-007, SHOW-027).</summary>
    public SimulationViewModel Simulation { get; }

    /// <summary>Brouillon en cours, ou nul si la fenêtre est fermée.</summary>
    public T? Draft { get; private set; }

    /// <summary>Identifiant de ce qui est édité, ou nul.</summary>
    public Guid? EditedId => IsOpen && Draft is not null ? IdOf(Draft) : null;

    /// <summary>« séquence » ou « show ».</summary>
    public abstract string Kind { get; }

    /// <summary>Titre de la fenêtre.</summary>
    public string WindowTitle => IsOpen ? $"LuXia – Édition de « {ItemName} » ({Kind}, brouillon)" : $"LuXia – Édition d'un(e) {Kind}";

    /// <summary>Couleur du bandeau : vert sur la sortie, bleu en aveugle (charte §4.3).</summary>
    public string ModeColor => IsBlind ? ControlColors.Blind : ControlColors.Edit;

    /// <summary>Rappel de ce que touche l'essai.</summary>
    public string SafetyText => IsBlind
        ? "Essai sur l'aperçu seulement (plan, simulateur) : la sortie sur scène ne change pas."
        : "L'essai joue le brouillon sur la sortie. Cochez pour ne rien changer sur scène.";

    /// <summary>Problèmes du brouillon (SHOW-024), en clair.</summary>
    public IReadOnlyList<CompileIssue> Issues { get; private set; } = [];

    /// <summary>Le brouillon a au moins une erreur.</summary>
    public bool HasErrors => Issues.Any(i => i.Severity == Fixtures.Rules.IssueSeverity.Error);

    /// <summary>Texte des problèmes (une ligne par problème), vide s'il n'y en a pas.</summary>
    public string IssuesText => string.Join(Environment.NewLine, IssueRows.Select(r => r.Text));

    /// <summary>
    /// Problèmes à afficher, chacun dans la teinte de sa gravité (essai P8, ex. 16c) : erreur ⛔ en rouge franc (refusée au
    /// lancement), avertissement ⚠ en jaune (n'empêche rien).
    /// </summary>
    public IReadOnlyList<IssueRow> IssueRows => Issues
        .OrderBy(i => i.Severity == Fixtures.Rules.IssueSeverity.Error ? 0 : 1)
        .Select(i => i.Severity == Fixtures.Rules.IssueSeverity.Error
            ? new IssueRow($"⛔ {Short(i)}", ControlColors.Error)
            : new IssueRow($"⚠ {Short(i)}", ControlColors.Warning))
        .ToList();

    private string Short(CompileIssue issue) => $"{issue.Item.Replace(ItemLabel + ", ", string.Empty, StringComparison.Ordinal)} – {issue.Message}";

    /// <summary>La fenêtre s'est fermée (validée, annulée, abandonnée).</summary>
    public event EventHandler? Closed;

    /// <summary>Le brouillon a changé (la vue redessine).</summary>
    public event EventHandler? DraftChanged;

    /// <summary>Désignation de l'objet dans les messages de validation (« show « X » »).</summary>
    protected abstract string ItemLabel { get; }

    /// <summary>Ouvre un objet du projet ; renvoie le motif d'un refus (un autre brouillon a des modifications).</summary>
    public string? Open(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (IsOpen && EditedId == IdOf(item))
        {
            return null;
        }

        if (IsOpen && HasChanges)
        {
            return $"« {ItemName} » a des modifications : validez ou annulez-la d'abord.";
        }

        if (IsOpen)
        {
            Close("remplacée");
        }

        _history.Clear();
        _original = item;
        Draft = item;
        _syncing = true;
        IsBlind = false;
        _syncing = false;
        Message = null;
        IsOpen = true;
        OnOpened();
        Publish();
        Runtime.TraceUi("Édition", $"ouverture de « {NameOf(item)} » ({Kind})");
        Journal?.Log($"✎ édition de « {NameOf(item)} » ({Kind}, brouillon)");
        return null;
    }

    /// <summary>Modifie le brouillon (un geste = une annulation).</summary>
    public void Change(Func<T, T> change, string description)
    {
        ArgumentNullException.ThrowIfNull(change);
        if (!IsOpen || Draft is null)
        {
            return;
        }

        var next = change(Draft);
        if (ReferenceEquals(next, Draft))
        {
            return;
        }

        _history.Record(Draft, description);
        Draft = next;
        Publish();
    }

    /// <summary>Rafraîchit ce qui dépend du moteur (supervision de l'essai), vingt fois par seconde.</summary>
    public virtual void Refresh() => Simulation.Refresh(EditedId);

    /// <summary>Écrit le brouillon dans le projet, sans fermer.</summary>
    [RelayCommand]
    public void Apply()
    {
        if (!IsOpen || Draft is null || Runtime.Project.Folder is null)
        {
            return;
        }

        Save(Draft);
        _original = Draft;
        Message = HasErrors ? "Enregistré, mais avec des erreurs : voir la liste des problèmes." : null;
        Journal?.Log($"✔ « {NameOf(Draft)} » appliqué(e)");

        // Essai P8, ex. 16c : l'erreur reste lisible au Journal après la fermeture de la fenêtre.
        foreach (var issue in Issues.Where(i => i.Severity == Fixtures.Rules.IssueSeverity.Error))
        {
            Journal?.Log($"⛔ « {NameOf(Draft)} » enregistré(e) avec une erreur, refusé(e) au lancement : {Short(issue)}");
        }

        Runtime.TraceUi("Édition", $"appliquer « {NameOf(Draft)} »");
        Publish();
    }

    /// <summary>
    /// Écrit le brouillon et ferme ; avec une erreur (refusée au lancement, SHOW-024), demande d'abord confirmation (essai P8,
    /// ex. 16c) : Non laisse la fenêtre ouverte pour corriger.
    /// </summary>
    [RelayCommand]
    public async Task ValidateAsync()
    {
        if (!IsOpen)
        {
            return;
        }

        if (HasErrors)
        {
            var errors = Issues.Count(i => i.Severity == Fixtures.Rules.IssueSeverity.Error);
            var save = await Dialogs.ConfirmAsync(
                "Enregistrer avec des erreurs ?",
                $"« {ItemName} » contient {errors} erreur(s) ⛔ : tant qu'elles restent, ce {Kind} sera refusé au lancement.{Environment.NewLine}{Environment.NewLine}Enregistrer quand même et fermer ? (Non : la fenêtre reste ouverte pour corriger.)").ConfigureAwait(true);
            if (!save || !IsOpen)
            {
                Message = "Pas enregistré : corrigez les erreurs ⛔, ou validez à nouveau pour enregistrer quand même.";
                return;
            }
        }

        Apply();
        Close("validée");
    }

    /// <summary>Abandonne le brouillon et ferme.</summary>
    [RelayCommand]
    public void Cancel()
    {
        if (IsOpen)
        {
            Close("annulée");
        }
    }

    /// <summary>Ctrl+Z.</summary>
    [RelayCommand]
    public void Undo()
    {
        if (Draft is not null && _history.Undo(Draft) is { } previous)
        {
            Draft = previous;
            Publish(rebuild: true);
        }
    }

    /// <summary>Ctrl+Y.</summary>
    [RelayCommand]
    public void Redo()
    {
        if (Draft is not null && _history.Redo(Draft) is { } next)
        {
            Draft = next;
            Publish(rebuild: true);
        }
    }

    /// <summary>La croix : ferme sans question s'il n'y a pas de modification, sinon demande (jamais de perte par mégarde).</summary>
    public async Task<bool> ConfirmCloseAsync()
    {
        if (!IsOpen)
        {
            return true;
        }

        if (!HasChanges)
        {
            Close("fermée sans modification");
            return true;
        }

        switch (await Dialogs.AskSaveAsync("Fermer la fenêtre d'édition", $"« {ItemName} » a des modifications non validées.{Environment.NewLine}{Environment.NewLine}Les valider avant de fermer ?").ConfigureAwait(true))
        {
            case SaveChoice.Save:
                await ValidateAsync().ConfigureAwait(true);
                return !IsOpen;
            case SaveChoice.Discard:
                Close("abandonnée");
                return true;
            default:
                return false;
        }
    }

    /// <summary>Fermeture de l'application : le brouillon est abandonné sans question.</summary>
    public void Abandon()
    {
        if (IsOpen)
        {
            Close("abandonnée");
        }
    }

    /// <summary>Identifiant d'un objet.</summary>
    protected abstract Guid IdOf(T item);

    /// <summary>Nom d'un objet.</summary>
    protected abstract string NameOf(T item);

    /// <summary>Couleur d'un objet.</summary>
    protected abstract string ColorOf(T item);

    /// <summary>Écrit l'objet dans le projet (remplace ou ajoute).</summary>
    protected abstract void Save(T item);

    /// <summary>Problèmes de l'objet (SHOW-024).</summary>
    protected abstract IReadOnlyList<CompileIssue> Validate(T item);

    /// <summary>Donne le brouillon aux séquenceurs (ou le retire, <paramref name="item"/> nul).</summary>
    protected abstract void PushDraft(T? item, bool previewOnly);

    /// <summary>Commande qui lance le brouillon à l'essai.</summary>
    protected abstract Messaging.Commands.SequencerCommand PlayCommandFor(bool stopping);

    /// <summary>À l'ouverture (remise à zéro de l'affichage).</summary>
    protected virtual void OnOpened()
    {
    }

    /// <summary>Le brouillon a changé : la vue redessine ; <paramref name="rebuild"/> = tout reconstruire (annuler, rétablir).</summary>
    protected virtual void OnDraftChanged(bool rebuild)
    {
    }

    partial void OnIsBlindChanged(bool value)
    {
        OnPropertyChanged(nameof(ModeColor));
        OnPropertyChanged(nameof(SafetyText));
        if (_syncing || !IsOpen)
        {
            return;
        }

        // Ce qui jouait à l'essai, de l'autre côté, s'arrête (en passant en aveugle : sur la sortie) ; on le relance du nouveau côté.
        Simulation.StopOn(output: value);
        Runtime.PreviewActive = value;
        PushDraft(Draft, value);
        Runtime.TraceUi("Édition", value ? "aveugle" : "sortie");
    }

    private void Publish(bool rebuild = false)
    {
        if (Draft is null)
        {
            return;
        }

        ItemName = NameOf(Draft);
        ItemColor = ColorOf(Draft);
        HasChanges = !ReferenceEquals(Draft, _original);
        UndoText = _history.UndoDescription is { } undo ? $"Annuler : {undo} (Ctrl+Z)" : "Rien à annuler";
        RedoText = _history.RedoDescription is { } redo ? $"Rétablir : {redo} (Ctrl+Y)" : "Rien à rétablir";
        Issues = Validate(Draft);
        OnPropertyChanged(nameof(Issues));
        OnPropertyChanged(nameof(IssuesText));
        OnPropertyChanged(nameof(IssueRows));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(EditedId));
        PushDraft(Draft, IsBlind);
        OnDraftChanged(rebuild);
        DraftChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Close(string what)
    {
        var name = ItemName;
        Simulation.StopOn(!IsBlind);
        Simulation.Release();
        if (IsBlind)
        {
            Runtime.PreviewActive = false;
        }

        IsOpen = false;
        HasChanges = false;
        Draft = null;
        _original = null;
        _history.Clear();
        PushDraft(null, false);
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(EditedId));
        Journal?.Log($"Édition de « {name} » {what}");
        Closed?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>Un problème du brouillon, dans la teinte de sa gravité.</summary>
/// <param name="Text">Texte, avec son icône (⛔ erreur, ⚠ avertissement).</param>
/// <param name="Color">Teinte.</param>
public sealed record IssueRow(string Text, string Color);
