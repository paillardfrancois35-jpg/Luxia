using Luxia.Messaging.Commands;
using Luxia.Patch.Model;
using Luxia.UI.Modules.Control;

namespace Luxia.UI.Tests;

/// <summary>Écran de jeu (ERG-032) : pas de mode, bande ✎ vers la fenêtre d'édition, retouches en direct, verrou soirée.</summary>
public sealed class GameViewModelTests : IAsyncLifetime
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

    private ControlSceneViewModel Button(string name) => _vm.Columns.Columns.SelectMany(c => c.Scenes).Single(s => s.Name == name);

    [Fact]
    [Trait("Exigence", "ERG-032")]
    public void EditBand_AsksForTheEditWindow_WithTheSceneId_AndChangesNothingElse()
    {
        var asked = new List<Guid>();
        _vm.EditRequested += (_, id) => asked.Add(id);
        var button = Button("Chenillard 4 couleurs");

        _vm.Columns.ChooseForEditCommand.Execute(button);

        asked.ShouldBe([button.Scene.Id]);
        _vm.Session.EditScene.ShouldBeNull("l'écran de jeu n'édite jamais lui-même : c'est la fenêtre d'édition");
        _vm.Session.Mode.ShouldBe(EditMode.Live);
    }

    [Fact]
    [Trait("Exigence", "ERG-032")]
    [Trait("Exigence", "ERG-021")]
    public void LockedEveningLock_RefusesTheEditWindow_WithItsReason_ButStillPlays()
    {
        var asked = 0;
        _vm.EditRequested += (_, _) => asked++;
        _vm.ToggleLockCommand.Execute(null);

        _vm.Columns.ChooseForEditCommand.Execute(Button("Plein feu"));
        _vm.Message.ShouldBe(ControlSession.LockedReason);
        asked.ShouldBe(0);

        _vm.Columns.Press(Button("Plein feu"));
        _host.Tick();
        _host.Runtime.Engine.Snapshot.Playbacks.ShouldContain(p => p.SceneId == Button("Plein feu").Scene.Id);

        _vm.ToggleLockCommand.Execute(null);
        _vm.Columns.ChooseForEditCommand.Execute(Button("Plein feu"));
        asked.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "ERG-032")]
    [Trait("Exigence", "ERG-033")]
    public void TheSceneOpenInTheEditor_IsOutlined_AndCannotBeRenamedOrDeletedFromTheGameScreen()
    {
        var button = Button("Chenillard 4 couleurs");
        _vm.EditedSceneId = button.Scene.Id;

        Button("Chenillard 4 couleurs").IsEditTarget.ShouldBeTrue();
        Button("Plein feu").IsEditTarget.ShouldBeFalse();

        _host.Dialogs.TextAnswers.Enqueue("Autre nom");
        _vm.Columns.RenameCommand.Execute(button);
        _host.Runtime.Project.Scenes.Scenes.ShouldContain(s => s.Name == "Chenillard 4 couleurs");
        _vm.Message.ShouldBe("Cette scène est ouverte dans la fenêtre d'édition : validez ou annulez-la d'abord.");
        _vm.Columns.DeleteCommand.Execute(button);
        _host.Runtime.Project.Scenes.Scenes.ShouldContain(s => s.Name == "Chenillard 4 couleurs");

        _vm.EditedSceneId = null;
        Button("Chenillard 4 couleurs").IsEditTarget.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "ERG-032")]
    [Trait("Exigence", "ERG-037")]
    public void RetouchBand_ShowsOnlyWhenADimmerIsRetouched_AndReleaseAllPutsThemBackTo100()
    {
        var pars = _host.Runtime.Project.Installation.Fixtures.Where(f => f.Name.StartsWith("PAR", StringComparison.Ordinal)).Select(f => f.Id).ToList();
        var group = new FixtureGroup { Name = "PAR", HasDimmer = true, FixtureIds = pars };
        _host.Runtime.Project.SaveGroups(new FixtureGroupSet { Groups = [group] });
        _host.Tick();
        _vm.Refresh();
        _vm.HasRetouches.ShouldBeFalse();

        _vm.Dimmers.Faders[0].Percent = 40;
        _host.Tick();
        _vm.Refresh();
        _vm.HasRetouches.ShouldBeTrue();
        _vm.RetouchText.ShouldContain("PAR — dimmer 40 %");

        _vm.ReleaseAllCommand.Execute(null);
        _host.Tick();
        _vm.Refresh();

        _vm.HasRetouches.ShouldBeFalse();
        _host.Runtime.Engine.Snapshot.DimmerLevels.ShouldBe([1.0], 1e-9);
    }

    [Fact]
    [Trait("Exigence", "ERG-026")]
    public void StopAndStopAll_StayAvailable_LockComprised()
    {
        _vm.Columns.Press(Button("Plein feu"));
        _vm.Columns.Press(Button("UV plein"));
        _host.Tick();
        _vm.ToggleLockCommand.Execute(null);

        _vm.StopAllCommand.Execute("sauf-protegees");
        _host.Tick();
        _host.Runtime.Engine.Snapshot.Playbacks.ShouldNotContain(p => p.SceneId == Button("Plein feu").Scene.Id && p.State == Luxia.Engine.Model.PlaybackState.Running);
        _vm.Refresh();
        _vm.Journal.Lines.ShouldContain(l => l.Contains("stop", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Exigence", "ERG-032")]
    public void SceneOperations_AreUndoneFromTheGameScreen()
    {
        var before = _host.Runtime.Project.Scenes.Scenes.Count;
        _host.Dialogs.TextAnswers.Enqueue("Ma scène");
        var column = _vm.Columns.Columns.Single(c => c.Layer.Name == "Libre");
        _vm.Columns.NewSceneCommand.Execute(column);
        _host.Runtime.Project.Scenes.Scenes.Count.ShouldBe(before + 1);

        _vm.UndoCommand.Execute(null);

        _host.Runtime.Project.Scenes.Scenes.Count.ShouldBe(before);
        _vm.Refresh();
        _vm.Journal.Lines.ShouldContain(l => l.Contains("annulé", StringComparison.Ordinal));
    }
}
