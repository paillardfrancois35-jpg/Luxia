using Dmx.Fixtures.Model;
using Dmx.Fixtures.Rules;

namespace Dmx.Fixtures.Tests;

/// <summary>T-BIB-03 : enregistrement / chargement sans perte ; versions ; génériques ; outils de plages.</summary>
public sealed class FixtureLibraryTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "dmx-lib-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    [Trait("Exigence", "BIB-001")]
    public void SaveThenLoad_IsLossless()
    {
        var library = new FixtureLibrary(_folder);
        var head = Samples.MovingHead with
        {
            Physical = new PhysicalInfo { PanRange = 540, TiltRange = 270, SourceType = "LED 60 W" },
            Wheels = [new Wheel { Key = "couleurs", Name = "Roue de couleur", Slots = [new WheelSlot("Blanc", ["#FFFFFF"]), new WheelSlot("Rouge/Vert", ["#FF0000", "#00FF00"])] }],
            Notes = "Pan inversé ?",
        };

        library.Save(head);
        var reloaded = new FixtureLibrary(_folder);
        reloaded.Load();

        var loaded = reloaded.Entries.Single(e => !e.IsBuiltIn).Fixture;
        Json(loaded).ShouldBe(Json(head));
    }

    [Fact]
    [Trait("Exigence", "BIB-009")]
    public void Save_IncrementsVersion_AndRenameMovesFile()
    {
        var library = new FixtureLibrary(_folder);
        var par = Samples.Par;

        library.Save(par).Fixture.Version.ShouldBe(1);
        var second = library.Save(par with { Model = "PAR RGB renommé" });

        second.Fixture.Version.ShouldBe(2);
        File.Exists(Path.Combine(_folder, "Test", "PAR RGB.json")).ShouldBeFalse();
        File.Exists(Path.Combine(_folder, "Test", "PAR RGB renommé.json")).ShouldBeTrue();
    }

    [Fact]
    public void Load_IncludesGenerics_AndSetsAsideCorruptFiles()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "X"));
        File.WriteAllText(Path.Combine(_folder, "X", "cassé.json"), "{ pas du json");
        var library = new FixtureLibrary(_folder);

        library.Load();

        library.Entries.Count(e => e.IsBuiltIn).ShouldBe(GenericFixtures.All.Count);
        library.Messages.Single().ShouldContain("mis de côté");
    }

    [Fact]
    public void Delete_RemovesFile_ButNotGenerics()
    {
        var library = new FixtureLibrary(_folder);
        var entry = library.Save(Samples.Par);

        library.Delete(entry.Fixture.Id);
        library.Delete(GenericFixtures.Rgb.Id);

        File.Exists(entry.FilePath).ShouldBeFalse();
    }

    [Fact]
    [Trait("Exigence", "BIB-023")]
    public void Split_InEightEqualRanges_CoversWholeChannel()
    {
        var ranges = CapabilityTools.Split(8);

        ranges.Count.ShouldBe(8);
        ranges[0].Min.ShouldBe(0);
        ranges[^1].Max.ShouldBe(255);
        ranges.Zip(ranges.Skip(1)).ShouldAllBe(p => p.Second.Min == p.First.Max + 1);
        ranges.ShouldAllBe(r => r.Max - r.Min + 1 == 32);
    }

    [Fact]
    [Trait("Exigence", "BIB-023")]
    public void FillNextGap_AddsRangeInFirstHole()
    {
        IReadOnlyList<Capability> ranges = [new Capability { Min = 0, Max = 10, Label = "A" }, new Capability { Min = 51, Max = 255, Label = "B" }];

        var filled = CapabilityTools.FillNextGap(ranges);

        filled.Select(r => (r.Min, r.Max)).ShouldBe([(0, 10), (11, 50), (51, 255)]);
        CapabilityTools.FirstGap(filled).ShouldBeNull();
    }

    [Fact]
    [Trait("Exigence", "BIB-062")]
    public void SplitAt_CreatesBoundaryAtDiscoveredValue()
    {
        IReadOnlyList<Capability> ranges = [new Capability { Min = 0, Max = 255, Label = "Tout" }];

        var split = CapabilityTools.SplitAt(ranges, 128, "Strobe");

        split.Select(r => (r.Min, r.Max, r.Label)).ShouldBe([(0, 127, "Tout"), (128, 255, "Strobe")]);
        CapabilityTools.SplitAt([], 10).Select(r => (r.Min, r.Max)).ShouldBe([(0, 9), (10, 255)]);
    }

    [Fact]
    [Trait("Exigence", "BIB-022")]
    public void MoveBoundary_KeepsRangesAdjacent()
    {
        IReadOnlyList<Capability> ranges = [new Capability { Min = 0, Max = 99, Label = "A" }, new Capability { Min = 100, Max = 255, Label = "B" }];

        var moved = CapabilityTools.MoveBoundary(ranges, 0, 150);

        moved.Select(r => (r.Min, r.Max)).ShouldBe([(0, 150), (151, 255)]);
        CapabilityTools.MoveBoundary(ranges, 0, 999).Select(r => (r.Min, r.Max)).ShouldBe([(0, 254), (255, 255)]);
    }

    private static string Json(FixtureType fixture) =>
        System.Text.Json.JsonSerializer.Serialize(fixture, Persistence.Json.DmxJson.Options);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
