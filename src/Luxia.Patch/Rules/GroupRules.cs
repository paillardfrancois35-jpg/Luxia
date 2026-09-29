using Luxia.Patch.Model;

namespace Luxia.Patch.Rules;

/// <summary>Un groupe dans l'ordre de l'arbre : son rang parent (-1 = racine) et sa profondeur.</summary>
/// <param name="Group">Groupe.</param>
/// <param name="Parent">Rang du parent dans la liste ordonnée, ou -1 ; toujours inférieur au rang du groupe.</param>
/// <param name="Depth">Profondeur (0 = racine).</param>
public sealed record GroupNode(FixtureGroup Group, int Parent, int Depth);

/// <summary>
/// Règles de l'arbre des groupes (ERG-036) : ordre parent avant enfant, problèmes à signaler. Fonctions pures :
/// un fichier fautif (parent inconnu, boucle) ne bloque jamais le projet, la valeur fautive est ignorée (GEN-056).
/// </summary>
public static class GroupRules
{
    /// <summary>
    /// Groupes dans l'ordre de l'arbre (parent avant enfants, frères dans l'ordre du fichier). Un parent inconnu ou une
    /// boucle fait remonter le groupe à la racine.
    /// </summary>
    /// <param name="set">Groupes.</param>
    public static IReadOnlyList<GroupNode> Layout(FixtureGroupSet set)
    {
        ArgumentNullException.ThrowIfNull(set);
        var groups = set.Groups;
        var first = new Dictionary<Guid, int>();
        for (var i = 0; i < groups.Count; i++)
        {
            first.TryAdd(groups[i].Id, i);
        }

        var parent = new int[groups.Count];
        for (var i = 0; i < groups.Count; i++)
        {
            parent[i] = groups[i].ParentId is { } id && first.TryGetValue(id, out var p) && p != i ? p : -1;
        }

        // Boucle : on remonte à la racine le groupe où la boucle est détectée, ce qui la casse pour tous les autres.
        for (var i = 0; i < groups.Count; i++)
        {
            var steps = 0;
            for (var cursor = parent[i]; cursor >= 0; cursor = parent[cursor])
            {
                if (cursor == i || ++steps > groups.Count)
                {
                    parent[i] = -1;
                    break;
                }
            }
        }

        var result = new List<GroupNode>();
        var rank = new Dictionary<int, int>();
        void Visit(int index, int parentRank, int depth)
        {
            rank[index] = result.Count;
            result.Add(new GroupNode(groups[index], parentRank, depth));
            for (var child = 0; child < groups.Count; child++)
            {
                if (parent[child] == index)
                {
                    Visit(child, rank[index], depth + 1);
                }
            }
        }

        for (var i = 0; i < groups.Count; i++)
        {
            if (parent[i] < 0)
            {
                Visit(i, -1, 0);
            }
        }

        return result;
    }

    /// <summary>Groupe qui contient directement un appareil (le premier, si le fichier en cite plusieurs), ou <c>null</c> = non assigné.</summary>
    /// <param name="set">Groupes.</param>
    /// <param name="fixtureId">Appareil.</param>
    public static FixtureGroup? GroupOf(FixtureGroupSet set, Guid fixtureId)
    {
        ArgumentNullException.ThrowIfNull(set);
        return set.Groups.FirstOrDefault(g => g.FixtureIds.Contains(fixtureId));
    }

    /// <summary>Problèmes de l'arbre : (objet, champ, message), à signaler sans jamais bloquer.</summary>
    /// <param name="set">Groupes.</param>
    /// <param name="fixtureIds">Appareils du patch.</param>
    public static IReadOnlyList<(string Item, string Field, string Message)> Problems(FixtureGroupSet set, IReadOnlyCollection<Guid> fixtureIds)
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(fixtureIds);
        var problems = new List<(string, string, string)>();
        foreach (var duplicate in set.Groups.GroupBy(g => g.Id).Where(g => g.Count() > 1))
        {
            problems.Add(($"groupe « {duplicate.First().Name} »", "id", "identifiant utilisé par plusieurs groupes (GEN-052)"));
        }

        var ids = set.Groups.Select(g => g.Id).ToHashSet();
        var layout = Layout(set);
        foreach (var group in set.Groups)
        {
            if (group.ParentId is { } parentId)
            {
                if (parentId == group.Id || !ids.Contains(parentId))
                {
                    problems.Add(($"groupe « {group.Name} »", "parentId", "parent introuvable : le groupe est remonté à la racine"));
                }
                else if (layout.First(n => ReferenceEquals(n.Group, group)).Parent < 0)
                {
                    problems.Add(($"groupe « {group.Name} »", "parentId", "boucle dans l'arbre : le groupe est remonté à la racine"));
                }
            }
        }

        var seen = new Dictionary<Guid, string>();
        foreach (var group in set.Groups)
        {
            foreach (var fixtureId in group.FixtureIds)
            {
                if (!fixtureIds.Contains(fixtureId))
                {
                    problems.Add(($"groupe « {group.Name} »", "fixtureIds", "appareil introuvable dans le patch : ignoré"));
                }
                else if (seen.TryGetValue(fixtureId, out var other))
                {
                    problems.Add(($"groupe « {group.Name} »", "fixtureIds", $"appareil déjà rangé dans « {other} » : un appareil n'est que dans un seul groupe, le premier l'emporte"));
                }
                else
                {
                    seen[fixtureId] = group.Name;
                }
            }
        }

        return problems;
    }
}
