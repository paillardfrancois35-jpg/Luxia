using Luxia.Fixtures.Import;
using Luxia.Fixtures.Model;
using Luxia.Fixtures.Rules;

namespace Luxia.Fixtures.Tests;

/// <summary>T-BIB-02 (OFL) : 5 fichiers de référence → modèles valides, conformes au résultat attendu.</summary>
public sealed class OflImporterTests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "assets", "ofl");

    [Theory]
    [InlineData("generique/rgb-par-7ch.json")]
    [InlineData("acme/spot-60.json")]
    [InlineData("acme/pixel-bar-8.json")]
    [InlineData("acme/fog-1000.json")]
    [InlineData("acme/strobe-750.json")]
    [Trait("Exigence", "BIB-080")]
    public void ReferenceFiles_ImportToValidModels(string file)
    {
        var result = OflImporter.ImportFile(Path.Combine(Folder, file));

        result.Succeeded.ShouldBeTrue(result.Error);
        FixtureValidator.Validate(result.Fixture!).Where(i => i.Severity == IssueSeverity.Error).ShouldBeEmpty();
        result.Fixture!.Source.ShouldBe(FixtureSource.Ofl);
    }

    [Fact]
    [Trait("Exigence", "BIB-080")]
    public void RgbPar_ModesChannelsAndStrobeRanges()
    {
        var par = Import("generique/rgb-par-7ch.json");

        par.Manufacturer.ShouldBe("Generique");
        par.Category.ShouldBe(FixtureCategory.Par);
        par.Modes.Select(m => m.ChannelCount).ShouldBe([3, 7]);
        Attributes(par, par.Modes[1]).ShouldBe([AttributeKind.Intensity, AttributeKind.Red, AttributeKind.Green, AttributeKind.Blue, AttributeKind.Shutter, AttributeKind.Program, AttributeKind.ProgramSpeed]);

        var strobe = par.Channel("strobe")!;
        strobe.Capabilities.Select(c => c.Strobe).ShouldBe([StrobeEffect.Closed, StrobeEffect.Open, StrobeEffect.Strobe, StrobeEffect.Random]);
        strobe.Capabilities[2].Parameter.ShouldBe(new ProgressiveParameter("vitesse", 1, 20, "Hz"));
        FixtureRules.HasVirtualIntensity(par, par.Modes[0]).ShouldBeTrue();
        par.Physical.BeamAngle.ShouldBe(25);
    }

    [Fact]
    [Trait("Exigence", "BIB-080")]
    [Trait("Exigence", "BIB-003")]
    [Trait("Exigence", "BIB-008")]
    public void Spot_FineChannelsAreOneAttribute_AndWheelsCarryColors()
    {
        var spot = Import("acme/spot-60.json");
        var mode11 = spot.Modes[1];

        spot.Channel("pan")!.Resolution.ShouldBe(ChannelResolution.Bit16);
        mode11.Channels[0].ShouldBe(new ModeChannel("pan"));
        mode11.Channels[1].ShouldBe(new ModeChannel("pan", ChannelPart.Fine));
        spot.Modes[0].Channels.ShouldNotContain(c => c.Part == ChannelPart.Fine);
        spot.Channels.ShouldNotContain(c => c.Name == "Pan fine");
        spot.Channel("pan")!.Default.ShouldBe(128);

        spot.Wheels.Select(w => w.Kind).ShouldBe([WheelKind.Color, WheelKind.Gobo]);
        var colorWheel = spot.Channel("color-wheel")!;
        colorWheel.Attribute.ShouldBe(AttributeKind.ColorWheel);
        colorWheel.Capabilities[1].Colors.ShouldBe(["#ff0000"]);
        colorWheel.Capabilities[1].Kind.ShouldBe(CapabilityKind.WheelSlot);
        spot.Channel("gobo-wheel")!.Attribute.ShouldBe(AttributeKind.Gobo);
        spot.Channel("control")!.Attribute.ShouldBe(AttributeKind.Reset);
        spot.Physical.PanRange.ShouldBe(540);
        mode11.Channels[^1].Channel.ShouldStartWith("inutilise");
    }

    [Fact]
    [Trait("Exigence", "BIB-080")]
    public void PixelBar_MatrixBecomesEightCells()
    {
        var bar = Import("acme/pixel-bar-8.json");

        bar.Category.ShouldBe(FixtureCategory.LedBar);
        bar.Modes[0].ChannelCount.ShouldBe(24);
        FixtureRules.CellCount(bar, bar.Modes[0]).ShouldBe(8);
        bar.Channel(bar.Modes[0].Channels[3].Channel)!.ShouldSatisfyAllConditions(
            c => c.Attribute.ShouldBe(AttributeKind.Red),
            c => c.Cell.ShouldBe(2));
        FixtureRules.FollowsIntensity(bar, bar.Modes[0], bar.Channel(bar.Modes[0].Channels[0].Channel)!).ShouldBeTrue();
        FixtureRules.FollowsIntensity(bar, bar.Modes[1], bar.Channel(bar.Modes[1].Channels[1].Channel)!).ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "BIB-080")]
    [Trait("Exigence", "BIB-007")]
    public void Fog_IsSmokeTagged()
    {
        var fog = Import("acme/fog-1000.json");

        fog.Category.ShouldBe(FixtureCategory.Smoke);
        FixtureRules.Safety(fog.Channels.Single()).ShouldBe(SafetyTags.Smoke);
    }

    [Fact]
    [Trait("Exigence", "BIB-082")]
    public void UnknownCapability_BecomesGeneric_AndIsReported()
    {
        var result = OflImporter.ImportFile(Path.Combine(Folder, "acme", "strobe-750.json"));

        result.Fixture!.Channel("blades")!.Attribute.ShouldBe(AttributeKind.Generic);
        result.Notes.ShouldContain(n => n.Contains("BladeInsertion", StringComparison.Ordinal));
        result.Fixture.Channel("speed")!.Attribute.ShouldBe(AttributeKind.Shutter);
    }

    [Fact]
    [Trait("Exigence", "BIB-082")]
    public void BrokenFile_GivesErrorWithoutThrowing()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "{ nope");

        var result = OflImporter.ImportFile(path);

        result.Succeeded.ShouldBeFalse();
        result.Error!.ShouldContain("illisible");
        File.Delete(path);
    }

    private static FixtureType Import(string file) => OflImporter.ImportFile(Path.Combine(Folder, file)).Fixture!;

    private static List<AttributeKind> Attributes(FixtureType fixture, FixtureMode mode) =>
        [.. mode.Channels.Select(c => fixture.Channel(c.Channel)!.Attribute)];
}
