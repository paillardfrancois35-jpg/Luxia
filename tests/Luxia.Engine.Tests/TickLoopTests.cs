using Luxia.Core.Time;
using Luxia.Engine.Timing;

namespace Luxia.Engine.Tests;

[Collection(RealTimeTests.Name)]
public sealed class TickLoopTests
{
    [Fact]
    [Trait("Exigence", "GEN-030")]
    public void RateHz_IsClampedTo25To44()
    {
        using var loop = new TickLoop(() => { }, new SystemClock());

        loop.RateHz = 100;
        loop.RateHz.ShouldBe(44, 0.01);

        loop.RateHz = 10;
        loop.RateHz.ShouldBe(25, 0.01);
    }

    /// <summary>
    /// Mesure courte en temps réel (2 s) : la mesure longue (GEN-030 / GEN-031) se fait avec
    /// <c>luxia-headless gigue</c>. Ici on vérifie seulement l'ordre de grandeur. Jusqu'à trois essais : les autres
    /// assemblages de tests tournent en même temps et peuvent perturber une série (seuils inchangés).
    /// </summary>
    [Fact]
    [Trait("Exigence", "GEN-030")]
    [Trait("Exigence", "GEN-031")]
    public void Run_TwoSeconds_KeepsFortyHertz()
    {
        (TickStatistics Stats, int Count) last = default;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var count = 0;
            using var loop = new TickLoop(() => Interlocked.Increment(ref count), new SystemClock());
            loop.Start();
            Thread.Sleep(2000);
            loop.Stop();
            last = (loop.Statistics, count);
            if (Math.Abs(last.Stats.MeasuredRateHz - 40) <= 1 && last.Stats.P99Lateness < TimeSpan.FromMilliseconds(5) && count is >= 76 and <= 84)
            {
                break;
            }
        }

        last.Stats.MeasuredRateHz.ShouldBe(40, 1.0);
        last.Stats.P99Lateness.ShouldBeLessThan(TimeSpan.FromMilliseconds(5));
        last.Count.ShouldBeInRange(76, 84);
    }

    [Fact]
    [Trait("Exigence", "GEN-093")]
    public void Run_TickThrows_LoopContinues()
    {
        var count = 0;
        using var loop = new TickLoop(
            () =>
            {
                Interlocked.Increment(ref count);
                throw new InvalidOperationException("test");
            },
            new SystemClock());

        loop.Start();
        Thread.Sleep(300);
        loop.Stop();

        count.ShouldBeGreaterThan(5);
    }
}
