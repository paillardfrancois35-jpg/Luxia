using Luxia.Fixtures.Model;
using Luxia.Patch;
using Luxia.Patch.Model;
using Luxia.Scenes.Compilation;
using Luxia.Scenes.Model;

namespace Luxia.Scenes.Tests;

/// <summary>Installation, lieux et modèles réels du show de référence (doc 41), pour tester la compilation sur le parc.</summary>
internal sealed class ReferenceProject
{
    public static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "samples", "Show de référence");

    public ReferenceProject()
    {
        Library = new ProjectFixtureLibrary(Folder);
        (Installation, _) = InstallationStore.Load(Folder);
        (Venues, _) = VenueStore.Load(Folder);
    }

    public ProjectFixtureLibrary Library { get; }

    public Installation Installation { get; set; }

    public VenueSet Venues { get; set; }

    public PatchContext Patch => new(Installation, Venues, Library.Find);

    /// <summary>
    /// Repatche les gros PAR en Betopper LPC120 8 canaux (RGBW) : le parc réel n'a plus d'appareil RGBW depuis que les gros
    /// PAR se sont révélés être des WT05 RGB (essai P5), mais la conversion vers un émetteur blanc reste à couvrir.
    /// </summary>
    public void PatchBigParsAsRgbw()
    {
        var lpc120 = Guid.Parse("76a6b0b5-b3d8-50dc-a4ad-0a294eb2a0e8");
        Installation = Installation with
        {
            Fixtures = [.. Installation.Fixtures.Select(f => f.Name.StartsWith("Gros PAR", StringComparison.Ordinal) ? f with { FixtureTypeId = lpc120, ModeName = "8 canaux" } : f)],
        };
    }

    public PatchedFixture Fixture(string name) => Installation.Fixtures.Single(f => f.Name == name);

    public FixtureType Type(string fixtureName) => Library.Find(Fixture(fixtureName).FixtureTypeId)!;

    public ProjectContent Content(SceneSet? scenes = null, PaletteSet? palettes = null, LayerSet? layers = null) =>
        new(Installation, Venues, Library.Find, layers ?? LayerSet.Default(), scenes ?? new SceneSet(), palettes ?? Rules.DefaultPalettes.Create());

    public static SceneValue Value(Guid fixtureId, AttributeKind attribute, double level, int cell = 0) =>
        new() { Target = ValueTarget.Fixture(fixtureId, cell), Attribute = attribute, Level = level };
}
