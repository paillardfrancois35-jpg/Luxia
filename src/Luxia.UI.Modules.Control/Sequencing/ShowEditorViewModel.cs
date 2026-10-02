using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.Messaging.Commands;
using Luxia.Scenes.Compilation;
using Luxia.Show.Model;
using Luxia.Show.Rules;
using Luxia.UI.Controls;
using Luxia.UI.Modules.Scenes;

namespace Luxia.UI.Modules.Control.Sequencing;

/// <summary>
/// Fenêtre d'édition d'un show (SHOW-020 à SHOW-031, maquette 12, Q45) : une carte par étape (actions, puis transitions sous elle),
/// un diagramme dessiné automatiquement pour lire le Grafcet, l'essai sans musique, les réglages pour le pilote automatique.
/// </summary>
public sealed partial class ShowEditorViewModel : DraftEditorViewModel<ShowDefinition>
{
    private bool _loading;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private Choice<ShowEnd> _atEnd = ShowOptions.Ends[0];

    [ObservableProperty]
    private Choice<ShowRole> _role = ShowOptions.Roles[0];

    [ObservableProperty]
    private string _styles = string.Empty;

    [ObservableProperty]
    private Choice<int> _energyMin = ShowOptions.EnergyLevels[0];

    [ObservableProperty]
    private Choice<int> _energyMax = ShowOptions.EnergyLevels[3];

    [ObservableProperty]
    private decimal _weight = 1;

    [ObservableProperty]
    private bool _secondary;

    [ObservableProperty]
    private string _variables = string.Empty;

    /// <summary>Crée l'éditeur.</summary>
    public ShowEditorViewModel(LuxiaRuntime runtime, IDialogService dialogs, JournalPanelViewModel? journal = null)
        : base(runtime, dialogs, journal)
    {
    }

    /// <inheritdoc />
    public override string Kind => "show";

    /// <summary>Cartes des étapes, dans l'ordre du show.</summary>
    public ObservableCollection<StepCardViewModel> Cards { get; } = [];

    /// <summary>Diagramme : étapes placées en rangées (distance depuis les étapes initiales).</summary>
    public IReadOnlyList<DiagramNode> Nodes { get; private set; } = [];

    /// <summary>Diagramme : liaisons d'une étape à une autre.</summary>
    public IReadOnlyList<DiagramEdge> Edges { get; private set; } = [];

    /// <summary>Scènes du projet.</summary>
    public IReadOnlyList<Choice<Guid>> SceneChoices { get; private set; } = [];

    /// <summary>Séquences du projet.</summary>
    public IReadOnlyList<Choice<Guid>> SequenceChoices { get; private set; } = [];

    /// <summary>Couches du projet.</summary>
    public IReadOnlyList<Choice<Guid>> LayerChoices { get; private set; } = [];

    /// <summary>Autres shows (macro-étapes) ; le premier choix est « aucun ».</summary>
    public IReadOnlyList<Choice<Guid>> MacroChoices { get; private set; } = [];

    /// <summary>Nom d'une scène, d'une séquence, d'une couche ou d'un show.</summary>
    public string? NameOf(Guid id) =>
        Runtime.Project.Scenes.Scenes.FirstOrDefault(s => s.Id == id)?.Name
        ?? Runtime.Project.Sequences.Sequences.FirstOrDefault(s => s.Id == id)?.Name
        ?? Runtime.Project.Layers.Layers.FirstOrDefault(l => l.Id == id)?.Name
        ?? Runtime.Project.Shows.Shows.FirstOrDefault(s => s.Id == id)?.Name;

    /// <inheritdoc />
    protected override string ItemLabel => Draft is null ? string.Empty : ShowRules.ShowItem(Draft);

    /// <inheritdoc />
    public override void Refresh()
    {
        base.Refresh();
        var active = Simulation.ActiveSteps;
        var changed = false;
        foreach (var card in Cards)
        {
            var isActive = active.Contains(card.Id);
            changed |= card.IsActive != isActive;
            card.IsActive = isActive;
        }

        if (changed)
        {
            BuildDiagram();
        }
    }

    /// <summary>Nouvelle étape (identifiant suivant libre), à la fin.</summary>
    [RelayCommand]
    private void AddStep()
    {
        if (Draft is null)
        {
            return;
        }

        var id = NextId();
        Change(s => s with { Steps = [.. s.Steps, new ShowStep { Id = id, Name = $"Étape {id}", Initial = s.Steps.Count == 0 }] }, "Nouvelle étape");
    }

    /// <summary>Supprime une étape et les transitions qui la touchent.</summary>
    public void DeleteStep(string id)
    {
        Change(
            s => s with
            {
                Steps = [.. s.Steps.Where(x => x.Id != id)],
                Transitions = [.. s.Transitions.Where(t => !t.From.Contains(id) && !t.To.Contains(id))],
            },
            $"Supprimer l'étape {id}");
    }

    /// <summary>Change une étape.</summary>
    public void UpdateStep(string id, Func<ShowStep, ShowStep> change, string description)
    {
        ArgumentNullException.ThrowIfNull(change);
        if (_loading)
        {
            return;
        }

        Change(s => s with { Steps = [.. s.Steps.Select(x => x.Id == id ? change(x) : x)] }, description);
    }

    /// <summary>Renomme l'identifiant d'une étape (les transitions suivent) ; refusé s'il est vide ou déjà pris.</summary>
    public bool RenameStep(string id, string newId)
    {
        newId = newId.Trim();
        if (_loading || Draft is null || newId.Length == 0 || newId == id || Draft.Steps.Any(s => s.Id == newId))
        {
            return false;
        }

        string Map(string x) => x == id ? newId : x;
        Change(
            s => s with
            {
                Steps = [.. s.Steps.Select(x => x.Id == id ? x with { Id = newId } : x)],
                Transitions = [.. s.Transitions.Select(t => t with { From = [.. t.From.Select(Map)], To = [.. t.To.Select(Map)] })],
            },
            "Identifiant de l'étape");
        return true;
    }

    /// <summary>Ajoute une transition qui part de l'étape (vers elle-même par défaut, à régler).</summary>
    public void AddTransition(string from)
    {
        if (Draft is null)
        {
            return;
        }

        var to = Draft.Steps.SkipWhile(s => s.Id != from).Skip(1).FirstOrDefault()?.Id ?? Draft.Steps[0].Id;
        Change(s => s with { Transitions = [.. s.Transitions, new ShowTransition { From = [from], To = [to], Condition = new ShowCondition { Kind = ConditionKind.Drop }, Quantize = ShowQuantize.Bar }] }, "Nouvelle transition");
    }

    /// <summary>Change une transition.</summary>
    public void UpdateTransition(int index, Func<ShowTransition, ShowTransition> change, string description)
    {
        ArgumentNullException.ThrowIfNull(change);
        if (_loading)
        {
            return;
        }

        Change(s => s with { Transitions = [.. s.Transitions.Select((t, i) => i == index ? change(t) : t)] }, description);
    }

    /// <summary>Supprime une transition.</summary>
    public void DeleteTransition(int index) =>
        Change(s => s with { Transitions = [.. s.Transitions.Where((_, i) => i != index)] }, "Supprimer la transition");

    /// <summary>Déplace une transition d'un rang (priorité, R4).</summary>
    public void MoveTransition(int index, int delta)
    {
        if (Draft is null || index + delta < 0 || index + delta >= Draft.Transitions.Count)
        {
            return;
        }

        Change(
            s =>
            {
                var list = s.Transitions.ToList();
                (list[index], list[index + delta]) = (list[index + delta], list[index]);
                return s with { Transitions = list };
            },
            "Priorité de la transition");
    }

    /// <inheritdoc />
    protected override Guid IdOf(ShowDefinition item) => item.Id;

    /// <inheritdoc />
    protected override string NameOf(ShowDefinition item) => item.Name;

    /// <inheritdoc />
    protected override string ColorOf(ShowDefinition item) => item.Color;

    /// <inheritdoc />
    protected override void Save(ShowDefinition item)
    {
        var set = Runtime.Project.Shows;
        var list = set.Shows.ToList();
        var index = list.FindIndex(s => s.Id == item.Id);
        if (index >= 0)
        {
            list[index] = item;
        }
        else
        {
            list.Add(item);
        }

        Runtime.Project.SaveShows(set with { Shows = list });
    }

    /// <inheritdoc />
    protected override IReadOnlyList<CompileIssue> Validate(ShowDefinition item) =>
        ShowRules.Validate(item, Runtime.Project.Sequences, Runtime.Project.Shows with { Shows = [.. Runtime.Project.Shows.Shows.Where(s => s.Id != item.Id), item] }, Runtime.Project.Scenes, Runtime.Project.Layers);

    /// <inheritdoc />
    protected override void PushDraft(ShowDefinition? item, bool previewOnly) => Runtime.SetSequencingDraft(null, item, previewOnly);

    /// <inheritdoc />
    protected override SequencerCommand PlayCommandFor(bool stopping) => stopping
        ? new StopShowCommand(CommandOrigin.User, Draft?.Id ?? Guid.Empty)
        : new LaunchShowCommand(CommandOrigin.User, Draft?.Id ?? Guid.Empty);

    /// <inheritdoc />
    protected override void OnOpened()
    {
        var project = Runtime.Project;
        SceneChoices = [.. project.Scenes.Scenes.Select(s => new Choice<Guid>(s.Id, s.Name))];
        SequenceChoices = [.. project.Sequences.Sequences.Select(s => new Choice<Guid>(s.Id, s.Name))];
        LayerChoices = [.. project.Layers.Layers.OrderBy(l => l.Priority).Select(l => new Choice<Guid>(l.Id, l.Name))];
        OnPropertyChanged(nameof(SceneChoices));
        OnPropertyChanged(nameof(SequenceChoices));
        OnPropertyChanged(nameof(LayerChoices));
        Cards.Clear();
    }

    /// <inheritdoc />
    protected override void OnDraftChanged(bool rebuild)
    {
        if (Draft is null)
        {
            return;
        }

        _loading = true;
        try
        {
            Name = Draft.Name;
            AtEnd = ShowOptions.Ends.FirstOrDefault(e => e.Value == Draft.AtEnd) ?? ShowOptions.Ends[0];
            Role = ShowOptions.Roles.FirstOrDefault(r => r.Value == Draft.Role) ?? ShowOptions.Roles[0];
            Styles = string.Join(", ", Draft.Styles);
            EnergyMin = ShowOptions.EnergyLevels[Math.Clamp(Draft.EnergyMin, 0, 3)];
            EnergyMax = ShowOptions.EnergyLevels[Math.Clamp(Draft.EnergyMax, 0, 3)];
            Weight = (decimal)Draft.Weight;
            Secondary = Draft.Secondary;
            if (Variables.Split(',').Select(v => v.Trim()).Where(v => v.Length > 0).SequenceEqual(Draft.Variables.Select(v => v.Name)) is false)
            {
                Variables = string.Join(", ", Draft.Variables.Select(v => v.Name));
            }

            MacroChoices = [new Choice<Guid>(Guid.Empty, "(aucun : étape simple)"), .. Runtime.Project.Shows.Shows.Where(s => s.Id != Draft.Id).Select(s => new Choice<Guid>(s.Id, s.Name))];
            OnPropertyChanged(nameof(MacroChoices));

            // Les cartes ne sont refaites qu'aux changements de structure (pour ne pas perdre le champ en cours de frappe).
            var structure = string.Join("|", Draft.Steps.Select(s => $"{s.Id}:{s.Actions.Count}:{string.Join(",", Draft.Transitions.Select((t, i) => (t, i)).Where(x => (x.t.From.Count > 0 ? x.t.From[0] : null) == s.Id).Select(x => x.i))}"));
            if (rebuild || structure != _structure || Cards.Count != Draft.Steps.Count)
            {
                _structure = structure;
                Cards.Clear();
                foreach (var step in Draft.Steps)
                {
                    Cards.Add(new StepCardViewModel(this, step, Draft));
                }
            }
            else
            {
                foreach (var card in Cards)
                {
                    card.Refresh(Draft);
                }
            }
        }
        finally
        {
            _loading = false;
        }

        BuildDiagram();
    }

    private string _structure = string.Empty;

    partial void OnNameChanged(string value)
    {
        if (!_loading && !string.IsNullOrWhiteSpace(value))
        {
            Change(s => s with { Name = value.Trim() }, "Nom du show");
        }
    }

    partial void OnAtEndChanged(Choice<ShowEnd> value)
    {
        if (!_loading && value is not null)
        {
            Change(s => s with { AtEnd = value.Value }, "Fin du show");
        }
    }

    partial void OnRoleChanged(Choice<ShowRole> value)
    {
        if (!_loading && value is not null)
        {
            Change(s => s with { Role = value.Value }, "Rôle du show");
        }
    }

    partial void OnStylesChanged(string value)
    {
        if (!_loading)
        {
            Change(s => s with { Styles = [.. value.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0)] }, "Styles visés");
        }
    }

    partial void OnEnergyMinChanged(Choice<int> value)
    {
        if (!_loading && value is not null)
        {
            Change(s => s with { EnergyMin = value.Value }, "Énergie minimale");
        }
    }

    partial void OnEnergyMaxChanged(Choice<int> value)
    {
        if (!_loading && value is not null)
        {
            Change(s => s with { EnergyMax = value.Value }, "Énergie maximale");
        }
    }

    partial void OnWeightChanged(decimal value)
    {
        if (!_loading && value > 0)
        {
            Change(s => s with { Weight = (double)value }, "Poids du show");
        }
    }

    partial void OnSecondaryChanged(bool value)
    {
        if (!_loading)
        {
            Change(s => s with { Secondary = value }, value ? "Show secondaire" : "Show principal");
        }
    }

    partial void OnVariablesChanged(string value)
    {
        if (!_loading)
        {
            var names = value.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.Ordinal).ToList();
            Change(s => s with { Variables = [.. names.Select(n => s.Variables.FirstOrDefault(v => v.Name == n) ?? new ShowVariable(n))] }, "Variables du show");
        }
    }

    private string NextId()
    {
        var used = Draft?.Steps.Select(s => s.Id).ToHashSet(StringComparer.Ordinal) ?? [];
        for (var i = 0; ; i++)
        {
            var id = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!used.Contains(id))
            {
                return id;
            }
        }
    }

    // Rangées : distance (en transitions) depuis les étapes initiales ; une étape inatteignable va en dernière rangée.
    private void BuildDiagram()
    {
        if (Draft is null)
        {
            Nodes = [];
            Edges = [];
            OnPropertyChanged(nameof(Nodes));
            OnPropertyChanged(nameof(Edges));
            return;
        }

        var depth = new Dictionary<string, int>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        foreach (var step in Draft.Steps.Where(s => s.Initial))
        {
            depth[step.Id] = 0;
            queue.Enqueue(step.Id);
        }

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            foreach (var to in Draft.Transitions.Where(t => t.From.Contains(id)).SelectMany(t => t.To))
            {
                if (!depth.ContainsKey(to) && Draft.Steps.Any(s => s.Id == to))
                {
                    depth[to] = depth[id] + 1;
                    queue.Enqueue(to);
                }
            }
        }

        var last = depth.Count == 0 ? 0 : depth.Values.Max() + 1;
        var active = Simulation.ActiveSteps;
        var columns = new Dictionary<int, int>();
        var nodes = new List<DiagramNode>();
        foreach (var step in Draft.Steps)
        {
            var row = depth.TryGetValue(step.Id, out var d) ? d : last;
            var column = columns.GetValueOrDefault(row);
            columns[row] = column + 1;
            nodes.Add(new DiagramNode(step.Id, step.Name, row, column, step.Initial, active.Contains(step.Id), step.MacroShowId is not null));
        }

        var edges = new List<DiagramEdge>();
        foreach (var transition in Draft.Transitions)
        {
            var label = ShowTexts.Label(transition, NameOf);
            if (transition.Quantize != ShowQuantize.None)
            {
                label += " ⏱";
            }

            foreach (var from in transition.From)
            {
                foreach (var to in transition.To)
                {
                    if (nodes.Any(n => n.Id == from) && nodes.Any(n => n.Id == to))
                    {
                        edges.Add(new DiagramEdge(from, to, transition.Weight != 1 ? $"{label} · {transition.Weight:0.##}" : label));
                    }
                }
            }
        }

        Nodes = nodes;
        Edges = edges;
        OnPropertyChanged(nameof(Nodes));
        OnPropertyChanged(nameof(Edges));
    }
}

/// <summary>Une étape du diagramme.</summary>
/// <param name="Id">Identifiant.</param>
/// <param name="Name">Nom.</param>
/// <param name="Row">Rangée (distance depuis les étapes initiales).</param>
/// <param name="Column">Place dans la rangée.</param>
/// <param name="Initial">Étape initiale (double bord).</param>
/// <param name="Active">Active pendant l'essai.</param>
/// <param name="Macro">Macro-étape.</param>
public sealed record DiagramNode(string Id, string Name, int Row, int Column, bool Initial, bool Active, bool Macro);

/// <summary>Une liaison du diagramme.</summary>
/// <param name="From">Étape amont.</param>
/// <param name="To">Étape aval.</param>
/// <param name="Label">Condition en clair.</param>
public sealed record DiagramEdge(string From, string To, string Label);
