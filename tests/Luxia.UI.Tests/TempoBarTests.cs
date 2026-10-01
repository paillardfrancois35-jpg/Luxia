using Luxia.Messaging.Commands;
using Luxia.UI.Modules.Control;

namespace Luxia.UI.Tests;

/// <summary>Bloc BPM de l'écran de jeu (Q42) : il lit l'horloge du moteur et lui envoie CMD-040 à 042.</summary>
public sealed class TempoBarTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private GameViewModel _vm = null!;

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
        _vm = new GameViewModel(_host.Runtime, _host.Dialogs);
        _host.Tick();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    [Trait("Exigence", "AUD-081")]
    public void Bar_StartsAt120_Fixed_AndShowsTheFirstBeat()
    {
        _vm.Refresh();
        _vm.Tempo.SourceText.ShouldBe("Fixe");
        _vm.Tempo.BpmText.ShouldBe("120");
        _vm.Tempo.AudioOn.ShouldBeFalse();
        _vm.Tempo.ManualEnabled.ShouldBeTrue();
    }

    [Fact]
    [Trait("Exigence", "AUD-025")]
    public void Tap_FourTaps_SwitchToTheTapSource()
    {
        for (var i = 0; i < 4; i++)
        {
            _vm.Tempo.Tap();
            for (var t = 0; t < 16; t++)
            {
                _host.Tick();
            }
        }

        _vm.Refresh();
        _vm.Tempo.SourceText.ShouldBe("Tap");
        _host.Runtime.Engine.Bpm.ShouldBe(150, 1.5);
    }

    [Fact]
    [Trait("Exigence", "AUD-023")]
    public void TimesTwoAndDivideByTwo_ChangeTheEngineTempo_AndTheBarFollows()
    {
        _vm.Tempo.TimesTwoCommand.Execute(null);
        _host.Tick();
        _vm.Refresh();
        _vm.Tempo.BpmText.ShouldBe("240");

        _vm.Tempo.DivideByTwoCommand.Execute(null);
        _vm.Tempo.DivideByTwoCommand.Execute(null);
        _host.Tick();
        _vm.Refresh();
        _vm.Tempo.BpmText.ShouldBe("60");
    }

    [Fact]
    [Trait("Exigence", "CMD-041")]
    public void Fixed_WithTypedBpm_SetsTheTempo_AndWrongInputKeepsTheCurrentOne()
    {
        _vm.Tempo.BpmInput = "95";
        _vm.Tempo.ApplyTypedBpmCommand.Execute(null);
        _host.Tick();
        _host.Runtime.Engine.Bpm.ShouldBe(95);

        _vm.Tempo.BpmInput = "abc";
        _vm.Tempo.ApplyTypedBpmCommand.Execute(null);
        _host.Tick();
        _host.Runtime.Engine.Bpm.ShouldBe(95);
    }

    [Fact]
    [Trait("Exigence", "AUD-024")]
    public void ResyncBar_MakesTheCurrentBeatTheFirst()
    {
        for (var i = 0; i < 25; i++)
        {
            _host.Tick();
        }

        _vm.Tempo.ResyncBarCommand.Execute(null);
        _host.Tick();
        _vm.Refresh();
        _host.Runtime.Engine.Snapshot.Tempo.BeatInBar.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "CMD-042")]
    public void Adjustments_AreLoggedInTheJournal()
    {
        _vm.Tempo.IncreaseCommand.Execute(null);
        _host.Tick();
        _host.Runtime.Engine.Bpm.ShouldBe(121);
        _vm.Refresh();
        _vm.Journal.Lines.ShouldContain(l => l.Contains("+1 BPM"));
    }

    [Fact]
    [Trait("Exigence", "AUD-025")]
    public void Tap_LightsTheButtonForAMoment()
    {
        _vm.Tempo.Tap();
        _vm.Tempo.TapFlash.ShouldBeTrue();
        for (var i = 0; i < 6; i++)
        {
            _vm.Tempo.Refresh();
        }

        _vm.Tempo.TapFlash.ShouldBeFalse();
    }
}
