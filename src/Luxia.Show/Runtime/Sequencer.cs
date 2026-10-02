using Luxia.Engine.Sequencing;
using Luxia.Messaging.Commands;
using Luxia.Messaging.Events;
using Luxia.Show.Model;

namespace Luxia.Show.Runtime;

/// <summary>
/// Séquenceur de shows et de séquences (doc 20, D37) : branché sur le moteur (<c>RenderEngine.SetSequencer</c>), il évolue à chaque
/// tick sur le fil du moteur et agit par les commandes du catalogue (origine <see cref="CommandOrigin.Show"/>). Un seul show
/// principal à la fois (SHOW-025), des shows secondaires en parallèle (SHOW-031), des séquences lancées à la main ou par un show.
/// </summary>
public sealed class Sequencer : ISequencer
{
    /// <summary>Temps par mesure (mesure à 4 temps, GEN-025 reportée).</summary>
    public const int BeatsPerBar = 4;

    private const int PublishEveryTicks = 4;
    private readonly List<ShowRun> _shows = [];
    private readonly List<SequenceRun> _sequences = [];
    private readonly List<(TimeSpan At, Command Command)> _timed = [];
    private Content? _pending;
    private Content _content = new([], []);
    private SequencerState _state = SequencerState.Empty;
    private bool _changed;
    private int _ticksSincePublish;
    private ISequencerHost? _host;

    /// <summary>Crée le séquenceur.</summary>
    /// <param name="seed">Graine des tirages au sort (reproductibles en test) ; <c>null</c> = au hasard.</param>
    public Sequencer(int? seed = null)
    {
#pragma warning disable CA5394 // Tirages de mise en lumière, pas de sécurité.
        Random = seed is { } value ? new Random(value) : new Random();
#pragma warning restore CA5394
    }

    /// <summary>Ce qui joue, pour la supervision (SHOW-026) ; lisible depuis n'importe quel fil.</summary>
    public SequencerState State => Volatile.Read(ref _state);

    /// <summary>Générateur des tirages au sort (SHOW-028).</summary>
    internal Random Random { get; }

    /// <summary>Énergie du tick précédent (franchissements de seuil).</summary>
    internal double PreviousEnergy { get; private set; } = double.NaN;

    /// <summary>
    /// Donne les séquences et les shows du projet (ouverture, modification) ; appliqué au tick suivant. Ce qui joue continue avec
    /// la nouvelle version ; un show ou une séquence supprimé s'arrête.
    /// </summary>
    public void Load(SequenceSet sequences, ShowSet shows)
    {
        ArgumentNullException.ThrowIfNull(sequences);
        ArgumentNullException.ThrowIfNull(shows);
        Volatile.Write(ref _pending, new Content(
            sequences.Sequences.DistinctBy(s => s.Id).ToDictionary(s => s.Id),
            shows.Shows.DistinctBy(s => s.Id).ToDictionary(s => s.Id)));
    }

    /// <inheritdoc />
    public string? Apply(SequencerCommand command, ISequencerHost host)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(host);
        _host = host;
        TakePending(host);
        switch (command)
        {
            case LaunchShowCommand launch:
                return LaunchShow(host, launch);
            case StopShowCommand stop:
                StopShows(host, stop.ShowId, stop.KeepSecondary);
                return null;
            case ForceTransitionCommand force:
                return _shows.FirstOrDefault(s => s.Definition.Id == force.ShowId && !s.Stopped) is { } run
                    ? run.Force(force.Transition, force.Immediate)
                    : "le show ne joue pas";
            case LaunchSequenceCommand launchSequence:
                if (!_content.Sequences.ContainsKey(launchSequence.SequenceId))
                {
                    return "séquence inconnue";
                }

                if (IsSequenceRunning(launchSequence.SequenceId))
                {
                    if (launchSequence.StopIfPlaying)
                    {
                        StopSequence(host, launchSequence.SequenceId);
                    }

                    return null;
                }

                StartSequence(host, launchSequence.SequenceId, null);
                return null;
            case StopSequenceCommand stopSequence:
                StopSequence(host, stopSequence.SequenceId);
                return null;
            default:
                return "commande inconnue du séquenceur";
        }
    }

    /// <inheritdoc />
    public void Tick(ISequencerHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        _host = host;
        TakePending(host);
        if (_shows.Count == 0 && _sequences.Count == 0 && _timed.Count == 0)
        {
            PreviousEnergy = host.Music.Energy;
            if (!ReferenceEquals(_state, SequencerState.Empty) || _changed)
            {
                Volatile.Write(ref _state, SequencerState.Empty);
                _changed = false;
            }

            return;
        }

        for (var i = 0; i < _timed.Count; i++)
        {
            if (_timed[i].At <= host.Now)
            {
                host.Execute(_timed[i].Command);
                _timed.RemoveAt(i);
                i--;
            }
        }

        foreach (var run in _shows.ToList())
        {
            run.Tick(host);
        }

        foreach (var run in _sequences.ToList())
        {
            run.Tick(host);
        }

        if (_shows.RemoveAll(s => s.Stopped) + _sequences.RemoveAll(s => s.Finished) > 0)
        {
            _changed = true;
        }

        PreviousEnergy = host.Music.Energy;
        if (_changed || ++_ticksSincePublish >= PublishEveryTicks)
        {
            Publish(host);
        }
    }

    // ------------------------------------------------------------------ services des shows et des séquences

    /// <summary>Frontière musicale suivante (en temps d'horloge) ; <c>null</c> = tout de suite (aucune, ou on y est).</summary>
    internal static double? NextBoundary(double position, ShowQuantize quantize)
    {
        if (quantize == ShowQuantize.None)
        {
            return null;
        }

        var unit = UnitBeats(quantize);
        var next = Math.Ceiling((position / unit) - 1e-9) * unit;
        return next - position < 0.02 ? null : next;
    }

    /// <summary>Longueur d'une frontière musicale en temps.</summary>
    internal static double UnitBeats(ShowQuantize quantize) => quantize switch
    {
        ShowQuantize.Beat => 1,
        ShowQuantize.Bar => BeatsPerBar,
        ShowQuantize.Phrase4 => 4 * BeatsPerBar,
        ShowQuantize.Phrase8 => 8 * BeatsPerBar,
        ShowQuantize.Phrase16 => 16 * BeatsPerBar,
        _ => 1,
    };

    /// <summary>Signale un changement à publier tout de suite (étape, lancement, arrêt).</summary>
    internal void Changed() => _changed = true;

    /// <summary>Show du projet par identifiant.</summary>
    internal ShowDefinition? FindShow(Guid id) => _content.Shows.GetValueOrDefault(id);

    /// <summary>Nom d'une scène, d'une couche, d'une séquence ou d'un show (textes de supervision).</summary>
    internal string? NameOf(Guid id) =>
        _host?.Show.Scene(id)?.Name ?? _host?.Show.Layer(id)?.Name ?? _content.Sequences.GetValueOrDefault(id)?.Name ?? _content.Shows.GetValueOrDefault(id)?.Name;

    /// <summary>Une séquence de cet identifiant joue (ou attend son départ).</summary>
    internal bool IsSequenceRunning(Guid id) => _sequences.Any(s => s.Definition.Id == id && !s.Finished);

    /// <summary>Passages terminés de la séquence qui joue (0 si elle ne joue pas).</summary>
    internal int SequenceLoops(Guid id) => _sequences.Where(s => s.Definition.Id == id && !s.Finished).Select(s => s.Loops).DefaultIfEmpty(0).Max();

    /// <summary>Lance une séquence à sa quantification (SHOW-005).</summary>
    internal SequenceRun? StartSequence(ISequencerHost host, Guid id, ShowRun? owner)
    {
        if (!_content.Sequences.TryGetValue(id, out var definition))
        {
            return null;
        }

        var start = NextBoundary(host.BeatPosition, definition.Quantize) ?? host.BeatPosition;
        var run = new SequenceRun(definition, start, owner);
        _sequences.Add(run);
        run.Tick(host);
        _changed = true;
        return run;
    }

    /// <summary>Arrête toutes les lectures d'une séquence.</summary>
    internal void StopSequence(ISequencerHost host, Guid id)
    {
        foreach (var run in _sequences.Where(s => s.Definition.Id == id))
        {
            run.Stop(host);
        }

        _changed = true;
    }

    /// <summary>Programme une commande (relâche d'un flash, fin d'un noir court).</summary>
    internal void Schedule(TimeSpan at, Command command) => _timed.Add((at, command));

    /// <summary>Un autre show que <paramref name="except"/> joue cette scène par une action continue (on ne la coupe pas).</summary>
    internal bool WantedElsewhere(Guid scene, ShowRun except) => _shows.Any(s => !ReferenceEquals(s, except) && !s.Stopped && s.Plays(scene));

    // ------------------------------------------------------------------ commandes

    private string? LaunchShow(ISequencerHost host, LaunchShowCommand command)
    {
        if (!_content.Shows.TryGetValue(command.ShowId, out var definition))
        {
            return "show inconnu";
        }

        if (Rules.ShowRules.BlockingProblem(definition) is { } problem)
        {
            return $"le show « {definition.Name} » ne peut pas jouer : {problem}";
        }

        if (_shows.FirstOrDefault(s => s.Definition.Id == definition.Id && !s.Stopped) is { } running)
        {
            if (command.StopIfPlaying)
            {
                running.Stop(host);
                host.Publish(new ShowStateChanged(definition.Id, definition.Name, false, host.Now));
            }

            return null;
        }

        if (!definition.Secondary)
        {
            // SHOW-025 : un seul show principal ; les scènes que le nouveau rejoue d'emblée ne sont pas coupées (R5).
            var keep = ShowRun.InitialScenes(definition);
            foreach (var other in _shows.Where(s => !s.Definition.Secondary && !s.Stopped))
            {
                other.Stop(host, keep);
                host.Publish(new ShowStateChanged(other.Definition.Id, other.Definition.Name, false, host.Now));
            }
        }

        _shows.Add(new ShowRun(definition, this));
        _changed = true;
        host.Publish(new ShowStateChanged(definition.Id, definition.Name, true, host.Now));
        return null;
    }

    private void StopShows(ISequencerHost host, Guid? id, bool keepSecondary)
    {
        foreach (var run in _shows.Where(s => !s.Stopped && (id is null ? !(keepSecondary && s.Definition.Secondary) : s.Definition.Id == id)))
        {
            run.Stop(host);
            host.Publish(new ShowStateChanged(run.Definition.Id, run.Definition.Name, false, host.Now));
        }

        if (id is null)
        {
            foreach (var run in _sequences)
            {
                run.Stop(host);
            }

            foreach (var (_, command) in _timed)
            {
                host.Execute(command);
            }

            _timed.Clear();
        }

        _changed = true;
    }

    private void TakePending(ISequencerHost host)
    {
        if (Interlocked.Exchange(ref _pending, null) is not { } content)
        {
            return;
        }

        _content = content;
        foreach (var run in _shows.Where(s => !s.Stopped))
        {
            if (content.Shows.TryGetValue(run.Definition.Id, out var definition))
            {
                run.Replace(definition, host);
            }
            else
            {
                run.Stop(host);
            }
        }

        foreach (var run in _sequences.Where(s => !s.Finished))
        {
            if (content.Sequences.TryGetValue(run.Definition.Id, out var definition))
            {
                run.Replace(definition, host.BeatPosition);
            }
            else
            {
                run.Stop(host);
            }
        }

        _changed = true;
    }

    private void Publish(ISequencerHost host)
    {
        _changed = false;
        _ticksSincePublish = 0;
        var shows = _shows.Where(s => !s.Stopped).OrderBy(s => s.Definition.Secondary).Select(s => s.Status(host, NameOf)).ToList();
        var sequences = _sequences.Where(s => !s.Finished)
            .Select(s => new SequenceStatus(s.Definition.Id, s.Definition.Name, s.PositionBars(host.BeatPosition), s.Definition.Bars, s.Loops, s.Owner?.Definition.Id))
            .ToList();
        Volatile.Write(ref _state, new SequencerState(shows, sequences));
    }

    private sealed record Content(Dictionary<Guid, Sequence> Sequences, Dictionary<Guid, ShowDefinition> Shows);
}
