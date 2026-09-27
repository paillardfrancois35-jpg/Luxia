using Luxia.Fixtures.Model;
using Luxia.Scenes.Model;
using Luxia.UI.Modules.Live;

namespace Luxia.UI.Tests;

/// <summary>Écran Live (doc 18) sur une copie du show de référence (flash blanc et strobe de la couche Flashs, P5).</summary>
public sealed class LiveViewModelTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private LiveViewModel _vm = null!;
    private Scene _flash = null!;

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

        // Flash blanc et Strobe flash : scènes de la couche Flashs du show de référence (P5).
        _flash = _host.Runtime.Project.Scenes.Scenes.Single(s => s.Name == "Flash blanc");
        _vm = new LiveViewModel(_host.Runtime);
        _host.Tick();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    [Trait("Exigence", "LIVE-002")]
    public void Columns_AreTheLayers_WithTheirLiveScenes_InOrder()
    {
        _vm.Columns.Select(c => c.Layer.Name).ShouldBe(["Intensité", "Couleurs", "Mouvements", "Faisceau", "Effets", "Ambiance", "Flashs"]);
        var colors = _vm.Columns.Single(c => c.Layer.Name == "Couleurs");
        colors.Scenes.Select(s => s.Name).ShouldBe(_host.Runtime.Project.Scenes.Scenes
            .Where(s => s.LayerId == LayerSet.ColorsLayerId && s.VisibleInLive).Select(s => s.Name));
        colors.Scenes[0].Key.ShouldBe("1");
        _vm.Columns.Single(c => c.Layer.Name == "Flashs").IsFlash.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "LIVE-003")]
    [Trait("Exigence", "LIVE-002")]
    public void ClickScene_Launches_ClickAgain_Stops()
    {
        var scene = _vm.Columns.Single(c => c.Layer.Name == "Couleurs").Scenes[0];

        _vm.Press(scene);
        _host.Tick();
        _vm.Refresh();
        scene.IsActive.ShouldBeTrue();
        _host.Runtime.Engine.CommandLog().ShouldContain(e => e.Command.Origin == Messaging.Commands.CommandOrigin.User);

        _vm.Press(scene);
        _host.Tick();
        _host.Tick();
        _vm.Refresh();
        scene.IsActive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "LIVE-003")]
    [Trait("Exigence", "COU-005")]
    public void FlashLayerScene_PlaysOnlyWhileHeld()
    {
        var flash = _vm.Columns.Single(c => c.Layer.Name == "Flashs").Scenes.Single(s => s.Name == "Flash blanc");

        _vm.Press(flash);
        _host.Tick();
        _host.Runtime.Engine.Snapshot.Playbacks.ShouldContain(p => p.SceneId == _flash.Id && p.Flash);

        _vm.Release(flash);
        _host.Tick();
        _host.Runtime.Engine.Snapshot.Playbacks.ShouldNotContain(p => p.SceneId == _flash.Id);
    }

    [Fact]
    [Trait("Exigence", "LIVE-004")]
    [Trait("Exigence", "LIVE-040")]
    [Trait("Exigence", "GEN-071")]
    public void Keys_FlashHeldWithAutoRepeat_ThenReleased()
    {
        _vm.HasFlash.ShouldBeTrue();
        _vm.HasStrobe.ShouldBeTrue();

        _vm.OnKey(LiveKey.Flash, down: true).ShouldBeTrue();
        _vm.OnKey(LiveKey.Flash, down: true);
        _vm.OnKey(LiveKey.Flash, down: true);
        _host.Tick();
        _host.Runtime.Engine.Snapshot.Playbacks.Count(p => p.Flash).ShouldBe(1, "la répétition automatique de la touche ne relance pas le flash");

        _vm.OnKey(LiveKey.Flash, down: false);
        _host.Tick();
        _host.Runtime.Engine.Snapshot.Playbacks.ShouldNotContain(p => p.Flash);
    }

    [Fact]
    [Trait("Exigence", "LIVE-040")]
    public void Keys_UpDownArrows_DriveTheMasterOfTheFramedLayer()
    {
        // Essai P5 (proposition de l'utilisateur) : ← → choisissent la couche, ↑ ↓ règlent son master, Page ↑ ↓ le Grand Master.
        LiveKeys.From(Avalonia.Input.Key.Up).ShouldBe(LiveKey.LayerMasterUp);
        LiveKeys.From(Avalonia.Input.Key.PageUp).ShouldBe(LiveKey.MasterUp);
        _vm.OnKey(LiveKey.NextLayer, down: true);
        var column = _vm.Columns.Single(c => c.IsSelected);

        _vm.OnKey(LiveKey.LayerMasterDown, down: true).ShouldBeTrue();
        _vm.OnKey(LiveKey.LayerMasterDown, down: true);
        _host.Tick();

        column.Master.ShouldBe(80);
        _host.Runtime.Engine.Snapshot.LayerMasters.ShouldContain(m => Math.Abs(m - 0.8) < 1e-9);
        _host.Runtime.Engine.Snapshot.GrandMaster.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "LIVE-040")]
    public void LayerMaster_RefreshBeforeTheEngineTick_DoesNotJumpBack()
    {
        // Essai P5 : 10 % → 20 % affichait 20, 10, puis 20 (relecture du moteur avant le traitement de la commande).
        _vm.OnKey(LiveKey.NextLayer, down: true);
        var column = _vm.Columns.Single(c => c.IsSelected);
        _vm.OnKey(LiveKey.LayerMasterDown, down: true);
        _host.Tick();
        _vm.Refresh();

        var shown = new List<double>();
        _vm.OnKey(LiveKey.LayerMasterDown, down: true);
        shown.Add(column.Master);
        _vm.Refresh(); // avant le tick
        shown.Add(column.Master);
        _host.Tick();
        _vm.Refresh();
        shown.Add(column.Master);

        shown.ShouldBe([80, 80, 80]);
    }

    [Fact]
    [Trait("Exigence", "LIVE-040")]
    public void Keys_ArrowsChooseLayer_DigitsLaunchItsScenes_GFreezes_PageDownLowersMaster()
    {
        _vm.OnKey(LiveKey.NextLayer, down: true);
        _vm.OnKey(LiveKey.NextLayer, down: false);
        _vm.Columns.Single(c => c.IsSelected).Layer.Name.ShouldBe("Couleurs");

        _vm.OnKey(LiveKey.Scene2, down: true);
        _vm.OnKey(LiveKey.Scene2, down: false);
        _vm.OnKey(LiveKey.Freeze, down: true);
        _vm.OnKey(LiveKey.MasterDown, down: true);
        _host.Tick();

        var second = _vm.Columns.Single(c => c.IsSelected).Scenes[1].Scene.Id;
        var snapshot = _host.Runtime.Engine.Snapshot;
        snapshot.Playbacks.ShouldContain(p => p.SceneId == second);
        snapshot.Frozen.ShouldBeTrue();
        snapshot.GrandMaster.ShouldBe(0.9, 1e-9);
    }

    [Fact]
    [Trait("Exigence", "LIVE-005")]
    public void QuickPalette_OverridesTheSelection_ThenReleaseGivesBack()
    {
        _vm.SelectQuickCommand.Execute(_vm.QuickSelections.Single(s => s.Label == "Toutes les lyres"));
        _vm.ApplyPaletteCommand.Execute(_vm.Positions.Single(p => p.Name == "Plafond"));
        _host.Tick();
        var lyre = _host.Runtime.Project.Installation.Fixtures.Single(f => f.Name == "Lyre 1").Id;
        var snapshot = _host.Runtime.Engine.Snapshot;
        var tilt = snapshot.Show.IndexOf(lyre, "tilt");
        double.IsNaN(snapshot.Overrides[tilt]).ShouldBeFalse("la lyre est forcée sur la palette");
        _vm.HasQuickOverrides.ShouldBeTrue();

        _vm.OnKey(LiveKey.Release, down: true);
        _host.Tick();
        double.IsNaN(_host.Runtime.Engine.Snapshot.Overrides[tilt]).ShouldBeTrue("« Libérer » rend la main aux couches");
        _vm.HasQuickOverrides.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "LIVE-009")]
    public async Task Journal_ShowsSceneStarts()
    {
        _vm.Press(_vm.Columns.Single(c => c.Layer.Name == "Couleurs").Scenes[0]);
        _host.Tick();

        // Les événements arrivent sur le fil du bus : on laisse le temps de les recevoir.
        for (var i = 0; i < 50 && _vm.Journal.Count == 0; i++)
        {
            await Task.Delay(20);
            _vm.Refresh();
        }

        _vm.Journal.ShouldContain(line => line.Contains('▶') && line.Contains("utilisateur"));
    }

    [Fact]
    [Trait("Exigence", "LIVE-001")]
    [Trait("Exigence", "LIVE-010")]
    [Trait("Exigence", "GEN-112")]
    public void StatusBand_AndCommandJournal()
    {
        _vm.ShowCommands = true;
        _vm.Press(_vm.Columns.Single(c => c.Layer.Name == "Couleurs").Scenes[0]);
        _host.Tick();
        _vm.Refresh();

        _vm.OutputText.ShouldNotBeNullOrEmpty();
        _vm.VenueText.ShouldContain("Générique");
        _vm.LimitsText.ShouldContain("aucune limite");
        _vm.Commands.ShouldContain(c => c.Contains("LaunchScene") && c.Contains("utilisateur"));
    }

    [Fact]
    [Trait("Exigence", "LIVE-060")]
    public void Refresh_IsFarUnderTheFrameBudget()
    {
        foreach (var column in _vm.Columns.Take(6))
        {
            if (column.Scenes.Count > 0)
            {
                _vm.Press(column.Scenes[0]);
            }
        }

        _host.Tick();
        _vm.Refresh();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < 100; i++)
        {
            _host.Tick();
            _vm.Refresh();
        }

        // 20 images/s = 50 ms par image : le rafraîchissement du Live (moteur compris) en prend bien moins.
        (watch.Elapsed.TotalMilliseconds / 100).ShouldBeLessThan(10);
    }
}
