using Dmx.Core.Time;
using Dmx.Engine.Timing;

namespace Dmx.Engine.Tests;

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
    /// Mesure courte en temps réel (2 s) : la mesure d'une heure (GEN-030 / GEN-031) se fait avec
    /// <c>dmx-headless gigue</c>. Ici on vérifie seulement l'ordre de grandeur.
    /// </summary>
    [Fact]
    [Trait("Exigence", "GEN-030")]
    [Trait("Exigence", "GEN-031")]
    public void Run_TwoSeconds_KeepsFortyHertz()
    {
        var count = 0;
        using var loop = new TickLoop(() => Interlocked.Increment(ref count), new SystemClock());

        loop.Start();
        Thread.Sleep(2000);
        loop.Stop();
        var stats = loop.Statistics;

        stats.MeasuredRateHz.ShouldBe(40, 1.0);
        stats.P99Lateness.ShouldBeLessThan(TimeSpan.FromMilliseconds(5));
        count.ShouldBeInRange(76, 84);
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
