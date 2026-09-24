using System.Diagnostics;

namespace Dmx.Core.Time;

/// <summary>Horloge haute précision du système (<see cref="Stopwatch"/>).</summary>
public sealed class SystemClock : IClock
{
    private readonly long _origin = Stopwatch.GetTimestamp();

    /// <inheritdoc />
    public TimeSpan Now => Stopwatch.GetElapsedTime(_origin);
}
