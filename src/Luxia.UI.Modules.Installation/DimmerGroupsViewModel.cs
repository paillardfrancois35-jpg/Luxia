using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luxia.Hosting;
using Luxia.Patch.Model;
using Luxia.Patch.Rules;
using Luxia.UI.Controls;

namespace Luxia.UI.Modules.Installation;

/// <summary>
/// Onglet « Gestion des dimmers » de l'écran Installation (ERG-036, maquette 8) : l'arbre des groupes d'appareils (un
/// appareil dans un seul groupe, les appareils sans groupe restant dans le groupe implicite « non assigné »), les groupes
/// qui ont un dimmer et le détail du groupe choisi. Les niveaux affichés sont ceux du moment, en lecture seule : les
/// dimmers se jouent à l'écran de jeu et sur la platine MIDI n° 2. Tout est enregistré aussitôt (<c>groupes.json</c>).
/// </summary>
public sealed partial class DimmerGroupsViewModel : ViewModelBase
{
    /// <summary>Nombre de faders de la seconde platine MIDI (ERG-038) : au-delà, un dimmer n'a pas de fader.</summary>
    public const int PlatineFaders = 8;

    private readonly LuxiaRuntime _runtime;
    private readonly IDialogService _dialogs;
    private bool _loading;

    [ObservableProperty]
    private GroupRowViewModel? _selectedRow;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private bool _hasDimmer;

    [ObservableProperty]
    private ParentChoice? _selectedParent;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private string _flowTitle = string.Empty;

    /// <summary>Crée l'onglet.</summary>
    public DimmerGroupsViewModel(LuxiaRuntime runtime, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(dialogs);
        _runtime = runtime;
        _dialogs = dialogs;
        Reload();
    }

    /// <summary>Arbre aplati (parent avant enfants), puis le groupe implicite « non assigné ».</summary>
    public ObservableCollection<GroupRowViewModel> Rows { get; } = [];

    /// <summary>Parents possibles du groupe choisi (« racine » et tous les groupes qui ne sont pas lui ou l'un de ses descendants).</summary>
    public ObservableCollection<ParentChoice> ParentChoices { get; } = [];

    /// <summary>Appareils du groupe choisi (ou non assignés).</summary>
    public ObservableCollection<GroupFixtureRow> Members { get; } = [];

    /// <summary>Appareils qu'on peut ranger dans le groupe choisi (ceux qui n'y sont pas déjà).</summary>
    public ObservableCollection<GroupFixtureRow> Candidates { get; } = [];

    /// <summary>Étapes de l'intensité jusqu'aux appareils du groupe choisi (exemple avec les niveaux actuels).</summary>
    public ObservableCollection<FlowStep> Flow { get; } = [];

    /// <summary>Un vrai groupe (pas le groupe implicite) est choisi.</summary>
    public bool IsGroupSelected => SelectedRow is { IsUnassigned: false };

    /// <summary>Le groupe implicite « non assigné » est choisi.</summary>
    public bool IsUnassignedSelected => SelectedRow is { IsUnassigned: true };

    /// <summary>Un groupe (réel ou implicite) est choisi.</summary>
    public bool HasSelection => SelectedRow is not null;

    /// <summary>Titre du détail.</summary>
    public string DetailTitle => SelectedRow is null ? "Aucun groupe choisi" : $"Groupe « {SelectedRow.Name} »";

    /// <summary>Relit les groupes du projet et reconstruit l'arbre (garde le groupe choisi).</summary>
    public void Reload()
    {
        var keep = SelectedRow?.Id;
        var unassigned = SelectedRow?.IsUnassigned == true;
        _loading = true;
        try
        {
            var set = _runtime.Project.Groups;
            var installation = _runtime.Project.Installation;
            var known = installation.Fixtures.Select(f => f.Id).ToHashSet();
            var nameOf = installation.Fixtures.ToDictionary(f => f.Id, f => f.Name);
            var layout = GroupRules.Layout(set);
            var faders = layout.Where(n => n.Group.HasDimmer).Select((n, i) => (n.Group.Id, Fader: i + 1)).ToDictionary(x => x.Id, x => x.Fader);
            var children = layout.Select(n => n.Parent).ToList();
            var seen = new HashSet<Guid>();

            Rows.Clear();
            for (var i = 0; i < layout.Count; i++)
            {
                var node = layout[i];
                var direct = node.Group.FixtureIds.Where(f => known.Contains(f) && seen.Add(f)).ToList();
                var summary = direct.Count > 0
                    ? string.Join(", ", direct.Select(f => nameOf[f]))
                    : children.Contains(i) ? "(sous-groupes)" : "(vide)";
                Rows.Add(new GroupRowViewModel(node.Group, node.Depth, summary, faders.TryGetValue(node.Group.Id, out var fader) && fader <= PlatineFaders ? fader : null));
            }

            var free = known.Where(id => !seen.Contains(id)).ToList();
            Rows.Add(new GroupRowViewModel(
                new FixtureGroup { Id = Guid.Empty, Name = set.UnassignedName },
                0,
                free.Count == 0 ? "(aucun appareil)" : string.Join(", ", installation.Fixtures.Where(f => free.Contains(f.Id)).Select(f => f.Name)),
                null,
                isUnassigned: true));

            SelectedRow = unassigned
                ? Rows.Last()
                : Rows.FirstOrDefault(r => !r.IsUnassigned && r.Id == keep);
            LoadDetail();
        }
        finally
        {
            _loading = false;
        }

        RefreshLevels();
    }

    /// <summary>Lit les niveaux du moteur (lecture seule) : à appeler au rythme du rafraîchissement de l'écran.</summary>
    public void RefreshLevels()
    {
        var snapshot = _runtime.Engine.Snapshot;
        foreach (var row in Rows.Where(r => r.Group.HasDimmer))
        {
            var index = snapshot.Show.IndexOfDimmerGroup(row.Id);
            row.Level = index >= 0 && index < snapshot.DimmerLevels.Length ? snapshot.DimmerLevels[index] : 1;
        }

        UpdateFlow();
    }

    partial void OnSelectedRowChanged(GroupRowViewModel? value)
    {
        OnPropertyChanged(nameof(IsGroupSelected));
        OnPropertyChanged(nameof(IsUnassignedSelected));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(DetailTitle));
        if (!_loading)
        {
            LoadDetail();
        }
    }

    partial void OnHasDimmerChanged(bool value)
    {
        if (_loading || SelectedRow is not { IsUnassigned: false } row || row.Group.HasDimmer == value)
        {
            return;
        }

        Save(GroupEdits.SetDimmer(_runtime.Project.Groups, row.Id, value), value ? $"« {row.Name} » a maintenant un dimmer." : $"« {row.Name} » n'a plus de dimmer.");
    }

    partial void OnSelectedParentChanged(ParentChoice? value)
    {
        if (_loading || value is null || SelectedRow is not { IsUnassigned: false } row || row.Group.ParentId == value.Id)
        {
            return;
        }

        var moved = GroupEdits.Move(_runtime.Project.Groups, row.Id, value.Id);
        if (moved is null)
        {
            Message = "Un groupe ne peut pas être rangé sous lui-même ni sous l'un de ses sous-groupes.";
            Reload();
            return;
        }

        Save(moved, $"« {row.Name} » déplacé.");
    }

    /// <summary>Ajoute un groupe à la racine.</summary>
    [RelayCommand]
    private Task AddGroupAsync() => AddAsync(parent: null);

    /// <summary>Ajoute un sous-groupe au groupe choisi.</summary>
    [RelayCommand]
    private Task AddSubGroupAsync() => AddAsync(parent: SelectedRow is { IsUnassigned: false } row ? row.Id : null);

    private async Task AddAsync(Guid? parent)
    {
        var name = await _dialogs.AskTextAsync("Nouveau groupe", parent is null ? "Nom du groupe :" : "Nom du sous-groupe :");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var (set, group) = GroupEdits.Add(_runtime.Project.Groups, name, parent);
        Save(set, $"Groupe « {group.Name} » créé.");
        SelectedRow = Rows.FirstOrDefault(r => !r.IsUnassigned && r.Id == group.Id);
    }

    /// <summary>Applique le nom saisi au groupe choisi (ou au groupe implicite).</summary>
    [RelayCommand]
    private void ApplyName()
    {
        if (SelectedRow is not { } row || string.IsNullOrWhiteSpace(EditName))
        {
            return;
        }

        var name = EditName.Trim();
        if (row.IsUnassigned)
        {
            Save(_runtime.Project.Groups with { UnassignedName = name }, $"Groupe des appareils sans groupe renommé « {name} ».");
            return;
        }

        Save(GroupEdits.Rename(_runtime.Project.Groups, row.Id, name), $"Groupe renommé « {name} ».");
    }

    /// <summary>Supprime le groupe choisi : ses sous-groupes et ses appareils remontent chez son parent.</summary>
    [RelayCommand]
    private async Task DeleteGroupAsync()
    {
        if (SelectedRow is not { IsUnassigned: false } row)
        {
            return;
        }

        if (!await _dialogs.ConfirmAsync("Supprimer le groupe", $"Supprimer « {row.Name} » ? Ses sous-groupes et ses appareils remontent chez son parent (ou deviennent non assignés)."))
        {
            return;
        }

        Save(GroupEdits.Delete(_runtime.Project.Groups, row.Id), $"Groupe « {row.Name} » supprimé.");
        SelectedRow = null;
    }

    /// <summary>Monte (-1) ou descend (+1) le groupe choisi parmi ses frères.</summary>
    [RelayCommand]
    private void ShiftGroup(string delta)
    {
        if (SelectedRow is not { IsUnassigned: false } row)
        {
            return;
        }

        Save(GroupEdits.Shift(_runtime.Project.Groups, row.Id, int.Parse(delta, CultureInfo.InvariantCulture)), null);
    }

    /// <summary>Range un appareil dans le groupe choisi (il quitte son groupe actuel).</summary>
    [RelayCommand]
    private void AddFixture(GroupFixtureRow? fixture)
    {
        if (fixture is null || SelectedRow is not { IsUnassigned: false } row)
        {
            return;
        }

        Save(GroupEdits.Assign(_runtime.Project.Groups, fixture.Id, row.Id), $"{fixture.Name} rangé dans « {row.Name} ».");
    }

    /// <summary>Retire un appareil de son groupe : il devient non assigné.</summary>
    [RelayCommand]
    private void RemoveFixture(GroupFixtureRow? fixture)
    {
        if (fixture is null)
        {
            return;
        }

        Save(GroupEdits.Assign(_runtime.Project.Groups, fixture.Id, null), $"{fixture.Name} n'est plus dans un groupe.");
    }

    private void Save(FixtureGroupSet set, string? message)
    {
        var ids = _runtime.Project.Installation.Fixtures.Select(f => f.Id).ToList();
        try
        {
            _runtime.Project.SaveGroups(GroupEdits.Prune(set, ids));
            Message = message;
        }
        catch (IOException ex)
        {
            // Écriture refusée un instant par le poste (doc 03 §11) : message, pas de plantage.
            Message = "Enregistrement des groupes impossible : " + ex.Message;
        }

        Reload();
    }

    private void LoadDetail()
    {
        var wasLoading = _loading;
        _loading = true;
        try
        {
            Members.Clear();
            Candidates.Clear();
            ParentChoices.Clear();
            if (SelectedRow is not { } row)
            {
                EditName = string.Empty;
                HasDimmer = false;
                SelectedParent = null;
                return;
            }

            EditName = row.Name;
            HasDimmer = row.Group.HasDimmer;
            var set = _runtime.Project.Groups;
            var installation = _runtime.Project.Installation;
            var library = _runtime.Project.FixtureLibrary;
            string KindOf(PatchedFixture f) => library?.Find(f.FixtureTypeId)?.DisplayName ?? string.Empty;
            var groupOf = installation.Fixtures.ToDictionary(f => f.Id, f => GroupRules.GroupOf(set, f.Id));

            if (row.IsUnassigned)
            {
                foreach (var fixture in installation.Fixtures.Where(f => groupOf[f.Id] is null))
                {
                    Members.Add(new GroupFixtureRow(fixture.Id, fixture.Name, KindOf(fixture), string.Empty));
                }

                return;
            }

            foreach (var fixture in installation.Fixtures)
            {
                if (groupOf[fixture.Id]?.Id == row.Id)
                {
                    Members.Add(new GroupFixtureRow(fixture.Id, fixture.Name, KindOf(fixture), string.Empty));
                }
                else
                {
                    var where = groupOf[fixture.Id] is { } other ? $"groupe « {other.Name} »" : $"« {set.UnassignedName} »";
                    Candidates.Add(new GroupFixtureRow(fixture.Id, fixture.Name, KindOf(fixture), where));
                }
            }

            ParentChoices.Add(new ParentChoice(null, "(racine)"));
            foreach (var other in set.Groups.Where(g => !GroupEdits.IsSelfOrDescendant(set, g.Id, row.Id)))
            {
                ParentChoices.Add(new ParentChoice(other.Id, other.Name));
            }

            SelectedParent = ParentChoices.FirstOrDefault(c => c.Id == row.Group.ParentId) ?? ParentChoices[0];
        }
        finally
        {
            _loading = wasLoading;
        }

        UpdateFlow();
    }

    // Flux d'intensité jusqu'aux appareils du groupe (règle proportionnelle, ERG-037) : Grand Master, puis chaque étage de la racine au groupe.
    private void UpdateFlow()
    {
        Flow.Clear();
        if (SelectedRow is not { IsUnassigned: false } row)
        {
            FlowTitle = string.Empty;
            return;
        }

        var set = _runtime.Project.Groups;
        var snapshot = _runtime.Engine.Snapshot;
        double LevelOf(Guid id)
        {
            var index = snapshot.Show.IndexOfDimmerGroup(id);
            return index >= 0 && index < snapshot.DimmerLevels.Length ? snapshot.DimmerLevels[index] : 1;
        }

        var chain = new List<FixtureGroup>();
        Guid? cursor = row.Id;
        for (var steps = 0; cursor is { } id && steps <= set.Groups.Count; steps++)
        {
            var group = set.Groups.FirstOrDefault(g => g.Id == id);
            if (group is null)
            {
                break;
            }

            chain.Insert(0, group);
            cursor = group.ParentId;
        }

        var total = 1.0;
        Flow.Add(new FlowStep("Scène / couches", "100 %", false));
        Flow.Add(new FlowStep("Grand Master", Percent(snapshot.GrandMaster), false));
        total *= snapshot.GrandMaster;
        foreach (var group in chain.Where(g => g.HasDimmer))
        {
            var level = LevelOf(group.Id);
            total *= level;
            Flow.Add(new FlowStep(group.Name, Percent(level), false));
        }

        Flow.Add(new FlowStep("Sortie DMX", Percent(total), true));
        FlowTitle = $"Flux d'intensité jusqu'aux appareils du groupe « {row.Name} » — exemple avec les niveaux actuels (lecture seule)";
    }

    private static string Percent(double value) => string.Create(CultureInfo.CurrentCulture, $"{Math.Round(value * 100)} %");
}

/// <summary>Une ligne de l'arbre des groupes.</summary>
public sealed partial class GroupRowViewModel : ObservableObject
{
    [ObservableProperty]
    private double _level = 1;

    /// <summary>Crée la ligne.</summary>
    public GroupRowViewModel(FixtureGroup group, int depth, string summary, int? fader, bool isUnassigned = false)
    {
        Group = group;
        Depth = depth;
        Summary = summary;
        Fader = fader;
        IsUnassigned = isUnassigned;
    }

    /// <summary>Groupe (le groupe implicite est un groupe factice d'identifiant vide).</summary>
    public FixtureGroup Group { get; }

    /// <summary>Identifiant du groupe.</summary>
    public Guid Id => Group.Id;

    /// <summary>Nom affiché.</summary>
    public string Name => Group.Name;

    /// <summary>Profondeur dans l'arbre.</summary>
    public int Depth { get; }

    /// <summary>Retrait de la ligne selon la profondeur.</summary>
    public Thickness Indent => new(Depth * 18, 0, 0, 0);

    /// <summary>Appareils rangés directement dans le groupe.</summary>
    public string Summary { get; }

    /// <summary>Numéro de fader sur la seconde platine (ERG-038), ou <c>null</c> (pas de dimmer ou au-delà de 8).</summary>
    public int? Fader { get; }

    /// <summary>Repère de fader (« ② 3 »).</summary>
    public string FaderText => Fader is { } n ? $"② {n}" : string.Empty;

    /// <summary>C'est le groupe implicite des appareils sans groupe.</summary>
    public bool IsUnassigned { get; }

    /// <summary>Le groupe a un dimmer (le niveau actuel est alors affiché).</summary>
    public bool HasDimmer => Group.HasDimmer && !IsUnassigned;

    /// <summary>Niveau actuel en pour cent, pour la barre.</summary>
    public double LevelPercent => Level * 100;

    /// <summary>Niveau actuel en clair.</summary>
    public string LevelText => string.Create(CultureInfo.CurrentCulture, $"{Math.Round(Level * 100)} %");

    partial void OnLevelChanged(double value)
    {
        OnPropertyChanged(nameof(LevelPercent));
        OnPropertyChanged(nameof(LevelText));
    }
}

/// <summary>Parent possible d'un groupe (« racine » = <c>null</c>).</summary>
/// <param name="Id">Groupe parent, ou <c>null</c> pour la racine.</param>
/// <param name="Label">Libellé affiché.</param>
public sealed record ParentChoice(Guid? Id, string Label)
{
    /// <inheritdoc />
    public override string ToString() => Label;
}

/// <summary>Un appareil dans la liste d'un groupe ou parmi ceux qu'on peut y ranger.</summary>
/// <param name="Id">Appareil.</param>
/// <param name="Name">Nom.</param>
/// <param name="Kind">Modèle.</param>
/// <param name="Where">Où il est rangé actuellement (pour les candidats).</param>
public sealed record GroupFixtureRow(Guid Id, string Name, string Kind, string Where);

/// <summary>Une étape du flux d'intensité.</summary>
/// <param name="Label">Nom de l'étage.</param>
/// <param name="Value">Niveau.</param>
/// <param name="IsResult">Dernière étape (la sortie).</param>
public sealed record FlowStep(string Label, string Value, bool IsResult);
