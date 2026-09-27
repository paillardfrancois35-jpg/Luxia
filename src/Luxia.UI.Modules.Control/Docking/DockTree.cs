using Dock.Model.Controls;
using Dock.Model.Core;

namespace Luxia.UI.Modules.Control.Docking;

/// <summary>Où se trouve un panneau dans la disposition.</summary>
public enum PanelPlace
{
    /// <summary>Absent de la disposition.</summary>
    Absent,

    /// <summary>Affiché (ancré, en onglet ou détaché dans une fenêtre).</summary>
    Visible,

    /// <summary>Épinglé sur un bord (replié).</summary>
    Pinned,

    /// <summary>Fermé : masqué, il peut revenir à sa place.</summary>
    Hidden,
}

/// <summary>Recherches dans l'arbre Dock, fenêtres détachées et panneaux masqués compris.</summary>
public static class DockTree
{
    /// <summary>Cherche un panneau par identifiant partout où Dock peut le ranger.</summary>
    public static (IDockable? Dockable, PanelPlace Place) Find(IRootDock root, string id)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (root.HiddenDockables?.FirstOrDefault(d => d.Id == id) is { } hidden)
        {
            return (hidden, PanelPlace.Hidden);
        }

        foreach (var pinned in new[] { root.LeftPinnedDockables, root.RightPinnedDockables, root.TopPinnedDockables, root.BottomPinnedDockables })
        {
            if (pinned?.FirstOrDefault(d => d.Id == id) is { } p)
            {
                return (p, PanelPlace.Pinned);
            }
        }

        if (FindIn(root, id) is { } visible)
        {
            return (visible, PanelPlace.Visible);
        }

        foreach (var window in root.Windows ?? [])
        {
            if (window.Layout is { } layout)
            {
                var (found, place) = Find(layout, id);
                if (found is not null)
                {
                    return (found, place);
                }
            }
        }

        return (null, PanelPlace.Absent);
    }

    /// <summary>Tous les éléments de la disposition : groupes et panneaux, fenêtres détachées et panneaux fermés compris.</summary>
    public static IEnumerable<IDockable> All(IRootDock root)
    {
        ArgumentNullException.ThrowIfNull(root);
        foreach (var dockable in Walk(root))
        {
            yield return dockable;
        }

        foreach (var hidden in root.HiddenDockables ?? [])
        {
            yield return hidden;
        }

        foreach (var window in root.Windows ?? [])
        {
            if (window.Layout is { } layout)
            {
                foreach (var dockable in All(layout))
                {
                    yield return dockable;
                }
            }
        }
    }

    private static IEnumerable<IDockable> Walk(IDockable dockable)
    {
        yield return dockable;
        if (dockable is IDock dock)
        {
            foreach (var child in dock.VisibleDockables ?? [])
            {
                foreach (var inner in Walk(child))
                {
                    yield return inner;
                }
            }
        }
    }

    /// <summary>Premier groupe d'onglets de panneaux (pour y remettre un panneau absent).</summary>
    public static IToolDock? FirstToolDock(IDock dock)
    {
        if (dock is IToolDock tools)
        {
            return tools;
        }

        foreach (var child in dock.VisibleDockables ?? [])
        {
            if (child is IDock inner && FirstToolDock(inner) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>Groupe (dock) d'identifiant donné dans la partie affichée de la disposition.</summary>
    public static IDock? FindDock(IDock dock, string id)
    {
        if (dock.Id == id)
        {
            return dock;
        }

        foreach (var child in dock.VisibleDockables ?? [])
        {
            if (child is IDock inner && FindDock(inner, id) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>Groupe qui contient directement le panneau d'identifiant donné (sans recourir à <c>Owner</c>).</summary>
    public static IDock? FindOwner(IDock dock, string id)
    {
        foreach (var child in dock.VisibleDockables ?? [])
        {
            if (child.Id == id && child is not IDock)
            {
                return dock;
            }

            if (child is IDock inner && FindOwner(inner, id) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static IDockable? FindIn(IDock dock, string id)
    {
        foreach (var child in dock.VisibleDockables ?? [])
        {
            if (child.Id == id && child is not IDock)
            {
                return child;
            }

            if (child is IDock inner && FindIn(inner, id) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
