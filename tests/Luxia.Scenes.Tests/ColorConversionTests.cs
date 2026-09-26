using Luxia.Fixtures.Model;
using Luxia.Scenes.Model;
using Luxia.Scenes.Rules;

namespace Luxia.Scenes.Tests;

/// <summary>T-MOT-03 / T-PAL-01 : couleur logique traduite sur RVB, RVBW, roue, UV, sur les appareils réels du parc.</summary>
public sealed class ColorConversionTests
{
    private readonly ReferenceProject _project = new();

    [Fact]
    [Trait("Exigence", "MOT-050")]
    [Trait("Exigence", "GEN-022")]
    public void Rgb_Par_TakesColorDirectly()
    {
        var levels = Levels("PAR 1", new LogicalColor { R = 1, G = 0.5 });

        levels["r"].ShouldBe(1);
        levels["g"].ShouldBe(0.5);
        levels["b"].ShouldBe(0);
        levels.ShouldNotContainKey("dim");
    }

    [Fact]
    [Trait("Exigence", "MOT-051")]
    public void Rgbw_Par_WhiteLogical_GoesToWhiteEmitter_ByDefault()
    {
        var levels = Levels("Gros PAR 1", new LogicalColor { R = 1, G = 1, B = 1 });

        levels["w"].ShouldBe(1);
        new[] { levels["r"], levels["g"], levels["b"] }.ShouldAllBe(v => v == 0);
    }

    [Theory]
    [InlineData(WhiteMode.Extract, 0.6, 0.0, 0.4)]
    [InlineData(WhiteMode.Off, 1.0, 0.4, 0.0)]
    [InlineData(WhiteMode.Boost, 1.0, 0.4, 0.4)]
    [Trait("Exigence", "MOT-051")]
    public void Rgbw_WhiteModes(WhiteMode mode, double red, double blue, double white)
    {
        var fixture = _project.Fixture("Gros PAR 1");
        var type = _project.Type("Gros PAR 1") with { WhiteMode = mode };
        var modeDef = type.Modes.Single(m => m.Name == fixture.ModeName);

        var levels = ColorConversion.Translate(type, modeDef, 0, new LogicalColor { R = 1, G = 0.6, B = 0.4 })
            .ToDictionary(x => x.Channel.Key, x => x.Level);

        levels["r"].ShouldBe(red, 1e-9);
        levels["b"].ShouldBe(blue, 1e-9);
        levels["w"].ShouldBe(white, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MOT-052")]
    public void ColorWheel_Red_PicksRedSlot_AtItsMedian()
    {
        var levels = Levels("Lyre 1", new LogicalColor { R = 1 });

        // Emplacement « Rouge » 8-15 → médiane 12.
        (levels["color"] * 255).ShouldBe(12, 1e-9);
    }

    [Theory]
    [InlineData(1.0, 1.0, 1.0)]
    [InlineData(1.0, 0.78, 0.45)]
    [InlineData(1.0, 1.0, 0.0)]
    [InlineData(0.0, 0.5, 1.0)]
    [Trait("Exigence", "MOT-052")]
    public void ColorWheel_NeverPicksHalfColors(double r, double g, double b)
    {
        var type = _project.Type("Lyre 1");
        var wheel = type.Channel("color")!;

        var slot = ColorConversion.NearestSlot(type, wheel, new LogicalColor { R = r, G = g, B = b })!;

        slot.Label.ShouldNotStartWith("Partagé");
    }

    [Fact]
    [Trait("Exigence", "MOT-052")]
    public void ColorWheel_WhiteLogical_PicksOpenPosition()
    {
        var levels = Levels("Lyre 1", new LogicalColor { R = 1, G = 1, B = 1 });

        (levels["color"] * 255).ShouldBe(4, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "MOT-053")]
    public void Uv_Fixture_IgnoresColor_UnlessUvIsSpecified()
    {
        Levels("UV 1", new LogicalColor { R = 1 }).ShouldBeEmpty();

        var uv = Levels("UV 1", new LogicalColor { B = 1, Uv = 1 });
        new[] { uv["uv1"], uv["uv2"], uv["uv3"], uv["uv4"] }.ShouldAllBe(v => v == 1);
    }

    [Fact]
    [Trait("Exigence", "GEN-022")]
    public void LedBar_24Channels_ColorOnWholeFixture_ReachesEverySection()
    {
        var levels = Levels("Barre 1", new LogicalColor { G = 1 });

        for (var section = 1; section <= 4; section++)
        {
            levels[$"g{section}"].ShouldBe(1);
            levels[$"r{section}"].ShouldBe(0);
        }
    }

    private Dictionary<string, double> Levels(string fixtureName, LogicalColor color)
    {
        var info = _project.Patch.Find(_project.Fixture(fixtureName).Id)!;
        return Compilation.ValueResolver
            .ResolveMember(info, 0, new SceneValue { Target = ValueTarget.Fixture(info.Fixture.Id), Color = color }, null)
            .ToDictionary(x => x.Key, x => x.Level);
    }
}
