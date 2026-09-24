using Dmx.Core.Dmx;
using Dmx.Messaging.Commands;

namespace Dmx.Engine;

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
        frame[channel] = command.Value;
    }
}
