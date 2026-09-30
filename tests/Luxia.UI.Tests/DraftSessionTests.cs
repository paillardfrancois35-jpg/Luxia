using Luxia.Fixtures.Model;
using Luxia.Scenes.Model;
using Luxia.UI.Modules.Control;

namespace Luxia.UI.Tests;

/// <summary>Brouillon d'une scène (ERG-033, ERG-034) : rien n'est écrit avant Appliquer / Valider, Annuler revient à l'origine.</summary>
public sealed class DraftSessionTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private ControlSession _session = null!;

    private Guid Par1 => _host.Runtime.Project.Installation.Fixtures.Single(f => f.Name == "PAR 1").Id;

    private Guid _chaserId;

    private Scene Chaser => _host.Runtime.Project.Scenes.Scenes.Single(s => s.Id == _chaserId);

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
        _session = new ControlSession(_host.Runtime);
        _host.Tick();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private void Edit(string name = "Renommée")
    {
        _session.UpdateScene(s => s with { Name = name }, "Nom de la scène");
        _session.Commit();
    }

    private void Tick()
    {
        _host.Tick();
        _host.Runtime.Preview.Tick();
    }

    [Fact]
    [Trait("Exigence", "ERG-033")]
    public void BeginDraft_ChoosesTheSceneInEdit_WithoutWritingAnything()
    {
        var before = _host.Runtime.Project.Scenes;

        _session.BeginDraft(Chaser.Id).ShouldBeNull();

        _session.IsDraft.ShouldBeTrue();
        _session.Mode.ShouldBe(EditMode.Edit);
        _session.EditScene!.Id.ShouldBe(Chaser.Id);
        _session.HasDraftChanges.ShouldBeFalse();
        _host.Runtime.Project.Scenes.ShouldBeSameAs(before);
    }

    [Fact]
    [Trait("Exigence", "ERG-033")]
    public void Editing_KeepsTheDraftInMemory_TheProjectSceneStaysAsItWas_TheEngineGetsTheDraft()
    {
        var original = Chaser;
        _session.BeginDraft(original.Id);

        Edit("Nouveau nom");

        _session.HasDraftChanges.ShouldBeTrue();
        Chaser.Name.ShouldBe("Chenillard 4 couleurs", "le projet n'est pas touché");
        _host.Runtime.Project.Scenes.Scenes.Single(s => s.Id == original.Id).ShouldBeSameAs(original);
        _host.Runtime.Show.WorkingCopy!.Name.ShouldBe("Nouveau nom");
        Tick();
        _host.Runtime.Engine.Snapshot.Show.Scene(original.Id)!.Name.ShouldBe("Nouveau nom", "la sortie joue le brouillon (hors aveugle)");
    }

    [Fact]
    [Trait("Exigence", "ERG-034")]
    public void ApplyDraft_WritesTheSceneIntoTheProject_AndTheDraftGoesOn()
    {
        _session.BeginDraft(Chaser.Id);
        Edit("Appliquée");

        _session.ApplyDraft();

        Chaser.Name.ShouldBe("Appliquée");
        _host.Runtime.Show.WorkingCopy.ShouldBeNull();
        _session.HasDraftChanges.ShouldBeFalse();
        _session.IsDraft.ShouldBeTrue("la fenêtre reste ouverte");

        Edit("Deuxième retouche");
        _session.HasDraftChanges.ShouldBeTrue();
        Chaser.Name.ShouldBe("Appliquée");
    }

    [Fact]
    [Trait("Exigence", "ERG-034")]
    public void DiscardDraft_GoesBackToTheOriginalScene_KeepingWhatWasApplied()
    {
        _session.BeginDraft(Chaser.Id);
        Edit("Appliquée");
        _session.ApplyDraft();
        Edit("Jetée");

        _session.DiscardDraft();

        Chaser.Name.ShouldBe("Appliquée", "Appliquer est définitif, Annuler ne revient qu'à la dernière application");
        _session.EditScene!.Name.ShouldBe("Appliquée");
        _session.HasDraftChanges.ShouldBeFalse();
        _host.Runtime.Show.WorkingCopy.ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "ERG-033")]
    public void EndDraft_LeavesNoSceneEdited_NoOutputChange_AndTheDraftIsGone()
    {
        _session.BeginDraft(Chaser.Id);
        Edit("Jetée");

        _session.EndDraft();

        _session.IsDraft.ShouldBeFalse();
        _session.EditScene.ShouldBeNull();
        _session.Mode.ShouldBe(EditMode.Live);
        Chaser.Name.ShouldBe("Chenillard 4 couleurs");
        _host.Runtime.Show.WorkingCopy.ShouldBeNull();
        Tick();
        _host.Runtime.Engine.Snapshot.Show.Scene(Chaser.Id)!.Name.ShouldBe("Chenillard 4 couleurs");
    }

    [Fact]
    [Trait("Exigence", "ERG-034")]
    public void Blind_TheOutputKeepsTheSavedScene_OnlyThePreviewSeesTheDraft()
    {
        _session.BeginDraft(Chaser.Id);
        _session.SetMode(EditMode.Blind).ShouldBeNull();

        Edit("Vue en aveugle");
        Tick();

        _host.Runtime.Engine.Snapshot.Show.Scene(Chaser.Id)!.Name.ShouldBe("Chenillard 4 couleurs", "la sortie ne change pas");
        _host.Runtime.Preview.Snapshot.Show.Scene(Chaser.Id)!.Name.ShouldBe("Vue en aveugle");

        _session.SetMode(EditMode.Edit).ShouldBeNull();
        Tick();
        _host.Runtime.Engine.Snapshot.Show.Scene(Chaser.Id)!.Name.ShouldBe("Vue en aveugle", "hors aveugle la sortie voit le brouillon");
    }

    [Fact]
    [Trait("Exigence", "ERG-033")]
    public void UndoRedo_WorkInsideTheDraft_WithoutTouchingTheProject()
    {
        _session.BeginDraft(Chaser.Id);
        Edit("Un");
        Edit("Deux");

        _session.Undo();
        _session.EditScene!.Name.ShouldBe("Un");
        _session.Undo();
        _session.EditScene!.Name.ShouldBe("Chenillard 4 couleurs");
        _session.HasDraftChanges.ShouldBeFalse("revenu à l'origine : plus de brouillon");
        _session.Redo();
        _session.EditScene!.Name.ShouldBe("Un");
        Chaser.Name.ShouldBe("Chenillard 4 couleurs");
    }

    [Fact]
    [Trait("Exigence", "ERG-033")]
    public void ValuesEdited_AreShownOnTheOutput_ThenGoneWhenTheDraftIsDiscarded()
    {
        _session.BeginDraft(Chaser.Id);
        _session.Select([Par1]);
        _session.Apply(t => new SceneValue { Target = t, Color = new LogicalColor { G = 1 } }, "Couleur");
        Tick();
        _host.Frame()[2].ShouldBe((byte)255, "vert du PAR 1 montré pendant l'édition");

        _session.EndDraft();
        Tick();

        _host.Frame()[2].ShouldBe((byte)0, "plus rien après l'annulation");
    }

    [Fact]
    [Trait("Exigence", "ERG-034")]
    public void SuspendShow_LetsTheDraftSceneBePlayedWithoutTheEditedStepOnTop()
    {
        _session.BeginDraft(Chaser.Id);
        _session.Select([Par1]);
        _session.Apply(t => new SceneValue { Target = t, Color = new LogicalColor { G = 1 } }, "Couleur");
        Tick();
        _host.Runtime.Engine.Snapshot.Overrides.ShouldContain(v => !double.IsNaN(v));

        _session.SuspendShow();
        Tick();
        _host.Runtime.Engine.Snapshot.Overrides.ShouldAllBe(v => double.IsNaN(v), "l'étape éditée ne recouvre plus la scène qu'on lance");

        _session.Apply(t => new SceneValue { Target = t, Color = new LogicalColor { B = 1 } }, "Couleur");
        Tick();
        _host.Runtime.Engine.Snapshot.Overrides.ShouldContain(v => !double.IsNaN(v), "le premier réglage reprend le montre");
    }

    [Fact]
    [Trait("Exigence", "ERG-021")]
    [Trait("Exigence", "ERG-033")]
    public void BeginDraft_IsRefusedUnderTheEveningLock_OrForAnUnknownScene()
    {
        _session.BeginDraft(Guid.NewGuid()).ShouldNotBeNull();
        _session.SetLocked(true);

        _session.BeginDraft(Chaser.Id).ShouldBe(ControlSession.LockedReason);

        _session.IsDraft.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "ERG-033")]
    public void OpeningTheProjectAgain_DropsTheDraft()
    {
        _session.BeginDraft(Chaser.Id);
        Edit("Jetée");

        _host.Runtime.Project.Open(_host.ProjectFolder).ShouldBeTrue();

        _session.IsDraft.ShouldBeFalse();
        _host.Runtime.Show.WorkingCopy.ShouldBeNull();
    }
}
