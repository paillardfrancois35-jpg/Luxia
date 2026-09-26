using Dmx.Fixtures;
using Dmx.Fixtures.Model;
using Dmx.Patch.Model;
using Dmx.Patch.Rules;

namespace Dmx.Patch.Tests;

public sealed class PatchLookupTests
{
    private static readonly PatchedFixture Par = new()
    {
        FixtureTypeId = GenericFixtures.Rgb.Id,
        ModeName = "3 canaux",
        Address = 8,
        Name = "PAR 1",
    };

    private static FixtureType? TypeOf(PatchedFixture fixture) =>
        fixture.FixtureTypeId == GenericFixtures.Rgb.Id ? GenericFixtures.Rgb : null;

    [Fact]
    [Trait("Exigence", "CONS-007")]
    public void FindChannel_ResolvesChannelWithinFixtureRange()
    {
        var info = PatchLookup.FindChannel([Par], TypeOf, universe: 1, channel: 9);

        info.ShouldNotBeNull();
        info.Fixture.ShouldBe(Par);
        info.Channel.Key.ShouldBe("g");
    }

    [Fact]
    [Trait("Exigence", "CONS-007")]
    public void FindChannel_OutsideRange_ReturnsNull()
    {
        PatchLookup.FindChannel([Par], TypeOf, universe: 1, channel: 20).ShouldBeNull();
        PatchLookup.FindChannel([Par], TypeOf, universe: 1, channel: 7).ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "CONS-007")]
    public void FindChannel_DifferentUniverse_ReturnsNull() =>
        PatchLookup.FindChannel([Par], TypeOf, universe: 2, channel: 8).ShouldBeNull();

    [Fact]
    [Trait("Exigence", "CONS-043")]
    public void FixtureRanges_ReturnsOneRangePerFixture()
    {
        var second = Par with { Id = Guid.NewGuid(), Address = 20, Name = "PAR 2" };

        var ranges = PatchLookup.FixtureRanges([Par, second], TypeOf, universe: 1);

        ranges.ShouldBe([(8, 10), (20, 22)]);
    }
}
