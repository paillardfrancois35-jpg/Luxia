using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace Luxia.Tools.Prototype.Docking;

/// <summary>
/// Fabrique Dock du prototype (ERG-001) : construit les dispositions prêtes. Tous les panneaux sont de simples
/// <see cref="Tool"/> identifiés par leur <see cref="IDockable.Id"/> ; leur contenu est donné par
/// <see cref="PanelTemplate"/>. Ainsi la disposition enregistrée ne contient que des types de la bibliothèque,
/// et un panneau inconnu (fichier d'une autre version) s'affiche sans faire échouer la lecture.
/// </summary>
internal sealed class PrototypeDockFactory : Factory
{
    public PrototypeDockFactory()
    {
        // Fermer un panneau le masque (il revient par le menu Panneaux) au lieu de le détruire.
        HideToolsOnClose = true;
    }

    /// <inheritdoc />
    public override IRootDock CreateLayout() => CreateLayout(LayoutPreset.Control);

    /// <summary>Disposition prête, à l'état neuf.</summary>
    public IRootDock CreateLayout(LayoutPreset preset)
    {
        var main = preset == LayoutPreset.Show ? CreateShow() : CreateControl();
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
        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow(),
        };
        DefaultHostWindowLocator = () => new HostWindow();
        base.InitLayout(layout);
    }

    /// <summary>
    /// Réaffiche un panneau : fermé, il revient à sa place ; absent, il est créé. Dock n'enregistre pas le groupe
    /// d'origine d'un panneau fermé : après relecture d'une disposition, il le perdrait (constaté par les tests).
    /// Dans ce cas, et pour un panneau absent, on le range dans le groupe qui le contient dans la disposition livrée,
    /// ou à défaut dans le premier groupe d'onglets. Rend le panneau, ou nul s'il n'y a nulle part où le mettre.
    /// </summary>
    public IDockable? ShowPanel(IRootDock root, LayoutPreset preset, string id)
    {
        ArgumentNullException.ThrowIfNull(root);
        var (dockable, place) = DockTree.Find(root, id);
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

            if (HomeDock(root, preset, id) is not { } home)
            {
                return null;
            }

            AddDockable(home, dockable!);
        }

        SetActiveDockable(dockable!);
        return dockable;
    }

    /// <summary>Nouveau panneau (onglet) pour un identifiant du catalogue.</summary>
    public static Tool CreatePanel(string id)
    {
        var info = PanelCatalog.Get(id);
        return new Tool { Id = info.Id, Title = info.Title, CanClose = true, CanFloat = true, CanPin = true };
    }

    // Groupe d'onglets qui accueille ce panneau dans la disposition livrée, s'il existe encore.
    private IToolDock? HomeDock(IRootDock root, LayoutPreset preset, string id)
    {
        if (DockTree.FindOwner(CreateLayout(preset), id) is { Id: { } homeId } && DockTree.FindDock(root, homeId) is IToolDock home)
        {
            return home;
        }

        return DockTree.FirstToolDock(root);
    }

    // Contrôle (doc 60 §6) : colonnes au centre, propriétés à droite, plan + réglages en bas, journal en onglet.
    private ProportionalDock CreateControl()
    {
        var center = Tools(0.62, Alignment.Top, PanelCatalog.Columns, PanelCatalog.Gallery);
        var plan = Tools(0.35, Alignment.Bottom, PanelCatalog.FixturePlan);
        var settings = Tools(0.65, Alignment.Bottom, PanelCatalog.Position, PanelCatalog.Color, PanelCatalog.Log);
        var bottom = Split(0.38, Orientation.Horizontal, plan, settings);
        var left = Split(0.74, Orientation.Vertical, center, bottom);
        var right = Split(0.26, Orientation.Vertical,
            Tools(0.6, Alignment.Right, PanelCatalog.Properties),
            Tools(0.4, Alignment.Right, PanelCatalog.Metrics));
        var main = Split(double.NaN, Orientation.Horizontal, left, right);
        main.Id = "controle";
        return main;
    }

    // Spectacle : colonnes en grand, pilote automatique et journal à droite (F10).
    private ProportionalDock CreateShow()
    {
        var right = Split(0.25, Orientation.Vertical,
            Tools(0.6, Alignment.Right, PanelCatalog.Pilot),
            Tools(0.4, Alignment.Right, PanelCatalog.Log));
        var main = Split(double.NaN, Orientation.Horizontal, Tools(0.75, Alignment.Left, PanelCatalog.Columns), right);
        main.Id = "spectacle";
        return main;
    }

    private ToolDock Tools(double proportion, Alignment alignment, params string[] ids)
    {
        var tools = ids.Select(CreatePanel).ToArray();
        return new ToolDock
        {
            Id = "onglets-" + ids[0],
            Proportion = proportion,
            Alignment = alignment,
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
