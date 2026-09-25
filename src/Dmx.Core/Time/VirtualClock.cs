namespace Dmx.Core.Time;

/// <summary>
/// Horloge virtuelle pour les tests et la simulation accélérée (GEN-033) : le temps n'avance que sur demande.
/// </summary>
public sealed class VirtualClock : IClock
{
    private long _ticks;

    /// <summary>Crée une horloge virtuelle positionnée sur <paramref name="start"/>.</summary>
    public VirtualClock(TimeSpan start = default) => _ticks = start.Ticks;

    /// <inheritdoc />
    public TimeSpan Now => new(Interlocked.Read(ref _ticks));

    /// <summary>Fait avancer le temps.</summary>
    public void Advance(TimeSpan delta)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delta, TimeSpan.Zero);
        Interlocked.Add(ref _ticks, delta.Ticks);
    }
}
