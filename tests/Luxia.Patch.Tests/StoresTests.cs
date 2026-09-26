using Luxia.Patch.Model;

namespace Luxia.Patch.Tests;

public sealed class StoresTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dmx-patch-tests-" + Guid.NewGuid());

    public StoresTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    [Trait("Exigence", "INST-001")]
    public void InstallationStore_Missing_ReturnsDefaultWithOneUniverse()
    {
        var (installation, message) = InstallationStore.Load(_folder);

        installation.Universes.ShouldHaveSingleItem();
        installation.Universes[0].Number.ShouldBe(1);
        message.ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "GEN-050")]
    [Trait("Exigence", "GEN-052")]
    public void InstallationStore_SaveThenLoad_RoundTrips()
    {
        var fixture = new PatchedFixture { FixtureTypeId = Guid.NewGuid(), ModeName = "7 canaux", Address = 1, Name = "PAR 1" };
        var installation = new Installation
        {
            Universes = [new PatchUniverse { Number = 1, Name = "Salle" }, new PatchUniverse { Number = 2 }],
            Fixtures = [fixture],
        };

        InstallationStore.Save(_folder, installation);
        var (loaded, _) = InstallationStore.Load(_folder);

        loaded.Universes.Count.ShouldBe(2);
        loaded.Fixtures.ShouldHaveSingleItem();
        loaded.Fixtures[0].Id.ShouldBe(fixture.Id);
        loaded.Fixtures[0].Name.ShouldBe("PAR 1");
    }

    [Fact]
    [Trait("Exigence", "INST-050")]
    public void VenueStore_Missing_ReturnsDefaultGenericVenue()
    {
        var (venues, _) = VenueStore.Load(_folder);

        venues.Venues.ShouldHaveSingleItem();
        venues.Active.Name.ShouldBe(VenueSet.DefaultVenueName);
    }

    [Fact]
    [Trait("Exigence", "INST-050")]
    public void VenueStore_SaveThenLoad_KeepsActiveVenue()
    {
        var salon = new Venue { Name = "Salon", WidthM = 4, DepthM = 3 };
        var generique = new Venue { Name = "Générique" };
        var venues = new VenueSet { Venues = [generique, salon], ActiveVenueId = salon.Id };

        VenueStore.Save(_folder, venues);
        var (loaded, _) = VenueStore.Load(_folder);

        loaded.Active.Name.ShouldBe("Salon");
    }

    [Fact]
    [Trait("Exigence", "GEN-053")]
    public void ProjectFixtureLibrary_EnsureCopied_CopiesFixtureIntoProjectFolder()
    {
        var shared = SharedModel();
        var library = new Luxia.Patch.ProjectFixtureLibrary(_folder);

        library.EnsureCopied(shared);

        File.Exists(Path.Combine(_folder, Luxia.Patch.ProjectFixtureLibrary.FolderName, "Betopper", "LPC008S.json")).ShouldBeTrue();
        library.Find(shared.Id).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Exigence", "GEN-053")]
    public void ProjectFixtureLibrary_EnsureCopied_ModifyingSharedCopyAfterwards_DoesNotAffectProject()
    {
        var shared = SharedModel();
        var library = new Luxia.Patch.ProjectFixtureLibrary(_folder);
        library.EnsureCopied(shared);

        var modifiedShared = shared with { Notes = "Modifié dans la bibliothèque partagée" };
        // Aucun appel à EnsureCopied / UpdateFrom avec la version modifiée : la copie du projet ne bouge pas (GEN-053).

        library.Find(shared.Id)!.Notes.ShouldBeNull();
        modifiedShared.Notes.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Exigence", "GEN-053")]
    public void ProjectFixtureLibrary_UpdateFrom_ReplacesProjectCopy()
    {
        var shared = SharedModel();
        var library = new Luxia.Patch.ProjectFixtureLibrary(_folder);
        library.EnsureCopied(shared);

        var updated = shared with { Version = 2, Notes = "Nouvelle version" };
        library.UpdateFrom(updated);

        library.Find(shared.Id)!.Notes.ShouldBe("Nouvelle version");
        library.Find(shared.Id)!.Version.ShouldBe(2);
    }

    // Un modèle « du commerce », distinct des génériques de l'application (toujours disponibles, jamais à copier).
    private static Luxia.Fixtures.Model.FixtureType SharedModel() => new()
    {
        Id = Guid.NewGuid(),
        Manufacturer = "Betopper",
        Model = "LPC008S",
        Channels = [new Luxia.Fixtures.Model.ChannelDefinition { Key = "r", Name = "Rouge", Attribute = Luxia.Fixtures.Model.AttributeKind.Red }],
        Modes = [new Luxia.Fixtures.Model.FixtureMode { Name = "1 canal", Channels = [new Luxia.Fixtures.Model.ModeChannel("r")] }],
    };
}
