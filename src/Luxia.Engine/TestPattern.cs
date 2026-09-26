using Luxia.Core.Dmx;
using Luxia.Messaging.Commands;

namespace Luxia.Engine;

/// <summary>État observable du test de sortie.</summary>
/// <param name="Active">Test en cours.</param>
/// <param name="Universe">Univers testé.</param>
/// <param name="CurrentChannel">Canal allumé (0 si aucun).</param>
public readonly record struct TestPatternState(bool Active, int Universe, int CurrentChannel);

/// <summary>
/// Chenillard de test canal par canal (CMD-024, SORT-007). Calculé à partir du temps écoulé réel
/// (et non du nombre de ticks) : un tick en retard ne ralentit pas le chenillard (GEN-032).
/// </summary>
internal sealed class TestPattern
{
    private TestOutputCommand? _command;
    private int[] _channels = [];
    private int[] _heldChannels = [];
    private TimeSpan _start;
    private volatile int _currentChannel;

    public TestPatternState State
    {
        get
        {
            var command = _command;
            return command is null ? default : new TestPatternState(true, command.Universe, _currentChannel);
        }
    }

    public void Apply(TestOutputCommand command, TimeSpan now)
    {
        if (!command.Active)
        {
            _command = null;
            _currentChannel = 0;
            return;
        }

        _channels = [.. Enumerable.Range(command.Range.First, command.Range.Count).Except(command.ExcludedChannels)];
        // SORT-008 : les canaux maintenus restent à la valeur de test tout le chenillard, exclusions toujours respectées.
        _heldChannels = [.. command.HeldChannelsOrEmpty.Except(command.ExcludedChannels).Where(c => c is >= 1 and <= DmxConstants.ChannelCount)];
        _start = now;
        _command = _channels.Length == 0 ? null : command;
    }

    public void Render(int universe, DmxFrame frame, TimeSpan now)
    {
        var command = _command;
        if (command is null || command.Universe != universe)
        {
            return;
        }

        var stepTicks = Math.Max(command.StepDuration.Ticks, 1);
        if (command.Mode == TestPatternMode.Ramp)
        {
            RenderRamp(command, frame, (now - _start).Ticks, stepTicks);
            return;
        }

        var step = (now - _start).Ticks / stepTicks;
        if (step >= _channels.Length && !command.Loop)
        {
            _command = null;
            _currentChannel = 0;
            return;
        }

        var channel = _channels[(int)(step % _channels.Length)];
        _currentChannel = channel;

        // D19 : le test remplace la restitution de l'univers ; seul le canal courant est allumé.
        frame.Clear();
        foreach (var held in _heldChannels)
        {
            frame[held] = command.Value;
        }

        frame[channel] = command.Value;
    }

    private void RenderRamp(TestOutputCommand command, DmxFrame frame, long elapsedTicks, long periodTicks)
    {
        frame.Clear();
        var values = frame.Values;
        for (var i = 0; i < _channels.Length; i++)
        {
            // Onde triangulaire 0 → valeur → 0, décalée d'un canal à l'autre : toutes les valeurs changent à chaque tick.
            var phase = ((elapsedTicks + (periodTicks * i / _channels.Length)) % periodTicks) / (double)periodTicks;
            var level = phase < 0.5 ? phase * 2 : (1 - phase) * 2;
            values[_channels[i] - 1] = (byte)Math.Round(level * command.Value);
        }

        _currentChannel = 0;
    }
}
