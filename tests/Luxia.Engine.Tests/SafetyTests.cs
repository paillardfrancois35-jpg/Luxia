using Luxia.Engine.Model;
using Luxia.Messaging.Commands;
using Luxia.Messaging.Events;

namespace Luxia.Engine.Tests;

/// <summary>Étape 9 de la chaîne de rendu : limiteurs de strobe, de fumée et zones interdites (doc 15 §9).</summary>
public sealed class SafetyTests
{
    private readonly CapturingBus _bus = new();

    [Fact]
    [Trait("Exigence", "MOT-080")]
    [Trait("Exigence", "GEN-083")]
    [Trait("Exigence", "GEN-042")]
    [Trait("Exigence", "MOT-083")]
    public void Strobe_Requested30s_CutAfter10s_ThenPause_ThenAllowedAgain()
    {
        var harness = ParWithStrobe(new StrobeLimits { MaxContinuousSeconds = 10, PauseSeconds = 10 });

        // Surcharge brute de la console (étape 11) : même elle ne contourne pas le limiteur.
        harness.Send(Raw(5, 200));
        harness.Run(9.9);
        harness[5].ShouldBe((byte)200);

        harness.Run(0.3);
        harness[5].ShouldBe((byte)0, "au-delà de 10 s continues, le canal revient à sa valeur de repos");
        harness.Engine.Snapshot.ActiveLimits.ShouldContain(l => l.Kind == SafetyLimitKind.Strobe);

        harness.Run(9.5);
        harness[5].ShouldBe((byte)0, "pause forcée de 10 s");

        harness.Run(0.5);
        harness[5].ShouldBe((byte)200, "après la pause, le strobe est de nouveau permis");

        _bus.Of<SafetyLimitReached>().Count.ShouldBe(1, "une seule fois par épisode");
    }

    [Fact]
    [Trait("Exigence", "MOT-080")]
    [Trait("Exigence", "BIB-101")]
    public void Strobe_ValueInNoStrobeRange_IsNeverCounted()
    {
        var harness = ParWithStrobe(new StrobeLimits { MaxContinuousSeconds = 1 });

        // 0-4 = « Pas de strobe » (Q28) : un PAR qui éclaire fixe n'est jamais coupé.
        harness.Send(Raw(5, 3));
        harness.Run(5);

        harness[5].ShouldBe((byte)3);
        _bus.Of<SafetyLimitReached>().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "MOT-080")]
    public void Strobe_ShortInterruption_DoesNotResetTheCount()
    {
        var harness = ParWithStrobe(new StrobeLimits { MaxContinuousSeconds = 2, PauseSeconds = 5 });

        harness.Send(Raw(5, 200));
        harness.Run(1.5);
        harness.Send(Raw(5, 0));
        harness.Run(0.5);
        harness.Send(Raw(5, 200));
        harness.Run(0.7);

        harness[5].ShouldBe((byte)0, "1,5 s + 0,7 s de strobe coupés de 0,5 s = un seul épisode de plus de 2 s");
    }

    [Fact]
    [Trait("Exigence", "MOT-080")]
    [Trait("Exigence", "GEN-083")]
    public void Strobe_Forbidden_ForcesRestImmediately()
    {
        var harness = ParWithStrobe(new StrobeLimits { Forbidden = true });

        harness.Send(Raw(5, 200));
        harness.Tick();

        harness[5].ShouldBe((byte)0);
        _bus.Of<SafetyLimitReached>().Single().Detail.ShouldContain("interdit");
    }

    [Fact]
    [Trait("Exigence", "MOT-080")]
    [Trait("Exigence", "GEN-083")]
    public void Strobe_MaxSpeed_CapsTheProgressiveRange()
    {
        var harness = ParWithStrobe(new StrobeLimits { MaxSpeed = 0.5 });

        harness.Send(Raw(5, 255));
        harness.Tick();

        // Plage 5-255 : 50 % de la vitesse = 5 + 125.
        harness[5].ShouldBe((byte)130);
    }

    [Fact]
    [Trait("Exigence", "MOT-081")]
    [Trait("Exigence", "GEN-084")]
    [Trait("Exigence", "GEN-042")]
    public void Smoke_Held20s_CutAt10s_ThenRest30s()
    {
        var harness = Smoke(new SmokeLimits { MaxEmissionSeconds = 10, MinRestSeconds = 30 });

        harness.Send(Raw(180, 255));
        harness.Run(9.9);
        harness[180].ShouldBe((byte)255);

        harness.Run(0.3);
        harness[180].ShouldBe((byte)0, "maintien de 20 s → coupure à 10 s");

        harness.Run(29);
        harness[180].ShouldBe((byte)0, "repos minimal de 30 s, même si la commande est toujours là");

        harness.Run(1.5);
        harness[180].ShouldBe((byte)255);
    }

    [Fact]
    [Trait("Exigence", "MOT-081")]
    [Trait("Exigence", "GEN-084")]
    public void Smoke_ShortPuff_StartsTheMinimalRest()
    {
        var harness = Smoke(new SmokeLimits());

        harness.Send(Raw(180, 255));
        harness.Run(2);
        harness.Send(Raw(180, 0));
        harness.Run(5);
        harness.Send(Raw(180, 255));
        harness.Tick();

        harness[180].ShouldBe((byte)0, "une nouvelle émission 5 s après la précédente attend la fin des 30 s de repos");
    }

    [Fact]
    [Trait("Exigence", "MOT-082")]
    [Trait("Exigence", "GEN-085")]
    [Trait("Exigence", "MOT-083")]
    public void Zone_TargetInside_IsBroughtToTheNearestEdge()
    {
        var builder = new ShowBuilder();
        var lyre = builder.Lyre(111);
        var layer = builder.Layer("Mouvements", 3);
        var scene = builder.Scene("Vers le public", layer, ShowBuilder.Step(0, 1, ShowBuilder.V(lyre["pan"], 0.45), ShowBuilder.V(lyre["tilt"], 0.9)));
        builder.Safety = new SafetyModel
        {
            Zones = [new MovementGuard { FixtureId = lyre.Id, Label = "Lyre 1", Pan = lyre["pan"], Tilt = lyre["tilt"], Zones = [new PanTiltZone(0.3, 0.7, 0.8, 1)] }],
        };
        var harness = new EngineHarness(builder.Build(), bus: _bus);

        harness.Launch(scene);
        harness.Run(0.5);

        // Bord le plus proche : Tilt 0,8 (distance 0,1) plutôt que Pan 0,3 (0,15).
        harness.Value(lyre["pan"]).ShouldBe(0.45, 1e-9);
        harness.Value(lyre["tilt"]).ShouldBe(0.8, 1e-9);
        harness[113].ShouldBe((byte)Math.Floor(0.8 * 255));
        _bus.Of<SafetyLimitReached>().Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "MOT-082")]
    public void NearestAllowed_ZoneTouchingTheTiltLimit_NeverStopsOnThatLimit()
    {
        PanTiltZone[] zones = [new(0.3, 0.7, 0.8, 1)];
        var pan = 0.5;
        var tilt = 0.97;

        SafetyLimiter.NearestAllowed(zones, ref pan, ref tilt).ShouldBeTrue();

        tilt.ShouldBe(0.8, 1e-9, "la butée à 100 % fait partie de la zone : on ressort par le bas");
    }

    [Fact]
    [Trait("Exigence", "MOT-082")]
    public void NearestAllowed_WithOverlappingZones_AvoidsAllOfThem()
    {
        PanTiltZone[] zones = [new(0.3, 0.7, 0.8, 1), new(0.2, 0.8, 0.7, 0.85)];
        var pan = 0.5;
        var tilt = 0.9;

        SafetyLimiter.NearestAllowed(zones, ref pan, ref tilt).ShouldBeTrue();

        zones.ShouldAllBe(z => !z.Contains(pan, tilt));
    }

    private static OverrideChannelsCommand Raw(int channel, byte value) =>
        new(CommandOrigin.User, 1, [new ChannelValue(channel, value)]);

    private EngineHarness ParWithStrobe(StrobeLimits limits)
    {
        var builder = new ShowBuilder();
        var par = builder.Par7(1);
        builder.Safety = new SafetyModel
        {
            Strobe = limits,
            StrobeChannels =
            [
                new GuardedChannel
                {
                    FixtureId = par.Id,
                    Label = "PAR 1 – Strobe",
                    Universe = 1,
                    Channel = 5,
                    Active = [new ByteRange(5, 255)],
                    Progressive = [new ByteRange(5, 255)],
                },
            ],
        };
        return new EngineHarness(builder.Build(), bus: _bus);
    }

    private EngineHarness Smoke(SmokeLimits limits)
    {
        var builder = new ShowBuilder
        {
            Safety = new SafetyModel
            {
                Smoke = limits,
                SmokeChannels = [new GuardedChannel { FixtureId = Guid.NewGuid(), Label = "Fumée", Universe = 1, Channel = 180, Active = [new ByteRange(1, 255)] }],
            },
        };
        return new EngineHarness(builder.Build(), bus: _bus);
    }
}
