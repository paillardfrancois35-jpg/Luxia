using Luxia.Fixtures;
using Luxia.Messaging.Commands;
using Luxia.Patch;
using Luxia.UI.Modules.Installation;

namespace Luxia.UI.Tests;

/// <summary>Onglet « Gestion des dimmers » de l'écran Installation (ERG-036) : arbre, détail, enregistrement, niveaux actuels.</summary>
public sealed class DimmerGroupsViewModelTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private InstallationViewModel _installation = null!;
    private DimmerGroupsViewModel _vm = null!;

    public ValueTask InitializeAsync()
    {
        _host.Runtime.Project.Create(_host.ProjectFolder, "Essai");
        _installation = new InstallationViewModel(_host.Runtime, _host.Dialogs);
        for (var i = 0; i < 3; i++)
        {
            _installation.SelectedModel = _installation.LibraryModels.Single(e => e.Fixture.Id == GenericFixtures.Rgb.Id);
            _installation.SelectedMode = "3 canaux";
            _installation.NewUniverse = 1;
            _installation.NewAddress = 1 + (i * 3);
            _installation.NewCount = 1;
            _installation.NewGap = 0;
            _installation.NewAsTwins = false;
            _installation.NewBaseName = "PAR";
            _installation.AddFixtureCommand.Execute(null);
        }

        _vm = _installation.Dimmers;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private async Task Add(string name, bool sub = false)
    {
        _host.Dialogs.TextAnswers.Enqueue(name);
        if (sub)
        {
            await _vm.AddSubGroupCommand.ExecuteAsync(null);
        }
        else
        {
            await _vm.AddGroupCommand.ExecuteAsync(null);
        }
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public void Start_NoGroup_ShowsOnlyTheImplicitUnassignedGroupHoldingEveryFixture()
    {
        _vm.Rows.ShouldHaveSingleItem();
        _vm.Rows[0].IsUnassigned.ShouldBeTrue();
        _vm.Rows[0].Name.ShouldBe("Non assigné");
        _vm.Rows[0].Summary.ShouldBe("PAR 1, PAR 2, PAR 3");
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public async Task AddGroup_ThenSubGroup_BuildsTheTree_AndSavesGroupsJson()
    {
        await Add("Parc");
        await Add("Face", sub: true);

        _vm.Rows.Select(r => r.Name).ShouldBe(["Parc", "Face", "Non assigné"]);
        _vm.Rows.Select(r => r.Depth).ShouldBe([0, 1, 0]);
        _vm.SelectedRow!.Name.ShouldBe("Face");
        GroupStore.Load(_host.ProjectFolder).Value.Groups.Select(g => g.Name).ShouldBe(["Parc", "Face"]);
        _host.Runtime.Engine.Snapshot.Show.DimmerGroups.Count.ShouldBe(0); // le moteur n'a pas encore tourné
        _host.Tick();
        _host.Runtime.Engine.Snapshot.Show.DimmerGroups.Select(g => g.Name).ShouldBe(["Parc", "Face"]);
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public async Task AddFixture_MovesItFromTheUnassignedGroup_AndRemoveGivesItBack()
    {
        await Add("Face");
        await _vm.AddFixtureCommand.ExecuteAsync(_vm.Candidates.Single(c => c.Name == "PAR 2"));

        _vm.Rows.Single(r => r.Name == "Face").Summary.ShouldBe("PAR 2");
        _vm.Rows.Single(r => r.IsUnassigned).Summary.ShouldBe("PAR 1, PAR 3");
        _vm.Members.Select(m => m.Name).ShouldBe(["PAR 2"]);
        _vm.Candidates.Single(c => c.Name == "PAR 1").Where.ShouldBe("« Non assigné »");

        _vm.RemoveFixtureCommand.Execute(_vm.Members[0]);
        _vm.Rows.Single(r => r.Name == "Face").Summary.ShouldBe("(vide)");
        _vm.Rows.Single(r => r.IsUnassigned).Summary.ShouldBe("PAR 1, PAR 2, PAR 3");
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public async Task Candidates_AreTheUnassignedOnesByDefault_ShowAllListsTheOthers_AndMovingAsksConfirmation()
    {
        await Add("Face");
        await _vm.AddFixtureCommand.ExecuteAsync(_vm.Candidates.Single(c => c.Name == "PAR 1"));
        await Add("Lyres");

        _vm.Candidates.Select(c => c.Name).ShouldBe(["PAR 2", "PAR 3"], "PAR 1 est rangé ailleurs : masqué");
        _vm.ShowAll = true;
        var moved = _vm.Candidates.Single(c => c.Name == "PAR 1");
        moved.CurrentGroup.ShouldBe("Face");

        _host.Dialogs.ConfirmAnswer = false;
        await _vm.AddFixtureCommand.ExecuteAsync(moved);
        _vm.Rows.Single(r => r.Name == "Face").Summary.ShouldBe("PAR 1", "refusé : il reste où il est");
        _host.Dialogs.Confirmations.ShouldHaveSingleItem().ShouldContain("fait déjà partie du groupe « Face »");

        _host.Dialogs.ConfirmAnswer = true;
        await _vm.AddFixtureCommand.ExecuteAsync(_vm.Candidates.Single(c => c.Name == "PAR 1"));
        _vm.Rows.Single(r => r.Name == "Lyres").Summary.ShouldBe("PAR 1");
        _host.Dialogs.Confirmations.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public async Task Rename_DimmerFlag_AndReparent_AreSaved_ALoopIsRefused()
    {
        await Add("Parc");
        await Add("Face", sub: true);
        var face = _vm.SelectedRow!;

        _vm.EditName = "Face scène";
        _vm.ApplyNameCommand.Execute(null);
        _vm.HasDimmer = false;
        _vm.Rows.Single(r => r.Name == "Face scène").Group.HasDimmer.ShouldBeFalse();

        // Le parent de « Parc » ne peut pas être « Face scène » (son propre sous-groupe) : la liste ne le propose même pas.
        _vm.SelectedRow = _vm.Rows.Single(r => r.Name == "Parc");
        _vm.ParentChoices.Select(c => c.Label).ShouldBe(["(racine)"]);

        _vm.SelectedRow = _vm.Rows.Single(r => r.Id == face.Id);
        _vm.SelectedParent = _vm.ParentChoices[0];
        _vm.Rows.Single(r => r.Id == face.Id).Depth.ShouldBe(0);
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public async Task Delete_AsksConfirmation_AndSubGroupsGoUp()
    {
        await Add("Parc");
        await Add("Face", sub: true);
        _vm.SelectedRow = _vm.Rows.Single(r => r.Name == "Parc");

        _host.Dialogs.ConfirmAnswer = false;
        await _vm.DeleteGroupCommand.ExecuteAsync(null);
        _vm.Rows.Count.ShouldBe(3);

        _host.Dialogs.ConfirmAnswer = true;
        await _vm.DeleteGroupCommand.ExecuteAsync(null);
        _vm.Rows.Select(r => r.Name).ShouldBe(["Face", "Non assigné"]);
        _vm.Rows[0].Depth.ShouldBe(0);
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    public void UnassignedGroup_CanBeRenamed_ButNotDeleted()
    {
        _vm.SelectedRow = _vm.Rows[0];
        _vm.EditName = "Divers";
        _vm.ApplyNameCommand.Execute(null);

        _vm.Rows[0].Name.ShouldBe("Divers");
        _vm.Rows[0].IsUnassigned.ShouldBeTrue();
        _vm.DeleteGroupCommand.ExecuteAsync(null).IsCompleted.ShouldBeTrue();
        _vm.Rows.Count.ShouldBe(1);
        GroupStore.Load(_host.ProjectFolder).Value.UnassignedName.ShouldBe("Divers");
    }

    [Fact]
    [Trait("Exigence", "ERG-036")]
    [Trait("Exigence", "ERG-037")]
    public async Task Levels_AreReadFromTheEngine_ReadOnly_WithFaderNumbersAndTheFlow()
    {
        await Add("Parc");
        await Add("Face", sub: true);
        _host.Tick();
        var parc = _host.Runtime.Engine.Snapshot.Show.DimmerGroups.Single(g => g.Name == "Parc");
        var face = _host.Runtime.Engine.Snapshot.Show.DimmerGroups.Single(g => g.Name == "Face");
        _host.Runtime.Engine.Send(new SetGroupDimmerCommand(CommandOrigin.User, parc.Id, 0.8));
        _host.Runtime.Engine.Send(new SetGroupDimmerCommand(CommandOrigin.User, face.Id, 0.5));
        _host.Tick();

        _vm.RefreshLevels();

        _vm.Rows.Single(r => r.Name == "Parc").LevelText.ShouldBe("80 %");
        _vm.Rows.Single(r => r.Name == "Face").LevelText.ShouldBe("50 %");
        _vm.Rows.Select(r => r.FaderText).ShouldBe(["② 1", "② 2", string.Empty]);
        _vm.Flow.Select(f => f.Label).ShouldBe(["Scène / couches", "Grand Master", "Parc", "Face", "Sortie DMX"]);
        _vm.Flow[^1].Value.ShouldBe("40 %");
        _vm.Flow[^1].IsResult.ShouldBeTrue();
    }
}
