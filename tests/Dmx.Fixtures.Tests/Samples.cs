using Dmx.Fixtures.Model;

namespace Dmx.Fixtures.Tests;

/// <summary>Modèles de test construits en code.</summary>
internal static class Samples
{
    /// <summary>PAR RGB façon LPC008S : 3 canaux (R, G, B) et 7 canaux (gradateur, RGB, strobe, fonction, vitesse).</summary>
    public static FixtureType Par => new()
    {
        Manufacturer = "Test",
        Model = "PAR RGB",
        Category = FixtureCategory.Par,
        Channels =
        [
            Channel("dim", AttributeKind.Intensity),
            Channel("r", AttributeKind.Red),
            Channel("g", AttributeKind.Green),
            Channel("b", AttributeKind.Blue),
            Channel("strobe", AttributeKind.Shutter) with
            {
                Capabilities =
                [
                    new Capability { Min = 0, Max = 10, Label = "Éteint", Kind = CapabilityKind.Closed, Strobe = StrobeEffect.Closed },
                    new Capability { Min = 11, Max = 50, Label = "Allumé fixe", Kind = CapabilityKind.Open, Strobe = StrobeEffect.Open },
                    new Capability { Min = 51, Max = 200, Label = "Strobe lent → rapide", Kind = CapabilityKind.Progressive, Strobe = StrobeEffect.Strobe, Parameter = new ProgressiveParameter("fréquence", 1, 20, "Hz") },
                    new Capability { Min = 201, Max = 255, Label = "Strobe aléatoire", Strobe = StrobeEffect.Random },
                ],
            },
            Channel("fn", AttributeKind.Mode),
            Channel("speed", AttributeKind.ProgramSpeed),
        ],
        Modes =
        [
            Mode("3 canaux", "d001", "r", "g", "b"),
            Mode("7 canaux", "A001", "dim", "r", "g", "b", "strobe", "fn", "speed"),
        ],
    };

    /// <summary>Lyre avec Pan / Tilt 16 bits : Pan, Tilt, Pan fin, Tilt fin (octets fins placés séparément).</summary>
    public static FixtureType MovingHead => new()
    {
        Manufacturer = "Test",
        Model = "Lyre",
        Category = FixtureCategory.MovingHead,
        Channels =
        [
            Channel("pan", AttributeKind.Pan) with { Resolution = ChannelResolution.Bit16, Default = 128 },
            Channel("tilt", AttributeKind.Tilt) with { Resolution = ChannelResolution.Bit16, Default = 128 },
            Channel("dim", AttributeKind.Intensity),
        ],
        Modes =
        [
            new FixtureMode
            {
                Name = "5 canaux",
                Channels = [new("pan"), new("tilt"), new("pan", ChannelPart.Fine), new("tilt", ChannelPart.Fine), new("dim")],
            },
        ],
    };

    public static ChannelDefinition Channel(string key, AttributeKind attribute) =>
        new() { Key = key, Name = key, Attribute = attribute };

    public static FixtureMode Mode(string name, string setting, params string[] channels) =>
        new() { Name = name, DeviceSetting = setting, Channels = [.. channels.Select(c => new ModeChannel(c))] };
}
