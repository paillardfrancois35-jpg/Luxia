using Luxia.Fixtures.Model;
using Luxia.Fixtures.Rules;

namespace Luxia.Fixtures.Tests;

/// <summary>Définitions des appareils du parc (doc 12 annexe A, doc 41 §11 P2) : chargées et valides.</summary>
public sealed class ParkLibraryTests
{
    private static readonly FixtureLibrary Library = LoadLibrary();

    [Fact]
    [Trait("Exigence", "BIB-001")]
    public void ParkLibrary_LoadsSevenModels_WithoutMessage()
    {
        Library.Messages.ShouldBeEmpty();
        Library.Entries.Count(e => !e.IsBuiltIn).ShouldBe(7);
    }

    [Theory]
    [InlineData("LPC008S")]
    [InlineData("LPC010")]
    [InlineData("LPC120")]
    [InlineData("Mini lyre gobo")]
    [InlineData("BUV463")]
    [InlineData("Effet 4 têtes 150 W")]
    [InlineData("LCB803")]
    [Trait("Exigence", "BIB-004")]
    public void ParkModel_HasNoValidationError(string model)
    {
        var issues = FixtureValidator.Validate(Get(model));

        issues.Where(i => i.Severity == IssueSeverity.Error).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "BIB-005")]
    [Trait("Exigence", "BIB-006")]
    public void Lpc008s_ModesAndIntensityRules()
    {
        var par = Get("LPC008S");

        par.Modes.Select(m => (m.ChannelCount, m.DeviceSetting)).ShouldBe([(3, "d001"), (7, "A001")]);
        FixtureRules.HasVirtualIntensity(par, par.Modes[0]).ShouldBeTrue();
        FixtureRules.HasVirtualIntensity(par, par.Modes[1]).ShouldBeFalse();
        FixtureRules.Safety(par.Channel("strobe")!).ShouldBe(SafetyTags.Strobe);
    }

    [Fact]
    [Trait("Exigence", "BIB-001")]
    [Trait("Exigence", "BIB-003")]
    public void Wzybuta_20And64Channels_12CellsIn64()
    {
        var effect = Get("Effet 4 têtes 150 W");

        effect.Modes.Select(m => m.ChannelCount).ShouldBe([20, 64]);
        effect.Modes[1].Channels[12].ShouldBe(new ModeChannel("r1"));
        effect.Modes[1].Channels[59].ShouldBe(new ModeChannel("w12"));
        effect.Channels.Max(c => c.Cell).ShouldBe(12);
        effect.Channel("reset")!.Attribute.ShouldBe(AttributeKind.Reset);
    }

    [Fact]
    [Trait("Exigence", "BIB-003")]
    public void Lyre_11Channels_Has16BitPanTilt_9ChannelsCoarseOnly()
    {
        var lyre = Get("Mini lyre gobo");

        lyre.Modes[1].ChannelCount.ShouldBe(11);
        lyre.Modes[1].Channels[1].ShouldBe(new ModeChannel("pan", ChannelPart.Fine));
        lyre.Modes[0].ChannelCount.ShouldBe(9);
        lyre.Channel("shutter")!.CapabilityAt(0)!.Strobe.ShouldBe(StrobeEffect.Open);
        lyre.Channel("control")!.Attribute.ShouldBe(AttributeKind.Reset);
        FixtureRules.Safety(lyre.Channel("pan")!).ShouldBe(SafetyTags.Movement);
    }

    [Fact]
    public void Buv463_HasFourUvCells()
    {
        var uv = Get("BUV463");

        FixtureRules.CellCount(uv, uv.Modes[0]).ShouldBe(4);
        FixtureRules.FollowsIntensity(uv, uv.Modes[0], uv.Channel("uv1")!).ShouldBeFalse(); // le maître porte l'intensité
    }

    [Fact]
    [Trait("Exigence", "BIB-001")]
    public void Lcb803_SectionsBecomeCells()
    {
        var bar = Get("LCB803");

        bar.Modes.Select(m => m.ChannelCount).ShouldBe([3, 6, 12, 24, 48]);
        bar.Modes.Select(m => FixtureRules.CellCount(bar, m)).ShouldBe([0, 0, 2, 4, 8]);
        FixtureRules.HasVirtualIntensity(bar, bar.Modes[0]).ShouldBeTrue();
        FixtureRules.FollowsIntensity(bar, bar.Modes[3], bar.Channel("r2")!).ShouldBeFalse(); // la section a son gradateur
    }

    private static FixtureType Get(string model) => Library.Entries.Single(e => e.Fixture.Model == model).Fixture;

    private static FixtureLibrary LoadLibrary()
    {
        var library = new FixtureLibrary(Path.Combine(AppContext.BaseDirectory, "samples", "Bibliothèque"));
        library.Load();
        return library;
    }
}
