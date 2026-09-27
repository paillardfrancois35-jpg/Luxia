using Luxia.Patch;
using Luxia.Patch.Rules;
using Luxia.Persistence;

namespace Luxia.Integration.Tests;

/// <summary>Non-régression de l'installation et du lieu du show de référence (P3, doc 41 §11).</summary>
public sealed class ReferenceShowP3Tests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");

    [Fact]
    [Trait("Exigence", "INST-010")]
    public void ReferenceShow_Installation_LoadsAndMatchesAddressPlan()
    {
        var report = ProjectStore.Open(Folder);
        report.Succeeded.ShouldBeTrue();

        var (installation, message) = InstallationStore.Load(Folder);

        message.ShouldBeNull();
        installation.Fixtures.Count.ShouldBe(14);

        // Plan d'adresses du show de référence (doc 41 §2).
        var addresses = installation.Fixtures.ToDictionary(f => f.Name, f => f.Address);
        addresses["PAR 1"].ShouldBe(1);
        addresses["PAR 2"].ShouldBe(8);
        addresses["PAR 3"].ShouldBe(15);
        addresses["PAR 4"].ShouldBe(22);
        addresses["Gros PAR 1"].ShouldBe(31);
        addresses["Gros PAR 2"].ShouldBe(41);
        addresses["Barre 1"].ShouldBe(51);
        addresses["Barre 2"].ShouldBe(81);
        addresses["Lyre 1"].ShouldBe(111);
        addresses["Lyre 2"].ShouldBe(126);
        addresses["Effet multi-têtes"].ShouldBe(141);
        addresses["UV 1"].ShouldBe(161);
        addresses["UV 2"].ShouldBe(169); // UV en 8 canaux (8e canal « lissage » découvert à l'essai P5)
        addresses["Fumée"].ShouldBe(180);
    }

    [Fact]
    [Trait("Exigence", "INST-013")]
    public void ReferenceShow_Installation_HasNoOverlap()
    {
        var (installation, _) = InstallationStore.Load(Folder);
        var library = new ProjectFixtureLibrary(Folder);
        var overlaps = PatchRules.DetectOverlaps(installation.Fixtures, f =>
            library.Find(f.FixtureTypeId)?.Modes.FirstOrDefault(m => m.Name == f.ModeName)?.ChannelCount ?? 0);

        overlaps.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Exigence", "GEN-053")]
    public void ReferenceShow_ProjectFixtureLibrary_HasEveryUsedModel()
    {
        var (installation, _) = InstallationStore.Load(Folder);
        var library = new ProjectFixtureLibrary(Folder);

        foreach (var fixture in installation.Fixtures)
        {
            library.Find(fixture.FixtureTypeId).ShouldNotBeNull($"Modèle introuvable dans la copie du projet pour « {fixture.Name} ».");
        }
    }

    [Fact]
    [Trait("Exigence", "INST-050")]
    public void ReferenceShow_Venue_PlacesEveryFixture_NoneAbsent()
    {
        var (installation, _) = InstallationStore.Load(Folder);
        var (venues, message) = VenueStore.Load(Folder);

        message.ShouldBeNull();
        venues.Active.Name.ShouldBe("Générique");
        foreach (var fixture in installation.Fixtures)
        {
            var placement = venues.Active.PlacementOf(fixture.Id);
            placement.ShouldNotBeNull($"« {fixture.Name} » n'est pas placé dans le lieu « Générique ».");
            placement.Absent.ShouldBeFalse();
        }
    }
}
