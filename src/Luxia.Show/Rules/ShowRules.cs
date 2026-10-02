using System.Globalization;
using Luxia.Fixtures.Rules;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;
using Luxia.Show.Model;

namespace Luxia.Show.Rules;

/// <summary>
/// Validation des séquences et des shows (SHOW-024, GEN-131) : références (scènes, couches, séquences, shows, variables),
/// structure du graphe (étape initiale, étapes atteignables, boucle sans condition), bornes des réglages. Une <b>erreur</b> rend
/// l'objet ou l'élément fautif injouable ; un <b>avertissement</b> n'empêche rien (l'élément est corrigé ou ignoré au jeu).
/// </summary>
public static class ShowRules
{
    /// <summary>Valide séquences et shows d'un projet.</summary>
    public static IReadOnlyList<CompileIssue> Validate(SequenceSet sequences, ShowSet shows, SceneSet scenes, LayerSet layers)
    {
        ArgumentNullException.ThrowIfNull(sequences);
        ArgumentNullException.ThrowIfNull(shows);
        ArgumentNullException.ThrowIfNull(scenes);
        ArgumentNullException.ThrowIfNull(layers);
        var context = new Context(
            scenes.Scenes.DistinctBy(s => s.Id).ToDictionary(s => s.Id),
            layers.Layers.DistinctBy(l => l.Id).ToDictionary(l => l.Id),
            sequences.Sequences.Select(s => s.Id).ToHashSet(),
            shows.Shows.DistinctBy(s => s.Id).ToDictionary(s => s.Id));
        var issues = new List<CompileIssue>();
        foreach (var sequence in sequences.Sequences)
        {
            issues.AddRange(CheckSequence(sequence, context));
        }

        foreach (var show in shows.Shows)
        {
            issues.AddRange(CheckShow(show, context));
        }

        Duplicates(issues, SequenceStore.FileName, "séquence", sequences.Sequences.Select(s => (s.Id, s.Name)));
        Duplicates(issues, ShowStore.FileName, "show", shows.Shows.Select(s => (s.Id, s.Name)));
        return issues;
    }

    /// <summary>Valide une seule séquence (éditeur).</summary>
    public static IReadOnlyList<CompileIssue> Validate(Sequence sequence, SequenceSet sequences, ShowSet shows, SceneSet scenes, LayerSet layers)
    {
        ArgumentNullException.ThrowIfNull(sequence);
        return [.. Validate(sequences, shows, scenes, layers).Where(i => i.File == SequenceStore.FileName && i.Item.StartsWith(SequenceItem(sequence), StringComparison.Ordinal))];
    }

    /// <summary>Valide un seul show (éditeur).</summary>
    public static IReadOnlyList<CompileIssue> Validate(ShowDefinition show, SequenceSet sequences, ShowSet shows, SceneSet scenes, LayerSet layers)
    {
        ArgumentNullException.ThrowIfNull(show);
        return [.. Validate(sequences, shows, scenes, layers).Where(i => i.File == ShowStore.FileName && i.Item.StartsWith(ShowItem(show), StringComparison.Ordinal))];
    }

    /// <summary>Désignation d'une séquence dans les messages.</summary>
    public static string SequenceItem(Sequence sequence) => $"séquence « {sequence?.Name} »";

    /// <summary>Désignation d'un show dans les messages.</summary>
    public static string ShowItem(ShowDefinition show) => $"show « {show?.Name} »";

    // ------------------------------------------------------------------ séquences

    private static IEnumerable<CompileIssue> CheckSequence(Sequence sequence, Context context)
    {
        var item = SequenceItem(sequence);
        if (string.IsNullOrWhiteSpace(sequence.Name))
        {
            yield return Warning(SequenceStore.FileName, item, "name", "la séquence n'a pas de nom");
        }

        if (sequence.Bars <= 0)
        {
            yield return Error(SequenceStore.FileName, item, "bars", "la longueur doit être d'au moins une fraction de mesure");
        }

        if (sequence.Speed is < 0.25 or > 4)
        {
            yield return Warning(SequenceStore.FileName, item, "speed", "vitesse hors de ¼ à 4 : ramenée dans ces bornes");
        }

        var seenLayers = new HashSet<Guid>();
        for (var t = 0; t < sequence.Tracks.Count; t++)
        {
            var track = sequence.Tracks[t];
            var trackItem = string.Create(CultureInfo.InvariantCulture, $"{item}, piste {t + 1}");
            if (track.LayerId is { } layerId)
            {
                if (!context.Layers.TryGetValue(layerId, out var layer))
                {
                    yield return Error(SequenceStore.FileName, trackItem, "layerId", "couche inconnue : la piste est ignorée");
                    continue;
                }

                trackItem = $"{item}, piste « {layer.Name} »";
                if (!seenLayers.Add(layerId))
                {
                    yield return Warning(SequenceStore.FileName, trackItem, "layerId", "deux pistes pour la même couche : leurs scènes se remplacent");
                }
            }
            else
            {
                trackItem = string.Create(CultureInfo.InvariantCulture, $"{item}, piste d'actions {t + 1}");
            }

            var ordered = track.Blocks.OrderBy(b => b.Start).ToList();
            for (var b = 0; b < ordered.Count; b++)
            {
                var block = ordered[b];
                var blockItem = string.Create(CultureInfo.CurrentCulture, $"{trackItem}, bloc à la mesure {block.Start + 1:0.##}");
                foreach (var issue in CheckBlock(block, track, sequence, blockItem, context))
                {
                    yield return issue;
                }

                if (b > 0 && ordered[b - 1].Start + ordered[b - 1].Length > block.Start + 1e-9)
                {
                    yield return Warning(SequenceStore.FileName, blockItem, "start", "chevauche le bloc précédent de la piste : il le remplace à son début");
                }
            }
        }
    }

    private static IEnumerable<CompileIssue> CheckBlock(SequenceBlock block, SequenceTrack track, Sequence sequence, string item, Context context)
    {
        if (block.Start < 0)
        {
            yield return Error(SequenceStore.FileName, item, "start", "début avant le début de la séquence : bloc ignoré");
        }

        if (block.Length <= 0)
        {
            yield return Error(SequenceStore.FileName, item, "length", "durée nulle : bloc ignoré");
        }
        else if (sequence.Bars > 0 && block.Start + block.Length > sequence.Bars + 1e-9)
        {
            yield return Warning(SequenceStore.FileName, item, "length", "dépasse la fin de la séquence : coupé à la fin");
        }

        if (track.LayerId is { } layerId)
        {
            if (block.Action is not null)
            {
                yield return Error(SequenceStore.FileName, item, "action", "une action se pose sur la piste d'actions, pas sur une piste de couche");
            }

            if (block.SceneId is not { } sceneId)
            {
                yield return Error(SequenceStore.FileName, item, "sceneId", "aucune scène : bloc ignoré");
            }
            else if (!context.Scenes.TryGetValue(sceneId, out var scene))
            {
                yield return Error(SequenceStore.FileName, item, "sceneId", "scène supprimée ou inconnue : bloc ignoré");
            }
            else if (scene.LayerId != layerId)
            {
                var name = context.Layers.TryGetValue(scene.LayerId, out var other) ? other.Name : "?";
                yield return Error(SequenceStore.FileName, item, "sceneId", $"la scène « {scene.Name} » appartient à la couche « {name} » : posez-la sur sa piste");
            }

            yield break;
        }

        if (block.SceneId is not null)
        {
            yield return Error(SequenceStore.FileName, item, "sceneId", "une scène se pose sur la piste de sa couche, pas sur la piste d'actions");
        }

        if (block.Action is not { } action)
        {
            yield return Error(SequenceStore.FileName, item, "action", "aucune action : bloc ignoré");
            yield break;
        }

        switch (action.Kind)
        {
            case BlockActionKind.LayerLevel when action.LayerId is not { } id || !context.Layers.ContainsKey(id):
                yield return Error(SequenceStore.FileName, item, "action.layerId", "couche inconnue : action ignorée");
                break;
            case BlockActionKind.Flash when action.SceneId is not { } id || !context.Scenes.ContainsKey(id):
                yield return Error(SequenceStore.FileName, item, "action.sceneId", "scène inconnue : flash ignoré");
                break;
        }

        if (action.Kind is BlockActionKind.LayerLevel or BlockActionKind.GrandMaster && (action.To is < 0 or > 1 || action.From is < 0 or > 1))
        {
            yield return Warning(SequenceStore.FileName, item, "action.to", "niveau hors de 0 à 1 : ramené dans ces bornes");
        }
    }

    // ------------------------------------------------------------------ shows

    private static List<CompileIssue> CheckShow(ShowDefinition show, Context context)
    {
        var item = ShowItem(show);
        var issues = new List<CompileIssue>();
        if (string.IsNullOrWhiteSpace(show.Name))
        {
            issues.Add(Warning(ShowStore.FileName, item, "name", "le show n'a pas de nom"));
        }

        if (show.Steps.Count == 0)
        {
            issues.Add(Error(ShowStore.FileName, item, "steps", "aucune étape : le show ne joue rien"));
            return issues;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in show.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Id))
            {
                issues.Add(Error(ShowStore.FileName, item, "steps.id", "une étape n'a pas d'identifiant"));
            }
            else if (!ids.Add(step.Id))
            {
                issues.Add(Error(ShowStore.FileName, $"{item}, étape {step.Id}", "id", "identifiant d'étape en double"));
            }
        }

        if (!show.Steps.Any(s => s.Initial))
        {
            issues.Add(Error(ShowStore.FileName, item, "steps.initial", "aucune étape initiale : le show ne peut pas démarrer"));
        }

        var variables = show.Variables.Select(v => v.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var step in show.Steps)
        {
            issues.AddRange(CheckStep(show, step, variables, context));
        }

        for (var i = 0; i < show.Transitions.Count; i++)
        {
            issues.AddRange(CheckTransition(show, i, ids, variables, context));
        }

        issues.AddRange(CheckGraph(show, ids));
        if (show.EnergyMin is < 0 or > 3 || show.EnergyMax is < 0 or > 3 || show.EnergyMin > show.EnergyMax)
        {
            issues.Add(Warning(ShowStore.FileName, item, "energyMin", "plage d'énergie incohérente (niveaux 0 à 3)"));
        }

        if (show.Weight <= 0)
        {
            issues.Add(Warning(ShowStore.FileName, item, "weight", "poids nul ou négatif : le Directeur ne le choisira jamais"));
        }

        return issues;
    }

    private static IEnumerable<CompileIssue> CheckStep(ShowDefinition show, ShowStep step, HashSet<string> variables, Context context)
    {
        var item = $"{ShowItem(show)}, étape {step.Id}{(string.IsNullOrWhiteSpace(step.Name) ? string.Empty : $" « {step.Name} »")}";
        if (step.MacroShowId is { } macro)
        {
            if (!context.Shows.ContainsKey(macro))
            {
                yield return Error(ShowStore.FileName, item, "macroShowId", "show de la macro-étape inconnu : l'étape ne joue rien et ne finit jamais");
            }
            else if (Reaches(macro, show.Id, context, []))
            {
                yield return Error(ShowStore.FileName, item, "macroShowId", "la macro-étape se contient elle-même (directement ou par un autre show)");
            }
        }

        for (var a = 0; a < step.Actions.Count; a++)
        {
            var action = step.Actions[a];
            var field = string.Create(CultureInfo.InvariantCulture, $"actions[{a}]");
            var needsScene = action.Kind is ShowActionKind.Play or ShowActionKind.Launch or ShowActionKind.Stop or ShowActionKind.Speed or ShowActionKind.Flash;
            var needsSequence = action.Kind is ShowActionKind.PlaySequence or ShowActionKind.LaunchSequence or ShowActionKind.StopSequence;
            var needsLayer = action.Kind is ShowActionKind.StopLayer or ShowActionKind.LayerLevel;
            if (needsScene && (action.SceneId is not { } scene || !context.Scenes.ContainsKey(scene)))
            {
                yield return Error(ShowStore.FileName, item, field + ".sceneId", "scène supprimée ou inconnue : action ignorée");
            }

            if (needsSequence && (action.SequenceId is not { } sequence || !context.Sequences.Contains(sequence)))
            {
                yield return Error(ShowStore.FileName, item, field + ".sequenceId", "séquence supprimée ou inconnue : action ignorée");
            }

            if (needsLayer && (action.LayerId is not { } layer || !context.Layers.ContainsKey(layer)))
            {
                yield return Error(ShowStore.FileName, item, field + ".layerId", "couche inconnue : action ignorée");
            }

            if (action.Kind == ShowActionKind.Variable && (action.Variable is null || !variables.Contains(action.Variable)))
            {
                yield return Error(ShowStore.FileName, item, field + ".variable", $"variable « {action.Variable} » non déclarée dans le show");
            }

            if (action.Kind is ShowActionKind.Flash or ShowActionKind.Smoke or ShowActionKind.Blackout && action.Seconds <= 0)
            {
                yield return Error(ShowStore.FileName, item, field + ".seconds", "durée nulle : action ignorée");
            }

            if (action.Kind == ShowActionKind.LayerLevel && action.Value is < 0 or > 1)
            {
                yield return Warning(ShowStore.FileName, item, field + ".value", "niveau hors de 0 à 1 : ramené dans ces bornes");
            }
        }
    }

    private static IEnumerable<CompileIssue> CheckTransition(ShowDefinition show, int index, HashSet<string> steps, HashSet<string> variables, Context context)
    {
        var transition = show.Transitions[index];
        var item = string.Create(CultureInfo.InvariantCulture, $"{ShowItem(show)}, transition {index + 1} ({string.Join(", ", transition.From)} → {string.Join(", ", transition.To)})");
        if (transition.From.Count == 0)
        {
            yield return Error(ShowStore.FileName, item, "from", "aucune étape amont : transition ignorée");
        }

        if (transition.To.Count == 0)
        {
            yield return Error(ShowStore.FileName, item, "to", "aucune étape aval : transition ignorée");
        }

        foreach (var id in transition.From.Concat(transition.To).Where(id => !steps.Contains(id)).Distinct())
        {
            yield return Error(ShowStore.FileName, item, "from", $"étape « {id} » inconnue : transition ignorée");
        }

        if (transition.Weight <= 0)
        {
            yield return Warning(ShowStore.FileName, item, "weight", "poids nul ou négatif : jamais tirée au sort");
        }

        foreach (var issue in CheckCondition(transition.Condition, item, "condition", variables, context))
        {
            yield return issue;
        }
    }

    private static IEnumerable<CompileIssue> CheckCondition(ShowCondition condition, string item, string field, HashSet<string> variables, Context context)
    {
        switch (condition.Kind)
        {
            case ConditionKind.After when condition.Duration is not { Value: > 0 }:
                yield return Error(ShowStore.FileName, item, field + ".duration", "durée absente ou nulle : la condition est vraie tout de suite");
                break;
            case ConditionKind.SceneEnded when condition.SceneId is not { } scene || !context.Scenes.ContainsKey(scene):
                yield return Error(ShowStore.FileName, item, field + ".sceneId", "scène inconnue : la condition n'est jamais vraie");
                break;
            case ConditionKind.SequenceEnded or ConditionKind.SequenceLoops when condition.SequenceId is not { } sequence || !context.Sequences.Contains(sequence):
                yield return Error(ShowStore.FileName, item, field + ".sequenceId", "séquence inconnue : la condition n'est jamais vraie");
                break;
            case ConditionKind.EnergyAbove or ConditionKind.EnergyBelow when condition.Value is < 0 or > 1:
                yield return Warning(ShowStore.FileName, item, field + ".value", "seuil d'énergie hors de 0 à 1");
                break;
            case ConditionKind.EnergyLevel when condition.Min is < 0 or > 3 || condition.Max is < 0 or > 3:
                yield return Warning(ShowStore.FileName, item, field + ".min", "niveaux d'énergie de 0 (Calme) à 3 (Explosif)");
                break;
            case ConditionKind.Random when condition.Value is <= 0 or > 1:
                yield return Error(ShowStore.FileName, item, field + ".value", "probabilité hors de 0 à 1 (0,25 = une chance sur quatre)");
                break;
            case ConditionKind.Variable when condition.Variable is null || !variables.Contains(condition.Variable):
                yield return Error(ShowStore.FileName, item, field + ".variable", $"variable « {condition.Variable} » non déclarée dans le show");
                break;
            case ConditionKind.Style when condition.Styles.Count == 0:
                yield return Warning(ShowStore.FileName, item, field + ".styles", "aucun style : la condition n'est jamais vraie");
                break;
            case ConditionKind.All or ConditionKind.Any when condition.Conditions.Count == 0:
                yield return Error(ShowStore.FileName, item, field + ".conditions", "combinaison vide");
                break;
            case ConditionKind.Not when condition.Conditions.Count != 1:
                yield return Error(ShowStore.FileName, item, field + ".conditions", "NON attend exactement une condition");
                break;
        }

        for (var i = 0; i < condition.Conditions.Count; i++)
        {
            foreach (var issue in CheckCondition(condition.Conditions[i], item, string.Create(CultureInfo.InvariantCulture, $"{field}.conditions[{i}]"), variables, context))
            {
                yield return issue;
            }
        }
    }

    // Étapes atteignables depuis les initiales ; boucle de transitions immédiates (toujours vraies, sans quantification) : interdit.
    private static IEnumerable<CompileIssue> CheckGraph(ShowDefinition show, HashSet<string> ids)
    {
        var item = ShowItem(show);
        var valid = show.Transitions.Where(t => t.From.Count > 0 && t.To.Count > 0 && t.From.Concat(t.To).All(ids.Contains)).ToList();
        var reached = show.Steps.Where(s => s.Initial).Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var transition in valid.Where(t => t.From.All(reached.Contains)))
            {
                foreach (var to in transition.To)
                {
                    changed |= reached.Add(to);
                }
            }
        }

        foreach (var step in show.Steps.Where(s => !reached.Contains(s.Id) && ids.Contains(s.Id)))
        {
            yield return Warning(ShowStore.FileName, $"{item}, étape {step.Id}", "id", "étape inatteignable depuis les étapes initiales");
        }

        // Graphe des enchaînements immédiats : une transition « toujours vraie », non quantifiée, d'une étape amont à une étape aval.
        var immediate = valid
            .Where(t => IsAlwaysTrue(t.Condition) && t.Quantize == ShowQuantize.None)
            .SelectMany(t => t.From.SelectMany(f => t.To.Select(to => (f, to))))
            .ToLookup(e => e.f, e => e.to, StringComparer.Ordinal);
        var cycle = FindCycle(show.Steps.Select(s => s.Id).Where(ids.Contains), immediate);
        if (cycle is not null)
        {
            yield return Error(ShowStore.FileName, item, "transitions", $"boucle sans condition ({string.Join(" → ", cycle)}) : le show tournerait sans fin ; ajoutez une condition ou une quantification");
        }
    }

    private static bool IsAlwaysTrue(ShowCondition condition) => condition.Kind switch
    {
        ConditionKind.Always => true,
        ConditionKind.All => condition.Conditions.All(IsAlwaysTrue),
        ConditionKind.Any => condition.Conditions.Any(IsAlwaysTrue),
        _ => false,
    };

    private static List<string>? FindCycle(IEnumerable<string> nodes, ILookup<string, string> edges)
    {
        var state = new Dictionary<string, int>(StringComparer.Ordinal);
        var path = new List<string>();

        List<string>? Visit(string node)
        {
            state[node] = 1;
            path.Add(node);
            foreach (var next in edges[node])
            {
                if (state.GetValueOrDefault(next) == 1)
                {
                    return [.. path.SkipWhile(n => n != next), next];
                }

                if (state.GetValueOrDefault(next) == 0 && Visit(next) is { } found)
                {
                    return found;
                }
            }

            path.RemoveAt(path.Count - 1);
            state[node] = 2;
            return null;
        }

        foreach (var node in nodes)
        {
            if (state.GetValueOrDefault(node) == 0 && Visit(node) is { } cycle)
            {
                return cycle;
            }
        }

        return null;
    }

    private static bool Reaches(Guid from, Guid target, Context context, HashSet<Guid> seen)
    {
        if (from == target)
        {
            return true;
        }

        if (!seen.Add(from) || !context.Shows.TryGetValue(from, out var show))
        {
            return false;
        }

        return show.Steps.Any(s => s.MacroShowId is { } macro && Reaches(macro, target, context, seen));
    }

    private static void Duplicates(List<CompileIssue> issues, string file, string kind, IEnumerable<(Guid Id, string Name)> items)
    {
        foreach (var group in items.GroupBy(i => i.Id).Where(g => g.Count() > 1))
        {
            issues.Add(Error(file, $"{kind} « {group.First().Name} »", "id", "identifiant en double : seul le premier est gardé"));
        }
    }

    private static CompileIssue Error(string file, string item, string field, string message) => new(IssueSeverity.Error, file, item, field, message);

    private static CompileIssue Warning(string file, string item, string field, string message) => new(IssueSeverity.Warning, file, item, field, message);

    private sealed record Context(
        Dictionary<Guid, Scene> Scenes,
        Dictionary<Guid, Layer> Layers,
        HashSet<Guid> Sequences,
        Dictionary<Guid, ShowDefinition> Shows);
}
