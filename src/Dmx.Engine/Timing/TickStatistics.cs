namespace Dmx.Engine.Timing;

/// <summary>Mesures de cadence du moteur (GEN-030, GEN-031).</summary>
/// <param name="TickCount">Nombre de ticks mesurés.</param>
/// <param name="Elapsed">Durée de la mesure.</param>
/// <param name="TargetRateHz">Fréquence visée.</param>
/// <param name="MeanLateness">Retard moyen d'un tick sur son échéance.</param>
/// <param name="P99Lateness">99e centile du retard (gigue, GEN-031 : &lt; 5 ms).</param>
/// <param name="MaxLateness">Retard maximal observé.</param>
/// <param name="SkippedTicks">Ticks sautés après un retard supérieur à une période.</param>
public readonly record struct TickStatistics(
    long TickCount,
    TimeSpan Elapsed,
    double TargetRateHz,
    TimeSpan MeanLateness,
    TimeSpan P99Lateness,
    TimeSpan MaxLateness,
    long SkippedTicks)
{
    /// <summary>Fréquence moyenne mesurée.</summary>
    public double MeasuredRateHz => Elapsed > TimeSpan.Zero ? TickCount / Elapsed.TotalSeconds : 0;
}

/// <summary>
/// Accumulateur de retards sans allocation : histogramme par pas de 0,1 ms jusqu'à 100 ms.
/// </summary>
internal sealed class LatenessHistogram
{
    private const int BucketCount = 1000;
    private static readonly long BucketTicks = TimeSpan.FromMilliseconds(0.1).Ticks;

    private readonly long[] _buckets = new long[BucketCount + 1];
    private readonly Lock _lock = new();
    private long _count;
    private long _sumTicks;
    private long _maxTicks;
    private long _skipped;

    public void Add(TimeSpan lateness)
    {
        var ticks = Math.Max(lateness.Ticks, 0);
        var bucket = (int)Math.Min(ticks / BucketTicks, BucketCount);
        lock (_lock)
        {
            _buckets[bucket]++;
            _count++;
            _sumTicks += ticks;
            _maxTicks = Math.Max(_maxTicks, ticks);
        }
    }

    public void AddSkipped(long count)
    {
        lock (_lock)
        {
            _skipped += count;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            Array.Clear(_buckets);
            _count = _sumTicks = _maxTicks = _skipped = 0;
        }
    }

    public TickStatistics Snapshot(TimeSpan elapsed, double targetRateHz)
    {
        lock (_lock)
        {
            if (_count == 0)
            {
                return new TickStatistics(0, elapsed, targetRateHz, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, _skipped);
            }

            var threshold = (long)Math.Ceiling(_count * 0.99);
            long cumulated = 0;
            var p99Bucket = BucketCount;
            for (var i = 0; i <= BucketCount; i++)
            {
                cumulated += _buckets[i];
                if (cumulated >= threshold)
                {
                    p99Bucket = i;
                    break;
                }
            }

            return new TickStatistics(
                _count,
                elapsed,
                targetRateHz,
                new TimeSpan(_sumTicks / _count),
                new TimeSpan((p99Bucket + 1) * BucketTicks),
                new TimeSpan(_maxTicks),
                _skipped);
        }
    }
}
