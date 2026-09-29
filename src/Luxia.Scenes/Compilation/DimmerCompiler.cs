using Luxia.Engine.Model;
using Luxia.Fixtures.Rules;
using Luxia.Patch.Model;
using Luxia.Patch.Rules;

namespace Luxia.Scenes.Compilation;

/// <summary>
/// Compile l'arbre des groupes (<c>groupes.json</c>) vers le moteur (ERG-036, ERG-037) : groupes dans l'ordre parent avant
/// enfant, et groupe de chaque paramètre. Les jumeaux (INST-014) partagent les paramètres de leur appareil de référence :
/// c'est le groupe de la référence qui compte (à défaut, celui d'un jumeau).
/// </summary>
internal static class DimmerCompiler
{
    /// <summary>Groupes du moteur et groupe de chaque paramètre (-1 = aucun).</summary>
    /// <param name="patch">Patch résolu.</param>
    /// <param name="installation">Installation (appareils du patch, pour repérer les identifiants inconnus).</param>
    /// <param name="groups">Arbre des groupes (<c>null</c> = aucun).</param>
    /// <param name="parameters">Paramètres du moteur.</param>
    /// <param name="issues">Problèmes trouvés (avertissements, GEN-056).</param>
    public static (IReadOnlyList<DimmerGroup> Groups, IReadOnlyList<int> ParameterGroups) Build(
        PatchContext patch,
        Installation installation,
        FixtureGroupSet? groups,
        IReadOnlyList<RigParameter> parameters,
        List<CompileIssue> issues)
    {
        if (groups is null || groups.Groups.Count == 0)
        {
            return ([], [.. Enumerable.Repeat(-1, parameters.Count)]);
        }

        foreach (var (item, field, message) in GroupRules.Problems(groups, [.. installation.Fixtures.Select(f => f.Id)]))
        {
            issues.Add(new CompileIssue(IssueSeverity.Warning, "groupes.json", item, field, message));
        }

        var layout = GroupRules.Layout(groups);
        var engineGroups = layout.Select(n => new DimmerGroup(n.Group.Id, n.Group.Name, n.Parent, n.Group.HasDimmer)).ToList();
        var rankOf = new Dictionary<Guid, int>();
        for (var rank = 0; rank < layout.Count; rank++)
        {
            foreach (var fixtureId in layout[rank].Group.FixtureIds)
            {
                rankOf.TryAdd(fixtureId, rank);
            }
        }

        var byReference = patch.Fixtures.GroupBy(f => f.ReferenceId).ToDictionary(g => g.Key, g => g.Select(f => f.Fixture.Id).ToList());
        var reported = new HashSet<Guid>();
        var result = new int[parameters.Count];
        for (var i = 0; i < parameters.Count; i++)
        {
            var reference = parameters[i].FixtureId;
            var rank = rankOf.GetValueOrDefault(reference, -1);
            var members = byReference.GetValueOrDefault(reference) ?? [];
            if (rank < 0)
            {
                rank = members.Select(id => rankOf.GetValueOrDefault(id, -1)).FirstOrDefault(r => r >= 0, -1);
            }
            else if (reported.Add(reference) && members.Any(id => rankOf.TryGetValue(id, out var r) && r != rank))
            {
                issues.Add(new CompileIssue(
                    IssueSeverity.Warning,
                    "groupes.json",
                    $"appareil « {patch.Find(reference)?.Fixture.Name ?? reference.ToString()} »",
                    "fixtureIds",
                    "des jumeaux sont rangés dans un autre groupe que leur appareil de référence : celui de la référence est utilisé"));
            }

            result[i] = rank;
        }

        return (engineGroups, result);
    }
}
