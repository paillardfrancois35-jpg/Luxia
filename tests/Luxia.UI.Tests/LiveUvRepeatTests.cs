using Luxia.Messaging.Commands;
using Luxia.UI.Modules.Live;

namespace Luxia.UI.Tests;

/// <summary>
/// Essai P5 de l'utilisateur rejoué : « Plein feu » puis dix clics sur « UV plein » ; chaque clic doit allumer puis éteindre
/// l'UV en alternance (LIVE-003), même quand l'écran se rafraîchit avant que le moteur ait traité le clic (course vécue :
/// l'écran relançait la scène au lieu de l'arrêter).
/// </summary>
public sealed class LiveUvRepeatTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private LiveViewModel _vm = null!;

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
        _vm = new LiveViewModel(_host.Runtime);
        _host.Tick();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    [Trait("Exigence", "LIVE-003")]
    public void FullOn_ThenTenClicksOnUv_EachClickTogglesTheUv()
    {
        var fullOn = _vm.Columns.SelectMany(c => c.Scenes).Single(s => s.Name == "Plein feu");
        var uv = _vm.Columns.SelectMany(c => c.Scenes).Single(s => s.Name == "UV plein");
        _vm.Press(fullOn);
        _vm.Release(fullOn);
        Run(40);

        var states = new List<(bool Ui, byte Row1)>();
        for (var i = 0; i < 10; i++)
        {
            _vm.Press(uv);
            _vm.Release(uv);
            _vm.Refresh(); // rafraîchissement de l'écran AVANT le tick qui traite le clic
            Run(80);
            states.Add((uv.IsActive, _host.Frame()[161]));
        }

        _host.Runtime.Engine.CommandLog().Count(e => e.Command is LaunchSceneCommand l && l.SceneId == uv.Scene.Id).ShouldBe(10);
        states.Select(s => s.Ui).ShouldBe([true, false, true, false, true, false, true, false, true, false]);
        states.Select(s => s.Row1).ShouldBe([255, 0, 255, 0, 255, 0, 255, 0, 255, 0], string.Join(" ", states));
    }

    private void Run(int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            _host.Tick();
            if (i % 2 == 0)
            {
                _vm.Refresh();
            }
        }
    }
}
