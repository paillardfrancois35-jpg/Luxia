using Luxia.Patch.Model;

namespace Luxia.Patch.Rules;

/// <summary>
/// Modifications de l'arbre des groupes (ERG-036) : fonctions pures qui rendent un nouvel ensemble, l'écran n'a plus qu'à
/// l'enregistrer. Un appareil reste toujours dans un seul groupe ; un groupe ne peut pas devenir son propre descendant.
/// </summary>
public static class GroupEdits
{
    /// <summary>Ajoute un groupe (à la fin de ses frères).</summary>
    /// <param name="set">Groupes.</param>
    /// <param name="name">Nom.</param>
    /// <param name="parentId">Parent (<c>null</c> = racine).</param>
    /// <param name="hasDimmer">Le groupe a un dimmer.</param>
    public static (FixtureGroupSet Set, FixtureGroup Group) Add(FixtureGroupSet set, string name, Guid? parentId = null, bool hasDimmer = true)
    {
        ArgumentNullException.ThrowIfNull(set);
        var parent = parentId is { } id && set.Groups.Any(g => g.Id == id) ? parentId : null;
        var group = new FixtureGroup { Name = name.Trim(), ParentId = parent, HasDimmer = hasDimmer };
        return (set with { Groups = [.. set.Groups, group] }, group);
    }

    /// <summary>Renomme un groupe.</summary>
    public static FixtureGroupSet Rename(FixtureGroupSet set, Guid id, string name) =>
        Update(set, id, g => g with { Name = name.Trim() });

    /// <summary>Donne ou retire le dimmer d'un groupe.</summary>
    public static FixtureGroupSet SetDimmer(FixtureGroupSet set, Guid id, bool hasDimmer) =>
        Update(set, id, g => g with { HasDimmer = hasDimmer });

    /// <summary>Le groupe <paramref name="candidate"/> est-il <paramref name="ancestor"/> lui-même ou l'un de ses descendants ?</summary>
    public static bool IsSelfOrDescendant(FixtureGroupSet set, Guid candidate, Guid ancestor)
    {
        ArgumentNullException.ThrowIfNull(set);
        Guid? cursor = candidate;
        for (var steps = 0; cursor is { } current && steps <= set.Groups.Count; steps++)
        {
            if (current == ancestor)
            {
                return true;
            }

            cursor = set.Groups.FirstOrDefault(g => g.Id == current)?.ParentId;
        }

        return false;
    }

    /// <summary>Déplace un groupe sous un autre parent (<c>null</c> = racine) ; <c>null</c> si ce serait sous lui-même ou l'un de ses descendants.</summary>
    public static FixtureGroupSet? Move(FixtureGroupSet set, Guid id, Guid? newParentId)
    {
        ArgumentNullException.ThrowIfNull(set);
        if (newParentId is { } parent && (IsSelfOrDescendant(set, parent, id) || set.Groups.All(g => g.Id != parent)))
        {
            return null;
        }

        return Update(set, id, g => g with { ParentId = newParentId });
    }

    /// <summary>
    /// Supprime un groupe : ses sous-groupes remontent chez son parent, ses appareils y sont rangés (ou deviennent non
    /// assignés si le groupe était à la racine).
    /// </summary>
    public static FixtureGroupSet Delete(FixtureGroupSet set, Guid id)
    {
        ArgumentNullException.ThrowIfNull(set);
        var removed = set.Groups.FirstOrDefault(g => g.Id == id);
        if (removed is null)
        {
            return set;
        }

        var parentId = removed.ParentId is { } p && set.Groups.Any(g => g.Id == p) && p != id ? p : (Guid?)null;
        var groups = new List<FixtureGroup>();
        foreach (var group in set.Groups.Where(g => g.Id != id))
        {
            var next = group;
            if (group.ParentId == id)
            {
                next = next with { ParentId = parentId };
            }

            if (group.Id == parentId)
            {
                next = next with { FixtureIds = [.. next.FixtureIds, .. removed.FixtureIds.Where(f => !next.FixtureIds.Contains(f))] };
            }

            groups.Add(next);
        }

        return set with { Groups = groups };
    }

    /// <summary>Range un appareil dans un groupe, en le retirant de son groupe actuel ; <c>null</c> = non assigné.</summary>
    public static FixtureGroupSet Assign(FixtureGroupSet set, Guid fixtureId, Guid? groupId)
    {
        ArgumentNullException.ThrowIfNull(set);
        var groups = set.Groups
            .Select(g => g.FixtureIds.Contains(fixtureId) ? g with { FixtureIds = [.. g.FixtureIds.Where(f => f != fixtureId)] } : g)
            .Select(g => g.Id == groupId ? g with { FixtureIds = [.. g.FixtureIds, fixtureId] } : g)
            .ToList();
        return set with { Groups = groups };
    }

    /// <summary>Décale un groupe parmi ses frères (-1 = plus haut, +1 = plus bas).</summary>
    public static FixtureGroupSet Shift(FixtureGroupSet set, Guid id, int delta)
    {
        ArgumentNullException.ThrowIfNull(set);
        var group = set.Groups.FirstOrDefault(g => g.Id == id);
        if (group is null)
        {
            return set;
        }

        var siblings = set.Groups.Select((g, i) => (g, i)).Where(x => x.g.ParentId == group.ParentId).Select(x => x.i).ToList();
        var position = siblings.FindIndex(i => set.Groups[i].Id == id);
        var target = position + delta;
        if (position < 0 || target < 0 || target >= siblings.Count)
        {
            return set;
        }

        var list = set.Groups.ToList();
        (list[siblings[position]], list[siblings[target]]) = (list[siblings[target]], list[siblings[position]]);
        return set with { Groups = list };
    }

    /// <summary>Retire des groupes les appareils qui ne sont plus dans le patch.</summary>
    public static FixtureGroupSet Prune(FixtureGroupSet set, IReadOnlyCollection<Guid> fixtureIds)
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(fixtureIds);
        return set with { Groups = [.. set.Groups.Select(g => g with { FixtureIds = [.. g.FixtureIds.Where(fixtureIds.Contains)] })] };
    }

    private static FixtureGroupSet Update(FixtureGroupSet set, Guid id, Func<FixtureGroup, FixtureGroup> change)
    {
        ArgumentNullException.ThrowIfNull(set);
        return set with { Groups = [.. set.Groups.Select(g => g.Id == id ? change(g) : g)] };
    }
}
