using Luxia.Fixtures.Model;

namespace Luxia.Fixtures;

/// <summary>Modèles génériques livrés avec l'application (doc 12 §7), en lecture seule.</summary>
public static class GenericFixtures
{
    /// <summary>Nom de fabricant des génériques.</summary>
    public const string Manufacturer = "Génériques";

    /// <summary>Gradateur 1 canal.</summary>
    public static FixtureType Dimmer { get; } = Build(
        "Gradateur", FixtureCategory.Dimmer, "a3f3b2c4-0001-4d6b-9c1e-000000000001",
        [Channel("dim", "Intensité", AttributeKind.Intensity)],
        [Mode("1 canal", "1CH", "dim")]);

    /// <summary>RGB : 3 canaux (R, G, B) et 4 canaux (intensité + RGB).</summary>
    public static FixtureType Rgb { get; } = Build(
        "RGB", FixtureCategory.Par, "a3f3b2c4-0002-4d6b-9c1e-000000000002",
        [Channel("dim", "Intensité", AttributeKind.Intensity), Channel("r", "Rouge", AttributeKind.Red), Channel("g", "Vert", AttributeKind.Green), Channel("b", "Bleu", AttributeKind.Blue)],
        [Mode("3 canaux", "3CH", "r", "g", "b"), Mode("4 canaux", "4CH", "dim", "r", "g", "b")]);

    /// <summary>RGBW : 4 canaux et 5 canaux (intensité + RGBW).</summary>
    public static FixtureType Rgbw { get; } = Build(
        "RGBW", FixtureCategory.Par, "a3f3b2c4-0003-4d6b-9c1e-000000000003",
        [Channel("dim", "Intensité", AttributeKind.Intensity), Channel("r", "Rouge", AttributeKind.Red), Channel("g", "Vert", AttributeKind.Green), Channel("b", "Bleu", AttributeKind.Blue), Channel("w", "Blanc", AttributeKind.White)],
        [Mode("4 canaux", "4CH", "r", "g", "b", "w"), Mode("5 canaux", "5CH", "dim", "r", "g", "b", "w")]);

    /// <summary>Machine à fumée 1 canal : 0 = arrêt, 1-255 = émission.</summary>
    public static FixtureType Smoke { get; } = Build(
        "Machine à fumée", FixtureCategory.Smoke, "a3f3b2c4-0004-4d6b-9c1e-000000000004",
        [Channel("smoke", "Fumée", AttributeKind.Smoke) with
        {
            Capabilities =
            [
                new Capability { Min = 0, Max = 0, Kind = CapabilityKind.Closed, Label = "Arrêt" },
                new Capability { Min = 1, Max = 255, Kind = CapabilityKind.Progressive, Label = "Émission", Parameter = new ProgressiveParameter("débit", 0, 100, "%") },
            ],
        }],
        [Mode("1 canal", "1CH", "smoke")]);

    /// <summary>Stroboscope simple 2 canaux : intensité, vitesse.</summary>
    public static FixtureType Strobe { get; } = Build(
        "Stroboscope simple", FixtureCategory.Strobe, "a3f3b2c4-0005-4d6b-9c1e-000000000005",
        [Channel("dim", "Intensité", AttributeKind.Intensity), Channel("speed", "Vitesse", AttributeKind.Shutter) with
        {
            Capabilities =
            [
                new Capability { Min = 0, Max = 9, Kind = CapabilityKind.Closed, Label = "Arrêt", Strobe = StrobeEffect.Closed },
                new Capability { Min = 10, Max = 255, Kind = CapabilityKind.Progressive, Label = "Strobe lent → rapide", Strobe = StrobeEffect.Strobe, Parameter = new ProgressiveParameter("fréquence", 1, 20, "Hz") },
            ],
        }],
        [Mode("2 canaux", "2CH", "dim", "speed")]);

    /// <summary>Canal générique 1 canal.</summary>
    public static FixtureType GenericChannel { get; } = Build(
        "Canal générique", FixtureCategory.Other, "a3f3b2c4-0006-4d6b-9c1e-000000000006",
        [Channel("ch", "Canal", AttributeKind.Generic)],
        [Mode("1 canal", "1CH", "ch")]);

    /// <summary>Tous les génériques.</summary>
    public static IReadOnlyList<FixtureType> All { get; } = [Dimmer, Rgb, Rgbw, Smoke, Strobe, GenericChannel];

    private static FixtureType Build(string model, FixtureCategory category, string id, ChannelDefinition[] channels, FixtureMode[] modes) => new()
    {
        Id = Guid.Parse(id),
        Manufacturer = Manufacturer,
        Model = model,
        Category = category,
        Source = FixtureSource.Generic,
        Author = "LuXia",
        Channels = channels,
        Modes = modes,
    };

    private static ChannelDefinition Channel(string key, string name, AttributeKind attribute) =>
        new() { Key = key, Name = name, Attribute = attribute };

    private static FixtureMode Mode(string name, string shortName, params string[] channels) =>
        new() { Name = name, ShortName = shortName, Channels = [.. channels.Select(c => new ModeChannel(c))] };
}
