using Luxia.UI.Controls;

namespace Luxia.UI.Tests.Controls;

public sealed class HsvColorTests
{
    [Theory]
    [Trait("Exigence", "ERG-004")]
    [InlineData(0, 1, 1, 255, 0, 0)]
    [InlineData(120, 1, 1, 0, 255, 0)]
    [InlineData(240, 1, 1, 0, 0, 255)]
    [InlineData(60, 1, 1, 255, 255, 0)]
    [InlineData(30, 1, 1, 255, 128, 0)]
    [InlineData(0, 0, 1, 255, 255, 255)]
    [InlineData(200, 0.5, 0, 0, 0, 0)]
    [InlineData(0, 0, 0.5, 128, 128, 128)]
    public void ToRgb_KnownColors_MatchesExpected(double hue, double saturation, double brightness, int r, int g, int b)
    {
        new LightColor(hue, saturation, brightness).ToRgb().ShouldBe(((byte)r, (byte)g, (byte)b));
    }

    [Fact]
    [Trait("Exigence", "ERG-004")]
    public void FromRgb_ThenToRgb_RoundTripsEveryStep()
    {
        for (var r = 0; r <= 255; r += 17)
        {
            for (var g = 0; g <= 255; g += 17)
            {
                for (var b = 0; b <= 255; b += 17)
                {
                    LightColor.FromRgb((byte)r, (byte)g, (byte)b).ToRgb().ShouldBe(((byte)r, (byte)g, (byte)b));
                }
            }
        }
    }

    [Fact]
    [Trait("Exigence", "ERG-004")]
    public void Normalized_OutOfRange_WrapsHueAndClampsTheRest()
    {
        new LightColor(-30, 1.5, -0.2).Normalized().ShouldBe(new LightColor(330, 1, 0));
        new LightColor(720, 0.5, 0.5).Normalized().Hue.ShouldBe(0);
    }
}
