using Dmx.Fixtures;
using Dmx.Fixtures.Model;
using Dmx.Patch.Model;
using Dmx.Patch.Rules;

namespace Dmx.Patch.Tests;

public sealed class FixtureDecoderTests
{
    [Fact]
    [Trait("Exigence", "SIM-003")]
    public void Decode_RgbChannels_MixIntoDisplayColor()
    {
        var type = GenericFixtures.Rgbw;
        var mode = type.Modes.Single(m => m.Name == "5 canaux"); // dim, r, g, b, w
        var fixture = Fixture(type, mode, address: 1);
        var frame = new byte[512];
        frame[0] = 255; // dim
        frame[1] = 255; // r
        frame[2] = 0; // g
        frame[3] = 0; // b

        var decoded = FixtureDecoder.Decode(type, mode, fixture, frame);

        var (color, intensity) = decoded.Overall();
        intensity.ShouldBe(1.0);
        color.R.ShouldBe(1.0, 0.01);
        color.G.ShouldBe(0.0, 0.01);
    }

    [Fact]
    [Trait("Exigence", "SIM-003")]
    [Trait("Exigence", "BIB-006")]
    public void Decode_NoDimmerChannel_UsesVirtualIntensity()
    {
        var type = GenericFixtures.Rgb;
        var mode = type.Modes.Single(m => m.Name == "3 canaux"); // r, g, b only
        var fixture = Fixture(type, mode, address: 1);
        var frame = new byte[512];
        frame[0] = 200; // r

        var decoded = FixtureDecoder.Decode(type, mode, fixture, frame);

        decoded.Overall().Intensity.ShouldBe(1.0);
    }

    [Fact]
    [Trait("Exigence", "SIM-003")]
    public void Decode_AllChannelsAtZero_IsDarkAndOff()
    {
        var type = GenericFixtures.Rgbw;
        var mode = type.Modes.Single(m => m.Name == "5 canaux");
        var fixture = Fixture(type, mode, address: 1);

        var decoded = FixtureDecoder.Decode(type, mode, fixture, new byte[512]);

        decoded.Overall().Intensity.ShouldBe(0.0);
    }

    [Fact]
    [Trait("Exigence", "SIM-004")]
    public void Decode_PanTilt_ComputesAngleFromAmplitude()
    {
        var type = MovingHead();
        var mode = type.Modes[0];
        var fixture = Fixture(type, mode, address: 1);
        var frame = new byte[512];
        frame[0] = 255; // pan full (540°)
        frame[1] = 0; // tilt at 0

        var decoded = FixtureDecoder.Decode(type, mode, fixture, frame);

        decoded.PanDegrees!.Value.ShouldBe(540, 1);
        decoded.TiltDegrees!.Value.ShouldBe(0, 1);
    }

    [Fact]
    [Trait("Exigence", "SIM-004")]
    [Trait("Exigence", "INST-021")]
    public void Decode_InvertedPan_ReversesDirection()
    {
        var type = MovingHead();
        var mode = type.Modes[0];
        var fixture = Fixture(type, mode, address: 1) with { Options = new FixtureOptions { InvertPan = true } };
        var frame = new byte[512];
        frame[0] = 255; // pan raw full

        var decoded = FixtureDecoder.Decode(type, mode, fixture, frame);

        decoded.PanDegrees!.Value.ShouldBe(0, 1);
    }

    [Fact]
    [Trait("Exigence", "SIM-012")]
    public void Decode_StrobeCapability_IsFlaggedAsStrobing()
    {
        var type = GenericFixtures.Strobe;
        var mode = type.Modes.Single(m => m.Name == "2 canaux");
        var fixture = Fixture(type, mode, address: 1);
        var frame = new byte[512];
        frame[0] = 255; // dim
        frame[1] = 100; // speed, dans la plage "strobe" (10-255)

        var decoded = FixtureDecoder.Decode(type, mode, fixture, frame);

        decoded.Cells.Single().Strobing.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "SIM-003")]
    public void Decode_ColorWheelCapability_UsesSlotColor()
    {
        var type = new FixtureType
        {
            Manufacturer = "Test",
            Model = "Lyre",
            Channels =
            [
                new ChannelDefinition
                {
                    Key = "wheel", Name = "Roue", Attribute = AttributeKind.ColorWheel,
                    Capabilities = [new Capability { Min = 0, Max = 20, Kind = CapabilityKind.WheelSlot, Label = "Rouge", Colors = ["#FF0000"] }],
                },
            ],
            Modes = [new FixtureMode { Name = "1CH", Channels = [new ModeChannel("wheel")] }],
        };
        var mode = type.Modes[0];
        var fixture = Fixture(type, mode, address: 1);
        var frame = new byte[512];
        frame[0] = 10;

        var decoded = FixtureDecoder.Decode(type, mode, fixture, frame);

        decoded.Cells.Single().Color.R.ShouldBe(1.0, 0.01);
        decoded.Cells.Single().Intensity.ShouldBe(1.0);
    }

    [Fact]
    [Trait("Exigence", "SIM-003")]
    public void Decode_MultiCellFixture_DecodesEachCellSeparately()
    {
        var type = new FixtureType
        {
            Manufacturer = "Test",
            Model = "Barre",
            Channels =
            [
                new ChannelDefinition { Key = "r1", Name = "Rouge 1", Attribute = AttributeKind.Red, Cell = 1 },
                new ChannelDefinition { Key = "r2", Name = "Rouge 2", Attribute = AttributeKind.Red, Cell = 2 },
            ],
            Modes = [new FixtureMode { Name = "2CH", Channels = [new ModeChannel("r1"), new ModeChannel("r2")] }],
        };
        var mode = type.Modes[0];
        var fixture = Fixture(type, mode, address: 1);
        var frame = new byte[512];
        frame[0] = 255;
        frame[1] = 0;

        var decoded = FixtureDecoder.Decode(type, mode, fixture, frame);

        decoded.Cells.Count.ShouldBe(2);
        decoded.Cells.Single(c => c.Cell == 1).Intensity.ShouldBe(1.0);
        decoded.Cells.Single(c => c.Cell == 2).Intensity.ShouldBe(0.0);
    }

    private static PatchedFixture Fixture(FixtureType type, FixtureMode mode, int address) => new()
    {
        FixtureTypeId = type.Id,
        ModeName = mode.Name,
        Address = address,
        Name = type.Model,
    };

    private static FixtureType MovingHead() => new()
    {
        Manufacturer = "Test",
        Model = "Lyre",
        Category = FixtureCategory.MovingHead,
        Physical = new PhysicalInfo { PanRange = 540, TiltRange = 270 },
        Channels =
        [
            new ChannelDefinition { Key = "pan", Name = "Pan", Attribute = AttributeKind.Pan },
            new ChannelDefinition { Key = "tilt", Name = "Tilt", Attribute = AttributeKind.Tilt },
        ],
        Modes = [new FixtureMode { Name = "2CH", Channels = [new ModeChannel("pan"), new ModeChannel("tilt")] }],
    };
}
