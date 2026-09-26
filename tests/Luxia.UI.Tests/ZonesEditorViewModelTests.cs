using Luxia.Fixtures.Model;
using Luxia.UI.Modules.Scenes;

namespace Luxia.UI.Tests;

/// <summary>Zones interdites saisies en visant au programmeur (INST-053), appliquées par le moteur (MOT-082).</summary>
public sealed class ZonesEditorViewModelTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private ScenesViewModel _vm = null!;

    public ValueTask InitializeAsync()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");
        foreach (var file in Directory.EnumerateFiles(source, "*.json", SearchOption.AllDirectories))
        {
            var target = Path.Combine(_host.ProjectFolder, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }

        _host.Runtime.Project.Open(_host.ProjectFolder).ShouldBeTrue();
        _vm = new ScenesViewModel(_host.Runtime, _host.Dialogs);
        _host.Tick();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    [Trait("Exigence", "INST-053")]
    [Trait("Exigence", "MOT-082")]
    public void AimTwoCorners_Save_ThenTheEngineKeepsTheLyreOut()
    {
        _vm.Programmer.Fixtures.Single(f => f.Name == "Lyre 1").IsSelected = true;
        var pan = _vm.Programmer.PositionTools.Single(t => t.Attribute == AttributeKind.Pan);
        var tilt = _vm.Programmer.PositionTools.Single(t => t.Attribute == AttributeKind.Tilt);
        var editor = _vm.CreateZonesEditor()!;

        // Le show de référence a des zones d'exemple (P5) : on repart de zéro pour ce test.
        while (editor.Zones.Count > 0)
        {
            editor.Selected = editor.Zones[0];
            editor.DeleteCommand.Execute(null);
        }

        pan.Percent = 40;
        tilt.Percent = 80;
        _host.Tick();
        editor.AddFromProgrammerCommand.Execute(null);
        editor.TakeFirstCornerCommand.Execute(null);
        pan.Percent = 60;
        tilt.Percent = 100;
        _host.Tick();
        editor.TakeSecondCornerCommand.Execute(null);
        var zone = editor.Zones.ShouldHaveSingleItem();
        (zone.PanMin, zone.PanMax, zone.TiltMin, zone.TiltMax).ShouldBe((40m, 60m, 80m, 100m));

        editor.SaveCommand.Execute(null);
        _host.Runtime.Project.Venues.Active.ForbiddenZones.ShouldHaveSingleItem().TiltMin.ShouldBe(0.8, 1e-9);

        // Viser dans la zone (Pan 45 %, Tilt 95 %) : le bord autorisé le plus proche est Pan 40 % (la zone touche la
        // butée du Tilt à 100 %, qui n'est donc pas un bord autorisé).
        pan.Percent = 45;
        tilt.Percent = 95;
        _host.Tick();
        _host.Tick();
        var snapshot = _host.Runtime.Engine.Snapshot;
        var lyre = _host.Runtime.Project.Installation.Fixtures.Single(f => f.Name == "Lyre 1").Id;
        snapshot.Values[snapshot.Show.IndexOf(lyre, "pan")].ShouldBe(0.4, 0.01);
        snapshot.Values[snapshot.Show.IndexOf(lyre, "tilt")].ShouldBe(0.95, 0.01);
        snapshot.ActiveLimits.ShouldContain(l => l.FixtureId == lyre);
    }

    [Fact]
    [Trait("Exigence", "INST-053")]
    public void AddWithoutLyre_ExplainsWhatToDo()
    {
        var editor = _vm.CreateZonesEditor()!;
        var before = editor.Zones.Count;

        editor.AddFromProgrammerCommand.Execute(null);

        editor.Zones.Count.ShouldBe(before);
        editor.Message.ShouldNotBeNull().ShouldContain("Sélectionnez une lyre");
    }
}
