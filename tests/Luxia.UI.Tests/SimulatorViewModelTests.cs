using Luxia.Fixtures;
using Luxia.Messaging.Commands;
using Luxia.Patch.Model;
using Luxia.UI.Modules.Simulator;

namespace Luxia.UI.Tests;

/// <summary>Écran Simulateur : décodage des trames vers le plan du lieu actif (doc 14).</summary>
public sealed class SimulatorViewModelTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private SimulatorViewModel _vm = null!;

    public ValueTask InitializeAsync()
    {
        _host.Runtime.Project.Create(_host.ProjectFolder, "Essai");
        _vm = new SimulatorViewModel(_host.Runtime);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    [Trait("Exigence", "SIM-001")]
    public void Refresh_OnlyShowsPlacedAndPresentFixtures()
    {
        var placed = Patch(GenericFixtures.Rgb, "3 canaux", 1, "PAR 1");
        var notPlaced = Patch(GenericFixtures.Rgb, "3 canaux", 10, "PAR 2");
        Place(placed, 1, 2, absent: false);
        // notPlaced n'a jamais de position dans le lieu actif : il n'apparaît pas au plan.

        _vm.Refresh();

        _vm.Fixtures.Select(f => f.Id).ShouldBe([placed.Id]);
    }

    [Fact]
    [Trait("Exigence", "INST-052")]
    public void Refresh_AbsentFixture_IsHidden()
    {
        var fixture = Patch(GenericFixtures.Rgb, "3 canaux", 1, "PAR 1");
        Place(fixture, 1, 2, absent: true);

        _vm.Refresh();

        _vm.Fixtures.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "SIM-003")]
    public void Refresh_DecodesEmittedFrameIntoFixtureColor()
    {
        var fixture = Patch(GenericFixtures.Rgb, "3 canaux", 1, "PAR 1");
        Place(fixture, 1, 2, absent: false);
        _host.Runtime.SetChannels(1, [new ChannelValue(1, 255)]);
        _host.Tick();

        _vm.Refresh();

        _vm.Fixtures.Single().Cells[0].Color.ShouldBe("#FF0000");
    }

    [Fact]
    [Trait("Exigence", "SIM-009")]
    public void Refresh_FixtureTypeMissingFromProjectLibrary_IsFlaggedAsError()
    {
        var missing = new PatchedFixture { FixtureTypeId = Guid.NewGuid(), ModeName = "?", Address = 1, Name = "Inconnu" };
        var installation = _host.Runtime.Project.Installation;
        _host.Runtime.Project.SaveInstallation(installation with { Fixtures = [missing] });
        Place(missing, 3, 3, absent: false);

        _vm.Refresh();

        _vm.Fixtures.Single().HasError.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "SIM-005")]
    public void OnFixtureHovered_DescribesFixture()
    {
        var fixture = Patch(GenericFixtures.Rgb, "3 canaux", 1, "PAR 1");
        Place(fixture, 1, 2, absent: false);

        _vm.OnFixtureHovered(fixture.Id);

        _vm.HoverText.ShouldContain("PAR 1");
        _vm.HoverText.ShouldContain("adresse 1");
    }

    private PatchedFixture Patch(Luxia.Fixtures.Model.FixtureType type, string modeName, int address, string name)
    {
        _host.Runtime.Project.FixtureLibrary!.EnsureCopied(type);
        var fixture = new PatchedFixture { FixtureTypeId = type.Id, ModeName = modeName, Address = address, Name = name };
        var installation = _host.Runtime.Project.Installation;
        _host.Runtime.Project.SaveInstallation(installation with { Fixtures = [.. installation.Fixtures, fixture] });
        return fixture;
    }

    private void Place(PatchedFixture fixture, double x, double y, bool absent)
    {
        var venues = _host.Runtime.Project.Venues;
        var active = venues.Active;
        var placement = new FixturePlacement { FixtureId = fixture.Id, X = x, Y = y, Absent = absent };
        var updated = active with { Placements = [.. active.Placements.Where(p => p.FixtureId != fixture.Id), placement] };
        _host.Runtime.Project.SaveVenues(venues with { Venues = [.. venues.Venues.Select(v => v.Id == updated.Id ? updated : v)] });
    }
}
