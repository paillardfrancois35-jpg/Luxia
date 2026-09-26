using Luxia.Fixtures;
using Luxia.Fixtures.Model;
using Luxia.Patch.Rules;

namespace Luxia.Patch.Tests;

public sealed class IdentifyRulesTests
{
    [Fact]
    [Trait("Exigence", "CMD-023")]
    public void IdentifyChannels_LightsIntensityAndColorEmitters_NotPosition()
    {
        // Un gradateur seul à 255 ne rend rien visible si RVB sont à 0 : il faut aussi pousser les émetteurs
        // de couleur (retour utilisateur du 2026-09-26, PAR réel qui ne s'allumait pas). Toujours pas de
        // changement de position (CONS-024).
        var fixture = GenericFixtures.Rgbw;
        var mode = fixture.Modes.Single(m => m.Name == "5 canaux");

        var channels = IdentifyRules.IdentifyChannels(fixture, mode, address: 10);

        channels.ShouldBe([(10, (byte)255), (11, (byte)255), (12, (byte)255), (13, (byte)255), (14, (byte)255)]);
    }

    [Fact]
    [Trait("Exigence", "CMD-023")]
    public void IdentifyChannels_UsesExplicitIdentifyValue_WhenDefined()
    {
        var fixture = new FixtureType
        {
            Manufacturer = "Test",
            Model = "Effet",
            Channels =
            [
                new ChannelDefinition { Key = "prog", Name = "Programme", Attribute = AttributeKind.Program, Identify = 200 },
                new ChannelDefinition { Key = "pan", Name = "Pan", Attribute = AttributeKind.Pan },
            ],
            Modes = [new FixtureMode { Name = "2CH", Channels = [new ModeChannel("prog"), new ModeChannel("pan")] }],
        };

        var channels = IdentifyRules.IdentifyChannels(fixture, fixture.Modes[0], address: 1);

        // Le canal Pan n'a ni valeur d'identification, ni famille intensité, ni émetteur de couleur : il ne bouge pas.
        channels.ShouldBe([(1, (byte)200)]);
    }

    [Fact]
    [Trait("Exigence", "CMD-023")]
    public void IdentifyChannels_SkipsFineByteOf16BitChannels()
    {
        var fixture = new FixtureType
        {
            Manufacturer = "Test",
            Model = "Gradateur 16 bits",
            Channels = [new ChannelDefinition { Key = "dim", Name = "Intensité", Attribute = AttributeKind.Intensity, Resolution = ChannelResolution.Bit16 }],
            Modes = [new FixtureMode { Name = "1CH16", Channels = [new ModeChannel("dim", ChannelPart.Coarse), new ModeChannel("dim", ChannelPart.Fine)] }],
        };

        var channels = IdentifyRules.IdentifyChannels(fixture, fixture.Modes[0], address: 1);

        channels.ShouldBe([(1, (byte)255)]);
    }
}
