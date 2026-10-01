using Luxia.Engine.Model;
using Luxia.Engine.Timing;
using Luxia.Messaging.Commands;

namespace Luxia.Engine;

// Partie « rythme » d'une lecture : événements qui font avancer les étapes (MOT-017), horloge propre de la scène (MOT-020), recalcul des
// durées musicales quand le tempo change (MOT-016) et cycles des effets en temps musicaux (MOT-062). L'état reste dans Playback.cs.
internal sealed partial class Playback
{
    /// <summary>Événements du tick qui comptent pour l'avance d'étape de la scène (MOT-017, SCN-052).</summary>
    private int CountEvents()
    {
        var clock = _own ?? Clock;
        if (Scene.AdvanceMultiplier > 1 && Scene.Advance is StepAdvanceMode.Beat or StepAdvanceMode.Bar)
        {
            // « ×2 », « ×4 » : on compte les sous-divisions franchies (demi-temps, quart de mesure…) sur la position de l'horloge.
            var unit = Scene.Advance == StepAdvanceMode.Bar ? Duration.BeatsPerBar : 1.0;
            var index = (long)Math.Floor((clock.EffectivePosition * Scene.AdvanceMultiplier / unit) + 1e-9);
            var crossed = _subIndex is { } last ? (int)Math.Clamp(index - last, 0, int.MaxValue) : 0;
            _subIndex = index;
            return crossed;
        }

        return Scene.Advance switch
        {
            StepAdvanceMode.Beat => clock.BeatsCrossed,
            StepAdvanceMode.Bar => clock.BarsCrossed,
            StepAdvanceMode.BassPulse => Events.AudioLive && _own is null ? Events.BassPulses : clock.BeatsCrossed,
            StepAdvanceMode.TreblePulse => Events.AudioLive && _own is null ? Events.TreblePulses : clock.BeatsCrossed,
            _ => 0,
        };
    }

    private void UpdateOwnClock()
    {
        if (Scene.OwnBpm is { } bpm)
        {
            _own ??= new MusicalClock();
            _own.SetFixed(bpm);
        }
        else
        {
            _own = null;
        }
    }

    /// <summary>Cycles d'un effet en temps musicaux pour une position donnée de l'horloge, en temps (MOT-062).</summary>
    private static double ClockCycle(double beats, Duration period)
    {
        var length = Math.Max(0.02, period.Value * (period.Unit == DurationUnit.Bars ? Duration.BeatsPerBar : 1));
        return beats / length;
    }

    /// <summary>
    /// MOT-016 : quand le tempo change en cours d'étape, la part musicale de la durée garde sa proportion :
    /// 2 temps à 120 BPM, à mi-étape le tempo passe à 60 → il reste 1 s. Les durées en secondes ne bougent pas.
    /// </summary>
    private void RescaleForTempo(double bpm)
    {
        if (Math.Abs(bpm - _stepBpm) < 1e-9 || Scene.Steps.Count == 0)
        {
            return;
        }

        var factor = _stepBpm / bpm;
        var fade = _fadeMusical ? _stepFade * factor : _stepFade;
        var holdOld = _stepLength - _stepFade;
        var hold = _holdMusical ? holdOld * factor : holdOld;
        _stepElapsed = Rescale(_stepElapsed, _stepFade, fade, factor);
        _transitionElapsed = Rescale(_transitionElapsed, _stepFade, fade, factor);
        _transitionEnd = _fadeMusical && _transitionEnd > 0 && Math.Abs(_transitionEnd - _stepFade) < 1e-9 ? fade : _transitionEnd;
        _stepFade = fade;
        _stepLength = fade + hold;
        _stepBpm = bpm;

        double Rescale(double elapsed, double oldFade, double newFade, double k)
        {
            if (elapsed < oldFade)
            {
                return _fadeMusical ? elapsed * k : elapsed;
            }

            return newFade + ((elapsed - oldFade) * (_holdMusical ? k : 1));
        }
    }
}
