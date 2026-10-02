using System.Globalization;
using Luxia.Engine.Sequencing;
using Luxia.Engine.Timing;
using Luxia.Messaging.Commands;
using Luxia.Messaging.Events;
using Luxia.Show.Model;
using Luxia.Show.Rules;

namespace Luxia.Show.Runtime;

/// <summary>
/// Un show qui joue (doc 20 §3.3, SHOW-023) : étapes actives, transitions validées, armées ou franchies, actions continues,
/// mémorisées et impulsionnelles. Règles : R1 (validée quand toutes les étapes amont sont actives, franchie quand la condition
/// est vraie, après quantification), R2 (désactivation et activation dans le même tick), R3 (transitions indépendantes
/// franchies ensemble), R4 (divergence OU : priorité ou tirage pondéré), R5 (pas de coupure d'une scène rejouée), R6 (fin).
/// Une évolution par tick (D39).
/// </summary>
internal sealed class ShowRun
{
    private const int MaxDepth = 8;
    private const int PathLength = 12;
    private readonly Sequencer _sequencer;
    private readonly int _depth;
    private readonly Dictionary<string, ActiveStep> _active = new(StringComparer.Ordinal);
    private readonly Dictionary<int, double> _armed = [];
    private readonly HashSet<int> _forced = [];
    private readonly HashSet<int> _forcedNow = [];
    private readonly Dictionary<string, double> _variables = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _lastChoice = new(StringComparer.Ordinal);
    private readonly Dictionary<ShowCondition, long> _randomBoundary = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<Guid> _playedScenes = [];
    private readonly Dictionary<Guid, SequenceRun> _playedSequences = [];
    private readonly List<string> _path = [];
    private bool _started;

    public ShowRun(ShowDefinition definition, Sequencer sequencer, int depth = 0)
    {
        Definition = definition;
        _sequencer = sequencer;
        _depth = depth;
        foreach (var variable in definition.Variables)
        {
            _variables[variable.Name] = variable.Initial;
        }
    }

    /// <summary>Définition (remplacée quand le projet change).</summary>
    public ShowDefinition Definition { get; private set; }

    /// <summary>Toutes les étapes actives sont des fins (R6) ; pour une macro-étape, ses transitions sortantes sont alors validées.</summary>
    public bool Ended { get; private set; }

    /// <summary>Le show est arrêté (fin, remplacement, commande).</summary>
    public bool Stopped { get; private set; }

    /// <summary>Scènes jouées par les actions continues des étapes initiales (relais sans coupure au remplacement d'un show).</summary>
    public static IReadOnlySet<Guid> InitialScenes(ShowDefinition definition) =>
        definition.Steps.Where(s => s.Initial).SelectMany(s => s.Actions).Where(a => a.Kind == ShowActionKind.Play && a.SceneId is not null).Select(a => a.SceneId!.Value).ToHashSet();

    /// <summary>Force une transition (CMD-051) : comme si sa condition était vraie, à sa quantification sauf <paramref name="immediate"/>.</summary>
    public string? Force(int index, bool immediate)
    {
        if (index < 0 || index >= Definition.Transitions.Count)
        {
            return "transition inconnue";
        }

        if (!Validated(Definition.Transitions[index]))
        {
            return "ses étapes amont ne sont pas toutes actives";
        }

        (immediate ? _forcedNow : _forced).Add(index);
        return null;
    }

    /// <summary>Fait évoluer le show d'un tick.</summary>
    public void Tick(ISequencerHost host)
    {
        if (Stopped)
        {
            return;
        }

        if (!_started)
        {
            _started = true;
            foreach (var step in Definition.Steps.Where(s => s.Initial))
            {
                Activate(host, step, "lancement");
            }

            ApplyContinuous(host);
            return;
        }

        foreach (var active in _active.Values)
        {
            active.Sub?.Tick(host);
        }

        if (Evolve(host))
        {
            ApplyContinuous(host);
        }

        CheckEnd(host);
    }

    /// <summary>Arrête le show : scènes et séquences de ses actions continues (sauf celles de <paramref name="keep"/>), sous-shows.</summary>
    public void Stop(ISequencerHost host, IReadOnlySet<Guid>? keep = null)
    {
        if (Stopped)
        {
            return;
        }

        Stopped = true;
        foreach (var active in _active.Values)
        {
            active.Sub?.Stop(host, keep);
        }

        _active.Clear();
        foreach (var scene in _playedScenes.Where(s => keep?.Contains(s) != true && host.IsPlaying(s)))
        {
            host.Execute(new StopSceneCommand(CommandOrigin.Show, scene));
        }

        _playedScenes.Clear();
        foreach (var run in _playedSequences.Values)
        {
            run.Stop(host, keep);
        }

        _playedSequences.Clear();
        _sequencer.Changed();
    }

    /// <summary>Remplace la définition (projet modifié) : les étapes qui existent encore restent actives.</summary>
    public void Replace(ShowDefinition definition, ISequencerHost host)
    {
        // Une transition inchangée (même objet : l'éditeur ne recrée que ce qu'il modifie) garde son attente : un drop retenu
        // jusqu'à la mesure ne se perd pas parce qu'on retouche le show pendant son essai.
        var previous = Definition;
        Definition = definition;
        foreach (var index in _armed.Keys.Concat(_forced).Concat(_forcedNow).Distinct().ToList())
        {
            if (index >= definition.Transitions.Count || index >= previous.Transitions.Count || !ReferenceEquals(previous.Transitions[index], definition.Transitions[index]))
            {
                _armed.Remove(index);
                _forced.Remove(index);
                _forcedNow.Remove(index);
            }
        }

        foreach (var id in _active.Keys.Where(id => definition.Steps.All(s => s.Id != id)).ToList())
        {
            _active[id].Sub?.Stop(host);
            _active.Remove(id);
        }

        foreach (var (id, active) in _active.ToList())
        {
            var step = definition.Steps.First(s => s.Id == id);
            var sub = active.Sub;
            var macro = sub is null ? null : _sequencer.FindShow(sub.Definition.Id);
            if (sub is not null && (step.MacroShowId != sub.Definition.Id || macro is null))
            {
                // Macro-étape qui change de show (ou dont le show a disparu) : l'ancien sous-show s'arrête, le nouveau démarre.
                sub.Stop(host);
                sub = step.MacroShowId is { } id2 && _depth < MaxDepth && _sequencer.FindShow(id2) is { } fresh ? new ShowRun(fresh, _sequencer, _depth + 1) : null;
            }
            else if (sub is not null && macro is not null)
            {
                sub.Replace(macro, host);
            }
            else if (step.MacroShowId is { } added && _depth < MaxDepth && _sequencer.FindShow(added) is { } show)
            {
                sub = new ShowRun(show, _sequencer, _depth + 1);
            }

            _active[id] = active with { Step = step, Sub = sub };
        }

        foreach (var variable in definition.Variables.Where(v => !_variables.ContainsKey(v.Name)))
        {
            _variables[variable.Name] = variable.Initial;
        }

        if (_active.Count == 0 && _started)
        {
            _started = false;
        }
        else if (_started)
        {
            // Les actions continues de l'étape active suivent la modification tout de suite (essai dans l'éditeur).
            ApplyContinuous(host);
        }
    }

    /// <summary>État lisible pour la supervision.</summary>
    public ShowStatus Status(ISequencerHost host, Func<Guid, string?> names)
    {
        var steps = _active.Values
            .Select(a => new StepStatus(
                a.Step.Id,
                a.Step.Name,
                host.BeatPosition - a.Beat,
                [.. a.Step.Actions.Select(x => ShowTexts.Describe(x, names))],
                a.Sub?.Status(host, names)))
            .ToList();
        var transitions = new List<TransitionStatus>();
        for (var i = 0; i < Definition.Transitions.Count; i++)
        {
            var transition = Definition.Transitions[i];
            if (!Validated(transition))
            {
                continue;
            }

            var armed = _armed.TryGetValue(i, out var target);
            transitions.Add(new TransitionStatus(
                i,
                transition.From,
                transition.To,
                [.. transition.To.Select(id => Definition.Steps.FirstOrDefault(s => s.Id == id)?.Name ?? id)],
                ShowTexts.Label(transition, names),
                transition.Quantize,
                armed,
                armed ? Math.Max(0, target - host.BeatPosition) : 0,
                Hint(transition.Condition, transition, host)));
        }

        return new ShowStatus(
            Definition.Id,
            Definition.Name,
            Definition.Color,
            Definition.Secondary,
            steps,
            transitions,
            [.. _path],
            [.. _variables.Select(v => (v.Key, v.Value))],
            Ended);
    }

    // ------------------------------------------------------------------ évolution

    private bool Evolve(ISequencerHost host)
    {
        List<int>? candidates = null;
        var forcedNow = new HashSet<int>(_forcedNow);
        for (var i = 0; i < Definition.Transitions.Count; i++)
        {
            var transition = Definition.Transitions[i];
            if (!Validated(transition))
            {
                _armed.Remove(i);
                _forced.Remove(i);
                _forcedNow.Remove(i);
                continue;
            }

            if (_forcedNow.Remove(i))
            {
                (candidates ??= []).Add(i);
                continue;
            }

            var isForced = _forced.Contains(i);
            var condition = isForced || Evaluate(transition.Condition, transition, host);
            if (transition.Quantize == ShowQuantize.None)
            {
                if (condition)
                {
                    (candidates ??= []).Add(i);
                }

                continue;
            }

            if (_armed.TryGetValue(i, out var target))
            {
                if (host.BeatPosition >= target - 1e-6)
                {
                    (candidates ??= []).Add(i);
                }

                continue;
            }

            if (condition)
            {
                // D39 : la condition arme la transition, qui part à la frontière musicale suivante (tout de suite si on y est).
                var next = Sequencer.NextBoundary(host.BeatPosition, transition.Quantize);
                if (next is null)
                {
                    (candidates ??= []).Add(i);
                }
                else
                {
                    _armed[i] = next.Value;
                    _sequencer.Changed();
                }
            }
        }

        if (candidates is null)
        {
            return false;
        }

        var forced = new HashSet<int>(_forced.Concat(forcedNow));
        var fired = Choose(candidates, forced);
        foreach (var index in fired)
        {
            var transition = Definition.Transitions[index];
            foreach (var from in transition.From)
            {
                if (_active.Remove(from, out var active))
                {
                    active.Sub?.Stop(host, CurrentlyWanted(transition));
                }

                _lastChoice[from] = index;
            }
        }

        foreach (var index in fired)
        {
            var transition = Definition.Transitions[index];
            var reason = forced.Contains(index) ? "forcée" : ShowTexts.Label(transition, _sequencer.NameOf);
            foreach (var to in transition.To)
            {
                if (Definition.Steps.FirstOrDefault(s => s.Id == to) is { } step)
                {
                    Activate(host, step, reason);
                }
            }
        }

        // Les transitions armées d'autres branches (divergence en ET) gardent leur attente ; celles qui ne sont plus validées
        // la perdent au tick suivant.
        foreach (var index in fired)
        {
            _armed.Remove(index);
            _forced.Remove(index);
        }

        return true;
    }

    // Scènes que les étapes aval vont rejouer : un sous-show qui s'arrête ne les coupe pas (R5).
    private HashSet<Guid> CurrentlyWanted(ShowTransition transition) =>
        [.. transition.To.SelectMany(id => Definition.Steps.Where(s => s.Id == id)).SelectMany(s => s.Actions).Where(a => a.Kind == ShowActionKind.Play && a.SceneId is not null).Select(a => a.SceneId!.Value)];

    // R3 / R4 : les transitions indépendantes partent ensemble ; parmi celles qui partagent une étape amont, une seule, choisie par
    // priorité (ordre du show) ou par tirage pondéré si l'étape le demande ; une transition forcée passe devant.
    private List<int> Choose(List<int> candidates, HashSet<int> forcedSet)
    {
        var fired = new List<int>();
        var claimed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var index in candidates)
        {
            var transition = Definition.Transitions[index];
            if (transition.From.Any(claimed.Contains))
            {
                continue;
            }

            var group = candidates.Where(i => !Definition.Transitions[i].From.Any(claimed.Contains) && Definition.Transitions[i].From.Intersect(transition.From).Any()).ToList();
            var forced = group.Where(forcedSet.Contains).ToList();
            var step = Definition.Steps.FirstOrDefault(s => s.Id == transition.From[0]);
            var chosen = forced.Count > 0 ? forced[0]
                : step is { Choice: StepChoice.Random } && group.Count > 1 ? Draw(group, step)
                : index;
            fired.Add(chosen);
            foreach (var from in Definition.Transitions[chosen].From)
            {
                claimed.Add(from);
            }
        }

        return fired;
    }

    // SHOW-028 : tirage pondéré ; « éviter la même branche » écarte la dernière transition tirée depuis l'étape s'il en reste d'autres.
    private int Draw(List<int> group, ShowStep step)
    {
        var pool = group;
        if (step.AvoidRepeat && _lastChoice.TryGetValue(step.Id, out var last) && group.Count > 1 && group.Contains(last))
        {
            pool = [.. group.Where(i => i != last)];
        }

        var total = pool.Sum(i => Math.Max(0, Definition.Transitions[i].Weight));
        if (total <= 0)
        {
            return pool[0];
        }

        var draw = _sequencer.Random.NextDouble() * total;
        foreach (var index in pool)
        {
            draw -= Math.Max(0, Definition.Transitions[index].Weight);
            if (draw < 0)
            {
                return index;
            }
        }

        return pool[^1];
    }

    private bool Validated(ShowTransition transition) =>
        transition.From.Count > 0 && transition.To.Count > 0 &&
        transition.From.All(id => _active.TryGetValue(id, out var active) && (active.Sub is null || active.Sub.Ended));

    // R6 : toutes les étapes actives sont des fins (et leurs sous-shows aussi).
    private void CheckEnd(ISequencerHost host)
    {
        var ended = _active.Count == 0 || _active.Values.All(a =>
            (a.Sub is null || a.Sub.Ended) && !Definition.Transitions.Any(t => t.From.Contains(a.Step.Id, StringComparer.Ordinal)));
        if (!ended)
        {
            Ended = false;
            return;
        }

        if (_depth > 0 || Definition.AtEnd == ShowEnd.Hold)
        {
            if (!Ended)
            {
                _sequencer.Changed();
            }

            Ended = true;
            return;
        }

        if (Definition.AtEnd == ShowEnd.Restart && Definition.Steps.Any(s => s.Initial))
        {
            foreach (var active in _active.Values)
            {
                active.Sub?.Stop(host);
            }

            _active.Clear();
            foreach (var step in Definition.Steps.Where(s => s.Initial))
            {
                Activate(host, step, "reprise au début");
            }

            ApplyContinuous(host);
            return;
        }

        Stop(host);
        host.Publish(new ShowStateChanged(Definition.Id, Definition.Name, false, host.Now));
    }

    // ------------------------------------------------------------------ actions

    private void Activate(ISequencerHost host, ShowStep step, string reason)
    {
        ShowRun? sub = null;
        if (step.MacroShowId is { } macroId && _depth < MaxDepth && _sequencer.FindShow(macroId) is { } macro)
        {
            sub = new ShowRun(macro, _sequencer, _depth + 1);
        }

        _active[step.Id] = new ActiveStep(step, host.BeatPosition, host.Now, sub);
        _path.Add(step.Id);
        if (_path.Count > PathLength)
        {
            _path.RemoveAt(0);
        }

        foreach (var action in step.Actions)
        {
            Run(host, action);
        }

        sub?.Tick(host);
        _sequencer.Changed();
        host.Publish(new ShowStepActivated(Definition.Id, Definition.Name, step.Id, step.Name, reason, host.Now));
    }

    // Actions mémorisées et impulsionnelles, à l'activation (SHOW-021) ; les continues sont tenues par ApplyContinuous.
    private void Run(ISequencerHost host, ShowAction action)
    {
        switch (action.Kind)
        {
            case ShowActionKind.Launch when action.SceneId is { } scene && !host.IsPlaying(scene):
                host.Execute(new LaunchSceneCommand(CommandOrigin.Show, scene, Immediate: true));
                break;
            case ShowActionKind.LaunchSequence when action.SequenceId is { } sequence:
                _sequencer.StartSequence(host, sequence, null);
                break;
            case ShowActionKind.Stop when action.SceneId is { } scene:
                host.Execute(new StopSceneCommand(CommandOrigin.Show, scene));
                _playedScenes.Remove(scene);
                break;
            case ShowActionKind.StopSequence when action.SequenceId is { } sequence:
                _sequencer.StopSequence(host, sequence);
                break;
            case ShowActionKind.StopLayer when action.LayerId is { } layer:
                host.Execute(new StopLayerCommand(CommandOrigin.Show, layer));
                break;
            case ShowActionKind.LayerLevel when action.LayerId is { } layer:
                host.Execute(new SetLayerMasterCommand(CommandOrigin.Show, layer, Math.Clamp(action.Value, 0, 1)));
                break;
            case ShowActionKind.Speed when action.SceneId is { } scene:
                host.Execute(new SetSceneSpeedCommand(CommandOrigin.Show, scene, action.Value));
                break;
            case ShowActionKind.Flash when action.SceneId is { } scene && action.Seconds > 0:
                host.Execute(new FlashSceneCommand(CommandOrigin.Show, scene, true));
                _sequencer.Schedule(host.Now + TimeSpan.FromSeconds(action.Seconds), new FlashSceneCommand(CommandOrigin.Show, scene, false));
                break;
            case ShowActionKind.Smoke when action.Seconds > 0:
                host.Execute(new SmokeCommand(CommandOrigin.Show, true, TimeSpan.FromSeconds(action.Seconds)));
                break;
            case ShowActionKind.Blackout when action.Seconds > 0:
                host.Execute(new BlackoutCommand(CommandOrigin.Show, true));
                _sequencer.Schedule(host.Now + TimeSpan.FromSeconds(action.Seconds), new BlackoutCommand(CommandOrigin.Show, false));
                break;
            case ShowActionKind.Variable when action.Variable is { } name:
                _variables[name] = action.Operation == VariableOperation.Set ? action.Value : _variables.GetValueOrDefault(name) + action.Value;
                break;
        }
    }

    // R5 : les scènes et séquences voulues par les étapes actives jouent ; celles qui ne le sont plus s'arrêtent ; une scène déjà en
    // jeu n'est pas relancée (pas de coupure visible).
    private void ApplyContinuous(ISequencerHost host)
    {
        var scenes = new HashSet<Guid>();
        var sequences = new HashSet<Guid>();
        foreach (var action in _active.Values.SelectMany(a => a.Step.Actions))
        {
            if (action is { Kind: ShowActionKind.Play, SceneId: { } scene })
            {
                scenes.Add(scene);
            }
            else if (action is { Kind: ShowActionKind.PlaySequence, SequenceId: { } sequence })
            {
                sequences.Add(sequence);
            }
        }

        foreach (var scene in _playedScenes.Where(s => !scenes.Contains(s)).ToList())
        {
            _playedScenes.Remove(scene);
            // Une scène reprise par le sous-show d'une macro-étape (ou par un autre show) continue.
            if (host.IsPlaying(scene) && !_sequencer.WantedElsewhere(scene, this) && !_active.Values.Any(x => x.Sub?.Plays(scene) == true))
            {
                host.Execute(new StopSceneCommand(CommandOrigin.Show, scene));
            }
        }

        foreach (var scene in scenes.Where(s => _playedScenes.Add(s)))
        {
            if (!host.IsPlaying(scene))
            {
                host.Execute(new LaunchSceneCommand(CommandOrigin.Show, scene, Immediate: true));
            }
        }

        foreach (var id in _playedSequences.Keys.Where(id => !sequences.Contains(id)).ToList())
        {
            _playedSequences[id].Stop(host, scenes);
            _playedSequences.Remove(id);
        }

        foreach (var id in sequences.Where(id => !_playedSequences.ContainsKey(id)))
        {
            if (_sequencer.StartSequence(host, id, this) is { } run)
            {
                _playedSequences[id] = run;
            }
        }
    }

    /// <summary>La scène est jouée par une action continue de ce show (ou d'un de ses sous-shows).</summary>
    public bool Plays(Guid scene) => _playedScenes.Contains(scene) || _active.Values.Any(a => a.Sub?.Plays(scene) == true);

    // ------------------------------------------------------------------ réceptivités

    private bool Evaluate(ShowCondition condition, ShowTransition transition, ISequencerHost host)
    {
        var music = host.Music;
        switch (condition.Kind)
        {
            case ConditionKind.Always:
                return true;
            case ConditionKind.Manual:
                return false;
            case ConditionKind.After:
                return Remaining(condition, transition, host) <= 1e-9;
            case ConditionKind.SceneEnded:
                return condition.SceneId is { } scene && !host.IsPlaying(scene);
            case ConditionKind.SequenceEnded:
                return condition.SequenceId is { } ended && !_sequencer.IsSequenceRunning(ended);
            case ConditionKind.SequenceLoops:
                return condition.SequenceId is { } looped && _sequencer.SequenceLoops(looped) >= Math.Max(1, condition.Count);
            case ConditionKind.Drop:
                return (music.Cues & MusicCues.Drop) != 0;
            case ConditionKind.Break:
                return (music.Cues & MusicCues.Break) != 0;
            case ConditionKind.BuildUp:
                return (music.Cues & MusicCues.BuildUp) != 0;
            case ConditionKind.Silence:
                return (music.Cues & MusicCues.Silence) != 0;
            case ConditionKind.Resumed:
                return (music.Cues & MusicCues.Resumed) != 0;
            case ConditionKind.SongChanged:
                return (music.Cues & MusicCues.SongChanged) != 0;
            case ConditionKind.EnergyAbove:
                return _sequencer.PreviousEnergy < condition.Value && music.Energy >= condition.Value;
            case ConditionKind.EnergyBelow:
                return _sequencer.PreviousEnergy > condition.Value && music.Energy <= condition.Value;
            case ConditionKind.EnergyLevel:
                return music.AudioLive && music.EnergyLevel >= (condition.Min ?? 0) && music.EnergyLevel <= (condition.Max ?? 3);
            case ConditionKind.Style:
                return music.Style is { } style && condition.Styles.Contains(style, StringComparer.OrdinalIgnoreCase);
            case ConditionKind.Tempo:
                return host.Bpm >= (condition.Min ?? 0) && host.Bpm <= (condition.Max ?? double.MaxValue);
            case ConditionKind.Random:
                return RandomDraw(condition, host);
            case ConditionKind.Variable:
                var value = condition.Variable is { } name ? _variables.GetValueOrDefault(name) : 0;
                return condition.Comparison switch
                {
                    Comparison.AtMost => value <= condition.Value + 1e-9,
                    Comparison.EqualTo => Math.Abs(value - condition.Value) < 1e-9,
                    Comparison.NotEqualTo => Math.Abs(value - condition.Value) >= 1e-9,
                    _ => value >= condition.Value - 1e-9,
                };
            case ConditionKind.All:
                return condition.Conditions.Count > 0 && condition.Conditions.All(c => Evaluate(c, transition, host));
            case ConditionKind.Any:
                return condition.Conditions.Any(c => Evaluate(c, transition, host));
            case ConditionKind.Not:
                return condition.Conditions.Count == 1 && !Evaluate(condition.Conditions[0], transition, host);
            default:
                return false;
        }
    }

    // Une chance par frontière (temps, mesure, phrase) : le premier passage ne tire pas, il note la frontière courante.
    private bool RandomDraw(ShowCondition condition, ISequencerHost host)
    {
        var unit = Sequencer.UnitBeats(condition.Every == ShowQuantize.None ? ShowQuantize.Bar : condition.Every);
        var boundary = (long)Math.Floor((host.BeatPosition + 1e-6) / unit);
        if (!_randomBoundary.TryGetValue(condition, out var last))
        {
            _randomBoundary[condition] = boundary;
            return false;
        }

        if (boundary == last)
        {
            return false;
        }

        _randomBoundary[condition] = boundary;
        return _sequencer.Random.NextDouble() < condition.Value;
    }

    // Reste d'une condition « après N » : depuis l'activation la plus récente des étapes amont (en secondes ou en temps d'horloge).
    private double Remaining(ShowCondition condition, ShowTransition transition, ISequencerHost host)
    {
        var duration = condition.Duration ?? Engine.Model.Duration.Zero;
        var latest = transition.From.Select(id => _active.GetValueOrDefault(id)).OfType<ActiveStep>().OrderByDescending(a => a.Beat).FirstOrDefault();
        if (latest is null)
        {
            return double.MaxValue;
        }

        return duration.Unit switch
        {
            Engine.Model.DurationUnit.Seconds => duration.Value - (host.Now - latest.At).TotalSeconds,
            Engine.Model.DurationUnit.Bars => (duration.Value * Sequencer.BeatsPerBar) - (host.BeatPosition - latest.Beat),
            _ => duration.Value - (host.BeatPosition - latest.Beat),
        };
    }

    private string Hint(ShowCondition condition, ShowTransition transition, ISequencerHost host)
    {
        switch (condition.Kind)
        {
            case ConditionKind.After:
                var left = Remaining(condition, transition, host);
                if (left <= 0)
                {
                    return string.Empty;
                }

                return (condition.Duration?.Unit ?? Engine.Model.DurationUnit.Seconds) switch
                {
                    Engine.Model.DurationUnit.Seconds => string.Create(CultureInfo.CurrentCulture, $"vraie dans {Math.Ceiling(left):0} s"),
                    Engine.Model.DurationUnit.Bars => string.Create(CultureInfo.CurrentCulture, $"vraie dans {Math.Ceiling(left / Sequencer.BeatsPerBar):0} mesure(s)"),
                    _ => string.Create(CultureInfo.CurrentCulture, $"vraie dans {Math.Ceiling(left):0} temps"),
                };
            case ConditionKind.Variable when condition.Variable is { } name:
                return string.Create(CultureInfo.CurrentCulture, $"{_variables.GetValueOrDefault(name):0.##} / {condition.Value:0.##}");
            case ConditionKind.SequenceLoops when condition.SequenceId is { } sequence:
                return string.Create(CultureInfo.CurrentCulture, $"{_sequencer.SequenceLoops(sequence)} / {condition.Count}");
            default:
                return string.Empty;
        }
    }

    private sealed record ActiveStep(ShowStep Step, double Beat, TimeSpan At, ShowRun? Sub);
}
