using Luxia.Fixtures.Model;
using Luxia.Fixtures.Rules;

namespace Luxia.Fixtures.Tests;

/// <summary>T-BIB-04 : intensité virtuelle, « Suit l'intensité » et étiquettes de sûreté déduites.</summary>
public sealed class FixtureRulesTests
{
    [Fact]
    [Trait("Exigence", "BIB-006")]
    public void ThreeChannelMode_HasVirtualIntensity_AndColorsFollowIntensity()
    {
        var par = Samples.Par;
        var mode = par.Modes[0];

        FixtureRules.HasVirtualIntensity(par, mode).ShouldBeTrue();
        foreach (var key in new[] { "r", "g", "b" })
        {
            FixtureRules.FollowsIntensity(par, mode, par.Channel(key)!).ShouldBeTrue();
        }
    }

    [Fact]
    [Trait("Exigence", "BIB-006")]
    public void SevenChannelMode_WithDimmer_NothingFollowsIntensity()
    {
        var par = Samples.Par;
        var mode = par.Modes[1];

        FixtureRules.HasVirtualIntensity(par, mode).ShouldBeFalse();
        par.Channels.ShouldAllBe(c => !FixtureRules.FollowsIntensity(par, mode, c));
    }

    [Fact]
    [Trait("Exigence", "BIB-006")]
    public void FollowsIntensity_Override_WinsOverDeduction()
    {
        var par = Samples.Par;
        var blue = par.Channel("b")! with { FollowsIntensity = false };
        par = par with { Channels = [.. par.Channels.Select(c => c.Key == "b" ? blue : c)] };

        FixtureRules.FollowsIntensity(par, par.Modes[0], par.Channel("b")!).ShouldBeFalse();
        FixtureRules.FollowsIntensity(par, par.Modes[0], par.Channel("r")!).ShouldBeTrue();
    }

    [Fact]
    public void NonLightChannels_NeverFollowIntensity()
    {
        var head = Samples.MovingHead;

        FixtureRules.FollowsIntensity(head, head.Modes[0], head.Channel("pan")!).ShouldBeFalse();
    }

    [Fact]
    public void CellEmitter_FollowsOnlyWithoutCellDimmer()
    {
        var bar = new FixtureType
        {
            Manufacturer = "Test",
            Model = "Barre",
            Channels =
            [
                Samples.Channel("r1", AttributeKind.Red) with { Cell = 1 },
                Samples.Channel("r2", AttributeKind.Red) with { Cell = 2 },
                Samples.Channel("dim2", AttributeKind.CellIntensity) with { Cell = 2 },
            ],
            Modes = [Samples.Mode("m", "x", "r1", "r2", "dim2")],
        };

        FixtureRules.FollowsIntensity(bar, bar.Modes[0], bar.Channel("r1")!).ShouldBeTrue();
        FixtureRules.FollowsIntensity(bar, bar.Modes[0], bar.Channel("r2")!).ShouldBeFalse();
        FixtureRules.CellCount(bar, bar.Modes[0]).ShouldBe(2);
    }

    [Theory]
    [InlineData(AttributeKind.Shutter, SafetyTags.Strobe)]
    [InlineData(AttributeKind.Smoke, SafetyTags.Smoke)]
    [InlineData(AttributeKind.Pan, SafetyTags.Movement)]
    [InlineData(AttributeKind.Red, SafetyTags.None)]
    [Trait("Exigence", "BIB-007")]
    public void SafetyTags_AreDeducedFromAttribute(AttributeKind attribute, SafetyTags expected) =>
        FixtureRules.Safety(Samples.Channel("x", attribute)).ShouldBe(expected);

    [Fact]
    [Trait("Exigence", "BIB-007")]
    public void ProgramChannel_WithStrobeRange_IsTaggedStrobe()
    {
        var program = Samples.Channel("prog", AttributeKind.Program) with
        {
            Capabilities =
            [
                new Capability { Min = 0, Max = 127, Label = "Programmes" },
                new Capability { Min = 128, Max = 255, Label = "Strobe", Strobe = StrobeEffect.Strobe },
            ],
        };

        FixtureRules.Safety(program).ShouldBe(SafetyTags.Strobe);
        FixtureRules.Safety(program with { Safety = SafetyTags.None }).ShouldBe(SafetyTags.None);
    }

    [Fact]
    [Trait("Exigence", "BIB-005")]
    [Trait("Exigence", "BIB-026")]
    public void SettingSheet_ShowsChannelCountAndDeviceSetting()
    {
        var par = Samples.Par;

        FixtureRules.SettingSheet(par, par.Modes[1], 8)
            .ShouldBe("Test PAR RGB : mode « 7 canaux » (7 canaux) – régler l'appareil sur A001 – adresse 8 à 14");
    }

    [Fact]
    public void Capability_Median_AndLookup()
    {
        var strobe = Samples.Par.Channel("strobe")!;

        strobe.CapabilityAt(100)!.Label.ShouldBe("Strobe lent → rapide");
        strobe.Capabilities[2].Median.ShouldBe(126);
        strobe.Capabilities[0].Median.ShouldBe(5);
    }
}
