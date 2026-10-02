using Luxia.Engine.Sequencing;
using Luxia.Messaging.Commands;
using Luxia.Show.Model;

namespace Luxia.Show.Runtime;

/// <summary>
/// Une séquence qui joue (SHOW-003 à SHOW-005, SHOW-008) : position calculée sur l'horloge musicale du moteur, blocs démarrés et
/// arrêtés quand la position franchit leurs bornes, rampes de niveau tick par tick, boucle sans coupure.
/// </summary>
internal sealed class SequenceRun
{
    private const double Epsilon = 1e-6;
    private readonly List<(int Track, SequenceBlock Block)> _blocks = [];
    private bool[] _started = [];
    private bool[] _ended = [];
    private double[] _rampFrom = [];

    public SequenceRun(Sequence definition, double startBeat, ShowRun? owner)
    {
        Owner = owner;
        StartBeat = startBeat;
        Definition = definition;
        Rebuild();
    }

    /// <summary>Définition (remplacée quand le projet change, la position est gardée).</summary>
    public Sequence Definition { get; private set; }

    /// <summary>Show qui l'a lancée par une action continue ; <c>null</c> = lancée à la main ou par une action mémorisée.</summary>
    public ShowRun? Owner { get; }

    /// <summary>Position de l'horloge (en temps) du début du passage en cours.</summary>
    public double StartBeat { get; private set; }

    /// <summary>Passages terminés (boucles).</summary>
    public int Loops { get; private set; }

    /// <summary>La séquence est arrivée à sa fin (une fois) ou a été arrêtée.</summary>
    public bool Finished { get; private set; }

    /// <summary>Vitesse relative bornée (SHOW-008).</summary>
    public double Speed => Math.Clamp(Definition.Speed, 0.25, 4);

    /// <summary>Longueur d'un passage, en temps de la séquence.</summary>
    public double LengthBeats => Math.Max(Definition.Bars, 0) * Sequencer.BeatsPerBar;

    /// <summary>Position dans le passage, en mesures (négative tant qu'elle attend son départ quantifié).</summary>
    public double PositionBars(double clockBeat) => (clockBeat - StartBeat) * Speed / Sequencer.BeatsPerBar;

    /// <summary>
    /// Remplace la définition (projet modifié) en gardant la position : les blocs déjà passés ou en cours sont tenus pour joués
    /// (pas de relance), les suivants partiront à leur heure.
    /// </summary>
    public void Replace(Sequence definition, double clockBeat)
    {
        Definition = definition;
        Rebuild();
        var local = (clockBeat - StartBeat) * Speed;
        for (var i = 0; i < _blocks.Count; i++)
        {
            var block = _blocks[i].Block;
            _started[i] = local >= (block.Start * Sequencer.BeatsPerBar) - Epsilon;
            _ended[i] = _started[i] && local >= End(i) - Epsilon;
            _rampFrom[i] = block.Action is { } action ? Math.Clamp(action.From ?? action.To, 0, 1) : 0;
        }
    }

    /// <summary>Avance d'un tick.</summary>
    public void Tick(ISequencerHost host)
    {
        if (Finished || LengthBeats <= 0)
        {
            Finished = true;
            return;
        }

        var local = (host.BeatPosition - StartBeat) * Speed;
        if (local < -Epsilon)
        {
            return;
        }

        while (local >= LengthBeats - Epsilon)
        {
            // Fin d'un passage : on termine les blocs encore ouverts (ceux qui relaient au début du passage suivant gardent leur scène).
            Process(host, LengthBeats);
            if (Definition.End != SequenceEnd.Loop)
            {
                StopRunningBlocks(host);
                Finished = true;
                return;
            }

            Loops++;
            StartBeat += LengthBeats / Speed;
            local -= LengthBeats;
            Array.Clear(_started);
            Array.Clear(_ended);
        }

        Process(host, local);
    }

    /// <summary>Arrête la séquence : ses blocs ouverts sont fermés (scènes qu'elle a lancées et qui jouent encore, flash, noir).</summary>
    public void Stop(ISequencerHost host, IReadOnlySet<Guid>? keep = null)
    {
        if (Finished)
        {
            return;
        }

        StopRunningBlocks(host, keep);
        Finished = true;
    }

    private void Rebuild()
    {
        _blocks.Clear();
        for (var t = 0; t < Definition.Tracks.Count; t++)
        {
            foreach (var block in Definition.Tracks[t].Blocks.Where(b => b.Length > 0 && b.Start >= 0))
            {
                _blocks.Add((t, block));
            }
        }

        _blocks.Sort((a, b) => a.Block.Start.CompareTo(b.Block.Start));
        _started = new bool[_blocks.Count];
        _ended = new bool[_blocks.Count];
        _rampFrom = new double[_blocks.Count];
    }

    private void Process(ISequencerHost host, double local)
    {
        // Les fins avant les débuts : un bloc qui se termine quand le suivant de la même piste commence lui passe le relais.
        for (var i = 0; i < _blocks.Count; i++)
        {
            if (_started[i] && !_ended[i] && local >= End(i) - Epsilon)
            {
                Close(host, i);
            }
        }

        for (var i = 0; i < _blocks.Count; i++)
        {
            var start = _blocks[i].Block.Start * Sequencer.BeatsPerBar;
            if (!_started[i] && local >= start - Epsilon && start < LengthBeats - Epsilon)
            {
                Open(host, i);
                if (local >= End(i) - Epsilon)
                {
                    Close(host, i);
                }
            }
        }

        for (var i = 0; i < _blocks.Count; i++)
        {
            if (_started[i] && !_ended[i] && _blocks[i].Block.Action is { Kind: BlockActionKind.LayerLevel or BlockActionKind.GrandMaster } action)
            {
                var start = _blocks[i].Block.Start * Sequencer.BeatsPerBar;
                var progress = Math.Clamp((local - start) / Math.Max(End(i) - start, Epsilon), 0, 1);
                Level(host, action, _rampFrom[i] + ((Math.Clamp(action.To, 0, 1) - _rampFrom[i]) * progress), log: false);
            }
        }
    }

    // Fin d'un bloc, coupée à la fin du passage (avertissement de valider).
    private double End(int i) => Math.Min((_blocks[i].Block.Start + _blocks[i].Block.Length) * Sequencer.BeatsPerBar, LengthBeats);

    private void Open(ISequencerHost host, int i)
    {
        _started[i] = true;
        var (track, block) = _blocks[i];
        if (block.SceneId is { } sceneId && Definition.Tracks[track].LayerId is not null)
        {
            // Même scène déjà en jeu (bloc précédent, passage précédent, autre lanceur) : elle continue, sans coupure (R5).
            if (!host.IsPlaying(sceneId))
            {
                host.Execute(new LaunchSceneCommand(CommandOrigin.Show, sceneId, Immediate: true));
            }

            return;
        }

        if (block.Action is not { } action)
        {
            return;
        }

        switch (action.Kind)
        {
            case BlockActionKind.LayerLevel when action.LayerId is { } layer:
                _rampFrom[i] = Math.Clamp(action.From ?? host.LayerLevel(layer), 0, 1);
                Level(host, action, _rampFrom[i], log: true);
                break;
            case BlockActionKind.GrandMaster:
                _rampFrom[i] = Math.Clamp(action.From ?? host.GrandMaster, 0, 1);
                Level(host, action, _rampFrom[i], log: true);
                break;
            case BlockActionKind.Smoke:
                var seconds = block.Length * Sequencer.BeatsPerBar * 60 / (Math.Max(host.Bpm, 1) * Speed);
                host.Execute(new SmokeCommand(CommandOrigin.Show, true, TimeSpan.FromSeconds(seconds)));
                break;
            case BlockActionKind.Flash when action.SceneId is { } scene:
                host.Execute(new FlashSceneCommand(CommandOrigin.Show, scene, true));
                break;
            case BlockActionKind.Blackout:
                host.Execute(new BlackoutCommand(CommandOrigin.Show, true));
                break;
        }
    }

    private void Close(ISequencerHost host, int i)
    {
        _ended[i] = true;
        var (track, block) = _blocks[i];
        if (block.SceneId is { } sceneId && Definition.Tracks[track].LayerId is { } layerId)
        {
            if (block.End == BlockEnd.Keep || HandsOver(i, sceneId, layerId, host))
            {
                return;
            }

            if (host.IsPlaying(sceneId))
            {
                host.Execute(new StopSceneCommand(CommandOrigin.Show, sceneId));
            }

            return;
        }

        switch (block.Action)
        {
            case { Kind: BlockActionKind.LayerLevel or BlockActionKind.GrandMaster } action:
                Level(host, action, Math.Clamp(action.To, 0, 1), log: true);
                break;
            case { Kind: BlockActionKind.Flash, SceneId: { } scene }:
                host.Execute(new FlashSceneCommand(CommandOrigin.Show, scene, false));
                break;
            case { Kind: BlockActionKind.Blackout }:
                host.Execute(new BlackoutCommand(CommandOrigin.Show, false));
                break;
        }
    }

    // Relais sur la même piste au même instant (aussi d'un passage au suivant en boucle) : même scène, ou couche exclusive
    // (le lancement suivant remplace celle-ci par le fondu croisé de la couche, sans noir).
    private bool HandsOver(int i, Guid sceneId, Guid layerId, ISequencerHost host)
    {
        var (track, block) = _blocks[i];
        var end = block.Start + block.Length;
        var wrap = Definition.End == SequenceEnd.Loop && end >= Definition.Bars - Epsilon;
        var exclusive = host.Show.Layer(layerId)?.Exclusive ?? true;
        foreach (var (otherTrack, other) in _blocks)
        {
            if (otherTrack != track || ReferenceEquals(other, block))
            {
                continue;
            }

            var next = (Math.Abs(other.Start - end) < Epsilon && other.Start < Definition.Bars - Epsilon) || (wrap && other.Start < Epsilon);
            if (next && (other.SceneId == sceneId || exclusive))
            {
                return true;
            }
        }

        return false;
    }

    private static void Level(ISequencerHost host, BlockAction action, double level, bool log)
    {
        if (action.Kind == BlockActionKind.GrandMaster)
        {
            host.Execute(new SetGrandMasterCommand(CommandOrigin.Show, level), log);
        }
        else if (action.LayerId is { } layer)
        {
            host.Execute(new SetLayerMasterCommand(CommandOrigin.Show, layer, level), log);
        }
    }

    private void StopRunningBlocks(ISequencerHost host, IReadOnlySet<Guid>? keep = null)
    {
        for (var i = 0; i < _blocks.Count; i++)
        {
            if (!_started[i] || _ended[i])
            {
                continue;
            }

            _ended[i] = true;
            var block = _blocks[i].Block;
            if (block.SceneId is { } sceneId && block.End == BlockEnd.Stop && keep?.Contains(sceneId) != true && host.IsPlaying(sceneId))
            {
                host.Execute(new StopSceneCommand(CommandOrigin.Show, sceneId));
            }
            else if (block.Action is { Kind: BlockActionKind.Flash, SceneId: { } scene })
            {
                host.Execute(new FlashSceneCommand(CommandOrigin.Show, scene, false));
            }
            else if (block.Action is { Kind: BlockActionKind.Blackout })
            {
                host.Execute(new BlackoutCommand(CommandOrigin.Show, false));
            }
        }

    }
}
