using Dmx.Fixtures.Model;
using Dmx.Fixtures.Rules;

namespace Dmx.Fixtures.Tests;

/// <summary>T-BIB-01 : chaque règle de BIB-004 sur un jeu de modèles invalides.</summary>
public sealed class FixtureValidatorTests
{
    [Fact]
    [Trait("Exigence", "BIB-004")]
    public void ValidModels_HaveNoError()
    {
        FixtureValidator.IsValid(Samples.Par).ShouldBeTrue();
        FixtureValidator.IsValid(Samples.MovingHead).ShouldBeTrue();
        GenericFixtures.All.ShouldAllBe(f => FixtureValidator.Validate(f).Count == 0);
    }

    [Fact]
    [Trait("Exigence", "BIB-004")]
    public void OverlappingRanges_AreAnError()
    {
        var fixture = WithStrobeRanges(
            new Capability { Min = 0, Max = 100, Label = "A" },
            new Capability { Min = 100, Max = 255, Label = "B" });

        ShouldHave(fixture, IssueSeverity.Error, "se chevauchent");
    }

    [Fact]
    [Trait("Exigence", "BIB-004")]
    public void GapBetweenRanges_IsAWarning()
    {
        var fixture = WithStrobeRanges(
            new Capability { Min = 0, Max = 99, Label = "A" },
            new Capability { Min = 120, Max = 255, Label = "B" });

        ShouldHave(fixture, IssueSeverity.Warning, "trou entre 99 et 120");
        FixtureValidator.IsValid(fixture).ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "BIB-004")]
    public void OrphanFineChannel_IsAnError()
    {
        var head = Samples.MovingHead;
        var mode = head.Modes[0] with { Channels = [.. head.Modes[0].Channels.Where(c => !(c.Channel == "pan" && c.Part == ChannelPart.Coarse))] };

        ShouldHave(head with { Modes = [mode] }, IssueSeverity.Error, "canal fin orphelin");
    }

    [Fact]
    [Trait("Exigence", "BIB-004")]
    public void FineOf8BitChannel_IsAnError()
    {
        var head = Samples.MovingHead;
        var mode = head.Modes[0] with { Channels = [.. head.Modes[0].Channels, new ModeChannel("dim", ChannelPart.Fine)] };

        ShouldHave(head with { Modes = [mode] }, IssueSeverity.Error, "octet fin d'un canal 8 bits");
    }

    [Fact]
    [Trait("Exigence", "BIB-004")]
    public void ModeWithoutChannel_IsAnError() =>
        ShouldHave(Samples.Par with { Modes = [new FixtureMode { Name = "vide" }] }, IssueSeverity.Error, "aucun canal");

    [Fact]
    [Trait("Exigence", "BIB-002")]
    public void FixtureWithoutMode_IsAnError() =>
        ShouldHave(Samples.Par with { Modes = [] }, IssueSeverity.Error, "au moins un mode");

    [Fact]
    [Trait("Exigence", "BIB-004")]
    public void SameChannelTwiceInMode_IsAnError()
    {
        var par = Samples.Par;

        ShouldHave(par with { Modes = [Samples.Mode("m", "x", "r", "g", "r")] }, IssueSeverity.Error, "plusieurs positions");
    }

    [Fact]
    public void UnknownChannelInMode_IsAnError() =>
        ShouldHave(Samples.Par with { Modes = [Samples.Mode("m", "x", "r", "inconnu")] }, IssueSeverity.Error, "inconnu");

    [Fact]
    [Trait("Exigence", "BIB-003")]
    public void CoarseWithoutFine_IsAWarning()
    {
        var head = Samples.MovingHead;
        var mode = head.Modes[0] with { Channels = [.. head.Modes[0].Channels.Where(c => c.Part == ChannelPart.Coarse)] };

        ShouldHave(head with { Modes = [mode] }, IssueSeverity.Warning, "seul l'octet grossier");
    }

    private static FixtureType WithStrobeRanges(params Capability[] ranges)
    {
        var par = Samples.Par;
        return par with { Channels = [.. par.Channels.Select(c => c.Key == "strobe" ? c with { Capabilities = ranges } : c)] };
    }

    private static void ShouldHave(FixtureType fixture, IssueSeverity severity, string text) =>
        FixtureValidator.Validate(fixture).ShouldContain(i => i.Severity == severity && i.Message.Contains(text, StringComparison.Ordinal));
}
