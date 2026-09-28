using Avalonia;
using Luxia.UI.Controls;

namespace Luxia.UI.Tests.Controls;

public sealed class PlanAndStepStripTests
{
    [Fact]
    [Trait("Exigence", "ERG-014")]
    [Trait("Exigence", "SIM-010")]
    public void FixturesIn_Rectangle_KeepsOnlyFixturesInside()
    {
        var a = Visual(1.5, 5.5);
        var b = Visual(4.3, 2.3);
        var c = Visual(7.2, 2.3);

        SimulatorCanvas.FixturesIn([a, b, c], new Rect(4, 2, 4, 1)).ShouldBe([b.Id, c.Id]);
        SimulatorCanvas.FixturesIn([a, b, c], new Rect(0, 0, 1, 1)).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "ERG-015")]
    public void Widths_AreProportionalToDurations_AndFill()
    {
        var widths = StepStrip.Widths([1, 1, 2], 409, 10);

        widths[2].ShouldBe(widths[0] * 2, 1e-9);
        (widths.Sum() + (3 * 2)).ShouldBe(409, 1e-9, "les cases et les deux espaces de 3 px remplissent la place");
    }

    [Fact]
    [Trait("Exigence", "ERG-015")]
    public void Widths_ShortOrZeroStep_KeepsMinimumWidth()
    {
        var widths = StepStrip.Widths([0, 10, 10], 500, 56);

        widths[0].ShouldBe(56);
        widths[1].ShouldBe(widths[2], 1e-9);
        (widths.Sum() + 6).ShouldBe(500, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "ERG-015")]
    public void Widths_TooNarrow_EveryCellAtMinimum()
    {
        StepStrip.Widths([1, 5, 9], 100, 56).ShouldAllBe(w => w == 56);
        StepStrip.Widths([], 100).ShouldBeEmpty();
    }

    private static SimulatorFixtureVisual Visual(double x, double y) =>
        new(Guid.NewGuid(), "A", x, y, 0, false, null, [], false, false);
}
