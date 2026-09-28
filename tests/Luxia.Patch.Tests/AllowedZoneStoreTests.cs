using Luxia.Patch.Model;

namespace Luxia.Patch.Tests;

public sealed class AllowedZoneStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "luxia-zones-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    [Trait("Exigence", "ERG-017")]
    public void SaveThenLoad_AllowedFlag_RoundTrips_AndOldFilesReadAsForbidden()
    {
        Directory.CreateDirectory(_folder);
        var lyre = Guid.NewGuid();
        var venues = new VenueSet
        {
            Venues =
            [
                new Venue
                {
                    Name = "Salle",
                    ForbiddenZones =
                    [
                        new ForbiddenZone { FixtureId = lyre, Name = "Limites", PanMin = 0.1, PanMax = 0.9, TiltMin = 0.2, TiltMax = 0.95, Allowed = true },
                        new ForbiddenZone { FixtureId = lyre, Name = "Public", PanMin = 0.3, PanMax = 0.7, TiltMin = 0.8, TiltMax = 1 },
                    ],
                },
            ],
        };

        VenueStore.Save(_folder, venues);
        var text = File.ReadAllText(Path.Combine(_folder, "lieux.json"));
        var (loaded, message) = VenueStore.Load(_folder);

        message.ShouldBeNull();
        loaded.Active.ForbiddenZones.Select(z => z.Allowed).ShouldBe([true, false]);
        text.ShouldContain("\"allowed\": true");

        // Fichier d'avant la zone permise : pas de propriété « allowed » → zone interdite, comme avant (pas de migration).
        File.WriteAllText(
            Path.Combine(_folder, "lieux.json"),
            text.Replace("\"allowed\": true,", string.Empty, StringComparison.Ordinal).Replace(",\n          \"allowed\": true", string.Empty, StringComparison.Ordinal));
        VenueStore.Load(_folder).Value.Active.ForbiddenZones.ShouldAllBe(z => !z.Allowed);
    }
}
