using Luxia.Engine.Model;
using Luxia.Engine.Sequencing;
using Luxia.Engine.Timing;
using Luxia.Messaging.Commands;
using Luxia.Messaging.Events;
using Microsoft.Extensions.Logging;

namespace Luxia.Engine;

// Partie « séquenceur » du moteur (D37, D38) : branchement du séquenceur de shows, appel à chaque tick, commandes qui lui sont
// destinées, mode simulation (CMD-053) et événement TempoChangé (EVT-024). Les champs propres à cette partie sont déclarés ici.
public sealed partial class RenderEngine
{
    private ISequencer? _sequencer;
    private SequencerHost? _sequencerHost;
    private MusicCues _simulatedCues;
    private double _simulatedEnergy = double.NaN;
    private string? _simulatedStyle;
    private double _lastTempoBpm = double.NaN;
    private TempoSourceKind _lastTempoSource;

    /// <summary>
    /// Branche (ou débranche) le séquenceur de shows et de séquences (D37) : appelé à chaque tick sur le fil du moteur ; reçoit les
    /// commandes CMD-050 à 052.
    /// </summary>
    public void SetSequencer(ISequencer? sequencer) => Volatile.Write(ref _sequencer, sequencer);

    private SequencerHost Host => _sequencerHost ??= new SequencerHost(this);

    private string? ApplySequencer(SequencerCommand command)
    {
        var sequencer = Volatile.Read(ref _sequencer);
        if (sequencer is null)
        {
            return "aucun séquenceur de shows";
        }

        try
        {
            return sequencer.Apply(command, Host);
        }
        catch (Exception ex)
        {
            // Un défaut du séquenceur ne doit jamais arrêter le moteur (GEN-117).
            _logger.LogError(ex, "Erreur du séquenceur sur {Commande}", command.GetType().Name);
            return "erreur du séquenceur";
        }
    }

    private void TickSequencer(TimeSpan now)
    {
        var sequencer = Volatile.Read(ref _sequencer);
        var host = Host;
        host.Begin(now, Signals());
        _simulatedCues = MusicCues.None;
        if (sequencer is null)
        {
            return;
        }

        try
        {
            sequencer.Tick(host);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur du séquenceur pendant un tick");
        }
    }

    // CMD-053 : le mode simulation provoque des événements, impose une énergie et un style, sans toucher à l'écoute réelle.
    private void Simulate(SimulateMusicCommand command)
    {
        _simulatedCues |= command.Cue switch
        {
            SimulatedCue.Drop => MusicCues.Drop,
            SimulatedCue.Break => MusicCues.Break,
            SimulatedCue.BuildUp => MusicCues.BuildUp,
            SimulatedCue.Silence => MusicCues.Silence,
            SimulatedCue.Resumed => MusicCues.Resumed | MusicCues.SongChanged,
            SimulatedCue.SongChanged => MusicCues.SongChanged,
            _ => MusicCues.None,
        };
        if (command.Energy is { } energy)
        {
            _simulatedEnergy = double.IsNaN(energy) ? double.NaN : Math.Clamp(energy, 0, 1);
        }

        if (command.Style is { } style)
        {
            _simulatedStyle = style.Length == 0 ? null : style;
        }
    }

    private MusicSignals Signals()
    {
        var simulated = !double.IsNaN(_simulatedEnergy);
        var energy = simulated ? _simulatedEnergy : _events.Energy;
        var level = simulated ? LevelOf(energy) : _events.EnergyLevel;
        return new MusicSignals(_events.AudioLive || simulated, energy, level, _events.Cues | _simulatedCues, _simulatedStyle);
    }

    // Seuils des niveaux d'énergie de l'écoute (doc 19 §10) : 0,30 / 0,52 / 0,72.
    private static int LevelOf(double energy) => energy switch
    {
        >= 0.72 => 3,
        >= 0.52 => 2,
        >= 0.30 => 1,
        _ => 0,
    };

    // EVT-024 : publié quand le tempo bouge d'au moins 1 BPM ou change de source (D38), pas à chaque tick.
    private void PublishTempoChange(TimeSpan now)
    {
        if (_bus is null)
        {
            return;
        }

        var bpm = _tempo.Bpm;
        var source = _tempo.Source;
        if (!double.IsNaN(_lastTempoBpm) && Math.Abs(bpm - _lastTempoBpm) < 1 && source == _lastTempoSource)
        {
            return;
        }

        _lastTempoBpm = bpm;
        _lastTempoSource = source;
        _bus.Publish(new TempoChanged(bpm, _tempo.Confidence, source, now));
    }

    private bool ScenePlaying(Guid sceneId)
    {
        foreach (var playback in _playbacks)
        {
            if (playback.Scene.Id == sceneId && !playback.Flash && playback.State is not (PlaybackState.FadingOut or PlaybackState.Done))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Vue du moteur donnée au séquenceur le temps d'un tick (une seule instance, réutilisée : aucune allocation par tick).</summary>
    private sealed class SequencerHost(RenderEngine engine) : ISequencerHost
    {
        public TimeSpan Now { get; private set; }

        public double Bpm => engine._tempo.Bpm;

        public double BeatPosition => engine._tempo.EffectivePosition;

        public MusicSignals Music { get; private set; }

        public ShowModel Show => engine._show;

        public double GrandMaster => engine._grandMaster;

        public void Begin(TimeSpan now, MusicSignals music)
        {
            Now = now;
            Music = music;
        }

        public bool IsPlaying(Guid sceneId) => engine.ScenePlaying(sceneId);

        public double LayerLevel(Guid layerId)
        {
            var index = engine._show.IndexOfLayer(layerId);
            return index >= 0 && index < engine._layerMasters.Length ? engine._layerMasters[index] : 1;
        }

        public string? Execute(Command command, bool log = true)
        {
            ArgumentNullException.ThrowIfNull(command);
            if (log)
            {
                var rejection = engine.ApplyCore(command, Now, Now);
                var entry = new CommandLogEntry(Now, Now, command, rejection);
                engine.Log(entry);
                engine.CommandApplied?.Invoke(entry);
                return rejection;
            }

            return engine.ApplyCore(command, Now, Now);
        }

        public void Publish<TEvent>(TEvent evt)
            where TEvent : class => engine._bus?.Publish(evt);
    }
}
