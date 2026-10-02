using System.Diagnostics;
using Luxia.Core.Dmx;
using Luxia.Engine.Model;
using Luxia.Messaging.Events;
using static Luxia.Engine.Tests.ShowBuilder;

namespace Luxia.Engine.Tests;

/// <summary>T-MOT-08 (partie automatisable) : budget de calcul d'un tick, pas d'allocation en régime établi, abonné lent.</summary>
[Collection(RealTimeTests.Name)]
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

        // Seuil strict, série recommencée jusqu'à trois fois (docs/03 §11) ; l'allocation est comptée sur la dernière série.
        // Le capteur de test copie chaque trame (un byte[512] par tick, part mesurée ci-dessous) dans une liste : on la
        // vide et on la dimensionne avant chaque série, sinon son agrandissement (doublement de capacité) tombe dans la
        // mesure selon le nombre d'essais — cause de l'échec intermittent sous charge (23 576 octets au 3e essai).
        const int Ticks = 400;
        var sinkBytesPerTick = AllocatedBy(() => _ = new byte[DmxConstants.ChannelCount]);
        // Temps retenu = la meilleure série (jusqu'à 5) : la charge des autres suites ne peut que ralentir le tick, jamais
        // l'accélérer ; mesuré le 2026-09-29 : ≈ 1 ms seul, jusqu'à 5,7 ms en pleine série complète (docs/03 §11).
        // P8 : avec un projet de tests de plus, la charge de la série complète dure plus longtemps que cinq séries ; au-delà, une
        // série par seconde pendant au plus 90 s, la durée d'une série complète (la charge des autres suites finit par retomber), seuil inchangé.
        var watch = new Stopwatch();
        var best = double.MaxValue;
        long allocated = 0;
        var deadline = Stopwatch.StartNew();
        for (var attempt = 0; best >= 5 && (attempt < 5 || deadline.Elapsed < TimeSpan.FromSeconds(90)); attempt++)
        {
            if (attempt >= 5)
            {
                Thread.Sleep(1000);
            }

            engine.Sink.Frames.Clear();
            engine.Sink.Frames.Capacity = Ticks;
            var before = GC.GetAllocatedBytesForCurrentThread();
            watch.Restart();
            for (var i = 0; i < Ticks; i++)
            {
                engine.Clock.Advance(EngineHarness.Period);
                engine.Engine.Tick();
            }

            watch.Stop();
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            best = Math.Min(best, watch.Elapsed.TotalMilliseconds / Ticks);
        }

        engine.Engine.Snapshot.Playbacks.Count.ShouldBe(40);
        best.ShouldBeLessThan(5);

        // Mesure par fil (celui du tick) : exacte, insensible aux autres tests. Le moteur seul n'alloue rien (0 octet
        // mesuré) ; la marge de 1 Kio sur 400 ticks refuse toute allocation par tick (un seul objet par tick dépasserait
        // 9 Kio), là où l'ancienne marge de 20 000 octets en laissait passer ≈ 50 par tick.
        (allocated - (Ticks * sinkBytesPerTick)).ShouldBeLessThan(1024);
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

        // Chaque tick publie un ÉtapeChangée que l'abonné met 50 ms à traiter : si le tick l'attendait, il durerait
        // au moins 50 ms. Seuil 25 ms, gardé strict ; la série de mesures recommence jusqu'à trois fois (docs/03 §11 :
        // un pic de charge de la suite l'avait fait échouer une fois, essai P5 2026-09-27).
        var worst = TimeSpan.MaxValue;
        for (var attempt = 0; attempt < 3 && worst.TotalMilliseconds >= 25; attempt++)
        {
            worst = TimeSpan.Zero;
            for (var i = 0; i < 80; i++)
            {
                var watch = Stopwatch.StartNew();
                engine.Tick();
                worst = worst > watch.Elapsed ? worst : watch.Elapsed;
            }
        }

        worst.TotalMilliseconds.ShouldBeLessThan(25);
    }

    private static long AllocatedBy(Action action)
    {
        action();
        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
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
                // Une scène sur deux porte aussi un effet (MOT-060) : les effets n'allouent pas non plus.
                var shape = (EffectShape)(s % 13);
                var effect = new EngineEffect
                {
                    Id = Guid.NewGuid(),
                    Shape = shape,
                    Relative = s % 4 == 0,
                    Direction = (EffectDirection)(s % 3),
                    Channels = [.. fixtures
                        .Where((_, i) => (i + s) % 5 == 0)
                        .Select((f, m) => new EffectChannel(f.Parameters.Values.Last(), m, m * 0.1, 0.5, 0.8, (EffectAxis)(m % 2), shape == EffectShape.Table ? [0.0, 0.5, 1.0] : null))],
                };
                steps.Add(Step(0.3, 0.2, values) with { Effects = s % 2 == 0 ? [effect] : [], HueFade = s % 3 == 0 });
            }

            scenes.Add(show.Scene($"Scène {s}", layers[s % 20], [.. steps]));
        }

        return (show.Build(), scenes);
    }
}
