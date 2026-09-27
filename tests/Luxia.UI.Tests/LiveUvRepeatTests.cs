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

    [Fact]
    [Trait("Exigence", "SORT-066")]
    public async Task Recording_JournalInterleavesClicksCommandsAndChannelChanges()
    {
        var uv = _vm.Columns.SelectMany(c => c.Scenes).Single(s => s.Name == "UV plein");
        var path = Path.Combine(_host.ProjectFolder, "trames-essai.dmxrec");
        _host.Runtime.StartRecording(path);
        await Settle();

        _vm.Press(uv);
        _vm.Release(uv);
        await Settle();
        _vm.Press(uv);
        _vm.Release(uv);
        await Settle();
        _host.Runtime.StopRecording();
        await Task.Delay(300, TestContext.Current.CancellationToken);

        var lines = File.ReadAllLines(Path.ChangeExtension(path, ".journal.txt"));
        var interesting = lines.Where(l => l.Contains("IHM") || l.Contains("MOTEUR") || l.Contains(" DMX ")).Select(l => l[25..]).ToList();
        var launch = interesting.FindIndex(l => l.StartsWith("IHM", StringComparison.Ordinal) && l.Contains("appui « UV plein »"));
        launch.ShouldBeGreaterThanOrEqualTo(0, string.Join(Environment.NewLine, lines));
        var command = interesting.FindIndex(launch, l => l.StartsWith("MOTEUR", StringComparison.Ordinal) && l.Contains("« UV plein »"));
        command.ShouldBeGreaterThan(launch, string.Join(Environment.NewLine, lines));
        var on = interesting.FindIndex(command, l => l.StartsWith("DMX", StringComparison.Ordinal) && l.Contains("162:0→255"));
        on.ShouldBeGreaterThan(command, string.Join(Environment.NewLine, lines));
        interesting.FindIndex(on, l => l.StartsWith("DMX", StringComparison.Ordinal) && l.Contains("162:255→0")).ShouldBeGreaterThan(on, string.Join(Environment.NewLine, lines));
    }

    [Fact]
    [Trait("Exigence", "LIVE-008")]
    [Trait("Exigence", "MOT-081")]
    public void SmokeCut_PillShowsTheTimeLeftBeforeTheRestEnds()
    {
        var smoke = _vm.Columns.SelectMany(c => c.Scenes).Single(s => s.Name == "Fumée longue (plafonnée à 10 s)");
        _vm.Press(smoke);
        _vm.Release(smoke);
        Run(12 * 40); // 12 s : coupée à 10 s, encore 28,x s de repos

        _vm.LimitsText.ShouldContain("fumée en repos");
        _vm.LimitsText.ShouldContain("(encore 29 s)"); // 28,x s arrondis au-dessus
    }

    [Fact]
    [Trait("Exigence", "MOT-081")]
    [Trait("Exigence", "CMD-030")]
    public void SmokeBurst_AfterTheRest_Emits3sWithoutAnyLimit()
    {
        // Essai P5 : maintien de FUMÉE 6 s, relâche, 18 s de repos (3 × 6 s), puis Rafale.
        _vm.Smoke(true);
        Run(6 * 40);
        _vm.Smoke(false);
        Run(10 * 40);
        _vm.SmokeResting.ShouldBeTrue("décompte visible même si rien ne demande de fumée");
        _vm.SmokeBurstText.ShouldBe("Rafale (9 s)"); // 6 s émises → 18 s de repos ; 8,x s restantes, arrondies au-dessus
        _vm.SmokeButtonText.ShouldBe("FUMÉE (Z) (9 s)");
        Run(10 * 40);
        _host.Frame()[179].ShouldBe((byte)0);

        _vm.SmokeResting.ShouldBeFalse();
        _vm.SmokeBurstText.ShouldBe("Rafale");
        _vm.SmokeBurstCommand.Execute(null);
        var trace = new List<(int Tick, byte Value, bool Limited)>();
        for (var i = 0; i < 6 * 40; i++)
        {
            _host.Tick();
            _vm.Refresh();
            trace.Add((i, _host.Frame()[179], _vm.HasLimits));
        }

        var emitted = trace.Count(t => t.Value == 255);
        emitted.ShouldBeInRange(119, 121, string.Join(" ", trace.Where(t => t.Tick % 20 == 0)));
        trace.ShouldNotContain(t => t.Limited, "aucune limite pendant ni après une rafale qui respecte le repos");
    }

    private async Task Settle()
    {
        for (var i = 0; i < 8; i++)
        {
            _host.Tick();
            _vm.Refresh();
            await Task.Delay(30, TestContext.Current.CancellationToken);
        }
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
