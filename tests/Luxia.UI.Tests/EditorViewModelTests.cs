using Luxia.Scenes.Model;
using Luxia.UI.Modules.Control;

namespace Luxia.UI.Tests;

/// <summary>Fenêtre d'édition d'une scène (ERG-033, ERG-034) : brouillon, Appliquer / Valider / Annuler, Aveugle, fermeture.</summary>
public sealed class EditorViewModelTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private EditorViewModel _vm = null!;
    private Guid _chaserId;
    private Guid _fullId;

    private Scene Stored(Guid id) => _host.Runtime.Project.Scenes.Scenes.Single(s => s.Id == id);

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
        _chaserId = _host.Runtime.Project.Scenes.Scenes.Single(s => s.Name == "Chenillard 4 couleurs").Id;
        _fullId = _host.Runtime.Project.Scenes.Scenes.Single(s => s.Name == "Plein feu").Id;
        _vm = new EditorViewModel(_host.Runtime, _host.Dialogs);
        _host.Tick();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private void Rename(string name)
    {
        _vm.Session.UpdateScene(s => s with { Name = name }, "Nom de la scène");
        _vm.Session.Commit();
        _vm.Refresh();
    }

    [Fact]
    [Trait("Exigence", "ERG-033")]
    public void Open_ShowsTheSceneAsADraft_InEditMode_NotBlind()
    {
        _vm.Open(_chaserId).ShouldBeNull();

        _vm.IsOpen.ShouldBeTrue();
        _vm.SceneId.ShouldBe(_chaserId);
        _vm.SceneName.ShouldBe("Chenillard 4 couleurs");
        _vm.WindowTitle.ShouldContain("Chenillard 4 couleurs");
        _vm.WindowTitle.ShouldContain("brouillon");
        _vm.IsBlind.ShouldBeFalse();
        _vm.HasChanges.ShouldBeFalse();
        _vm.Session.Mode.ShouldBe(EditMode.Edit);
    }

    [Fact]
    [Trait("Exigence", "ERG-033")]
    public void Validate_WritesTheScene_AndCloses()
    {
        var closed = 0;
        _vm.Closed += (_, _) => closed++;
        _vm.Open(_chaserId);
        Rename("Validée");
        _vm.HasChanges.ShouldBeTrue();

        _vm.ValidateCommand.Execute(null);

        Stored(_chaserId).Name.ShouldBe("Validée");
        _vm.IsOpen.ShouldBeFalse();
        _vm.SceneId.ShouldBeNull();
        closed.ShouldBe(1);
        _vm.Session.IsDraft.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "ERG-034")]
    public void Apply_WritesWithoutClosing_ThenCancelKeepsTheApplication()
    {
        _vm.Open(_chaserId);
        Rename("Appliquée");

        _vm.ApplyCommand.Execute(null);

        Stored(_chaserId).Name.ShouldBe("Appliquée");
        _vm.IsOpen.ShouldBeTrue();
        _vm.HasChanges.ShouldBeFalse();

        Rename("Jetée");
        _vm.CancelCommand.Execute(null);

        Stored(_chaserId).Name.ShouldBe("Appliquée");
        _vm.IsOpen.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "ERG-033")]
    public void Cancel_BringsTheSceneBackToItsOriginalState_WithoutAskingAnything()
    {
        _vm.Open(_chaserId);
        Rename("Jetée");

        _vm.CancelCommand.Execute(null);

        Stored(_chaserId).Name.ShouldBe("Chenillard 4 couleurs");
        _host.Dialogs.Confirmations.ShouldBeEmpty("aucune question, même en spectacle (P9)");
        _vm.IsOpen.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "ERG-033")]
    public async Task TheCross_ClosesWithoutChanges_AsksBeforeLosingADraft()
    {
        _vm.Open(_chaserId);
        (await _vm.ConfirmCloseAsync()).ShouldBeTrue("aucune modification : la fenêtre se ferme");
        _vm.IsOpen.ShouldBeFalse();
        _host.Dialogs.Confirmations.ShouldBeEmpty();

        _vm.Open(_chaserId);
        Rename("À garder");
        _host.Dialogs.ConfirmAnswer = false;
        (await _vm.ConfirmCloseAsync()).ShouldBeFalse("Non : la fenêtre reste ouverte");
        _vm.IsOpen.ShouldBeTrue();
        Stored(_chaserId).Name.ShouldBe("Chenillard 4 couleurs");
        _host.Dialogs.Confirmations.ShouldHaveSingleItem().ShouldContain("À garder");

        _host.Dialogs.ConfirmAnswer = true;
        (await _vm.ConfirmCloseAsync()).ShouldBeTrue("Oui : brouillon abandonné");
        _vm.IsOpen.ShouldBeFalse();
        Stored(_chaserId).Name.ShouldBe("Chenillard 4 couleurs");
    }

    [Fact]
    [Trait("Exigence", "ERG-033")]
    public void AWarning_GoesAwayWhenTheDraftIsIdenticalAgain()
    {
        _vm.Open(_chaserId);
        Rename("Un");
        _vm.Message = "Modifications non validées.";

        _vm.UndoCommand.Execute(null);
        _vm.Refresh();

        _vm.HasChanges.ShouldBeFalse();
        _vm.Message.ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "ERG-033")]
    public void OpeningAnotherScene_IsRefusedWhileTheFirstHasChanges_AllowedOtherwise()
    {
        _vm.Open(_chaserId);
        _vm.Open(_chaserId).ShouldBeNull("rouvrir la même scène : rien à faire");

        _vm.Open(_fullId).ShouldBeNull("sans modification, on change de scène");
        _vm.SceneId.ShouldBe(_fullId);

        Rename("Modifiée");
        _vm.Open(_chaserId).ShouldNotBeNull();
        _vm.SceneId.ShouldBe(_fullId);
    }

    [Fact]
    [Trait("Exigence", "ERG-034")]
    public void BlindCheckbox_MovesTheDraftToThePreviewOnly_AndBack()
    {
        _vm.Open(_chaserId);
        Rename("Vue");
        _host.Tick();
        _host.Runtime.Engine.Snapshot.Show.Scene(_chaserId)!.Name.ShouldBe("Vue");

        _vm.IsBlind = true;
        _host.Tick();
        _host.Runtime.Preview.Tick();

        _vm.Session.Mode.ShouldBe(EditMode.Blind);
        _vm.SafetyText.ShouldContain("ne change pas");
        _host.Runtime.Engine.Snapshot.Show.Scene(_chaserId)!.Name.ShouldBe("Chenillard 4 couleurs", "la sortie revient à la scène enregistrée");
        _host.Runtime.Preview.Snapshot.Show.Scene(_chaserId)!.Name.ShouldBe("Vue");

        _vm.IsBlind = false;
        _host.Tick();
        _vm.Session.Mode.ShouldBe(EditMode.Edit);
        _host.Runtime.Engine.Snapshot.Show.Scene(_chaserId)!.Name.ShouldBe("Vue");
    }

    [Fact]
    [Trait("Exigence", "ERG-021")]
    [Trait("Exigence", "ERG-033")]
    public void EveningLock_RefusesToOpen()
    {
        _vm.Session.SetLocked(true);

        _vm.Open(_chaserId).ShouldBe(ControlSession.LockedReason);
        _vm.IsOpen.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "ERG-033")]
    public void Undo_InsideTheWindow_StaysInTheDraft()
    {
        _vm.Open(_chaserId);
        Rename("Un");
        Rename("Deux");

        _vm.UndoCommand.Execute(null);

        _vm.Session.EditScene!.Name.ShouldBe("Un");
        Stored(_chaserId).Name.ShouldBe("Chenillard 4 couleurs");
        _vm.RedoCommand.Execute(null);
        _vm.Session.EditScene!.Name.ShouldBe("Deux");
    }

    [Fact]
    [Trait("Exigence", "ERG-033")]
    public void ProjectReopened_TheWindowLosesItsDraftAndSaysSo()
    {
        var closed = 0;
        _vm.Closed += (_, _) => closed++;
        _vm.Open(_chaserId);
        Rename("Perdue");

        _host.Runtime.Project.Open(_host.ProjectFolder).ShouldBeTrue();
        _vm.Refresh();

        _vm.IsOpen.ShouldBeFalse();
        closed.ShouldBe(1);
    }

    [Fact]
    [Trait("Exigence", "ERG-033")]
    public void Abandon_AtShutdown_DropsTheDraftSilently()
    {
        _vm.Open(_chaserId);
        Rename("Perdue");

        _vm.Abandon();

        Stored(_chaserId).Name.ShouldBe("Chenillard 4 couleurs");
        _vm.IsOpen.ShouldBeFalse();
    }
}
