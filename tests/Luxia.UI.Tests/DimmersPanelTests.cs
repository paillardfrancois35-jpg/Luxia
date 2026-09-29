using Luxia.Messaging.Commands;
using Luxia.Patch.Model;
using Luxia.UI.Modules.Control;

namespace Luxia.UI.Tests;

/// <summary>Panneau « Groupes dimmer » de l'écran Contrôle (ERG-037) : faders, retouche en direct, niveau effectif.</summary>
public sealed class DimmersPanelTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private ControlViewModel _vm = null!;
    private FixtureGroup _parc = null!;
    private FixtureGroup _face = null!;

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
        _vm = new ControlViewModel(_host.Runtime, _host.Dialogs);
        _host.Tick();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private void CreateGroups()
    {
        var pars = _host.Runtime.Project.Installation.Fixtures.Where(f => f.Name.StartsWith("PAR", StringComparison.Ordinal)).Select(f => f.Id).ToList();
        _parc = new FixtureGroup { Name = "Parc", HasDimmer = true };
        _face = new FixtureGroup { Name = "Face", ParentId = _parc.Id, HasDimmer = true, FixtureIds = pars };
        _host.Runtime.Project.SaveGroups(new FixtureGroupSet { Groups = [_face, _parc, new FixtureGroup { Name = "Organisation", HasDimmer = false }] });
        _host.Tick();
        _vm.Dimmers.Refresh();
    }

    [Fact]
    [Trait("Exigence", "ERG-037")]
    public void NoGroup_PanelIsEmpty_WithAnExplanation()
    {
        _vm.Dimmers.IsEmpty.ShouldBeTrue();
        _vm.Dimmers.Faders.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "ERG-037")]
    [Trait("Exigence", "ERG-038")]
    public void OneFaderPerDimmerGroup_InTreeOrder_WithTheirPlatineNumber()
    {
        CreateGroups();

        _vm.Dimmers.Faders.Select(f => f.Name).ShouldBe(["Parc", "Face"]);
        _vm.Dimmers.Faders.Select(f => f.FaderText).ShouldBe(["② 1", "② 2"]);
        _vm.Dimmers.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "ERG-037")]
    public void MovingAFader_SendsTheCommand_TheEngineMultiplies_AndTheEffectiveLevelFollows()
    {
        CreateGroups();
        var par = _host.Runtime.Project.Installation.Fixtures.First(f => f.Name.StartsWith("PAR", StringComparison.Ordinal));

        _vm.Dimmers.Faders[0].Percent = 80;
        _vm.Dimmers.Faders[1].Percent = 50;
        _host.Tick();
        _vm.Dimmers.Refresh();

        _host.Runtime.Engine.Snapshot.DimmerLevels.ShouldBe([0.8, 0.5, 1.0], 1e-9);
        _vm.Dimmers.Faders[1].EffectiveText.ShouldBe("= 40 %");
        _vm.Dimmers.Faders[0].EffectiveText.ShouldBe("= 80 %");
        _vm.Dimmers.IsRetouched.ShouldBeTrue();
        _host.Runtime.Engine.CommandLog().Count(e => e.Command is SetGroupDimmerCommand).ShouldBeGreaterThanOrEqualTo(1);
        par.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Exigence", "ERG-037")]
    public void ResetAll_PutsEveryRetouchedDimmerBackTo100()
    {
        CreateGroups();
        _vm.Dimmers.Faders[0].Percent = 30;
        _vm.Dimmers.Faders[1].Percent = 60;
        _host.Tick();

        _vm.Dimmers.ResetAllCommand.Execute(null);
        _host.Tick();
        _vm.Dimmers.Refresh();

        _host.Runtime.Engine.Snapshot.DimmerLevels.ShouldBe([1.0, 1.0, 1.0], 1e-9);
        _vm.Dimmers.IsRetouched.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "ERG-037")]
    public void AnOutsideChange_MidiForExample_IsShownOnTheFader_WithoutSendingACommandBack()
    {
        CreateGroups();
        _host.Runtime.Engine.Send(new SetGroupDimmerCommand(CommandOrigin.Midi, _parc.Id, 0.25));
        _host.Tick();
        var before = _host.Runtime.Engine.CommandLog().Count;

        _vm.Dimmers.Refresh();

        _vm.Dimmers.Faders[0].Percent.ShouldBe(25);
        _host.Runtime.Engine.CommandLog().Count.ShouldBe(before, "l'affichage suit le moteur sans lui renvoyer sa valeur");
    }

    [Fact]
    [Trait("Exigence", "ERG-037")]
    public void PanelExistsInTheCatalog_WithItsHelp()
    {
        Luxia.UI.Modules.Control.Docking.ControlPanels.All.ShouldContain(p => p.Id == Luxia.UI.Modules.Control.Docking.ControlPanels.Dimmers && p.Help.Contains("retouche en direct", StringComparison.Ordinal));
    }
}
