using Avalonia;
using Luxia.UI.Controls;

namespace Luxia.UI.Tests.Controls;

public sealed class ColorPickerLayoutTests
{
    private static readonly ColorPickerLayout Layout = ColorPickerLayout.For(new Size(300, 230));

    [Fact]
    [Trait("Exigence", "ERG-004")]
    public void For_Size_SquareBarAndSwatchesDoNotOverlap()
    {
        Layout.Square.Right.ShouldBeLessThan(Layout.Bar.Left);
        Layout.Square.Bottom.ShouldBeLessThan(Layout.SwatchRow.Top);
        Layout.Bar.Right.ShouldBe(300);
        Layout.SwatchRow.Bottom.ShouldBe(230);
    }

    [Fact]
    [Trait("Exigence", "ERG-004")]
    public void HueSaturationAt_Corners_GivesHueAcrossAndSaturationDown()
    {
        Layout.HueSaturationAt(Layout.Square.TopLeft).ShouldBe((0, 1));
        var (hue, saturation) = Layout.HueSaturationAt(Layout.Square.BottomRight);
        hue.ShouldBe(360);
        saturation.ShouldBe(0);
        Layout.HueSaturationAt(new Point(-50, 9999)).ShouldBe((0, 0));
    }

    [Fact]
    [Trait("Exigence", "ERG-004")]
    public void SquarePoint_IsInverseOfHueSaturationAt()
    {
        var color = new LightColor(200, 0.25, 0.8);
        var (hue, saturation) = Layout.HueSaturationAt(Layout.SquarePoint(color));
        hue.ShouldBe(200, 1e-9);
        saturation.ShouldBe(0.25, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "ERG-004")]
    public void BrightnessAt_BarTopAndBottom_IsFullAndBlack()
    {
        Layout.BrightnessAt(Layout.Bar.TopLeft).ShouldBe(1);
        Layout.BrightnessAt(Layout.Bar.BottomLeft).ShouldBe(0);
        Layout.BrightnessAt(new Point(0, Layout.BarY(0.3))).ShouldBe(0.3, 1e-9);
    }
}
