using Luxia.Core.Time;
using Luxia.Engine.Model;
using Luxia.Engine.Timing;
using Luxia.Messaging.Commands;
using Microsoft.Extensions.Logging;

namespace Luxia.Engine;

// Partie « musique » du moteur : branchement de l'écoute, lecture de ses mesures à chaque tick et lancements quantifiés (MOT-018).
// Les champs (`_audioFeed`, `_quantized`, `_events`, `_tempo`) restent dans RenderEngine.cs avec le reste de l'état.
public sealed partial class RenderEngine
{
    /// <summary>
    /// Branche (ou débranche) l'écoute de la musique (doc 19) : le moteur lit son tempo et ses impulsions à chaque tick.
    /// Sans écoute, les scènes à impulsion avancent au temps de l'horloge (SCN-052).
    /// </summary>
    public void SetAudioFeed(IAudioFeed? feed) => Volatile.Write(ref _audioFeed, feed);

    private void ReadAudio()
    {
        var feed = Volatile.Read(ref _audioFeed);
        if (feed is null)
        {
            _events.AudioLive = false;
            _events.BassPulses = 0;
            _events.TreblePulses = 0;
            _events.Energy = 0;
            return;
        }

        var reading = feed.Read();
        _events.AudioLive = reading.Live;
        _events.BassPulses = reading.BassPulses;
        _events.TreblePulses = reading.TreblePulses;
        _events.Energy = reading.Energy;
        if (_tempo.Source == TempoSourceKind.Audio)
        {
            _tempo.FollowAudio(reading);
        }
    }

    /// <summary>Position de l'horloge (en temps) où démarrer une scène quantifiée ; <c>null</c> = tout de suite.</summary>
    private double? QuantizeTarget(LaunchQuantize quantize)
    {
        var unit = quantize switch
        {
            LaunchQuantize.Beat => 1.0,
            LaunchQuantize.Bar => Duration.BeatsPerBar,
            LaunchQuantize.Phrase4 => 4.0 * Duration.BeatsPerBar,
            LaunchQuantize.Phrase8 => 8.0 * Duration.BeatsPerBar,
            _ => 0,
        };
        if (unit <= 0)
        {
            return null;
        }

        var position = _tempo.EffectivePosition;
        var next = Math.Ceiling((position / unit) - 1e-9) * unit;
        return next - position < 0.02 ? null : next;
    }

    private void LaunchDueQuantized(TimeSpan now)
    {
        if (_quantized.Count == 0)
        {
            return;
        }

        var position = _tempo.EffectivePosition;
        for (var i = 0; i < _quantized.Count; i++)
        {
            var pending = _quantized[i];
            if (position + 1e-6 < pending.TargetBeat)
            {
                continue;
            }

            _quantized.RemoveAt(i);
            i--;
            var rejection = LaunchScene(pending.Command with { Immediate = true }, now);
            if (rejection is not null)
            {
                _logger.LogWarning("Lancement quantifié refusé : {Motif}", rejection);
            }
        }
    }

    /// <summary>Annule les lancements en attente dont la scène satisfait le filtre (arrêts de scène ou de couche).</summary>
    private int CancelQuantized(Func<EngineScene?, bool> filter) =>
        _quantized.RemoveAll(q => filter(_show.Scene(q.SceneId)));
}
