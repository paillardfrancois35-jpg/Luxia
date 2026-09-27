using Luxia.Engine.Model;
using Luxia.Patch.Model;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Tests;

/// <summary>Compilation des limites de sûreté sur le parc réel (doc 15 §9).</summary>
public sealed class SafetyCompilerTests
{
    private readonly ReferenceProject _project = new();

    [Fact]
    [Trait("Exigence", "MOT-080")]
    [Trait("Exigence", "BIB-101")]
    public void StrobeChannels_OfReferenceRig_UseTheNoStrobeRanges()
    {
        var safety = ShowCompiler.Compile(_project.Content()).Model.Safety;

        // PAR 1 (LPC008S 7CH, adresse 1) : Strobe au canal 5, 0-4 = pas de strobe (Q28).
        var par = safety.StrobeChannels.Single(c => c.FixtureId == _project.Fixture("PAR 1").Id);
        par.Channel.ShouldBe(5);
        par.Rest.ShouldBe((byte)0);
        par.IsActive(4).ShouldBeFalse();
        par.IsActive(5).ShouldBeTrue();

        // Lyre 1 (Tomshine 11CH, adresse 111) : obturateur 0-7 ouvert, 8-131 strobe croissant.
        var lyre = safety.StrobeChannels.Where(c => c.FixtureId == _project.Fixture("Lyre 1").Id).ShouldHaveSingleItem();
        lyre.IsActive(0).ShouldBeFalse();
        lyre.IsActive(100).ShouldBeTrue();
        lyre.Progressive.ShouldContain(r => r.Min == 8 && r.Max == 131 && r.Increasing);

        // Barre 1 (LCB803 24CH) : une voie de strobe par section, 0 = pas de strobe.
        var bar = safety.StrobeChannels.Where(c => c.FixtureId == _project.Fixture("Barre 1").Id).ToList();
        bar.Count.ShouldBe(4);
        bar.ShouldAllBe(c => !c.IsActive(0) && c.IsActive(1));
    }

    [Fact]
    [Trait("Exigence", "MOT-081")]
    public void SmokeChannel_OfReferenceRig_IsChannel180()
    {
        var safety = ShowCompiler.Compile(_project.Content()).Model.Safety;

        var smoke = safety.SmokeChannels.ShouldHaveSingleItem();
        smoke.Channel.ShouldBe(180);
        smoke.IsActive(0).ShouldBeFalse();
        smoke.IsActive(1).ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "GEN-083")]
    [Trait("Exigence", "GEN-084")]
    public void Settings_AreCarriedToTheEngine()
    {
        var settings = new SafetySettings
        {
            Strobe = new StrobeSafety { MaxContinuousSeconds = 5, PauseSeconds = 20, Forbidden = true, MaxSpeedPercent = 60 },
            Smoke = new SmokeSafety { MaxEmissionSeconds = 4, MinRestSeconds = 60 },
        };

        var safety = ShowCompiler.Compile(_project.Content() with { Safety = settings }).Model.Safety;

        safety.Strobe.ShouldBe(new StrobeLimits { MaxContinuousSeconds = 5, PauseSeconds = 20, Forbidden = true, MaxSpeed = 0.6 });
        safety.Smoke.ShouldBe(new SmokeLimits { MaxEmissionSeconds = 4, MinRestSeconds = 60 });
    }

    [Fact]
    [Trait("Exigence", "INST-053")]
    [Trait("Exigence", "MOT-082")]
    public void Zones_OfActiveVenue_TargetThePanTiltParameters()
    {
        var lyre = _project.Fixture("Lyre 1");
        var active = _project.Venues.Active;
        var venues = _project.Venues with
        {
            Venues = [.. _project.Venues.Venues.Select(v => v.Id != active.Id ? v : v with
            {
                ForbiddenZones =
                [
                    new ForbiddenZone { FixtureId = lyre.Id, Name = "Public", PanMin = 0.3, PanMax = 0.7, TiltMin = 0.8, TiltMax = 1 },
                    new ForbiddenZone { FixtureId = lyre.Id, Name = "Vide", PanMin = 0.5, PanMax = 0.5 },
                ],
            })],
        };

        var result = ShowCompiler.Compile(_project.Content() with { Venues = venues });

        var guard = result.Model.Safety.Zones.ShouldHaveSingleItem();
        guard.Pan.ShouldBe(result.Model.IndexOf(lyre.Id, "pan"));
        guard.Tilt.ShouldBe(result.Model.IndexOf(lyre.Id, "tilt"));
        guard.Zones.ShouldBe([new PanTiltZone(0.3, 0.7, 0.8, 1)]);
        result.Issues.ShouldContain(i => i.Message.Contains("rectangle vide"));
    }
}
