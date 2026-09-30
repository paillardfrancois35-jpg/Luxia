using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Control;

/// <summary>
/// Fenêtre d'édition d'une scène (chantier « Contrôle 2 », ERG-033 et ERG-034, maquettes 6 et 7) : on travaille sur un
/// <b>brouillon</b> de la scène. <b>Appliquer</b> l'écrit dans le projet sans fermer, <b>Valider</b> l'écrit et ferme,
/// <b>Annuler</b> revient à l'état d'origine, sans question. La case <b>Aveugle</b> ne montre le brouillon qu'à l'aperçu :
/// la sortie sur scène ne change pas. Non bloquante : la sortie et l'écran de jeu continuent pendant l'édition.
/// </summary>
/// <remarks>
/// Les panneaux d'édition (Plan, Réglages, Effets, Propriétés) sont ceux de l'établi <see cref="ControlViewModel"/>, avec
/// sa propre session en mode brouillon (<see cref="ControlSession.IsDraft"/>) ; l'écran de jeu n'en a plus.
/// </remarks>
public sealed partial class EditorViewModel : ViewModelBase
{
    private readonly LuxiaRuntime _runtime;
    private readonly IDialogService _dialogs;
    private bool _syncing;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _sceneName = string.Empty;

    [ObservableProperty]
    private string _sceneColor = ControlColors.Edit;

    [ObservableProperty]
    private bool _hasChanges;

    [ObservableProperty]
    private bool _isBlind;

    [ObservableProperty]
    private string? _message;

    /// <summary>Crée la fenêtre d'édition (fermée).</summary>
    public EditorViewModel(LuxiaRuntime runtime, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(dialogs);
        _runtime = runtime;
        _dialogs = dialogs;
        Workbench = new ControlViewModel(runtime, dialogs);
        Workbench.Session.Changed += (_, _) => UpdateState();
    }

    /// <summary>Panneaux d'édition et session en mode brouillon.</summary>
    public ControlViewModel Workbench { get; }

    /// <summary>Session d'édition (brouillon).</summary>
    public ControlSession Session => Workbench.Session;

    /// <summary>Scène ouverte, ou <c>null</c>.</summary>
    public Guid? SceneId => IsOpen ? Session.EditScene?.Id : null;

    /// <summary>Titre de la fenêtre.</summary>
    public string WindowTitle => IsOpen ? $"LuXia – Édition de « {SceneName} » (brouillon)" : "LuXia – Édition d'une scène";

    /// <summary>Couleur du mode : vert (la sortie montre le brouillon) ou bleu clair (aveugle).</summary>
    public string ModeColor => IsBlind ? ControlColors.Blind : ControlColors.Edit;

    /// <summary>Ce que la case Aveugle change, en toutes lettres.</summary>
    public string SafetyText => IsBlind
        ? "Aperçu au plan seulement : la sortie sur scène ne change pas."
        : "Le brouillon est montré sur la sortie (un fondu de 0,5 s à chaque retouche). Cochez pour ne rien changer sur scène.";

    /// <summary>Levé quand la fenêtre se ferme (Valider, Annuler) ou perd son brouillon (projet rouvert).</summary>
    public event EventHandler? Closed;

    /// <summary>
    /// Ouvre le brouillon d'une scène. Renvoie la raison d'un refus (verrou soirée, scène introuvable, autre scène modifiée).
    /// Rouvrir la scène déjà ouverte ne fait rien : la fenêtre passe au premier plan.
    /// </summary>
    public string? Open(Guid sceneId)
    {
        if (IsOpen && SceneId == sceneId)
        {
            return null;
        }

        if (IsOpen && HasChanges)
        {
            return $"« {SceneName} » a des modifications : validez ou annulez-la d'abord.";
        }

        var reason = Session.BeginDraft(sceneId);
        if (reason is not null)
        {
            return reason;
        }

        _syncing = true;
        IsBlind = false;
        _syncing = false;
        Message = null;
        IsOpen = true;
        UpdateState();
        _runtime.TraceUi("Édition", $"ouverture de « {SceneName} »");
        Workbench.Journal.Log($"✎ édition de « {SceneName} » (brouillon)");
        return null;
    }

    /// <summary>Rafraîchit les panneaux (20 fois par seconde, par la fenêtre) et écrit le geste en cours à son heure.</summary>
    public void Refresh()
    {
        if (!IsOpen)
        {
            return;
        }

        Workbench.Refresh();
        UpdateState();
    }

    /// <summary><b>Appliquer</b> : écrit la scène dans le projet, sans fermer.</summary>
    [RelayCommand]
    public void Apply()
    {
        if (!IsOpen)
        {
            return;
        }

        Workbench.Flush();
        Session.ApplyDraft();
        Message = Session.Message;
        _runtime.TraceUi("Édition", $"appliquer « {SceneName} »");
        Workbench.Journal.Log($"✔ « {SceneName} » appliquée");
        UpdateState();
    }

    /// <summary><b>Valider</b> : écrit la scène dans le projet et ferme.</summary>
    [RelayCommand]
    public void Validate()
    {
        if (!IsOpen)
        {
            return;
        }

        Apply();
        if (Session.Message is not null)
        {
            return;
        }

        Close("validée");
    }

    /// <summary><b>Annuler</b> : la scène revient à l'état d'origine (ou à la dernière application) ; la fenêtre se ferme, sans question.</summary>
    [RelayCommand]
    public void Cancel()
    {
        if (!IsOpen)
        {
            return;
        }

        Workbench.Flush();
        Session.DiscardDraft();
        Close("annulée");
    }

    /// <summary>Annuler (Ctrl+Z) dans le brouillon.</summary>
    [RelayCommand]
    public void Undo() => Workbench.Undo();

    /// <summary>Rétablir (Ctrl+Y) dans le brouillon.</summary>
    [RelayCommand]
    public void Redo() => Workbench.Redo();

    /// <summary>
    /// Fermeture demandée par la croix de la fenêtre : sans modification, la fenêtre se ferme ; avec des modifications, on demande
    /// s'il faut les abandonner (Oui : la scène revient à l'état d'origine ; Non : la fenêtre reste, Valider et Appliquer
    /// sont dans le pied). Renvoie vrai si la fenêtre est fermée.
    /// </summary>
    public async Task<bool> ConfirmCloseAsync()
    {
        if (!IsOpen)
        {
            return true;
        }

        Workbench.Flush();
        UpdateState();
        if (!HasChanges)
        {
            Close("fermée sans modification");
            return true;
        }

        var abandon = await _dialogs.ConfirmAsync(
            "Fermer la fenêtre d'édition",
            $"« {SceneName} » a des modifications non validées.{Environment.NewLine}{Environment.NewLine}Oui : les abandonner (la scène revient à l'état d'origine).{Environment.NewLine}Non : garder la fenêtre ouverte (Valider ou Appliquer dans le pied de la fenêtre).").ConfigureAwait(true);
        if (!abandon)
        {
            return false;
        }

        Session.DiscardDraft();
        Close("abandonnée");
        return true;
    }

    /// <summary>Ferme sans rien demander (fin de l'application) : le brouillon est abandonné.</summary>
    public void Abandon()
    {
        if (IsOpen)
        {
            Session.DiscardDraft();
            Close("abandonnée");
        }
    }

    partial void OnIsBlindChanged(bool value)
    {
        OnPropertyChanged(nameof(ModeColor));
        OnPropertyChanged(nameof(SafetyText));
        if (_syncing || !IsOpen)
        {
            return;
        }

        Workbench.SetModeCommand.Execute(value ? "aveugle" : "edition");
        Message = Workbench.Message;
        _runtime.TraceUi("Édition", value ? "aveugle" : "sortie");
    }

    private void Close(string what)
    {
        var name = SceneName;

        // Fermée d'abord : la fin du brouillon prévient la session, que UpdateState prendrait pour un projet rouvert.
        IsOpen = false;
        HasChanges = false;
        SceneName = string.Empty;
        Session.EndDraft();
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(SceneId));
        Workbench.Journal.Log($"Édition de « {name} » {what}");
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateState()
    {
        if (IsOpen && !Session.IsDraft)
        {
            // Projet rouvert ou fermé pendant l'édition : le brouillon n'existe plus.
            IsOpen = false;
            HasChanges = false;
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(SceneId));
            Closed?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (!IsOpen)
        {
            return;
        }

        var scene = Session.EditScene;
        SceneName = scene?.Name ?? SceneName;
        SceneColor = scene?.Color ?? SceneColor;
        HasChanges = Session.HasDraftChanges;
        if (!HasChanges)
        {
            // Le refus ou l'avertissement ne vaut plus quand le brouillon redevient identique à la scène (Ctrl+Z, Appliquer).
            Message = null;
        }

        var blind = Session.Mode == EditMode.Blind;
        if (blind != IsBlind)
        {
            _syncing = true;
            IsBlind = blind;
            _syncing = false;
        }

        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(SceneId));
    }
}
