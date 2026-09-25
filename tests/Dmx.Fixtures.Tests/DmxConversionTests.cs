using Dmx.Fixtures.Model;
using Dmx.Fixtures.Rules;

namespace Dmx.Fixtures.Tests;

/// <summary>GEN-020 (valeurs normalisées) et GEN-021 (unités d'affichage).</summary>
public sealed class DmxConversionTests
{
    [Fact]
    [Trait("Exigence", "GEN-020")]
    public void Half_Is128In8Bit_And0x8000In16Bit()
    {
        DmxConversion.To8Bit(0.5).ShouldBe((byte)128);
        DmxConversion.To16Bit(0.5).ShouldBe(((byte)0x80, (byte)0x00));
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(1.0, 255)]
    [InlineData(-1.0, 0)]
    [InlineData(2.0, 255)]
    [Trait("Exigence", "GEN-020")]
    public void To8Bit_ClampsAndRounds(double normalized, int expected) =>
        DmxConversion.To8Bit(normalized).ShouldBe((byte)expected);

    [Fact]
    [Trait("Exigence", "GEN-020")]
    public void SixteenBit_KeepsMoreResolutionThan8Bit()
    {
        var (coarse, fine) = DmxConversion.To16Bit(0.001);

        DmxConversion.To8Bit(0.001).ShouldBe((byte)0);
        ((coarse << 8) | fine).ShouldBe(66);
        DmxConversion.From16Bit(0xFF, 0xFF).ShouldBe(1.0);
        DmxConversion.From8Bit(255).ShouldBe(1.0);
    }

    [Fact]
    [Trait("Exigence", "GEN-021")]
    public void Describe_UsesMostMeaningfulUnit()
    {
        var dimmer = new ChannelDefinition { Key = "d", Name = "Dim", Attribute = AttributeKind.Intensity };
        var pan = new ChannelDefinition { Key = "p", Name = "Pan", Attribute = AttributeKind.Pan };
        var generic = new ChannelDefinition { Key = "g", Name = "G", Attribute = AttributeKind.Generic };
        var strobe = Samples.Par.Channel("strobe")!;

        DmxConversion.Describe(dimmer, 128).ShouldBe("128 – 50 %");
        DmxConversion.Describe(pan, 128, new PhysicalInfo { PanRange = 540 }).ShouldBe("128 – 271°");
        DmxConversion.Describe(pan, 128).ShouldBe("128");
        DmxConversion.Describe(generic, 7).ShouldBe("7");
        DmxConversion.Describe(strobe, 30).ShouldBe("30 – Allumé fixe");
    }
}
