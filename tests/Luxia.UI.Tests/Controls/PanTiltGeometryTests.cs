using Avalonia;
using Luxia.UI.Controls;

namespace Luxia.UI.Tests.Controls;

public sealed class PanTiltGeometryTests
{
    private static readonly Rect Area = new(10, 10, 400, 200);

    [Fact]
    [Trait("Exigence", "ERG-003")]
    public void ToScreen_PanRightTiltUp()
    {
        PanTiltGeometry.ToScreen(0, 0, Area).ShouldBe(Area.BottomLeft);
        PanTiltGeometry.ToScreen(1, 1, Area).ShouldBe(Area.TopRight);
        PanTiltGeometry.FromScreen(Area.Center, Area).ShouldBe((0.5, 0.5));
    }

    [Fact]
    [Trait("Exigence", "ERG-003")]
    public void FromScreen_OutsideGrid_IsClamped()
    {
        PanTiltGeometry.FromScreen(new Point(-100, -100), Area).ShouldBe((0, 1));
        PanTiltGeometry.FromScreen(new Point(1000, 1000), Area).ShouldBe((1, 0));
    }

    [Fact]
    [Trait("Exigence", "ERG-003")]
    public void MoveGroup_NearEdge_KeepsShapeInsteadOfSquashing()
    {
        var group = new[] { new PanTiltTarget("a", 0.2, 0.5), new PanTiltTarget("b", 0.9, 0.6) };

        var moved = PanTiltGeometry.MoveGroup(group, 0.3, 0);

        // Seul 0,1 de marge à droite : tout le groupe avance de 0,1 et garde son écart de 0,7.
        moved[0].Pan.ShouldBe(0.3, 1e-9);
        moved[1].Pan.ShouldBe(1, 1e-9);
        (moved[1].Pan - moved[0].Pan).ShouldBe(0.7, 1e-9);
        moved[0].Tilt.ShouldBe(0.5);
    }

    [Fact]
    [Trait("Exigence", "ERG-003")]
    public void MoveGroupTo_Target_MovesCentroidThere()
    {
        var group = new[] { new PanTiltTarget("a", 0.4, 0.4), new PanTiltTarget("b", 0.6, 0.6) };

        var moved = PanTiltGeometry.MoveGroupTo(group, 0.3, 0.7);

        PanTiltGeometry.Centroid(moved).Pan.ShouldBe(0.3, 1e-9);
        PanTiltGeometry.Centroid(moved).Tilt.ShouldBe(0.7, 1e-9);
        moved.Select(t => t.Id).ShouldBe(["a", "b"]);
    }

    [Fact]
    [Trait("Exigence", "ERG-003")]
    public void MoveGroupTo_SingleFixture_IsAbsolute()
    {
        var moved = PanTiltGeometry.MoveGroupTo([new PanTiltTarget("lyre", 0.5, 0.5)], 0.1, 0.95);

        moved[0].Pan.ShouldBe(0.1, 1e-9);
        moved[0].Tilt.ShouldBe(0.95, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "ERG-003")]
    public void FromCorners_AnyOrder_GivesOrderedRect()
    {
        PanTiltGeometry.FromCorners(0.8, 0.1, 0.2, 0.6).ShouldBe(new PanTiltRect(0.2, 0.8, 0.1, 0.6));
    }

    [Theory]
    [Trait("Exigence", "ERG-003")]
    [InlineData(PanTiltHandle.Left, 0.1, 0.5, 0.1, 0.7, 0.3, 0.6)]
    [InlineData(PanTiltHandle.TopRight, 0.9, 0.9, 0.3, 0.9, 0.3, 0.9)]
    [InlineData(PanTiltHandle.Bottom, 0.5, 0.0, 0.3, 0.7, 0.0, 0.6)]
    [InlineData(PanTiltHandle.Right, 0.0, 0.5, 0.3, 0.32, 0.3, 0.6)]
    public void ResizeZone_Handle_MovesOnlyItsEdgesAndNeverFlips(
        PanTiltHandle handle, double pan, double tilt, double panMin, double panMax, double tiltMin, double tiltMax)
    {
        var zone = new PanTiltRect(0.3, 0.7, 0.3, 0.6);

        var resized = PanTiltGeometry.ResizeZone(zone, handle, pan, tilt);

        resized.PanMin.ShouldBe(panMin, 1e-9);
        resized.PanMax.ShouldBe(panMax, 1e-9);
        resized.TiltMin.ShouldBe(tiltMin, 1e-9);
        resized.TiltMax.ShouldBe(tiltMax, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "ERG-003")]
    public void MoveZone_PastEdge_StopsAtEdgeWithSameSize()
    {
        var moved = PanTiltGeometry.MoveZone(new PanTiltRect(0.6, 0.9, 0.1, 0.3), 0.5, -0.5);

        moved.PanMin.ShouldBe(0.7, 1e-9);
        moved.PanMax.ShouldBe(1, 1e-9);
        moved.TiltMin.ShouldBe(0, 1e-9);
        moved.TiltMax.ShouldBe(0.2, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "ERG-003")]
    public void HitTest_HandlesBodyAndOutside()
    {
        var zone = new PanTiltRect(0.25, 0.75, 0.25, 0.75);
        var screen = PanTiltGeometry.ZoneToScreen(zone, Area);

        PanTiltGeometry.HitTest(zone, Area, screen.TopLeft, 4).ShouldBe(PanTiltHandle.TopLeft);
        PanTiltGeometry.HitTest(zone, Area, new Point(screen.Right + 2, screen.Center.Y), 4).ShouldBe(PanTiltHandle.Right);
        PanTiltGeometry.HitTest(zone, Area, screen.Center, 4).ShouldBe(PanTiltHandle.Body);
        PanTiltGeometry.HitTest(zone, Area, Area.TopLeft, 4).ShouldBe(PanTiltHandle.None);
        PanTiltGeometry.Handles(zone, Area).Count().ShouldBe(8);
    }

    [Fact]
    [Trait("Exigence", "ERG-003")]
    public void ToDegrees_UsesRange()
    {
        PanTiltGeometry.ToDegrees(0.5, 540).ShouldBe(270);
        PanTiltGeometry.ToDegrees(1, 270).ShouldBe(270);
    }
}
