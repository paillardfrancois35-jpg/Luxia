using System.Diagnostics;
using Luxia.Engine.Model;
using Luxia.Messaging.Events;
using static Luxia.Engine.Tests.ShowBuilder;

namespace Luxia.Engine.Tests;

/// <summary>T-MOT-08 (partie automatisable) : budget de calcul d'un tick, pas d'allocation en régime établi, abonné lent.</summary>
public sealed class EnginePerformanceTests
{
    [Fact]
    [Trait("Exigence", "MOT-002")]
    public void Tick_With100Fixtures20Layers40Playbacks_StaysUnderFiveMilliseconds_WithoutAllocating()
    {
        var (model, scenes) = BigShow();
        var engine = new EngineHarness(model);
        foreach (var scene in scenes)
        {
            engine.Launch(scene);
        }

        engine.Run(2);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        const int Ticks = 400;
        for (var i = 0; i < Ticks; i++)
        {
            engine.Clock.Advance(EngineHarness.Period);
            engine.Engine.Tick();
        }

        watch.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        engine.Engine.Snapshot.Playbacks.Count.ShouldBe(40);
        (watch.Elapsed.TotalMilliseconds / Ticks).ShouldBeLessThan(5);

        // Le capteur de test copie chaque trame : on retire sa part (≈ 512 octets + en-tête par tick).
        (allocated - (Ticks * 600L)).ShouldBeLessThan(20_000);
    }

    [Fact]
    [Trait("Exigence", "GEN-013")]
    [Trait("Exigence", "MOT-100")]
    public async Task SlowSubscriber_DoesNotDelayTicks()
    {
        await using var bus = new EventBus();
        bus.Subscribe<StepChanged>(_ => Thread.Sleep(50));
        var show = new ShowBuilder();
        var par = show.Par7(1);
        var layer = show.Layer("Tout", 1);
        var chase = show.Scene("Chase rapide", layer, Step(0, 0.025, V(par["r"], 1)), Step(0, 0.025, V(par["g"], 1)));
        var engine = new EngineHarness(show.Build(), bus: bus);
        engine.Launch(chase);
        engine.Tick();

        var worst = TimeSpan.Zero;
        for (var i = 0; i < 80; i++)
        {
            var watch = Stopwatch.StartNew();
            engine.Tick();
            worst = worst > watch.Elapsed ? worst : watch.Elapsed;
        }

        // Chaque tick publie un ÉtapeChangée que l'abonné met 50 ms à traiter : le tick n'attend pas.
        worst.TotalMilliseconds.ShouldBeLessThan(5);
    }

    private static (ShowModel Model, IReadOnlyList<EngineScene> Scenes) BigShow()
    {
        var show = new ShowBuilder();
        var fixtures = new List<TestFixture>();
        for (var i = 0; i < 100; i++)
        {
            // 100 appareils sur 4 univers virtuels ramenés à 1 : on répartit sur 512 canaux en boucle, sans importance pour la mesure.
            fixtures.Add(i % 2 == 0 ? show.Par7(1 + (i * 5 % 500)) : show.Lyre(1 + (i * 5 % 500)));
        }

        var layers = Enumerable.Range(0, 20).Select(i => show.Layer($"Couche {i}", i, exclusive: false, mode: (IntensityMode)(i % 4))).ToList();
        var scenes = new List<EngineScene>();
        var random = new Random(7);
        for (var s = 0; s < 40; s++)
        {
            var steps = new List<EngineStep>();
            for (var k = 0; k < 4; k++)
            {
                var values = fixtures
                    .Where((_, i) => (i + s) % 3 == 0)
                    .SelectMany(f => f.Parameters.Values.Take(4))
                    .Select(p => new StepValue(p, random.NextDouble()))
                    .ToArray();
                steps.Add(Step(0.3, 0.2, values));
            }

            scenes.Add(show.Scene($"Scène {s}", layers[s % 20], [.. steps]));
        }

        return (show.Build(), scenes);
    }
}
