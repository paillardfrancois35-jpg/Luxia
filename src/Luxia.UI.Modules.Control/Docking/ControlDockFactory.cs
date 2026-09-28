using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace Luxia.UI.Modules.Control.Docking;

/// <summary>
/// Fabrique Dock de l'écran Contrôle (ERG-001, E4) : disposition livrée (doc 60 §6 : colonnes au centre, propriétés à
/// droite, plan et réglages en bas, journal en onglet), réaffichage d'un panneau fermé. Tous les panneaux sont de
/// simples <see cref="Tool"/> identifiés par leur <see cref="IDockable.Id"/> : la disposition enregistrée ne contient
/// que des types de la bibliothèque ; leur contenu (<see cref="IDockable.Context"/>) est rendu par
/// la fabrique (ContextLocator) et par la vue à chaque initialisation.
/// </summary>
public sealed class ControlDockFactory : Factory
{
    private readonly IReadOnlyDictionary<string, Func<object?>> _contexts;

    /// <summary>Crée la fabrique ; <paramref name="contexts"/> donne le modèle de vue de chaque panneau.</summary>
    public ControlDockFactory(IReadOnlyDictionary<string, Func<object?>> contexts)
    {
        _contexts = contexts;

        // Fermer un panneau le masque : il revient par « Panneaux ▾ ».
        HideToolsOnClose = true;
    }

    /// <summary>Disposition prête que construit <see cref="CreateLayout()"/> (et où revient un panneau réaffiché).</summary>
    public ControlLayoutPreset Preset { get; set; }

    /// <summary>Crée la fenêtre d'un panneau détaché (remplaçable pour les tests, sans fenêtrage).</summary>
    public Func<IHostWindow?> HostWindowFactory { get; init; } = () => new ControlHostWindow();

    /// <inheritdoc />
    public override IRootDock CreateLayout() => Preset == ControlLayoutPreset.Show ? CreateShow() : CreateControl();

    // Contrôle (doc 60 §6) : colonnes au centre, propriétés et looks à droite, plan + réglages en bas, journal en onglet.
    private IRootDock CreateControl()
    {
        var bottom = Split(0.46, Orientation.Horizontal, Tools(0.34, ControlPanels.Plan), Tools(0.66, ControlPanels.Settings, ControlPanels.Journal));
        var left = Split(0.78, Orientation.Vertical, Tools(0.58, ControlPanels.Columns), bottom);
        var right = Split(0.24, Orientation.Vertical, Tools(0.66, ControlPanels.Properties), Tools(0.34, ControlPanels.Looks));
        var main = Split(double.NaN, Orientation.Horizontal, left, right);
        main.Id = "controle";
        return Root(main);
    }

    // Spectacle (doc 60 §6, F10) : les colonnes en grand, le pilote automatique et ses interventions, le journal.
    private IRootDock CreateShow()
    {
        var right = Split(0.28, Orientation.Vertical, Tools(0.62, ControlPanels.Pilot, ControlPanels.Looks), Tools(0.38, ControlPanels.Journal));
        var main = Split(double.NaN, Orientation.Horizontal, Tools(0.72, ControlPanels.Columns), right);
        main.Id = "spectacle";
        return Root(main);
    }

    private IRootDock Root(IDockable main)
    {
        var root = CreateRootDock();
        root.Id = "racine";
        root.IsCollapsable = false;
        root.VisibleDockables = CreateList<IDockable>(main);
        root.ActiveDockable = main;
        root.DefaultDockable = main;
        return root;
    }

    /// <inheritdoc />
    public override void InitLayout(IDockable layout)
    {
        ContextLocator = _contexts.ToDictionary(kv => kv.Key, kv => kv.Value);
        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>> { [nameof(IDockWindow)] = HostWindowFactory };
        DefaultHostWindowLocator = HostWindowFactory;
        base.InitLayout(layout);
    }

    /// <summary>Nouveau panneau (onglet).</summary>
    public static Tool CreatePanel(string id) =>
        new() { Id = id, Title = ControlPanels.Get(id).Title, CanClose = true, CanFloat = true, CanPin = true };

    /// <summary>
    /// Réaffiche un panneau : fermé, il revient à sa place ; absent, il est créé dans le groupe qui l'accueille dans la
    /// disposition livrée (Dock perd le groupe d'origine d'un panneau fermé après relecture : écart vu au prototype).
    /// Détaché, ou fermé depuis une fenêtre détachée, il revient dans la fenêtre principale à sa place livrée, et la
    /// fenêtre vide se ferme (essai 1.005.210 : il revenait détaché, derrière la fenêtre principale).
    /// </summary>
    public IDockable? ShowPanel(IRootDock root, string id)
    {
        ArgumentNullException.ThrowIfNull(root);
        var (dockable, place) = DockTree.Find(root, id);
        if (DockTree.WindowOf(root, id) is { } window)
        {
            TakeOutOfWindow(root, window, dockable!, place);
            return DockHome(root, id, dockable!);
        }

        if (place == PanelPlace.Hidden && dockable!.OriginalOwner is not null)
        {
            RestoreDockable(dockable);
        }
        else if (place is PanelPlace.Hidden or PanelPlace.Absent)
        {
            if (place == PanelPlace.Hidden)
            {
                root.HiddenDockables?.Remove(dockable!);
            }
            else
            {
                dockable = CreatePanel(id);
            }

            return DockHome(root, id, dockable!);
        }

        SetActiveDockable(dockable!);
        return dockable;
    }

    // Dans le groupe qui l'accueille dans la disposition livrée ; s'il a disparu (Dock retire un groupe vidé, par exemple
    // quand on détache son seul panneau), il est recréé à côté de son voisin livré ; à défaut, le premier groupe.
    private IDockable? DockHome(IRootDock root, string id, IDockable dockable)
    {
        var reference = CreateLayout();
        var home = DockTree.FindOwner(reference, id) is { Id: { } homeId } homeRef
            ? DockTree.FindDock(root, homeId) as IToolDock ?? RecreateGroup(root, reference, homeRef)
            : null;
        home ??= DockTree.FirstToolDock(root);
        if (home is null)
        {
            return null;
        }

        AddDockable(home, dockable);
        InitDockable(dockable, home);
        SetActiveDockable(dockable);
        return dockable;
    }

    // Recrée un groupe livré disparu : même identifiant, placé contre le groupe voisin (du même partage) encore présent,
    // du même côté que dans la disposition livrée.
    private ToolDock? RecreateGroup(IRootDock root, IRootDock reference, IDock homeRef)
    {
        if (DockTree.ParentOf(reference, homeRef) is not IProportionalDock parent || parent.VisibleDockables is not { } siblings)
        {
            return null;
        }

        var parts = siblings.Where(d => d is not IProportionalDockSplitter).ToList();
        var index = parts.IndexOf(homeRef);
        foreach (var neighbour in parts.Where(d => !ReferenceEquals(d, homeRef)).OrderBy(d => Math.Abs(parts.IndexOf(d) - index)))
        {
            var target = neighbour is IToolDock { Id: { } neighbourId } ? DockTree.FindDock(root, neighbourId) : null;
            target ??= neighbour is IDock inner && DockTree.FirstToolDock(inner) is { Id: { } innerId } ? DockTree.FindDock(root, innerId) : null;
            if (target is null)
            {
                continue;
            }

            var before = index < parts.IndexOf(neighbour);
            var operation = parent.Orientation == Orientation.Horizontal
                ? before ? DockOperation.Left : DockOperation.Right
                : before ? DockOperation.Top : DockOperation.Bottom;
            var group = new ToolDock { Id = homeRef.Id, Proportion = homeRef.Proportion, VisibleDockables = CreateList<IDockable>() };
            SplitToDock(target, group, operation);
            return group;
        }

        return null;
    }

    private void TakeOutOfWindow(IRootDock root, IDockWindow window, IDockable dockable, PanelPlace place)
    {
        var windowRoot = (IRootDock)window.Layout!;
        if (place == PanelPlace.Hidden)
        {
            windowRoot.HiddenDockables?.Remove(dockable);
        }
        else if (dockable.Owner is IDock owner)
        {
            owner.VisibleDockables?.Remove(dockable);
            if (ReferenceEquals(owner.ActiveDockable, dockable))
            {
                owner.ActiveDockable = owner.VisibleDockables?.FirstOrDefault();
            }
        }

        foreach (var pinned in new[] { windowRoot.LeftPinnedDockables, windowRoot.RightPinnedDockables, windowRoot.TopPinnedDockables, windowRoot.BottomPinnedDockables })
        {
            pinned?.Remove(dockable);
        }

        dockable.Owner = null;
        dockable.OriginalOwner = null;

        // Plus aucun panneau dans la fenêtre : on la ferme.
        if (!DockTree.All(windowRoot).Any(d => d is not IDock))
        {
            window.Exit();
            root.Windows?.Remove(window);
        }
    }

    private ToolDock Tools(double proportion, params string[] ids)
    {
        var tools = ids.Select(CreatePanel).ToArray();
        return new ToolDock
        {
            Id = "onglets-" + ids[0],
            Proportion = proportion,
            ActiveDockable = tools[0],
            VisibleDockables = CreateList<IDockable>(tools),
        };
    }

    private ProportionalDock Split(double proportion, Orientation orientation, IDockable first, IDockable second) =>
        new()
        {
            Proportion = proportion,
            Orientation = orientation,
            VisibleDockables = CreateList<IDockable>(first, new ProportionalDockSplitter(), second),
        };
}
