using Luxia.Fixtures;
using Luxia.Patch.Model;
using Luxia.UI.Modules.Installation;

namespace Luxia.UI.Tests;

/// <summary>Écran Installation : patch, sélections, lieux (doc 13).</summary>
public sealed class InstallationViewModelTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private InstallationViewModel _vm = null!;

    public ValueTask InitializeAsync()
    {
        _host.Runtime.Project.Create(_host.ProjectFolder, "Essai");
        _vm = new InstallationViewModel(_host.Runtime, _host.Dialogs);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    [Trait("Exigence", "INST-010")]
    [Trait("Exigence", "INST-011")]
    [Trait("Exigence", "GEN-053")]
    public void AddFixture_Multiple_CreatesConsecutiveAddressesAndCopiesModelIntoProject()
    {
        _vm.SelectedModel = _vm.LibraryModels.Single(e => e.Fixture.Id == GenericFixtures.Rgb.Id);
        _vm.SelectedMode = "3 canaux";
        _vm.NewAddress = 1;
        _vm.NewCount = 4;
        _vm.NewGap = 4;

        _vm.AddFixtureCommand.Execute(null);

        _vm.PatchRows.Select(r => r.Address).ShouldBe([1, 8, 15, 22]);
        _host.Runtime.Project.FixtureLibrary!.Find(GenericFixtures.Rgb.Id).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Exigence", "INST-012")]
    public void SuggestAddress_ProposesFirstFreeAddress()
    {
        Patch(GenericFixtures.Rgb, "3 canaux", 1, "PAR 1");
        _vm.SelectedModel = _vm.LibraryModels.Single(e => e.Fixture.Id == GenericFixtures.Rgb.Id);
        _vm.SelectedMode = "3 canaux";
        _vm.NewUniverse = 1;

        _vm.SuggestAddressCommand.Execute(null);

        _vm.NewAddress.ShouldBe(4);
    }

    [Fact]
    [Trait("Exigence", "INST-013")]
    public void OverlappingFixtures_AreFlagged()
    {
        Patch(GenericFixtures.Rgb, "3 canaux", 1, "PAR 1");
        Patch(GenericFixtures.Rgb, "3 canaux", 2, "PAR 2");

        _vm.PatchRows.ShouldAllBe(r => r.HasOverlap);
    }

    [Fact]
    [Trait("Exigence", "INST-014")]
    public void Twins_SameAddress_AreNotFlaggedAsOverlap()
    {
        _vm.SelectedModel = _vm.LibraryModels.Single(e => e.Fixture.Id == GenericFixtures.Rgb.Id);
        _vm.SelectedMode = "3 canaux";
        _vm.NewAddress = 1;
        _vm.NewCount = 2;
        _vm.NewAsTwins = true;

        _vm.AddFixtureCommand.Execute(null);

        _vm.PatchRows.Select(r => r.Address).ShouldBe([1, 1]);
        _vm.PatchRows.ShouldAllBe(r => !r.HasOverlap);
    }

    [Fact]
    [Trait("Exigence", "INST-015")]
    [Trait("Exigence", "INST-017")]
    public void RenameAndMove_UpdateThePatchedFixture()
    {
        Patch(GenericFixtures.Rgb, "3 canaux", 1, "PAR 1");
        var row = _vm.PatchRows[0];

        row.EditName = "PAR gauche";
        _vm.RenameFixture(row);

        // La ligne est reconstruite après enregistrement (comme dans l'écran réel, où le clic suivant vise la
        // ligne rafraîchie de la liste) : on reprend la nouvelle instance avant de la déplacer.
        row = _vm.PatchRows[0];
        row.EditUniverse = 2;
        row.EditAddress = 50;
        _vm.MoveFixture(row);

        var fixture = _host.Runtime.Project.Installation.Fixtures.Single();
        fixture.Name.ShouldBe("PAR gauche");
        fixture.Universe.ShouldBe(2);
        fixture.Address.ShouldBe(50);
    }

    [Fact]
    [Trait("Exigence", "INST-016")]
    public async Task ChangeMode_WithImpact_AsksConfirmation()
    {
        Patch(GenericFixtures.Rgbw, "5 canaux", 1, "Gros PAR 1");
        var row = _vm.PatchRows[0];
        row.EditModeName = "4 canaux";

        _host.Dialogs.ConfirmAnswer = false;
        await _vm.ChangeModeAsync(row);
        _host.Runtime.Project.Installation.Fixtures.Single().ModeName.ShouldBe("5 canaux");

        _host.Dialogs.ConfirmAnswer = true;
        await _vm.ChangeModeAsync(row);
        _host.Runtime.Project.Installation.Fixtures.Single().ModeName.ShouldBe("4 canaux");
        _host.Dialogs.Confirmations.Last().ShouldContain("Intensité");
    }

    [Fact]
    [Trait("Exigence", "GEN-053")]
    public async Task UpdateFromLibrary_WithImpact_AsksConfirmation()
    {
        // Modèle « du commerce » (pas un générique de l'application, pour ne pas être toujours déjà présent).
        var older = new Luxia.Fixtures.Model.FixtureType
        {
            Manufacturer = "Betopper",
            Model = "LPC008S",
            Version = 1,
            Channels = [new Luxia.Fixtures.Model.ChannelDefinition { Key = "r", Name = "Rouge", Attribute = Luxia.Fixtures.Model.AttributeKind.Red }],
            Modes = [new Luxia.Fixtures.Model.FixtureMode { Name = "1 canal", Channels = [new Luxia.Fixtures.Model.ModeChannel("r")] }],
        };
        _host.Runtime.Project.FixtureLibrary!.EnsureCopied(older);
        var installation = _host.Runtime.Project.Installation;
        var fixture = new PatchedFixture { FixtureTypeId = older.Id, ModeName = "1 canal", Address = 1, Name = "PAR 1" };
        _host.Runtime.Project.SaveInstallation(installation with { Fixtures = [fixture] });
        _vm = new InstallationViewModel(_host.Runtime, _host.Dialogs);
        var row = _vm.PatchRows[0];

        // La bibliothèque partagée reçoit une version 2 qui retire le mode « 1 canal ».
        var newer = older with { Version = 2, Modes = [older.Modes[0] with { Name = "2 canaux" }] };
        var sharedLibrary = new Luxia.Fixtures.FixtureLibrary(_host.Runtime.Library.Folder);
        Luxia.Persistence.Json.VersionedJsonFile.Save(sharedLibrary.PathFor(newer), newer, Luxia.Fixtures.FixtureLibrary.DocumentType);
        _host.Runtime.Library.Load();

        _host.Dialogs.ConfirmAnswer = false;
        await _vm.UpdateFromLibraryAsync(row);
        _host.Runtime.Project.FixtureLibrary!.Find(older.Id)!.Version.ShouldBe(1);

        _host.Dialogs.ConfirmAnswer = true;
        await _vm.UpdateFromLibraryAsync(row);
        _host.Runtime.Project.FixtureLibrary!.Find(older.Id)!.Version.ShouldBe(2);
        _host.Dialogs.Confirmations.Last().ShouldContain("repatché");
    }

    [Fact]
    [Trait("Exigence", "INST-020")]
    [Trait("Exigence", "GEN-103")]
    public async Task DeleteFixture_AsksConfirmation()
    {
        Patch(GenericFixtures.Rgb, "3 canaux", 1, "PAR 1");
        var row = _vm.PatchRows[0];

        _host.Dialogs.ConfirmAnswer = false;
        await _vm.DeleteFixtureAsync(row);
        _host.Runtime.Project.Installation.Fixtures.ShouldHaveSingleItem();

        _host.Dialogs.ConfirmAnswer = true;
        await _vm.DeleteFixtureAsync(row);
        _host.Runtime.Project.Installation.Fixtures.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "INST-019")]
    public void NeedsBackgroundRefresh_TrueOnlyWhileIdentifying()
    {
        // La coquille rafraîchit cet écran même quand un autre est affiché tant qu'Identifier tourne
        // (sinon l'appareil se fige au dernier état si l'utilisateur regarde le Simulateur, SIM-009).
        Patch(GenericFixtures.Rgbw, "5 canaux", 1, "Gros PAR 1");
        _vm.NeedsBackgroundRefresh.ShouldBeFalse();

        _vm.ToggleIdentifyFixture(_vm.PatchRows[0]);
        _vm.NeedsBackgroundRefresh.ShouldBeTrue();

        _vm.ToggleIdentifyFixture(_vm.PatchRows[0]);
        _vm.NeedsBackgroundRefresh.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "CMD-023")]
    [Trait("Exigence", "INST-019")]
    public void Identify_LightsIntensityChannel_AndChaseAdvancesToNextFixture()
    {
        Patch(GenericFixtures.Rgbw, "5 canaux", 1, "Gros PAR 1");
        Patch(GenericFixtures.Rgbw, "5 canaux", 10, "Gros PAR 2");

        _vm.ToggleIdentifyFixture(_vm.PatchRows[0]);
        _vm.Refresh();
        _host.Tick();
        _host.Frame()[0].ShouldBe((byte)255);
        _vm.PatchRows[0].Identifying.ShouldBeTrue();

        _vm.IdentifyNextCommand.Execute(null);
        _vm.Refresh();
        _host.Tick();
        _vm.PatchRows[0].Identifying.ShouldBeFalse();
        _vm.PatchRows[1].Identifying.ShouldBeTrue();
        _host.Frame()[9].ShouldBe((byte)255);
    }

    [Fact]
    [Trait("Exigence", "INST-030")]
    [Trait("Exigence", "INST-033")]
    public void CreateSelection_FromCheckedFixtures_ThenReverse()
    {
        Patch(GenericFixtures.Rgb, "3 canaux", 1, "PAR 1");
        Patch(GenericFixtures.Rgb, "3 canaux", 10, "PAR 2");
        _vm.PatchRows[0].IsChecked = true;
        _vm.PatchRows[1].IsChecked = true;
        _vm.NewSelectionName = "PAR gauche→droite";

        _vm.CreateSelectionCommand.Execute(null);

        var selection = _host.Runtime.Project.Installation.Selections.Single();
        selection.Items.Select(i => i.FixtureId).ShouldBe([_vm.PatchRows[0].Id, _vm.PatchRows[1].Id]);

        _vm.SelectedSelection = _vm.Selections[0];
        _vm.ReorderSelectionCommand.Execute("reverse");

        _host.Runtime.Project.Installation.Selections.Single().Items.Select(i => i.FixtureId)
            .ShouldBe([_vm.PatchRows[1].Id, _vm.PatchRows[0].Id]);
    }

    [Fact]
    [Trait("Exigence", "INST-033")]
    public void ReorderSelection_ByPosition_UsesActiveVenuePlacements()
    {
        Patch(GenericFixtures.Rgb, "3 canaux", 1, "PAR 1");
        Patch(GenericFixtures.Rgb, "3 canaux", 10, "PAR 2");
        _vm.PatchRows[0].IsChecked = true;
        _vm.PatchRows[1].IsChecked = true;
        _vm.NewSelectionName = "PAR";
        _vm.CreateSelectionCommand.Execute(null);

        // Le second appareil (index 1) est placé plus à gauche (X plus petit) que le premier.
        _vm.SelectedVenue!.Placements.Single(p => p.FixtureId == _vm.PatchRows[1].Id).X = 0;
        _vm.SelectedVenue!.Placements.Single(p => p.FixtureId == _vm.PatchRows[0].Id).X = 5;
        _vm.SavePlacementsCommand.Execute(null);

        _vm.SelectedSelection = _vm.Selections[0];
        _vm.ReorderSelectionCommand.Execute("left-right");

        var ids = _host.Runtime.Project.Installation.Selections.Single().Items.Select(i => i.FixtureId).ToList();
        ids.ShouldBe([_vm.PatchRows[1].Id, _vm.PatchRows[0].Id]);
    }

    [Fact]
    [Trait("Exigence", "INST-031")]
    public void AutoSelections_IncludeAllAndByCategory()
    {
        Patch(GenericFixtures.Rgb, "3 canaux", 1, "PAR 1");

        _vm.AutoSelections.ShouldContain(a => a.Title == "Tous" && a.Count == 1);
        _vm.AutoSelections.ShouldContain(a => a.Title == "Tous les PAR" && a.Count == 1);
    }

    [Fact]
    [Trait("Exigence", "INST-050")]
    public void Venues_HaveAGenericVenueByDefault()
    {
        _vm.Venues.ShouldHaveSingleItem();
        _vm.SelectedVenue!.Name.ShouldBe(VenueSet.DefaultVenueName);
    }

    [Fact]
    [Trait("Exigence", "INST-050")]
    public void CreateVenue_ThenActivate_ChangesActiveVenue()
    {
        _vm.NewVenueName = "Salon";
        _vm.CreateVenueCommand.Execute(null);
        _vm.SelectedVenue = _vm.Venues.Single(v => v.Name == "Salon");

        _vm.ActivateVenueCommand.Execute(null);

        _host.Runtime.Project.Venues.Active.Name.ShouldBe("Salon");

        // Mis en évidence dans la liste (retour utilisateur : le changement de lieu actif n'était pas visible).
        _vm.Venues.Single(v => v.Name == "Salon").IsActive.ShouldBeTrue();
        _vm.Venues.Single(v => v.Name == VenueSet.DefaultVenueName).IsActive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "INST-051")]
    [Trait("Exigence", "INST-052")]
    public void SavePlacements_PersistsPositionAndAbsence()
    {
        Patch(GenericFixtures.Rgb, "3 canaux", 1, "PAR 1");
        var placement = _vm.SelectedVenue!.Placements.Single();
        placement.X = 3.5;
        placement.Y = 1.2;
        placement.Absent = true;

        _vm.SavePlacementsCommand.Execute(null);

        var saved = _host.Runtime.Project.Venues.Active.PlacementOf(_vm.PatchRows[0].Id)!;
        saved.X.ShouldBe(3.5);
        saved.Y.ShouldBe(1.2);
        saved.Absent.ShouldBeTrue();
    }

    private void Patch(Luxia.Fixtures.Model.FixtureType type, string modeName, int address, string baseName)
    {
        _vm.SelectedModel = _vm.LibraryModels.Single(e => e.Fixture.Id == type.Id);
        _vm.SelectedMode = modeName;
        _vm.NewUniverse = 1;
        _vm.NewAddress = address;
        _vm.NewCount = 1;
        _vm.NewGap = 0;
        _vm.NewAsTwins = false;
        _vm.NewBaseName = baseName;
        _vm.AddFixtureCommand.Execute(null);
    }
}
